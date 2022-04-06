using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;


[RequireComponent(typeof(Terrain))]

public class Terr_Rnd_MPD : MonoBehaviour
{
    // How much randomness do we want to add to the new generated values, the higher the more the terrain will look jagged
    [SerializeField] private float maxRandomAddition = 0.5f;

    // As the algorithm progresses we want to reduce randomness to create a more smooth surface.
    // The higher this value is the more steep (and diverse) the terrain will be.
    [SerializeField] private float randomValueReduction = 0.45f;

    [SerializeField] private int terrainScale = 1; // Terrain scale

    // Seed so we can reproduce results, if you tick the useRandomSeed variable it will just pick a random one
    [SerializeField] private int customSeed;
    [SerializeField] private bool useRandomSeed = true;

    // Minimum and maximum height of the heightmap (0 is min, 1 is max)
    [SerializeField] private float minHeight = 0f;
    [SerializeField] private float maxHeight = 0.5f;

    Terrain terrain;

    private void Awake()
    {
        terrain = GetComponent<Terrain>();
        GenerateTerrain(terrain);
    }


    private void Start()
    {
        Debug.Log(terrain.terrainData.size);
    }


    //  Generate Terrain function
    public void GenerateTerrain(Terrain t)
    {
        // Using specific seed, if cosen.
        if (!useRandomSeed) Random.InitState(customSeed);

        // 1 2 4 8 16    -> one dimension
        // 1 4 16 64 256 -> two dimensions
        // Mid-point Displacement needs the array size to be a 2^n + 1
        int side = (int)Mathf.Pow(2, terrainScale) + 1;
        // Once we know how long is a side, let's set up the terrain resolution to that
        t.terrainData.heightmapResolution = side;

        // we create the hts matrix, where we will store our new heighmap values. This matrix will have size "side x side"
        float[,] hts = new float[t.terrainData.heightmapResolution, t.terrainData.heightmapResolution];

        //  ################### My Code ###################
        //  Corner values
        float[,] height = new float[side, side];
        height[0, 0] = Random.Range(0f, 1f);
        height[0, side - 1] = Random.Range(0f, 1f);
        height[side - 1, 0] = Random.Range(0f, 1f);
        height[side - 1, side - 1] = Random.Range(0f, 1f);
        //  ################### My Code ###################


        //  Core loop.
        for (int step = side - 1; step >= 2; step /= 2, maxRandomAddition *= randomValueReduction)
        {
            int halfStep = step / 2;
            //  ################### My Code ###################
            // Calculating midpoints
            for (int y = 0; y < side - 1; y += step)
            {
                //  Index of y-midpoint.
                int midpointY = y + (halfStep);

                for (int x = 0; x < side - 1; x += step)
                {
                    //  Index of x-midpoint.
                    int midpointX = x + (halfStep);

                    // Setting the height of square-side midpoints to average of its 2 cornerHeights +/- a random DISPLACEMENT
                    float p1 = height[midpointX, y] = ((height[x, y] + height[x + step, y]) / 2f) + Random.Range(-maxRandomAddition, maxRandomAddition);
                    float p2 = height[x, midpointY] = ((height[x, y] + height[x, y + step]) / 2f) + Random.Range(-maxRandomAddition, maxRandomAddition);
                    float p3 = height[midpointX, y + step] = ((height[x, y + step] + height[x + step, y + step]) / 2f) + Random.Range(-maxRandomAddition, maxRandomAddition);
                    float p4 = height[x + step, midpointY] = ((height[x + step, y] + height[x + step, y + step]) / 2f) + Random.Range(-maxRandomAddition, maxRandomAddition);

                    // Setting height of center square to average of our 4 new midpoints +/- a random DISPLACEMENT
                    height[midpointX, midpointY] = ((p1 + p2 + p3 + p4) / 4f) + Random.Range(-maxRandomAddition, maxRandomAddition);

                    //  Debug.Log("HL: " + height.Length);
                    //  Debug.Log("step: " + step + ", halfStep: " + halfStep + ", x: " + x + ", midpointX: " + midpointX + ", y: " + x + ", midpointY: " + midpointY);
                    //  Debug.Log(p1 + "   " + p2 + "   " + p3 + "   " + p4);
                    //  Debug.Log("Midpoint: " + height[midpointX, midpointY]);
                }

            }
            //  ################### My Code ###################

        }

        //  Copying the heights into new array
        for (int y = 0; y < t.terrainData.heightmapResolution; y++)
            for (int x = 0; x < t.terrainData.heightmapResolution; x++)
                hts[x, y] = height[x, y];


        //  Normalizing within min to max
        hts = NormalizeHeightmap(hts, minHeight, maxHeight);

        //  Passing heights to terrain
        t.terrainData.SetHeights(0, 0, hts);
    }


    float[,] NormalizeHeightmap(float[,] map, float minValue, float maxValue)
    {
        // Normalize between maxHeight and minHeight

        float max = float.NegativeInfinity;
        float min = float.PositiveInfinity;

        // First we go through the matrix and we find the max and min values
        for (int x = 0; x < map.GetLength(0); x++)
        {
            for (int y = 0; y < map.GetLength(1); y++)
            {
                if (map[x, y] > max)
                {
                    max = map[x, y];
                }
                if (map[x, y] < min)
                {
                    min = map[x, y];
                }
            }
        }

        // Once we have max and min we can calculate the range
        float range = max - min;

        // Finally we go through the matrix again and normalize the values
        for (int x = 0; x < map.GetLength(0); x++)
        {
            for (int y = 0; y < map.GetLength(1); y++)
            {
                // Normalize from 0 to 1
                float zeroToOneNormalization = (map[x, y] - min) / range;
                // Then transform the value to be between minValue and maxValue
                map[x, y] = zeroToOneNormalization * (maxValue - minValue) + minValue;
            }
        }

        return map;
    }
}