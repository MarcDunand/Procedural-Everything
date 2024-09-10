using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TerrainGenerator : MonoBehaviour
{
    const int visW = 64;  //aspect ratio of screen (used to determine which chunks will be visible on screen)
    const int visH = 36;
    
    [SerializeField]   
    private int worldSeed;  //The source seed that everything is derived from

    [SerializeField]
    private GameObject chunkGen;  //The chunk generator

    [SerializeField]
    private GameObject gridPrefab;  //The world grid (used for tiles)

    public int chunkWidth = 30;  //dimensions of each chunk
    public int chunkHeight = 30;

    System.Random worldRand;  //The random variable generated from the world seed

    public Dictionary<(int, int), ChunkGenerator> activeChunks = new Dictionary<(int, int), ChunkGenerator>();  //The list of chunks that are currently generated (in the world), keys: x pos (middle), y pos (middle), value: the live chunk generator object

    Dictionary<(int, int), int> savedChunks = new Dictionary<(int, int), int>();  //The list of all chunks that have been visited keys: x, y chunk coord, value: seed of that chunk

    
    //calculates which chunks are visible from the given x and y coords

    List<(int, int)> getVisibleChunks(float x, float y) {
        List<(int, int)> visibleChunks = new List<(int, int)>();

        //calculates the chunks furthest to the left, right, above and below the given x and y coords
        int xn = (int)(((x%chunkWidth)-visW)/chunkWidth) + (int)(x/chunkWidth) - 1;
        int xp = (int)(((x%chunkWidth)+visW)/chunkWidth) + (int)(x/chunkWidth) + 1;
        int yn = (int)(((y%chunkHeight)-visH)/chunkHeight) + (int)(y/chunkHeight) - 1;
        int yp = (int)(((y%chunkHeight)+visH)/chunkHeight) + (int)(y/chunkHeight) + 1;

        //iterates across the above bounds, adding all chunks in bounds to the returned list
        for (int xr = xn; xr <= xp; xr++) {
            for (int yr = yn; yr <= yp; yr++) {
                visibleChunks.Add((xr*chunkWidth, yr*chunkHeight));
            }
        }

        return visibleChunks;
    }
    
    // Start is called before the first frame update
    void Start()
    {
        //variable setup
        worldRand = new System.Random(worldSeed);  //initializes the random variable object used to determine all other random variables
    
    }

    void Update()
    {
        Vector3 pos = transform.position;

        List<(int, int)> visibleChunks = getVisibleChunks(pos.x, pos.y);

        List<(int, int)> newChunks = (visibleChunks.Except(activeChunks.Keys)).ToList();  //chunks that entered visible range this tick
        List<(int, int)> deadChunks = (activeChunks.Keys.Except(visibleChunks)).ToList();  //chunks that exited visible range this tick


        //disinstantiates each chunk in deadChunks
        foreach ((int xChunk, int yChunk) in deadChunks)
        {
            ChunkGenerator chunkGenerator = activeChunks[(xChunk, yChunk)];
            activeChunks.Remove((xChunk, yChunk));
            chunkGenerator.DestroyChunk();
        }


        //"deals with" each chunk in newChunks
        foreach ((int xChunk, int yChunk) in newChunks) 
        {
            //initializes each chunk into the gameworld
            GameObject chunkGenObject = Instantiate(chunkGen);

            ChunkGenerator chunkGenerator = chunkGenObject.GetComponent<ChunkGenerator>();

            int foundValue = 0;
            if(!savedChunks.TryGetValue((xChunk, yChunk), out foundValue))  //if this chunk has never been visted
            {
                //assignes a new seed to this chunk and then generates the chunk
                int newChunkSeed = worldRand.Next();
                chunkGenerator.SetConsts((xChunk)/chunkWidth, (yChunk)/chunkHeight, newChunkSeed, chunkWidth, chunkHeight);
                chunkGenerator.GenerateChunk();

                savedChunks.Add((xChunk, yChunk), newChunkSeed);
            }
            else  //if it has been visted before
            {
                //generates this chunk with the seed retrieved from savedChunks with TryGetValue
                chunkGenerator.SetConsts((xChunk)/chunkWidth, (yChunk)/chunkHeight, foundValue, chunkWidth, chunkHeight);
                chunkGenerator.GenerateChunk();
            }

            activeChunks.Add((xChunk, yChunk), chunkGenerator);
        }
    }
}
