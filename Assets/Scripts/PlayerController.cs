using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using DG.Tweening;
[SelectionBase]
public class PlayerController : MonoBehaviour
{
    public bool walking = false;//是否正在行走 控制待机与走路动画的切换
    private bool isSearching = false;//是否正在寻路
    private bool findPath=false;//是否成功找到路径
    [Space]

    public Transform currentCube;//玩家当前所在方块
    public Transform clickedCube;//玩家点击的目标方块
    public Transform indicator;//点击指示器

    [Space]

    public List<Transform> finalPath = new List<Transform>();//最终的寻路路径
    private float blend;//上下楼梯极值控制
    void Start()
    {
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
        //finalPath.Insert(0, clickedCube);
        
        FollowPath();
    }

    void FollowPath()
    {
        Sequence s = DOTween.Sequence();

        walking = true;

        for (int i = finalPath.Count - 1; i >= 0; i--)
        {
            float time = finalPath[i].GetComponent<Walkable>().isStair ? 1.5f : 1;

            s.Append(transform.DOMove(finalPath[i].GetComponent<Walkable>().GetWalkPoint(), .2f * time).SetEase(Ease.Linear));

            if(!finalPath[i].GetComponent<Walkable>().dontRotate)
               s.Join(transform.DOLookAt(finalPath[i].position, .1f, AxisConstraint.Y, currentCube.up));//移动的同时播放朝向动画
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
                    DOVirtual.Float(GetBlend(), blend, .1f, SetBlend);//在0.1s内过渡到blend
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
