using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class WorldGrid : MonoBehaviour
{
    [SerializeField]
    TerrainGenerator terrainGenerator;
    
    Vector3Int cellSize;  //the size of each cell, in terms of the scene's coordinate system
    Vector2Int tilemapSize;  //the length and width of the tilemap in terms of tiles (not coordinates)
    
    public Dictionary<Vector2Int, TileBase> worldGrid = new Dictionary<Vector2Int, TileBase>();

    Grid grid;  //parent grid object

    void Start()
    {
        grid = GetComponent<Grid>();
        cellSize = Vector3Int.FloorToInt(grid.cellSize);
        tilemapSize = new Vector2Int(terrainGenerator.chunkWidth, terrainGenerator.chunkHeight);
    }


    //Adds all the tiles of a tilemap to worldGrid
    public int AddTilemap(Vector2Int tilemapPos, Tilemap tilemap)
    {
        // Iterate through each position within the bounds using a nested for loop
        for (int y = 0; y < tilemapSize.y; y++)
        {
            for (int x = 0; x < tilemapSize.x; x++)
            {
                Vector3Int position = new Vector3Int(x, y, 0);

                // Check if there is a tile at the current position
                if (tilemap.HasTile(position))
                {
                    TileBase tile = tilemap.GetTile(position);
                    worldGrid.Add(new Vector2Int(tilemapPos.x + x*cellSize.x, tilemapPos.y + y*cellSize.y), tile);
                }
                else
                {
                    print("ERROR: no tile found at (" + x + ", " + y + ").");
                }
            }
        }

        return 0;
    }

    public int RemoveTilemap(Vector2Int tilemapPos, Tilemap tilemap)
    {
        // Get the bounds of the Tilemap
        BoundsInt bounds = tilemap.cellBounds;
        
        for (int y = 0; y < tilemapSize.y; y++)
        {
            for (int x = 0; x < tilemapSize.x; x++)
            {
                worldGrid.Remove(new Vector2Int(tilemapPos.x + x*cellSize.x, tilemapPos.y + y*cellSize.y));
            }
        }
        return 0;
    }


    //Returns the tile at the given coordinates
    public TileBase GetTile(Vector2Int tilePos)
    {
        return worldGrid[tilePos];
    }


    //Returns the current length of worldGrid
    public int Length()
    {
        return worldGrid.Count;
    }
}
