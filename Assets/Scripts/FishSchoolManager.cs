using UnityEngine;
using System.Collections.Generic;

public class FishSchoolManager : MonoBehaviour
{
    public GameObject fishPrefab;
    public int fishCount = 20;
    public float spawnRadius = 4.5f;

    private List<FishBoid> allFish = new List<FishBoid>();

    void Start()
    {
        if (fishPrefab == null) // 添加null检查
        {
            Debug.LogError("Fish prefab is not assigned in FishSchoolManager!");
            return;
        }

        for (int i = 0; i < fishCount; i++)
        {
            Vector3 spawnPos = Random.insideUnitSphere * spawnRadius;
            GameObject fish = Instantiate(fishPrefab, spawnPos, Quaternion.identity);
            FishBoid boid = fish.GetComponent<FishBoid>();
            if (boid != null) // 确保组件存在
            {
                allFish.Add(boid);
            }
            else
            {
                Debug.LogWarning("Instantiated fish is missing FishBoid component!");
            }
        }
    }

    public List<FishBoid> GetAllFish()
    {
        return allFish;
    }
}