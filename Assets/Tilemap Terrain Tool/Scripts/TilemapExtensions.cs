using System.Collections;
using System.Collections.Generic;
using TilemapTerrainTools;
using UnityEngine;

namespace TilemapTerrainTools
{
    public static class TilemapExtensions
    {
        public static TileDirection Vector2ToDirection(Vector2Int v)
        {
            // Southwest
            if (v.x < 0 && v.y < 0)
            {
                return TileDirection.SouthWest;
            }
            // Southeast
            if (v.x > 0 && v.y < 0)
            {
                return TileDirection.SouthEast;
            }
            // Northwest
            if (v.x < 0 && v.y > 0)
            {
                return TileDirection.NorthWest;
            }
            // Northeast
            if (v.x > 0 && v.y > 0)
            {
                return TileDirection.NorthEast;
            }
            
            // South
            if (v.y < 0)
            {
                return TileDirection.South;
            }
            // North
            if (v.y > 0)
            {
                return TileDirection.North;
            }
            // West
            if (v.x < 0)
            {
                return TileDirection.West;
            }
            // East
            if (v.x > 0)
            {
                return TileDirection.East;
            }
            // None
            return TileDirection.None;
        }

        private static Dictionary<int, int> _pathMap;

        public static Dictionary<int, int> PathMap
        {
            get
            {
                if (_pathMap == null)
                {
                    CreatePathMap();
                }

                return _pathMap;
            }
        }

        static void CreatePathMap()
        {
            _pathMap = new Dictionary<int, int>();
            for (int i = 0; i < PathArray.Length; i++)
            {
                _pathMap.Add(PathArray[i], i);
            }
        }

        static int[] PathArray =
        {
            20, 68, 92, 112, 28, 124, 116, 80,
            21, 84, 87, 221, 127, 255, 241, 17,
            29, 117, 85, 95, 247, 215, 209, 1,
            23, 213, 81, 31, 253, 125, 113, 16,
            5, 69, 93, 119, 223, 256, 245, 65,
            0, 4, 71, 193, 7, 199, 197, 64
        };

        public static Vector2Int Get2dCoordsFrom1dIndex(int index, int width = 8)
        {
            return new Vector2Int(index % width, index / width);
        }

        public static Vector2Int IndexSheetDimensions = new Vector2Int(8, 6);
    }
}
