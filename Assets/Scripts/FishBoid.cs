using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody))]
public class FishBoid : MonoBehaviour
{
    [Header("Boid Settings")]
    public float maxSpeed = 3f;
    public float perceptionRadius = 2f;
    public float separationWeight = 1f;
    public float alignmentWeight = 1f;
    public float cohesionWeight = 1f;
    public float boundsWeight = 2f;

    private Rigidbody rb;
    private Vector3 sphereCenter = Vector3.zero;
    private float sphereRadius = 5f;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        // 随机初始位置和速度
        rb.position = Random.insideUnitSphere * sphereRadius;
        rb.velocity = Random.onUnitSphere * maxSpeed;
    }

    void Update()
    {
        // 获取邻近鱼群
        List<FishBoid> neighbors = GetNeighbors();

        // 计算Boids三原则
        Vector3 separation = CalculateSeparation(neighbors) * separationWeight;
        Vector3 alignment = CalculateAlignment(neighbors) * alignmentWeight;
        Vector3 cohesion = CalculateCohesion(neighbors) * cohesionWeight;
        Vector3 bounds = CalculateBounds() * boundsWeight;

        // 综合所有力
        Vector3 acceleration = separation + alignment + cohesion + bounds;

        // 更新速度
        rb.velocity += acceleration * Time.deltaTime;
        rb.velocity = Vector3.ClampMagnitude(rb.velocity, maxSpeed);

        // 更新朝向
        if (rb.velocity != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(rb.velocity);
    }

    List<FishBoid> GetNeighbors()
    {
        // 实现获取邻近鱼群的逻辑
        List<FishBoid> neighbors = new List<FishBoid>();
        FishSchoolManager manager = FindObjectOfType<FishSchoolManager>();

        foreach (FishBoid fish in manager.GetAllFish())
        {
            if (fish != this && Vector3.Distance(rb.position, fish.rb.position) < perceptionRadius)
            {
                neighbors.Add(fish);
            }
        }
        return neighbors;
    }

    Vector3 CalculateSeparation(List<FishBoid> neighbors)
    {
        if (neighbors.Count == 0) return Vector3.zero;

        Vector3 separation = Vector3.zero;
        int closeNeighbors = 0;
        float minDistance = 0.5f; // 最小舒适距离

        foreach (FishBoid fish in neighbors)
        {
            float distance = Vector3.Distance(rb.position, fish.rb.position);
            if (distance < minDistance)
            {
                Vector3 diff = rb.position - fish.rb.position;
                // 距离越近排斥力越大(使用反比平方)
                separation += diff.normalized / (distance * distance + 0.01f);
                closeNeighbors++;
            }
        }

        return closeNeighbors > 0 ? (separation / closeNeighbors) : Vector3.zero;

    }

    Vector3 CalculateAlignment(List<FishBoid> neighbors)
    {
        // 实现对齐逻辑
        if (neighbors.Count == 0) return Vector3.zero;

        Vector3 avgVelocity = Vector3.zero;
        foreach (FishBoid fish in neighbors)
        {
            avgVelocity += fish.rb.velocity;
        }
        return avgVelocity / neighbors.Count;
    }

    Vector3 CalculateCohesion(List<FishBoid> neighbors)
    {
        // 实现聚集逻辑
        if (neighbors.Count == 0) return Vector3.zero;

        Vector3 avgPosition = Vector3.zero;
        foreach (FishBoid fish in neighbors)
        {
            avgPosition += fish.rb.position;
        }
        avgPosition /= neighbors.Count;
        return (avgPosition - rb.position).normalized;
    }

    Vector3 CalculateBounds()
    {
        // 实现边界约束
        Vector3 offsetToCenter = sphereCenter - rb.position;
        float distanceToCenter = offsetToCenter.magnitude;
        float pushForce = 0f;

        if (distanceToCenter > sphereRadius)
        {
            pushForce = (distanceToCenter - sphereRadius) * 0.5f;
            return offsetToCenter.normalized * pushForce;
        }

        return Vector3.zero;
    }
}
