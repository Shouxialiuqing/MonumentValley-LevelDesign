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
        UpdateCurrentCube();
    }

    void Update()
    {
        UpdateCurrentCube();
        //获取玩家当前所在的方块
        GetComponentInChildren<Animator>().SetBool("walking", walking);
        transform.parent = currentCube.parent;
        //Debug.Log(currentCube+" "+transform.parent);
        if (isSearching) return;
        ////确保物体旋转时玩家跟随着一起动
        //if (!currentCube.GetComponent<Walkable>().movingGround)
        //{
        //    transform.parent = null;
        //}

        // 玩家点击方块触发的逻辑

        // 鼠标点击触发寻路（只响应特定标签的方块）
        if (Input.GetMouseButtonDown(0))
        {
            Ray mouseRay = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit[] mouseHits; // 用于存储所有碰撞结果

            // 检测射线上所有碰撞体（按距离排序）
            mouseHits = Physics.RaycastAll(mouseRay);

            // 遍历所有碰撞结果，找到第一个符合条件的方块
            foreach (var mouseHit in mouseHits)
            {
                // 同时检查标签和是否有Walkable组件
                if (mouseHit.transform.CompareTag("Player") &&  // 替换为你的目标标签
                    mouseHit.transform.GetComponent<Walkable>() != null)
                {
                    isSearching = true;
                    clickedCube = mouseHit.transform;
                    DOTween.Kill(gameObject.transform);
                    finalPath.Clear();

                    // 调用静态类方法寻路
                    finalPath = PathfindingUtility.FindPath(currentCube, clickedCube);

                    blend = transform.position.y - clickedCube.position.y > 0 ? -1 : 1;
                    indicator.position = mouseHit.transform.GetComponent<Walkable>().GetWalkPoint();
                    PlayIndicatorAnimation();

                    // 找到路径则移动
                    if (finalPath.Count > 0)
                    {
                        FollowPath();
                    }
                    else
                    {
                        isSearching = false;
                    }

                    break; // 找到第一个符合条件的就退出循环
                }
            }
        }
        Debug.DrawRay(transform.position, transform.up * 5f, Color.green); // 角色上方向
        Debug.DrawRay(transform.position, temp * 5f, Color.yellow); // 角色上方向
        //Debug.Log(temp);
        Debug.DrawRay(currentCube.position, currentCube.up * 5f, Color.red); // 方块上方向
    }
    // 鼠标点击后点击处指示器动画
    private void PlayIndicatorAnimation()
    {
        Sequence s = DOTween.Sequence();
        s.AppendCallback(() => indicator.GetComponentInChildren<ParticleSystem>().Play());
        s.Append(indicator.GetComponent<Renderer>().material.DOColor(Color.white, 0.1f));
        s.Append(indicator.GetComponent<Renderer>().material.DOColor(Color.black, 0.3f).SetDelay(0.2f));
        s.Append(indicator.GetComponent<Renderer>().material.DOColor(Color.clear, 0.3f));
    }

    void FollowPath()
    {
        Sequence s = DOTween.Sequence();

        walking = true;

        for (int i = finalPath.Count - 1; i >= 0; i--)
        {
            float time = finalPath[i].GetComponent<Walkable>().isStair ? 1.5f : 1;

            s.Append(transform.DOMove(finalPath[i].GetComponent<Walkable>().GetWalkPoint(), time * 0.2f).SetEase(Ease.Linear));

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
                    Quaternion targetRot = Quaternion.LookRotation(dir, finalPath[i].up);

                    // 然后平滑过渡到它（局部旋转）
                    s.Join(transform.DORotateQuaternion(targetRot, .1f));

                }


            }

        }

        if (finalPath[0].GetComponent<Walkable>().isButton)
        {
            s.AppendCallback(()=>GameManager.instance.RotateRightPivot());
        }

        s.AppendCallback(() => Clear());
    }

    void Clear()
    {
        isSearching = false; // 寻路结束解锁       
        finalPath.Clear();
        walking = false;
    }


    public void UpdateCurrentCube()
    {
        //Debug.Log("在调用玩家检测");
        Transform hitCube = RayCastDown(transform, "Player");
        if (hitCube != null)
        {
            currentCube = hitCube;

            if (currentCube.GetComponent<Walkable>().isStair)
            {
                DOVirtual.Float(GetBlend(), blend, 10.0f, SetBlend);
            }
            else
            {
                DOVirtual.Float(GetBlend(), 0, 0.1f, SetBlend);
            }
        }
        else
        {
            Debug.Log("玩家脚下的方块为空");
        }
    }
    //检测角色脚下的方块
    private Transform RayCastDown(Transform player, string targetTag, float rayLength = 10f)
    {
        Ray playerRay = new Ray(player.GetChild(0).position, -player.up);
        RaycastHit[] hits;

        // 检测所有碰撞体（按距离排序）
        hits = Physics.RaycastAll(playerRay, rayLength);

        // 遍历所有碰撞结果，找到第一个符合标签的方块
        foreach (var hit in hits)
        {
            string hitInfo = $"{player}的[RayCastDown] 检测到对象: {hit.transform.name}";
            hitInfo += $", 标签: {hit.transform.tag}";
            hitInfo += $", 距离: {hit.distance:F2}";
            //Debug.Log(hitInfo);
            if (hit.transform.CompareTag(targetTag) &&
                hit.transform.GetComponent<Walkable>() != null)
            {
                return hit.transform;
            }
        }

        return null;
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
