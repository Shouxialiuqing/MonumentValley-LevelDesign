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

    [Header("敌人检测")]
    public float detectionRadius = 0.5f; // 球形射线检测半径
    public float detectionDistance = 5f; // 检测距离
    private bool enemyInFront = false; // 改为更准确的命名
    private Vector3 lastClickPosition; // 记录最后一次点击位置
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
        if (currentCube.GetComponent<Walkable>().movingGround)
        {
            transform.parent = currentCube.parent;
        }
        else
        {
            transform.parent = null;
        }
        CheckForEnemyAhead();
        //Debug.Log(currentCube+" "+transform.parent);
        if (isSearching) return;//完成寻路再允许点击下一次

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
                if (mouseHit.transform.CompareTag("Player") &&  
                    mouseHit.transform.GetComponent<Walkable>() != null)
                {
                    AudioManager.Instance.PlayOneShot("Click");
                    lastClickPosition = mouseHit.point; // 记录点击位置
                    if (!IsClickInFrontWithEnemy(lastClickPosition))
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
    // 检测前方是否有敌人
    private void CheckForEnemyAhead()
    {
        if (transform.childCount == 0) return;

        // 从玩家的第一个孩子的位置向前发射球形射线
        Transform firstChild = transform.GetChild(0);
        RaycastHit[] hits = Physics.SphereCastAll(firstChild.position, detectionRadius, transform.forward, detectionDistance);

        // 可视化检测射线
        Debug.DrawRay(firstChild.position, transform.forward * detectionDistance,
                     hits.Length > 0 ? Color.red : Color.blue, 0.1f);

        enemyInFront = false; // 默认可以移动

        // 检查所有碰撞体，看是否有敌人
        foreach (var hit in hits)
        {
            
            if (hit.collider.CompareTag("EnemyCat"))
            {
               enemyInFront = true;
                // 停止所有移动
                DOTween.Kill(transform);
                //Debug.Log("检测到敌人");
                walking = false;
                isSearching = false;
                finalPath.Clear();
                break; // 只要发现一个敌人就停止
            }
        }
    }
    // 判断点击位置是否在玩家正前方且有敌人
    private bool IsClickInFrontWithEnemy(Vector3 clickPosition)
    {
        if (!enemyInFront) return false;

        Vector3 toClickDirection = (clickPosition - transform.position).normalized;
        float dotProduct = Vector3.Dot(transform.forward, toClickDirection);

        // 如果点击方向与玩家前方夹角小于45度(约0.7的cos值)，则认为在正前方
        bool isInFront = dotProduct > 0.7f;

        Debug.Log($"点击方向检测: 点积={dotProduct}, 是否正前方={isInFront}");

        return isInFront;
    }

    void FollowPath()
    {
        Sequence s = DOTween.Sequence();

        walking = true;
        AudioManager.Instance.PlayLooping("Run");
        for (int i = finalPath.Count - 1; i >= 0; i--)
        {
            // 如果检测到敌人需要停止移动，则终止移动
            if (enemyInFront && IsClickInFrontWithEnemy(lastClickPosition))
            {
                DOTween.Kill(transform);
                Clear();
                Debug.Log("移动中检测到正前方敌人，停止移动");
                return;
            }
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

                    // 然后平滑过渡到它
                    s.Join(transform.DORotateQuaternion(targetRot, .1f));

                }


            }

        }

        if (finalPath[0].GetComponent<Walkable>().isButton)
        {
            s.AppendCallback(()=>
            {
                GameManager.instance.RotatePivot();
                AudioManager.Instance.PlayOneShot("Button");
            });
        }
        if (finalPath[0].GetComponent<Walkable>().isEnd)
        {
            s.AppendCallback(() =>EventManager.TriggerSceneTransition());
        }
        s.AppendCallback(() => {
            if (AudioManager.Instance.IsLooping("Run"))
                AudioManager.Instance.StopLooping("Run");
            Clear();
        });
    }

    void Clear()
    {
        isSearching = false; // 寻路结束解锁       
        finalPath.Clear();
        walking = false;
    }


    public void UpdateCurrentCube()
    {
        Transform hitCube = RayCastDown(transform);
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
        //else
        //{
        //    Debug.Log("玩家脚下的方块为空");
        //}
    }
    // 射线检测方法（带Gizmo可视化）
    public Transform RayCastDown(Transform player)
    {
        if (player == null) return null;

        Vector3 origin = player.GetChild(0).position;
        Vector3 direction = -player.up;

        // 球形射线检测
        RaycastHit[] hits = Physics.SphereCastAll(origin, 0.3f, direction, 10f);

        // 遍历检测结果
        foreach (var hit in hits)
        {
            //Debug.Log(gameObject+" "+hit.transform.tag+" "+hit.transform);
            if (hit.transform.CompareTag("Player") &&
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
