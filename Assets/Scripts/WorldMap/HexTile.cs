using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Assets.Scripts.WorldMap
{
    public class HexTile : Tile
    {
        public Tilemap mainMap;
        public GridManager gridManager;
        
        public Color TileColor;
        public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
        {
            tileData.sprite = sprite;

            color = gridManager.GetColor(position.x, position.y);

            tileData.color = color;
            tileData.flags = TileFlags.LockColor;
        }

        public override void RefreshTile(Vector3Int position, ITilemap tilemap)
        {
        }

        public void Refresh(Vector3Int position)
        {
         
        }

        public void SetMap(GridManager manager, Tilemap map)
        {
            mainMap = map;
            gridManager = manager;
        }
    }
}
