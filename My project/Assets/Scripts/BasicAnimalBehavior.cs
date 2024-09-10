using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;



//struct used to hold all relevant information about a tile
public struct TileData
{
    public Vector2Int pos;
    public TileBase tile;

    public TileData(Vector2Int pos, TileBase tile)
    {
        this.pos = pos;
        this.tile = tile;
    }
}



public class BasicAnimalBehavior : MonoBehaviour
{
    [SerializeField]
    public float speed;

    [SerializeField]
    float visRange;  //Total visible range from center of animal

    [SerializeField]
    int tileVisRange;

    [SerializeField]
    private TileBase hallTile;

    [SerializeField]
    private TileBase wallTile;

    [SerializeField]
    private TerrainGenerator terrainGenerator;  //the terrainGenerator script

    [SerializeField]
    WorldGrid worldGrid;
    
    [SerializeField]
    GameObject visSquare;

    const bool debug = false;
    
    int chunkWidth;
    int chunkHeight;

    Vector2 moveDir;

    Vector2Int prevTilePos;

    Collider2D[] overlapArr;  //The list of all objects in animmal's visible range

    Vector3Int[] visTilePos;  //The relative transforms on x and y to access each visible tile given a center x and y

    TileData[] visTiles;  //All tiles currently in visible range

    LayerMask foregroundLayer;  //The layer at which the animal can see

    private Rigidbody2D rb;  //animal's rigidbody
    private CircleCollider2D cl;  //animal's collider

    GameObject[] allSquares;


    //true modulo operation (% in unity give remainder)
    int mod(int a, int n)
    {
        return ((a%n)+n) % n;
    }

    Vector3Int[] findCircle(int r)
    {
        List<Vector3Int> allPos = new List<Vector3Int>();

        //Checks each tile in a 2*r box if it's within radius r of the middle
        //WARNING: expensive, do not run every tick
        for(int x = -1*r; x <= r; x++)
        {
            for(int y = -1*r; y <= r; y++)
            {
                if(x*x + y*y < r*r)
                {
                    allPos.Add(new Vector3Int(x, y, 0));
                }
            }
        }

        return allPos.ToArray();
    }


    // Function to get a random vector within a certain angle 'ang' of the original direction
    public static Vector2 GetRandomVectorWithinAngle(Vector2 originalVector, float ang)
    {
        // Normalize the original vector (in case it isn't normalized)
        originalVector.Normalize();

        // Convert the angle 'ang' from degrees to radians, and calculate half the angle
        float halfAngRad = ang * Mathf.Deg2Rad / 2f;

        // Generate a random angle between -halfAngRad and +halfAngRad
        float randomAngle = Random.Range(-halfAngRad, halfAngRad);

        // Calculate the cos and sin of the random angle
        float cos = Mathf.Cos(randomAngle);
        float sin = Mathf.Sin(randomAngle);

        // Construct the rotation matrix for the random angle
        Vector2 randomRotatedVector = new Vector2(
            originalVector.x * cos - originalVector.y * sin,
            originalVector.x * sin + originalVector.y * cos
        );

        // Normalize the resulting vector to ensure it's a unit vector
        return randomRotatedVector.normalized;
    }


    void Start()
    {
        //set runtime variables
        
        moveDir = new Vector2(Random.Range(-1F, 1F), Random.Range(-1F, 1F));

        foregroundLayer = LayerMask.GetMask("Foreground");
        
        rb = GetComponent<Rigidbody2D>();
        cl = GetComponent<CircleCollider2D>();

        chunkWidth = terrainGenerator.chunkWidth;
        chunkHeight = terrainGenerator.chunkHeight;

        visTilePos = findCircle(tileVisRange);  //Gets the relative transforms for all tiles in visible range from a given center point
       
        visTiles = new TileData[visTilePos.Length];  //Sets the length of the visible tile array to the correct length

        if(debug){
            allSquares = new GameObject[visTilePos.Length];
        }
    }


