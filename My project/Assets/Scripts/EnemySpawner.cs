using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public Transform[] SpawnPoints;
    public GameObject EnemyPrefab;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        for(int i = 0; i < SpawnPoints.Length; i++)
        {
            Instantiate(EnemyPrefab, SpawnPoints[i]);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
