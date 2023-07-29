using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TilemapTerrainTools
{
    public class TilemapLayer : MonoBehaviour
    {
        public int layerIndex = -1;

        public Dictionary<Vector2Int, TileObject> Tiles = new Dictionary<Vector2Int, TileObject>();

        public void AddTileToDictionary(Vector2Int pos, TileObject tile)
        {
            if (!Tiles.ContainsKey(pos))
            {
                Tiles.Add(pos, tile);
            }
        }

        public void RemoveTileFromDictionary(Vector2Int pos)
        {
            Tiles.Remove(pos);
        }
    }
}