    void FixedUpdate()
    {       
        Vector2 pos = transform.position;  //the animal's current location
        Vector2Int curTilePos = new Vector2Int((int)Mathf.Floor(pos.x), (int)Mathf.Floor(pos.y));  //the x and y indeces relative to the tilemap the animal is on
        Vector2Int curTilemapPos = new Vector2Int(mod(curTilePos.x, chunkWidth), mod(curTilePos.y, chunkHeight));
        TileBase curTile = worldGrid.GetTile(curTilePos);  //The tile the animal is currently on
        
        //updates the set of visible tile if animal is centered on a new tile
        if(curTilePos != prevTilePos)
        {
            //iterates through the list of current tiles and updates each
            for(int i = 0; i < visTiles.Length; i++)
            {
                Vector2Int curCoords = new Vector2Int(curTilePos.x + visTilePos[i].x, curTilePos.y + visTilePos[i].y);
                visTiles[i] = new TileData(curCoords, worldGrid.GetTile(curCoords));
            }

            if(debug)
            {
                Vector2 debugPos = new Vector2(curTilePos.x + 0.5F, curTilePos.y + 0.5F);
                for(int i = 0; i < allSquares.Length; i++)
                {
                    Destroy(allSquares[i]);
                    allSquares[i] = Instantiate(visSquare, new Vector3(debugPos.x + visTilePos[i].x, debugPos.y + visTilePos[i].y, 0), Quaternion.identity);
                }
            }
        }

        //identifies relevant tiles from those in visible range
        for(int i = 0; i < visTiles.Length; i++)
        {
            TileData thisTile = visTiles[i];

            if(thisTile.tile == hallTile)
            {
                //print("hallTile!");
                Vector2 dir = pos - new Vector2(thisTile.pos.x+0.5F, thisTile.pos.y+0.5F);
                float attractionMagnitude = (Mathf.Min(dir.magnitude, visRange)-visRange)*0.02F;
                rb.AddForce(dir.normalized * attractionMagnitude);
            }
            if(thisTile.tile == wallTile)
            {
                //print("wallTile!");
                Vector2 dir = pos - new Vector2(thisTile.pos.x+0.5F, thisTile.pos.y+0.5F);
                float attractionMagnitude = (Mathf.Pow(visRange - Mathf.Min(dir.magnitude, visRange), 2)/Mathf.Pow(visRange, 4F)*0.4F);
                rb.AddForce(dir.normalized * attractionMagnitude);
            }
        }



        // overlapArr = Physics2D.OverlapCircleAll(pos, visRange, foregroundLayer);  //lists all objects (by their colliders) in foreground layer in visible range
        // //print(overlapArr.Length);
        // foreach (Collider2D collider in overlapArr)
        // {
        //     GameObject currentObject = collider.gameObject;  //finds the parent GameObject of the collider
        //     Vector2 closestPoint = collider.ClosestPoint(pos);  //finds the closest point of the  collider to the animal

        //     //repels animal
        //     //if(closestPoint.x == pos.x || closestPoint.y == pos.y) {
        //         Vector2 dir = pos - closestPoint;
        //         float repulsionMagnitude = (visRange - dir.magnitude)*100;
        //         rb.AddForce(dir.normalized * repulsionMagnitude);
        //     //}
        // }

        //randomly moves animal
        if(Random.Range(0F, 1F) < 0.0F) {
            float translation = Random.Range(-1F, 1F);
            float straffe = Random.Range(-1F, 1F);

            Vector2 forceVec = new Vector2(straffe, translation);

            rb.AddForce(forceVec * speed);
        }

        moveDir = GetRandomVectorWithinAngle(moveDir, 30);
        
        rb.AddForce(moveDir*0.1F);
        


        // if(debug)
        // {
        //     foreach(TileBase t in visTiles)
        //     {
        //         print(t);
        //     }
        //     print("_________________________");
        // }


        prevTilePos = curTilePos;
    }
}
