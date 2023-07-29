using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using TilemapTerrainTools;
using UnityEditor;
using UnityEngine;

namespace TilemapTerrainTools
{
    public class TilemapTerrain : MonoBehaviour
    {
        // Tile prefab
        public GameObject prefab;
        // The height at which we are painting our tiles
        //  Changing levels should create a new container if a new tile is placed for that level
        public int LayerIndex = 1;
        public int brushSize = 1;
        public int pathIndex = 0;

        public bool buildTopOnly;
        
        // Dictionaries
        public Dictionary<int, TilemapLayer> Containers = new Dictionary<int, TilemapLayer>();

        public Dictionary<Vector2Int, TileObject> CurrentLayer = new Dictionary<Vector2Int, TileObject>();

        // References to trees and objects placed
        public List<GameObject> objectPrefabs = new List<GameObject>();

        public bool pathPaintDebugging = false;
        public Material pathPaintDebugMaterial;
        
        public void ClearLayer()
        {
            if (Containers.ContainsKey(LayerIndex))
            {
                #if UNITY_EDITOR
                // Destroy game objects
                Transform container;
                if (Containers.TryGetValue(LayerIndex, out TilemapLayer layer))
                {
                    if (layer != null)
                    {
                        for (int i = layer.transform.childCount - 1; i > -1; i--)
                        {
                            DestroyImmediate(layer.transform.GetChild(i).gameObject);
                        }
                        DestroyImmediate(layer.gameObject);
                    }
                    
                }
                #endif
                Containers.Remove(LayerIndex);
                CurrentLayer.Clear();
            }
        }
    
        public void ClearAll()
        {
            for (int i = transform.childCount - 1; i > -1; i--)
            {
                DestroyImmediate(transform.GetChild(i).gameObject);
            }
            
            Containers.Clear();
            CurrentLayer.Clear();
        }
    
        public void ReinitializeContainers()
        {
            Containers.Clear();
            
            // Get layer children
            for (int i = 0; i < transform.childCount; i++)
            {
                TilemapLayer layer = transform.GetChild(i).GetComponent<TilemapLayer>();
                Containers.Add(layer.layerIndex, layer);
                
                // Clear previous data in each layer
                layer.Tiles = new Dictionary<Vector2Int, TileObject>();
                
                // Add back all tiles
                foreach (Transform t in layer.transform)
                {
                    TileObject tile = t.GetComponent<TileObject>();
                    layer.AddTileToDictionary(tile.position, tile);
                }
            }
        }

        public void InitializeCurrentLayer()
        {
            CurrentLayer = new Dictionary<Vector2Int, TileObject>();
            
            // Retrieve layer
            TilemapLayer layer;
            if (Containers.TryGetValue(LayerIndex, out layer))
            {
                Vector2Int[] positions = layer.Tiles.Keys.ToArray();
                if (positions.Length > 0)
                {
                    foreach (var p in positions)
                    {
                        CurrentLayer.Add(p, layer.Tiles[p]);
                    }    
                }
            }
        }

        public void AddTile(Vector2Int pos, TileObject tile, TilemapLayer layer = null)
        {
            bool layerValid;
            if (layer == null)
            {
                layerValid = (Containers.TryGetValue(LayerIndex, out layer));
            }
            else
            {
                layerValid = true;
            }
            
            if (layerValid)
            {
                layer.AddTileToDictionary(pos, tile);
                // Add tile to current layer
                if (!CurrentLayer.TryGetValue(pos, out var t))
                {
                    CurrentLayer.Add(pos, tile);
                    //Undo.RegisterCompleteObjectUndo(tile, "add tile");
                }
                
                tile.Initialize(pos, CurrentLayer, buildTopOnly);

                // Update all neighbors
                List<TileObject> immediateNeighbors = tile.neighbors.GetImmediateNeighbors();
                foreach (var n in immediateNeighbors)
                {
                    n.Initialize(n.position, CurrentLayer, buildTopOnly);
                }
            }
        }

        public void DeleteTile(Vector2Int pos, TilemapLayer layer = null)
        {
            bool layerValid;
            if (layer == null)
            {
                layerValid = (Containers.TryGetValue(LayerIndex, out layer));
            }
            else
            {
                layerValid = true;
            }

            if (layerValid)
            {
                layer.RemoveTileFromDictionary(pos);

                if (CurrentLayer.ContainsKey(pos))
                {
                    TileObject tile = CurrentLayer[pos];
                    CurrentLayer.Remove(pos);
                    if (tile != null)
                        DestroyImmediate(tile.gameObject);
                }
                
                // Reinitializes ALL tiles
                Vector2Int[] tileCoords = CurrentLayer.Keys.ToArray();
                foreach (var p in tileCoords)
                {
                    CurrentLayer[p].Initialize(p, CurrentLayer, buildTopOnly);
                }
            }
        }

        public void UpdateAllTilePrefabs()
        {
            int[] layerIndices = Containers.Keys.ToArray();
            for (int i = 0; i < layerIndices.Length; i++)
            {
                Vector2Int[] tileCoords = Containers[layerIndices[i]].Tiles.Keys.ToArray();
                for (int j = 0; j < tileCoords.Length; j++)
                {
                    Containers[layerIndices[i]].Tiles[tileCoords[j]].UpdateTilePrefab(buildTopOnly);
                }
            }
        }

        public void TryPathPaint(Vector2Int pos)
        {
            if (CurrentLayer.TryGetValue(pos, out var tile))
            {
                // Can't paint on this tile
                if (tile.textureIndex < 0)
                {
                    return;
                }
                
                PathPaintTile(tile);
                
                // Update all tiles
                Vector2Int[] tileCoords = CurrentLayer.Keys.ToArray();
                foreach (var coord in tileCoords)
                {
                    TileObject to = CurrentLayer[coord];
                    if (to != tile && to.textureIndex > -1)
                    {
                        to.PaintPathTexture();
                    }
                }
                 
                #region DEPRECATED
                // // Update all tile's neighbors
                //  List<TileObject> immediateNeighbors = tile.neighbors.GetImmediateNeighbors();
                //  foreach (var t in immediateNeighbors)
                //  {
                //      if (t.textureIndex > -1)
                //      {
                //         t.PaintPathTexture();
                //      }
                //  }
                //
                //  List<TileObject> cornerNeighborCoords = tile.neighbors.GetCornerNeighbors();
                //  foreach (var t in cornerNeighborCoords)
                //  {
                //      if (t.textureIndex > -1)
                //      {
                //         t.PaintPathTexture();
                //      }
                //  }
                #endregion
                 
                 // Debug mode
                 if (pathPaintDebugging)
                 {
                     if (pathPaintDebugMaterial != null)
                     {
                         tile.Renderer.sharedMaterial = pathPaintDebugMaterial;
                     }
                 }
            }
        }

        void PathPaintTile(TileObject tile)
        {
            // Paint index for current tile
            tile.textureIndex = pathIndex;
            // Clear and calculate all neighbor weights
            Vector2Int[] tileCoords = CurrentLayer.Keys.ToArray();
            foreach (var p in tileCoords)
            {
                TileObject t = CurrentLayer[p];
                if (t.textureIndex > -1)
                {
                    t.neighbors.NeighborWeights.Clear();
                    t.ComputeNeighborWeights(CurrentLayer);
                }
            }
                
            // Repaint all tiles' weights
            foreach (var p in tileCoords)
            {
                TileObject t = CurrentLayer[p];
                t.UpdateAllNeighborsPathWeights(CurrentLayer);
            }
                 
            tile.PaintPathTexture(true);
        }
    }
}


