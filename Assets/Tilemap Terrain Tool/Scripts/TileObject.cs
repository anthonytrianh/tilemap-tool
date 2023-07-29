using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace TilemapTerrainTools
{
    public class TileObject : MonoBehaviour
    {
        public Vector2Int position;
        
        public TilePrefabs prefabs;
        public TileNeighbors neighbors = new TileNeighbors();
        
        public Vector2Int Direction;
        public TileDirection tileDirection;
        public int immediateNeighborCount;

        public int textureIndex = 0;
        public int pathIndex = 0;
        
        [SerializeField] private GameObject tilePrefab;
        [SerializeField] TMPro.TMP_Text debugText;

        public MeshRenderer Renderer;

        private Vector2Int[] ImmediateOffsets = new Vector2Int[]
        {
            new Vector2Int(-1, 0), // west
            new Vector2Int(1, 0),
            new Vector2Int(0, -1),
            new Vector2Int(0, 1)  // north
        };

        private Vector2Int[] CornerOffsets = new Vector2Int[]
        {
            new Vector2Int(-1, -1),
            new Vector2Int(-1, 1),
            new Vector2Int(1, -1),
            new Vector2Int(1, 1)
        };

        private Vector2Int[] AllNeighborsOffsets = new Vector2Int[]
        {
            new Vector2Int(0, 1),
            new Vector2Int(1, 1),
            new Vector2Int(1, 0),
            new Vector2Int(1, -1),
            new Vector2Int(0, -1),
            new Vector2Int(-1, -1),
            new Vector2Int(-1, 0),
            new Vector2Int(-1, 1),
        };
        
        private void OnEnable()
        {
            Renderer = GetComponentInChildren<MeshRenderer>();
        }

        public void Initialize(Vector2Int pos, Dictionary<Vector2Int, TileObject> layer, bool buildTopOnly = false)
        {
            position = pos;

            neighbors = new TileNeighbors();
            Direction = Vector2Int.zero;
            
            // Immediate neighbors
            foreach (var offset in ImmediateOffsets)
            {
                TileObject neighbor = GetNeighbor(offset, layer);
                if (SetImmediateNeighbor(pos + offset, neighbor))
                {
                    Direction -= offset;
                }
            }

            tileDirection = TilemapExtensions.Vector2ToDirection(Direction);
            
            // Corner neighbors
            foreach (var offset in CornerOffsets)
            {
               SetCornerNeighbor(offset, GetNeighbor(offset, layer));
            }
            
            UpdateTilePrefab(buildTopOnly);
        }

        public void UpdateTilePrefab(bool buildTopOnly = false)
        {
            if (Renderer != null)
            {
                DestroyImmediate(Renderer.gameObject);
            }
            
            // By default, set to top
            GameObject prefab;

            if (buildTopOnly)
            {
                prefab = prefabs.Top;
            }
            else
            {
                immediateNeighborCount = neighbors.ImmediateNeighbors.Count;

                switch (immediateNeighborCount)
                {
                    case 0:
                        prefab = prefabs.SinglePiece;
                        break;
                    case 1:
                        prefab = prefabs.TripleEdge;
                        break;
                    case 2:
                        prefab = Direction == Vector2Int.zero ? prefabs.DoubleEdge : prefabs.Corner;
                        break;
                    case 3:
                        prefab = prefabs.SingleEdge;
                        break;
                    default:
                        prefab = prefabs.Top;
                        break;
                }

            }
            
            
            
            // Only allow path drawing for top tiles
            if (prefab != prefabs.Top)
            {
                textureIndex = -1;
            }
            else
            {
                textureIndex = (textureIndex < 0) ? 0 : textureIndex;
            }
            
            // Spawn new prefab
            GameObject spawnedPrefab = Instantiate(prefab, transform);
            Undo.RegisterCreatedObjectUndo(spawnedPrefab, "update tile Prefab");
            Renderer = spawnedPrefab.GetComponent<MeshRenderer>();
            Renderer.transform.localEulerAngles = new Vector3(0, GetYawRotation(), 0);

            if (textureIndex > 0)
            {
                PaintPathTexture();
            }
        }

        public bool SetImmediateNeighbor(Vector2Int pos, TileObject neighborToSet)
        {
            if (neighborToSet == null)
            {
                return false;
            }

            if (!neighbors.ImmediateNeighbors.ContainsKey(pos))
            {
                // Register adjacent tile as neighbor
                neighbors.ImmediateNeighbors.Add(pos, neighborToSet);
                return true;
            }

            return false;
        }

        public void SetCornerNeighbor(Vector2Int offset, TileObject neighbor)
        {
            if (neighbor == null) return;

            if (!neighbors.CornerNeighbors.ContainsKey(position + offset))
            {
                neighbors.CornerNeighbors.Add(position + offset, neighbor);
            }
        }

        TileObject GetNeighbor(Vector2Int offset, Dictionary<Vector2Int, TileObject> layer)
        {
            if (layer.TryGetValue(position + offset, out var n))
            {
                return n;
            }

            return null;
        }

        float GetYawRotation()
        {
            if (immediateNeighborCount == 2 && Direction == Vector2Int.zero)
            {
                // Connector
                Vector2Int[] neighborCoords = neighbors.ImmediateNeighbors.Keys.ToArray();
                Vector2Int delta = neighborCoords[0] - neighborCoords[1];
                return Mathf.Abs(delta.x) > Mathf.Abs(delta.y) ? 90 : 0;
            }


            switch (tileDirection)
            {
                case TileDirection.SouthEast:
                case TileDirection.South:
                    return 0;
                case TileDirection.SouthWest:
                case TileDirection.West:
                    return 90;
                case TileDirection.NorthWest:
                case TileDirection.North:
                    return 180;
                case TileDirection.NorthEast:
                case TileDirection.East:
                    return 270;
            }

            return 0;
        }

        private bool repaintPath;
        
        // Iterate through all neighbors and compute texture index
        // http://www.cr31.co.uk/stagecast/wang/blob.html
        public void UpdateAllNeighborsPathWeights(Dictionary<Vector2Int, TileObject> layer)
        {
            // Unpaintable tiles
            if (textureIndex < 0)
            {
                return;
            }
            
            int totalIndex = 0;
            for (int i = 0; i < AllNeighborsOffsets.Length; i++)
            {
                TileObject neighbor = GetNeighbor(AllNeighborsOffsets[i], layer);
                if (neighbor != null && neighbor.textureIndex > -1)
                {
                    TileDirection relativeDirection = TilemapExtensions.Vector2ToDirection(AllNeighborsOffsets[i]);
                    int neighborWeight = GetNeighborWeight(AllNeighborsOffsets[i]);
                    int neighborIndex = (int)relativeDirection * neighbor.textureIndex * neighborWeight;
                    totalIndex += neighborIndex;
                }
            }

            totalIndex *= textureIndex;
            // If total neighbor weight has changed, then request repaint
            if (totalIndex != pathIndex)
            {
                repaintPath = true;
            }
            pathIndex = totalIndex;

            // Debug
            if (debugText != null)
            {
                debugText.text = "" + textureIndex + "," + pathIndex;
            }
        }

        public void ComputeNeighborWeights(Dictionary<Vector2Int, TileObject> layer)
        {
            // Edge neighbors
            foreach (var o in ImmediateOffsets)
            {
                // Neighbor is valid for painting
                TileObject t = GetNeighbor(o, layer);
                if (t != null && t.textureIndex > -1)
                {
                    // Make edge 1 if this neighbor and current tile are the same texture index (which is texture index value anyway)
                    neighbors.NeighborWeights.TryAdd(o, t.textureIndex);
                }
            }
            
            // Corner CELLS
            //  1   12   2
            // 13  1234  24
            //  3   34   4
            foreach (var o in CornerOffsets)
            {
                // Neighbor is valid for painting
                TileObject t = GetNeighbor(o, layer);
                if (t != null && t.textureIndex > -1)
                {
                    int cornerCheck = 0;

                    Vector2Int[] cornerCellOffsets = new Vector2Int[] { };
                    // Check if corner cell is surrounded by all 1 tiles
                    TileDirection dir = TilemapExtensions.Vector2ToDirection(o);
                    switch (dir)
                    {
                        case TileDirection.NorthWest:
                            cornerCellOffsets = new Vector2Int[]
                            {
                                new Vector2Int(-1, 1),
                                new Vector2Int(0, 1),
                                new Vector2Int(-1, 0),
                                new Vector2Int(0, 0)
                            };
                            break;
                        case TileDirection.NorthEast:
                            cornerCellOffsets = new Vector2Int[]
                            {
                                new Vector2Int(0, 1),
                                new Vector2Int(1, 1),
                                new Vector2Int(0, 0),
                                new Vector2Int(1, 0)
                            };
                            break;
                        case TileDirection.SouthWest:
                            cornerCellOffsets = new Vector2Int[]
                            {
                                new Vector2Int(-1, -1),
                                new Vector2Int(0, -1),
                                new Vector2Int(-1, 0),
                                new Vector2Int(0, 0)
                            };
                            break;
                        case TileDirection.SouthEast:
                            cornerCellOffsets = new Vector2Int[]
                            {
                                new Vector2Int(0, -1),
                                new Vector2Int(1, -1),
                                new Vector2Int(0, 0),
                                new Vector2Int(1, 0)
                            };
                            break;
                    }

                    foreach (var c in cornerCellOffsets)
                    {
                        TileObject n = GetNeighbor(c, layer);
                        if (n != null)
                        {
                            cornerCheck += n.textureIndex;
                        }
                    }

                    int cornerWeight = (cornerCheck == 4) ? 1 : 0;
                    neighbors.NeighborWeights.TryAdd(o, cornerWeight);
                }
            }
        }
        
        int GetNeighborWeight(Vector2Int neighborOffset)
        {
            if (neighbors.NeighborWeights.TryGetValue(neighborOffset, out var weight))
            {
                return weight;
            }
            
            return 0;
        }
        
        // Repaint path texture by generating a new quad
        //  with uvs matching the current (x,y) cell on the index sheet
        public void PaintPathTexture(bool repaintOverride = false)
        {
            // Skip if there is no need to repaint path texture
            if (!repaintPath && !repaintOverride) { return; }
            
#if true
            if (TilemapExtensions.PathMap.ContainsKey(pathIndex))
            {
                // Make new mesh
                Material currentMat = Renderer.sharedMaterial;
                MeshFilter filter = Renderer.GetComponent<MeshFilter>();
                Vector3[] verts = filter.sharedMesh.vertices;
                int[] triangles = filter.sharedMesh.triangles;
                Vector3[] normals = filter.sharedMesh.normals;
                 
                // Update uvs here
                float w = 1f / TilemapExtensions.IndexSheetDimensions.x;
                float h = 1f / TilemapExtensions.IndexSheetDimensions.y;

                Vector2Int texCoords = TilemapExtensions.Get2dCoordsFrom1dIndex(TilemapExtensions.PathMap[pathIndex]);
                int x = texCoords.x;
                int y = texCoords.y;
                
                // Index sheet coords go from (x) left to right, (y) top to bottom
                //  UVs go from (x) left to right, (y) bottom to top
                Vector2 min = new Vector2(w * x, h * (TilemapExtensions.IndexSheetDimensions.y - y - 1));
                Vector2 max = new Vector2( w * (x + 1), h * (TilemapExtensions.IndexSheetDimensions.y - y));
            
                Vector2[] uv = new Vector2[4];

                uv[0] = new Vector2(min.x, min.y);
                uv[1] = new Vector2(min.x, max.y);
                uv[2] = new Vector2(max.x, max.y);
                uv[3] = new Vector2(max.x, min.y);

                Mesh mesh = new Mesh();
                
                mesh.vertices = verts;
                mesh.triangles = triangles;
                mesh.normals = normals;
                mesh.SetUVs(0, uv);

                //filter.mesh = currentMesh;
                DestroyImmediate(Renderer.gameObject);
                
                GameObject newPrefab = new GameObject("Top", typeof(MeshFilter), typeof(MeshRenderer), typeof(BoxCollider));
                
                newPrefab.transform.SetParent(transform, false);
                newPrefab.GetComponent<MeshFilter>().mesh = mesh;
                Renderer = newPrefab.GetComponent<MeshRenderer>();
                Renderer.material = currentMat;
                newPrefab.GetComponent<BoxCollider>().size = new Vector3(1, 0, 1);
                
                // Reset repaint
                repaintPath = false;
            }
#endif            
        }
    }

   

    [System.Serializable]
    public class TileNeighbors
    {
        public Dictionary<Vector2Int, TileObject> ImmediateNeighbors;
        public Dictionary<Vector2Int, TileObject> CornerNeighbors;

        public Dictionary<Vector2Int, int> NeighborWeights;

        public TileNeighbors()
        {
            ImmediateNeighbors = new Dictionary<Vector2Int, TileObject>();
            CornerNeighbors = new Dictionary<Vector2Int, TileObject>();
            NeighborWeights = new Dictionary<Vector2Int, int>();
        }

        public List<TileObject> GetImmediateNeighbors()
        {
            List<TileObject> neighbors = new List<TileObject>();
            Vector2Int[] coords = ImmediateNeighbors.Keys.ToArray();
            for (int i = 0; i < coords.Length; i++)
            {
                neighbors.Add(ImmediateNeighbors[coords[i]]);
            }

            return neighbors;
        }
        
        public List<TileObject> GetCornerNeighbors()
        {
            List<TileObject> neighbors = new List<TileObject>();
            Vector2Int[] coords = CornerNeighbors.Keys.ToArray();
            for (int i = 0; i < coords.Length; i++)
            {
                neighbors.Add(CornerNeighbors[coords[i]]);
            }

            return neighbors;
        }
    }


    [System.Serializable]
    public struct TilePrefabs
    {
        // 0 neighbors
        public GameObject SinglePiece;
        // 1 neighbor
        public GameObject TripleEdge;
        // 2 neighbors
        public GameObject DoubleEdge;
        // 3 neighbors
        public GameObject SingleEdge;
        // 4 neighbors
        public GameObject Top;
        public GameObject Corner;
    }

    public enum TileDirection
    {
        North = 1,
        NorthEast = 2,
        East = 4,
        SouthEast = 8,
        South = 16,
        SouthWest = 32,
        West = 64,
        NorthWest = 128,
        None = 0,
    }
}