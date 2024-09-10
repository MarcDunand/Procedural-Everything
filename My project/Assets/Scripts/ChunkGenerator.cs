using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Tilemaps;

public class ChunkGenerator : MonoBehaviour
{
    [SerializeField]
    private int chunkX;         //chunk coordinate of this chunk (increases by 1 by chunk)

    [SerializeField]
    private int chunkY;

    [SerializeField]
    private int chunkSeed;

    [SerializeField]
    private Grid grid;

    [SerializeField]
    private GameObject tilemapPrefab;

    [SerializeField]
    private Tile baseTile;

    [SerializeField]
    private Tile roomTile;

    [SerializeField]
    private Tile hallTile;

    [SerializeField]
    private Tile smallVoidTile;

    [SerializeField]
    private Tile largeVoidTile;

    [SerializeField]
    private WorldGrid worldGrid;                //The script that contains the worldGrid object (keeps track of all tiles in one data struct)

    [SerializeField]
    private Tile whiteTile;

    int chunkWidth;                             //width of chunk in tiles
    int chunkHeight;                            //height of chunk in tiles
    Vector2Int chunkPos;                        //the position of this chunk in terms of the scene's coordinate system

    const float borderWidth = 0.33F;            //maximum border on a binary split room, if borderWidth = 0.5, the maximum border size will consume the entire room
    const int avgRoomLen = 10;
    const int stdvRoomLen = 15;

    const float smallPNoise = 0.1F;             //scale of small-scale perlin noise
    const float smallPThresh = 0.3F;            //threshold below which small-scale Pnoise generates voids
    const float largePNoise = 0.002F;           //scale of large-scale perlin noise
    const float largePThres = 0.2F;             //threshold below which large-scale Pnoise generates voids

    const int hallLen = 40;                     //minimum length for in-chunk halls
    const int distPHall = 50;                   //room tiles per new hall spawn
    const double turnChance = 0.05;             //chance of turning left/right
    const int turnRadius = 4;                   //minimum dist before two hall turns

    const int edgeConnectors = 5;               //number of connecting halls between chunk edges

    System.Random rand;                         //for generating chunk-local features
    
    GameObject tilemapObj;                      //the tilemap object used for this chunk
    
    public Tilemap tilemap;                            //the actual tilemap component of the tilemap object

    int[][] tileArr;                            //stores material of each tile in chunk

    List<(int, int, int, int)> roomList;        //stores set of rooms from binary split


    
    //called by terrain generator to set inter-chunk constants before execution

    public void SetConsts(int ChunkX, int ChunkY, int ChunkSeed, int ChunkWidth, int ChunkHeight)
    {
        chunkX = ChunkX;
        chunkY = ChunkY;
        chunkSeed = ChunkSeed;

        chunkWidth = ChunkWidth;
        chunkHeight = ChunkHeight;

        chunkPos = new Vector2Int((chunkX*chunkWidth), (chunkY*chunkHeight));
    }


    //finds random gaussian for given mean and stdDev
    
