using UnityEngine;
using System.Collections.Generic;

public class spawner : MonoBehaviour
{

    [SerializeField] private List<GameObject> legoPrefabs = new List<GameObject>();
    [SerializeField] private float spawnInterval = 2f;
    private float timer;
    public bool canrun = false;
    
   

    // Update is called once per frame
    void Update()
    {
        if (!canrun) return;

        timer += Time.deltaTime;
        if (timer >= spawnInterval)
        {
            SpawnWillekeurigObject();
            timer = 0f;
        }
    }

    void SpawnWillekeurigObject()
    {
        if (legoPrefabs.Count == 0)
        {
            Debug.LogWarning("Geen lego prefabs toegevoegd aan de lijst.");
            return;
        }

        int randomIndex = Random.Range(0, legoPrefabs.Count);
        GameObject prefabToSpawn = legoPrefabs[randomIndex];

        Instantiate(prefabToSpawn, transform.position, transform.rotation);
    }
}
