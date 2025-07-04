using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using DG.Tweening;
using UnityEngine.UIElements;
[SelectionBase]
public class PlayerController : MonoBehaviour
{
    public static PlayerController instance;
    public bool walking = false;//是否正在行走 控制待机与走路动画的切换
    private bool isSearching = false;//是否正在寻路
    private bool findPath=false;//是否成功找到路径
    [Space]

    public Transform currentCube;//玩家当前所在方块
    public Transform clickedCube;//玩家点击的目标方块
    public Transform indicator;//点击指示器
    static Vector3 temp=Vector3.zero;
   [Space]

    public List<Transform> finalPath = new List<Transform>();//最终的寻路路径
    private float blend;//上下楼梯极值控制

    private void Awake()
    {
        instance = this;
    }
    void Start()
    {
        DOTween.SetTweensCapacity(10000, 100);
        RayCastDown();
    }

    void Update()
    {
        //GET CURRENT CUBE (UNDER PLAYER) 获取玩家当前所在的方块

        RayCastDown();
        transform.parent = currentCube.parent;
        //Debug.Log(currentCube+" "+transform.parent);
        if (isSearching) return;
        ////确保物体旋转时玩家跟随着一起动
        //if (!currentCube.GetComponent<Walkable>().movingGround)
        //{
        //    transform.parent = null;
        //}

        // 玩家点击方块触发的逻辑

        if (Input.GetMouseButtonDown(0))
        {
            Ray mouseRay = Camera.main.ScreenPointToRay(Input.mousePosition); RaycastHit mouseHit;

            if (Physics.Raycast(mouseRay, out mouseHit))
            {
                if (mouseHit.transform.GetComponent<Walkable>() != null)
                {
                    isSearching = true;
                    clickedCube = mouseHit.transform;
                    DOTween.Kill(gameObject.transform);
                    finalPath.Clear();
                    FindPath();

                    blend = transform.position.y - clickedCube.position.y > 0 ? -1 : 1;

                    indicator.position = mouseHit.transform.GetComponent<Walkable>().GetWalkPoint();
                    Sequence s = DOTween.Sequence();
                    s.AppendCallback(() => indicator.GetComponentInChildren<ParticleSystem>().Play());
                    s.Append(indicator.GetComponent<Renderer>().material.DOColor(Color.white, .1f));
                    s.Append(indicator.GetComponent<Renderer>().material.DOColor(Color.black, .3f).SetDelay(.2f));
                    s.Append(indicator.GetComponent<Renderer>().material.DOColor(Color.clear, .3f));

                }
            }
        }
        Debug.DrawRay(transform.position, transform.up * 5f, Color.green); // 角色上方向
        Debug.DrawRay(transform.position, temp * 5f, Color.yellow); // 角色上方向
        //Debug.Log(temp);
        Debug.DrawRay(currentCube.position, currentCube.up * 5f, Color.red); // 方块上方向
    }

    void FindPath()
    {
        List<Transform> nextCubes = new List<Transform>();//待探索的方块
        List<Transform> pastCubes = new List<Transform>();//已探索的方块

        foreach (WalkPath path in currentCube.GetComponent<Walkable>().possiblePaths)
        {
            if (path.active)
            {
                nextCubes.Add(path.target);
                //Debug.Log("下一个的" + path.target);
                path.target.GetComponent<Walkable>().previousBlock = currentCube;
            }
        }
        //Debug.Log("过去的" + currentCube);
        pastCubes.Add(currentCube);
        if (nextCubes.Count > 0)
        {
            ExploreCube(nextCubes, pastCubes);
            BuildPath(pastCubes);
        }
        
    }
    //用的广搜，同时找所有可能的路径，最先找到的那条路径成功返回，否则遍历所有路径再返回
    void ExploreCube(List<Transform> nextCubes, List<Transform> visitedCubes)
    {
        Transform current = nextCubes.First();
        nextCubes.Remove(current);

        if (current == clickedCube)
        {
            findPath = true;
            return;
        }

        foreach (WalkPath path in current.GetComponent<Walkable>().possiblePaths)
        {
            if (!visitedCubes.Contains(path.target) && path.active)
            {
                nextCubes.Add(path.target);
                path.target.GetComponent<Walkable>().previousBlock = current;
            }
        }

        visitedCubes.Add(current);

        if (nextCubes.Any())
        {
            ExploreCube(nextCubes, visitedCubes);
        }
    }