    float randGaussian(float mean, float stdDev)
    {
        double u1 = 1.0-rand.NextDouble();  //uniform(0,1] random doubles
        double u2 = 1.0-rand.NextDouble();
        double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) *
                    Math.Sin(2.0 * Math.PI * u2);  //random normal(0,1)
        return mean + stdDev*((float)randStdNormal);
    }


    //returns an integer representing how many of a tile's surrounding tiles are fg or bg, += 1 for fg, -= 1 for bg

    int topoCount(int xTile, int yTile)
    {
        int topoSum = 0;
            
        topoSum += Math.Sign(tileArr[yTile-1][xTile-1]);
        topoSum += Math.Sign(tileArr[yTile-1][xTile+1]);
        topoSum += Math.Sign(tileArr[yTile+1][xTile-1]);
        topoSum += Math.Sign(tileArr[yTile+1][xTile+1]);

        return topoSum;
    }


    //creates tileArr and fills it with small and large perlin noise
    
    void initTileArr() 
    {
        tileArr = new int[chunkHeight][];

        for(int i = 0; i < chunkHeight; i++) {
            tileArr[i] = new int[chunkWidth];
            for(int j = 0; j < chunkWidth; j++) {
                if (Mathf.PerlinNoise((i + chunkY*chunkHeight)*largePNoise, (j + chunkX*chunkHeight)*largePNoise) < largePThres) {  //sets tile to large-scale void code
                    tileArr[i][j] = -2;
                }
                else if(Mathf.PerlinNoise((i + chunkY*chunkHeight)*smallPNoise, (j + chunkX*chunkHeight)*smallPNoise) < smallPThresh) {  //sets tile to small-scale void code
                    tileArr[i][j] = -1;
                }
                else {  //sets tile to default foreground code
                    tileArr[i][j] = 1;
                }
            }
        }
    }


    //takes roomList and converts the relevant tiles to empty space in tileArr
    
    void roomsToTileArr() {
        int x, y, w, h, i, row, col;

        for(i = 0; i < roomList.Count; i++) {
            x = roomList[i].Item1;
            y = roomList[i].Item2;
            w = roomList[i].Item3;
            h = roomList[i].Item4;

            for(row = 0; row < w; row++) { 
                for(col = 0; col < h; col++) {
                    if(tileArr[y + col][x + row] != -2){  //if tile does not belong to a large void
                        tileArr[y + col][x + row] = -3;  //sets tile to empty room code
                    }
                }
            }
        }
    }


    //iterates through tileArr adding the relevant gameObjects to the game
    
    void tileChunk(int[][] arr)
    {
        for(int i = 0; i < chunkHeight; i++) {
            for(int j = 0; j < chunkWidth; j++) {
                if(arr[i][j] > 0) {
                    tilemap.SetTile(new Vector3Int(j, i, 0), baseTile);  //sets the tile to solid if code > 0
                }
                if(arr[i][j] == -4) {
                    tilemap.SetTile(new Vector3Int(j, i, 0), hallTile);  //sets the tile for hallways
                }
                if(arr[i][j] == -3) {
                    tilemap.SetTile(new Vector3Int(j, i, 0), roomTile);  //sets the tile for bin split rooms
                }
                if(arr[i][j] == -2) {
                    tilemap.SetTile(new Vector3Int(j, i, 0), largeVoidTile);  //sets the tile for large voids
                }
                if(arr[i][j] == -1) {
                    tilemap.SetTile(new Vector3Int(j, i, 0), smallVoidTile);  //sets the tile for small voids
                }
            }
        }

        worldGrid.AddTilemap(chunkPos, tilemap);

    }
    

    //uses a recursive algorithm to split a chunk into a bunch of rooms of random size
    
    void binarySplit(int x, int y, int w, int h)
    {
        if(w < (int)randGaussian(avgRoomLen, stdvRoomLen) || 
            h < (int)randGaussian(avgRoomLen, stdvRoomLen))   //w or h is low enough to stop splitting, stop recursing
        {
            int maxW = ((int)(w*borderWidth));
            int maxH = ((int)(h*borderWidth));

            int xshift;  //how much the xpos of room is shifted from given x, a random percentage of borderWidth
            int yshift;

            if(maxW > 0) {
                xshift = 1 + rand.Next(maxW-1);
            }
            else {
                xshift = 0;
            }

            if(maxH > 0) {
                yshift = 1 + rand.Next(maxH-1);
            }
            else {
                yshift = 0;
            }

            int wshift = rand.Next(maxW);  //how much the width of the room is shifted from the given w, a random percentage of borderWidth
            int hshift = rand.Next(maxH);

            int finalW = (w - xshift) - wshift;
            int finalH = (h - yshift) - hshift;

            if(finalW >= 2 && finalH >= 2) {
                roomList.Add((x + xshift, y + yshift, (w - xshift) - wshift, (h - yshift) - hshift));  //records a room with x, y, w, h all shifted inwards by calculated amounts
            }
        }
        else  //w and h are large enough to keep splitting, keep recursing
        {
            int vSplit = rand.Next(w);
            int hSplit = rand.Next(h);

            bool isVSplit = (Math.Min(vSplit, w-vSplit) >= Math.Min(hSplit, h-hSplit));  //decides if to split horizontally or vertically to maximize the minimum dimenstion of the resulting rooms

            if(isVSplit)
            {
                binarySplit(x, y, vSplit, h);
                binarySplit(x+vSplit, y, w-vSplit, h);
            }
            else
            {
                binarySplit(x, y, w, hSplit);
                binarySplit(x, y+hSplit, w, h-hSplit);
            }
        }
    }


    //uses a modified random walk algorithm to generate hallways that are mostly
    //straight but sometimes turn

    void randWalk(int x, int y, int dir, int minLen, double turnProb)
    {
        int[,] dirMap = {{0, -1}, {1, 0}, {0, 1}, {-1, 0}};  //how much to add to (x, y) to move: {up, right, down, left}
        List<Vector2Int> walkedTiles = new List<Vector2Int>();  //records the tiles that have been walked over
        int i = 0;  //counts how many tiles have been walked
        double r;  //used to determine if the boid should turn
        int sinceTurn = 0;

        while(i < minLen || (tileArr[y][x] != -3 && tileArr[y][x] != -4))
        {
            r = rand.NextDouble();

            if(x <= 0 || y <= 0 || x >= chunkWidth-1 || y >= chunkHeight-1) {  //kill if out of chunk bounds
                return;
            }

            if(r < turnProb && sinceTurn > turnRadius)  //turn if probability met and hasn't turned recently
            {
                dir = (dir + 1) % 4;
                sinceTurn = 0;
            }
            else if(r > (1.0 - turnProb) && sinceTurn > turnRadius)  //turn if other probability met and hasn't turned recently
            {
                dir--;
                if(dir == -1) {
                    dir = 3;
                }
                sinceTurn = 0;
            }

            walkedTiles.Add(new Vector2Int(x, y));

            x = x+dirMap[dir,0];
			y = y+dirMap[dir,1];
            sinceTurn++;
            i++;
        }

        for(int t = 0; t < walkedTiles.Count; t++)
        {
            int xTile = walkedTiles[t].x;
            int yTile = walkedTiles[t].y;

            if(tileArr[yTile][xTile] > 0) { //TODO: make tilearr properly determine if a tile should be a hall or not, if a hall is inside another larger empty space, that should be a part of that empty space, not the hall. Change this here and on the hallways that stitch together chunks. Change what the tile assignment in this case from -3 to what is appropriate
                if(topoCount(walkedTiles[t].x, walkedTiles[t].y) >= 0) {
                    tileArr[yTile][xTile] = -4;  //sets a tile to hall code if the tile used to be foreground
                }
                else {
                    tileArr[yTile][xTile] = -3; 
                }
            }
        }
                
    }


    //generates all hallways from all rooms

    void hallGen()
    {
        int x, y, w, h, circum, hallCount;
        float wallChance;

        for(int r = 0; r < roomList.Count; r++) {  //iterates across all rooms
            x = roomList[r].Item1;
            y = roomList[r].Item2;
            w = roomList[r].Item3;
            h = roomList[r].Item4;

            circum = 2*(w+h);
            hallCount = 1 + (int)(circum/distPHall);  //calculates the number of halls for a room to scale with the circumference of the room
            wallChance = ((float)h)/((float)(w + h));  //calculates how much of a room is its walls: h/(w+h)

            for(int i = 0; i < hallCount; i++) {  //distributes the calculated number of halls statistically across the four sides of the room
                if(rand.NextDouble() < wallChance) {
                    if(rand.NextDouble() < 0.5) 
                    {
                        randWalk(x, rand.Next(y, y+h), 3, hallLen, turnChance);  //calls our hall generator
                    }
                    else
                    {
                        randWalk(x+w, rand.Next(y, y+h), 1, hallLen, turnChance);
                    }
                }
                else
                {
                    if(rand.NextDouble() < 0.5) 
                    {
                        randWalk(rand.Next(x, x+w), y, 0, hallLen, turnChance);
                    }
                    else
                    {
                        randWalk(rand.Next(x, x+w), y+h, 2, hallLen, turnChance);
                    }
                }
            }
        }
    }


    //generates the hallways that connect chunks to one another

    void connectorGen()
    {
        //this uses an imaginary second coordinate system in which adjacent chunks have a difference in coordinates of 2 instead of 1 (so all chunks cover all positive numbers)
        //the odd numbers map to the edges of chunks, so for two chunks side by side, the right side of left chunk and the left side of right chunk share the same coordinate
        //this allows us to calculate the same random seed for a given edge from either the chunk to its left or the chynk to its right (works the same for up and down)
        
        //finds the imaginary coords of each edge of the chunk
        int xleft = 2*chunkX - 1;
        int xmid = 2*chunkX;
        int xright = 2*chunkX + 1;
        int yup = 2*chunkY - 1;
        int ymid = 2*chunkY;
        int ydown = 2*chunkY + 1;

        //generates a random number class for each x and y coord
        System.Random xleftRand = new System.Random(xleft);
        System.Random xmidRand = new System.Random(xmid);
        System.Random xrightRand = new System.Random(xright);
        System.Random yupRand = new System.Random(yup);
        System.Random ymidRand = new System.Random(ymid);
        System.Random ydownRand = new System.Random(ydown);

        //generatea a random number for each x and y coord
        int xleftSeed = xleftRand.Next();
        int xmidSeed = xmidRand.Next();
        int xrightSeed = xrightRand.Next();
        int yupSeed = yupRand.Next();
        int ymidSeed = ymidRand.Next();
        int ydownSeed = ydownRand.Next();

        //generates a different number if the x or y coord is negative
        if(xleft < 0) {
            xleftSeed = xleftRand.Next();
        }
        if(xmid < 0) {
            xmidSeed = xmidRand.Next();
        }
        if(xright < 0) {
            xrightSeed = xrightRand.Next();
        }
        if(yup < 0) {
            yupSeed = yupRand.Next();
        }
        if(ymid < 0) {
            ymidSeed = ymidRand.Next();
        }
        if(ydown < 0) {
            ydownSeed = ydownRand.Next();
        }
        
        //generates a random number class for each edge, guarenteed to be unrelated to its neighbors
        System.Random upRand = new System.Random(xmidSeed ^ yupSeed);
        System.Random downRand = new System.Random(xmidSeed ^ ydownSeed);
        System.Random leftRand = new System.Random(xleftSeed ^ ymidSeed);
        System.Random rightRand = new System.Random(xrightSeed ^ ymidSeed);


        int randCoord;

        //generates hallways starting from the edges that are guarenteed to be continuous from both sides of the edge as the calcualted seed is consistent across chunks
        for(int i = 0; i < edgeConnectors; i++)
        {
            randCoord = upRand.Next(1, chunkWidth-1);  //chooses where on the edge to seed the hallway
            if(tileArr[0][randCoord] != -2) {  //does not generate a hallway on a large void
                tileArr[0][randCoord] = -4;
                randWalk(randCoord, 1, 2, 0, 0.0);
            }
        }
        for(int i = 0; i < edgeConnectors; i++)
        {
            randCoord = downRand.Next(1, chunkWidth-1);
            if(tileArr[chunkHeight-1][randCoord] != -2) {
                tileArr[chunkHeight-1][randCoord] = -4;
                randWalk(randCoord, chunkHeight-2, 0, 0, 0.0);
            }
        }
        for(int i = 0; i < edgeConnectors; i++)
        {
            randCoord = rightRand.Next(1, chunkHeight-1);
            if(tileArr[randCoord][chunkWidth-1] != -2) {
                tileArr[randCoord][chunkWidth-1] = -4;
                randWalk(chunkWidth-2, randCoord, 3, 0, 0.0);
            }
        }
        for(int i = 0; i < edgeConnectors; i++)
        {
            randCoord = leftRand.Next(1, chunkHeight-1);
            if(tileArr[randCoord][0] != -2) {
                tileArr[randCoord][0] = -4;
                randWalk(1, randCoord, 1, 0, 0.0);
            }
        }
    }
    

    //mother function, generates the chunk from the assigned constant values
    
    public void GenerateChunk()
    {     
        

       //instantiate variables

        rand = new System.Random(chunkSeed);  //sets the random class for chunk-specific features from the chunkSeed fed to the chunk when it was made

        tilemapObj = Instantiate(tilemapPrefab, (Vector2)chunkPos, Quaternion.identity, grid.transform);  //sets the tilemap object to the location of this chunk and childs it to the grid

        //sets tilemap layers 
        tilemapObj.layer = LayerMask.NameToLayer("Foreground");
        tilemapObj.GetComponent<TilemapRenderer>().sortingLayerName = "Foreground";
        
        tilemap = tilemapObj.GetComponent<Tilemap>();  //gets the tilemap component from the tilemap object
        
        roomList = new List<(int, int, int, int)>();  //holds the x, y, w, h of each room created from binary split
        

        //generate chunk
        
        initTileArr();  //initializes chunk and adds small and large perlin noise

        binarySplit(0, 0, chunkWidth, chunkHeight);  //records rooms to roomList

        roomsToTileArr();  //takes rooms in roomList and writes them into tileArr

        hallGen();  //generates halls from rooms

        connectorGen();  //generates halls from edge of chunk (that will match up with the halls generated from chunk adjacent to the edge)

        tileChunk(tileArr);  //takes tileArr and initializes tiles according to its contents
    }


    //disinstantiates this chunk

    public void DestroyChunk()
    {
        worldGrid.RemoveTilemap(chunkPos, tilemap);
        Destroy(tilemapObj);
        Destroy(this.gameObject);
    }
}
