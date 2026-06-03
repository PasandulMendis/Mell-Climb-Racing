using UnityEngine;
using System.Collections.Generic;

public class MapGenerator : MonoBehaviour
{
    [Header("Chunk Prefabs")]
    // Drag your different hill/terrain chunks here!
    public GameObject[] chunkPrefabs; 

    [Header("Generation Settings")]
    public int chunksToSpawnAhead = 3;  // How many chunks to keep in front of the car
    public float chunkWidth = 20f;      // EXACTLY how wide each chunk is (crucial for seamless connection)
    public float destroyDistanceBehind = 30f; // How far behind the car before deleting a chunk

    private Transform playerCar;
    private float nextSpawnX = 0f;
    private List<GameObject> activeChunks = new List<GameObject>();

    void Start()
    {
        // Spawn the initial starting chunks
        for (int i = 0; i < chunksToSpawnAhead; i++)
        {
            SpawnChunk();
        }
    }

    void Update()
    {
        // We need to wait until the GameManager spawns the car before we can track it
        if (playerCar == null)
        {
            if (GameManager.instance != null && GameManager.instance.playerCar != null)
            {
                playerCar = GameManager.instance.playerCar;
            }
            return;
        }

        // --- SPAWN NEW CHUNKS AHEAD ---
        // If the car gets close to the end of our spawned chunks, spawn another one
        if (playerCar.position.x > (nextSpawnX - (chunksToSpawnAhead * chunkWidth)))
        {
            SpawnChunk();
        }

        // --- DESTROY OLD CHUNKS BEHIND ---
        if (activeChunks.Count > 0)
        {
            // Check the oldest chunk in the list (the one furthest back)
            GameObject oldestChunk = activeChunks[0];
            
            // If the car has driven far enough past it...
            if (playerCar.position.x - oldestChunk.transform.position.x > destroyDistanceBehind)
            {
                // Remove it from our list and destroy it to save memory!
                activeChunks.RemoveAt(0);
                Destroy(oldestChunk);
            }
        }
    }

    void SpawnChunk()
    {
        if (chunkPrefabs.Length == 0) return;

        // Pick a random chunk design from the array
        int randomIndex = Random.Range(0, chunkPrefabs.Length);
        
        // Calculate the spawn position (just moving X forward by the chunk width)
        Vector3 spawnPosition = new Vector3(nextSpawnX, 0f, 0f);

        // Spawn it and add it to our tracking list
        GameObject newChunk = Instantiate(chunkPrefabs[randomIndex], spawnPosition, Quaternion.identity);
        activeChunks.Add(newChunk);

        // Update the marker for where the next chunk should start!
        nextSpawnX += chunkWidth;
    }
}