    void BuildPath(List<Transform> visitedCubes)
    {
        float minDistance = float.MaxValue;
        Transform nearestCube = null;
        Transform cube = null;
        foreach (Transform singleCube in visitedCubes)
        {
            float distance = Vector3.Distance(singleCube.position, clickedCube.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                 nearestCube = singleCube;
            }
        }
        cube = findPath?clickedCube:nearestCube;
        while (cube != currentCube)
        {
            finalPath.Add(cube);
            if (cube.GetComponent<Walkable>().previousBlock != null)
                cube = cube.GetComponent<Walkable>().previousBlock;
            else
                return;
        }
        cube = findPath ? clickedCube : nearestCube;
        //finalPath.Insert(0, cube);
        
        FollowPath();
    }

    void FollowPath()
    {
        Sequence s = DOTween.Sequence();

        walking = true;

        for (int i = finalPath.Count - 1; i >= 0; i--)
        {
            float time = finalPath[i].GetComponent<Walkable>().isStair ? 1.5f : 1;

            s.Append(transform.DOMove(finalPath[i].GetComponent<Walkable>().GetWalkPoint(), time*0.2f).SetEase(Ease.Linear)).OnUpdate(() =>
            {
                if (finalPath == null || i < 0 || i >= finalPath.Count)
                {
                    return; // 安全退出
                }
                transform.up = finalPath[i].up;
            });

            if (!finalPath[i].GetComponent<Walkable>().dontRotate)
            {
                Vector3 dir = Vector3.zero;
                if (i == finalPath.Count - 1) dir = finalPath[i].position - currentCube.position;
                else dir = finalPath[i].position - finalPath[i+1].position;
                //鬼知道我做了多少次实验才得出的这个计算方法，又被线性代数玩了哈哈
                if (dir.sqrMagnitude > 0.0001f)
                {

                    // 1. 获取 finalPath[i] 的 up (Y) 和 forward (Z) 方向
                    Vector3 up = finalPath[i].up;
                    Vector3 forward = finalPath[i].forward;
                    //Debug.Log($"局部坐标系：Up方向 = {up}, Forward方向 = {forward}");

                    // 2. 计算平面的法向量 (normal = up × forward)
                    Vector3 planeNormal = Vector3.Cross(up, forward).normalized;

                    // 3. 计算 dir 在平面上的投影 (dir - (dir·normal) * normal)
                    float dotDirNormal = Vector3.Dot(dir, planeNormal);
                    Vector3 projectedDir = dir - dotDirNormal * planeNormal;

                    // 4. 计算 projectedDir 垂直于up方向的分量作为前向量
                    if (projectedDir != Vector3.zero && up != Vector3.zero)
                    {
                        Vector3 proj = Vector3.Project(projectedDir, up);
                        //Debug.Log($"投影方向在向上的方向上的投影{proj}");
                        // 最终 dir = 投影后的向量 * sinθ
                        dir = projectedDir-proj;
                        //Debug.Log($"最终方向向量 = {dir}");

                    }
                    
                    temp = dir;
                    Debug.DrawRay(transform.position, temp * 5f, Color.yellow); // 角色上方向
                    Quaternion targetRot = Quaternion.LookRotation(dir, finalPath[i].up);

                    // 然后平滑过渡到它（局部旋转）
                    s.Join(transform.DORotateQuaternion(targetRot, .1f));

                }


            }

        }

        if (clickedCube.GetComponent<Walkable>().isButton)
        {
            s.AppendCallback(()=>GameManager.instance.RotateRightPivot());
        }

        s.AppendCallback(() => Clear());
    }

    void Clear()
    {
        isSearching = false; // 寻路结束解锁
        foreach (Transform t in finalPath)
        {
            t.GetComponent<Walkable>().previousBlock = null;
        }
        finalPath.Clear();
        walking = false;
        findPath = false;
    }


    //从玩家向下发射射线用于获取当前玩家脚下方块的transform
    public void RayCastDown()
    {

        Ray playerRay = new Ray(transform.GetChild(0).position, -transform.up);
        RaycastHit playerHit;

        if (Physics.Raycast(playerRay, out playerHit))
        {
            if (playerHit.transform.GetComponent<Walkable>() != null)
            {
                currentCube = playerHit.transform;

                if (playerHit.transform.GetComponent<Walkable>().isStair)
                {
                    DOVirtual.Float(GetBlend(), blend, 10.0f, SetBlend);//在0.1s内过渡到blend
                }
                else
                {
                    DOVirtual.Float(GetBlend(), 0, .1f, SetBlend);
                }//进出楼梯动画控制数的切换
            }
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Ray ray = new Ray(transform.GetChild(0).position, -transform.up);
        Gizmos.DrawRay(ray);
    }

    float GetBlend()
    {
        return GetComponentInChildren<Animator>().GetFloat("Blend");
    }
    void SetBlend(float x)
    {
        GetComponentInChildren<Animator>().SetFloat("Blend", x);
    }
}
