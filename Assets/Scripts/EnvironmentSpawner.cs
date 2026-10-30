using UnityEngine;

/// <summary>
/// Расставляет деревья и дома вокруг трассы.
/// Запуск: правый клик по компоненту → «Сгенерировать окружение».
/// </summary>
public class EnvironmentSpawner : MonoBehaviour
{
    public GameObject treePrefab;
    public GameObject housePrefab;
    public int treeCount = 40;
    public int houseCount = 10;
    public float spawnRadius = 120f;

    [ContextMenu("Сгенерировать окружение")]
    private void SpawnEnvironment()
    {
        if (treePrefab == null || housePrefab == null)
        {
            Debug.LogWarning("EnvironmentSpawner: не заданы префабы дерева или дома.");
            return;
        }

        for (int i = 0; i < treeCount; i++)
        {
            Vector3 position = new Vector3(
                Random.Range(-spawnRadius, spawnRadius),
                -2.0f,
                Random.Range(-spawnRadius, spawnRadius));

            GameObject tree = Instantiate(treePrefab, position, Quaternion.Euler(0f, Random.Range(0, 360), 0f));
            tree.transform.SetParent(transform);
        }

        for (int i = 0; i < houseCount; i++)
        {
            Vector3 position = new Vector3(
                Random.Range(-spawnRadius, spawnRadius),
                -2.0f,
                Random.Range(-spawnRadius, spawnRadius));

            GameObject house = Instantiate(housePrefab, position, Quaternion.Euler(0f, Random.Range(0, 360), 0f));
            house.transform.SetParent(transform);
        }
    }
}
