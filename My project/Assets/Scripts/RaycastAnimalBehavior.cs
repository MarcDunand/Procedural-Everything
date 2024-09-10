using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

public class RaycastAnimalBehavior : MonoBehaviour
{
    [SerializeField]
    public LayerMask wallLayerMask;     // LayerMask for wall tiles


    public const bool debug = false;
    public const int rayAngle = 15;
    public int rayCount = (int)(360/rayAngle);
    public float maxRayDistance = 2f;  // Maximum distance the ray should cast

    (int, float) findPath(Vector2 origin)
    {
        int noCollidsionCount = 0;  //counts how many of the rays never intersect a wall
        
        float[] collisionDists = new float[(int)(360/rayAngle)];
        
        // Loop through each angle
        for (int i = 0; i < rayCount; i++)
        {
            // Convert the angle to radians and calculate the direction vector
            float radian = i*rayAngle * Mathf.Deg2Rad;
            Vector2 direction = new Vector2(Mathf.Cos(radian), Mathf.Sin(radian));

            // Perform the raycast
            RaycastHit2D hit = Physics2D.Raycast(origin, direction, maxRayDistance, wallLayerMask);

            // If the ray hits something (e.g., a wall)
            if (hit.collider != null)
            {
                // Get the distance to the wall
                float distanceToWall = hit.distance;
                collisionDists[i] = distanceToWall;

                if(debug) {
                    //Debug.Log("Ray at angle " + angle + " hit a wall at distance: " + distanceToWall);
                    Debug.DrawRay(origin, direction * distanceToWall, Color.red);
                }
            }
            else
            {
                // No wall hit, the ray reached its maximum distance
                collisionDists[i] = maxRayDistance;

                if(debug) {
                    Debug.DrawRay(origin, direction * maxRayDistance, Color.red);
                }
            }
        
        }

        float longestRay = collisionDists.Max();
        int indexOfLongest = Array.IndexOf(collisionDists, longestRay);

        return(indexOfLongest*rayAngle, longestRay);
    }

    void Update()
    {
        // Position of the animal (where the rays will start from)
        Vector2 origin = transform.position;

        (int, float) path = findPath(origin);

        
    }
}
