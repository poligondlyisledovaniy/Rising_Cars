using UnityEngine;

public class EnvironmentSpawner : MonoBehaviour
{
    public GameObject treePrefab;
    public GameObject housePrefab;
    public int treeCount = 40;
    public int houseCount = 10;
    public float spawnRadius = 120f;

    [ContextMenu("Расставить окружение!")]
    void SpawnEnvironment()
    {
        // Деревья
        for (int i = 0; i < treeCount; i++)
        {
            Vector3 randomPos = new Vector3(Random.Range(-spawnRadius, spawnRadius), -2.0f, Random.Range(-spawnRadius, spawnRadius));
            GameObject tree = Instantiate(treePrefab, randomPos, Quaternion.identity);
            tree.transform.SetParent(transform);
        }

        // Дома
        for (int i = 0; i < houseCount; i++)
        {
            Vector3 randomPos = new Vector3(Random.Range(-spawnRadius, spawnRadius), -2.0f, Random.Range(-spawnRadius, spawnRadius));
            GameObject house = Instantiate(housePrefab, randomPos, Quaternion.Euler(0, Random.Range(0, 360), 0));
            house.transform.SetParent(transform);
        }
    }
}