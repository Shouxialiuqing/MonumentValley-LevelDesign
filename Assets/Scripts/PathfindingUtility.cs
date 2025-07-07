using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using DG.Tweening;

public static class PathfindingUtility
{
    private static bool findPath = false; // 静态寻路标志

    // 静态寻路主方法，直接通过类名调用
    public static List<Transform> FindPath(Transform startCube, Transform targetCube)
    {
        List<Transform> finalPath = new List<Transform>();
        findPath = false;

        List<Transform> nextCubes = new List<Transform>(); // 待探索的方块
        List<Transform> pastCubes = new List<Transform>(); // 已探索的方块

        // 初始化起点的可探索路径
        foreach (WalkPath path in startCube.GetComponent<Walkable>().possiblePaths)
        {
            if (path.active)
            {
                nextCubes.Add(path.target);
                path.target.GetComponent<Walkable>().previousBlock = startCube;
            }
        }

        pastCubes.Add(startCube);

        // 开始探索
        if (nextCubes.Count > 0)
        {
            ExploreCube(nextCubes, pastCubes, targetCube);
            BuildPath(pastCubes, targetCube, startCube, finalPath);
        }

        return finalPath;
    }

    // 广度优先搜索探索方块
    private static void ExploreCube(List<Transform> nextCubes, List<Transform> visitedCubes, Transform targetCube)
    {
        if (nextCubes.Count == 0) return;

        Transform current = nextCubes.First();
        nextCubes.Remove(current);

        // 找到目标方块
        if (current == targetCube)
        {
            findPath = true;
            return;
        }

        // 探索当前方块的所有可能路径
        foreach (WalkPath path in current.GetComponent<Walkable>().possiblePaths)
        {
            if (!visitedCubes.Contains(path.target) && path.active)
            {
                nextCubes.Add(path.target);
                path.target.GetComponent<Walkable>().previousBlock = current;
            }
        }

        visitedCubes.Add(current);
        ExploreCube(nextCubes, visitedCubes, targetCube);
    }

    // 构建路径
    private static void BuildPath(List<Transform> visitedCubes, Transform targetCube, Transform startCube, List<Transform> finalPath)
    {
        float minDistance = float.MaxValue;
        Transform nearestCube = null;
        Transform cube = null;
        if (findPath)
        {
            cube = targetCube;
        }
        else
        {
            // 找到距离目标最近的方块
            foreach (Transform singleCube in visitedCubes)
            {
                float distance = Vector3.Distance(singleCube.position, targetCube.position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearestCube = singleCube;
                }
            }
            cube = nearestCube;
        }
        // 回溯路径       
        while (cube != startCube)
        {
            finalPath.Add(cube);
            if (cube.GetComponent<Walkable>().previousBlock != null)
                cube = cube.GetComponent<Walkable>().previousBlock;
            else
                return;
        }

        // 清理路径数据
        ClearPathData(visitedCubes);
    }

    // 清理路径数据
    private static void ClearPathData(List<Transform> visitedCubes)
    {
        foreach (Transform t in visitedCubes)
        {
            if (t != null && t.GetComponent<Walkable>() != null)
                t.GetComponent<Walkable>().previousBlock = null;
        }
    }
}