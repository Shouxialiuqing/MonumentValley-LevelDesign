using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;


[SelectionBase]
public class EnemyController : MonoBehaviour
{
    public static EnemyController instance;

    public Transform currentCube;      // 当前所在方块
    public Transform[] patrolPoints;   // 巡逻点数组（两个方块）
    public Transform player;           // 玩家引用
    public Transform parent;//所在道路的父物体

    public float attackRange = 1.5f;     // 攻击距离阈值
    
    private int currentPatrolIndex = 0; // 当前巡逻点索引
    private bool isPatrolling = false;   // 是否正在巡逻
    private bool isAttacking = false;    // 是否正在攻击
    public List<Transform> path = new List<Transform>(); // 当前路径
    private Coroutine moveCoroutine;     // 移动协程引用，用于停止协程
    Vector3 temp = Vector3.zero;
    Vector3 dir = Vector3.zero;

    // 射线参数
    public string targetTag = "Enemy";
    public float rayLength = 10f;
    public float sphereRadius = 0.3f;
    public Color gizmoColor = Color.yellow;

    public bool isReady = true;
    private void Awake()
    {
        instance = this;
    }
    void Start()
    {
        if (!isReady) return;
        // 初始化巡逻点和当前位置
        if (patrolPoints.Length != 2)
        {
            Debug.LogError("巡逻点数量必须为2个！");
            return;
        }
        transform.parent = parent;
        // 获取初始位置
        UpdateCurrentCube();
        // 确保currentCube不为null
        if (currentCube == null)
        {
            Debug.LogError("无法获取currentCube，请检查场景中方块设置");
            return;
        }
        StartPatrol();
    }

    void Update()
    {
        
        // 每帧更新当前方块
        UpdateCurrentCube();
        if (!isReady) return;
        // 检查与玩家的距离
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // 切换攻击/巡逻状态
        if (distanceToPlayer <= attackRange && !isAttacking)
        {
            StopPatrol();
            StartAttack();
        }
        else if (distanceToPlayer > attackRange && isAttacking)
        {
            StopAttack();
            StartPatrol();
        }

        
        //Debug.DrawRay(transform.position, transform.up * 5f, Color.green); // 角色上方向
        Debug.DrawRay(transform.position, temp * 5f, Color.yellow); // 角色上方向
        //Debug.Log(gameObject+" "+temp);
        //Debug.DrawRay(currentCube.position, currentCube.up * 5f, Color.red); // 方块上方向
    }

    // 开始巡逻
    public void StartPatrol()
    {
        Debug.Log("开始寻路");
        isPatrolling = true;
        isAttacking = false;
        GetComponentInChildren<Animator>().SetBool("IsWalking", true);
        GetComponentInChildren<Animator>().SetBool("IsAttacking", false);
        FindNewPath();
    }

    // 停止巡逻
    public void StopPatrol()
    {
        isPatrolling = false;

        // 停止移动协程
        if (moveCoroutine != null)
        {
            StopCoroutine(MoveAlongPathCoroutine());
            moveCoroutine = null;
        }
        // 清理路径和动画
        path.Clear();
        //DOTween.Kill(transform.gameObject);
        
    }

    // 开始攻击
    private void StartAttack()
    {
        isAttacking = true;
        isPatrolling = false;
        GetComponentInChildren<Animator>().SetBool("IsAttacking", true);
        GetComponentInChildren<Animator>().SetBool("IsWalking", false);
    }

    // 停止攻击
    private void StopAttack()
    {
        DOTween.Kill(transform);
        transform.DOKill();
        isAttacking = false;
        GetComponentInChildren<Animator>().SetBool("IsAttacking", false);
    }

    // 寻找新路径
    public void FindNewPath()
    {
        Transform targetPoint = patrolPoints[currentPatrolIndex];
        path = PathfindingUtility.FindPath(currentCube, targetPoint);

        // 如果找不到路径，切换到另一个巡逻点
        if (path.Count == 0)
        {
            SwitchPatrolPoint();
            if (currentCube != null)
            {
                path = PathfindingUtility.FindPath(currentCube, patrolPoints[currentPatrolIndex]);
               
            }
            else Debug.Log("检测不到脚下的方块");
            // 如果还是找不到路径，停在原地
            if (path.Count == 0)
            {
                return;
            }
        }

        // 开始沿路径移动（启动协程）
        moveCoroutine = StartCoroutine(MoveAlongPathCoroutine());
    }

    // 沿路径移动（协程版本）
    private IEnumerator MoveAlongPathCoroutine()
    {
        if (path == null || path.Count == 0)
            yield break;

        for (int i = path.Count - 1; i >= 0; i--)
        {
            // 检查是否仍在巡逻状态，否则退出协程
            if (!isPatrolling)
                yield break;

            // 安全检查：确保路径和索引有效
            if (path.Count == 0 || i < 0 || i >= path.Count)
                yield break;
            Sequence s = DOTween.Sequence();

            float time = path[i].GetComponent<Walkable>().isStair ? 1.5f : 1;

            //s.Append(transform.DOMove(path[i].GetComponent<Walkable>().GetWalkPoint(), time * 2f).SetEase(Ease.Linear));
            UpdateCurrentCube();
            parent = currentCube.parent;
            transform.parent = parent;
            Vector3 targetLocalPos = parent.InverseTransformPoint(path[i].GetComponent<Walkable>().GetWalkPoint());
            s.Append(transform.DOLocalMove(targetLocalPos, time * .8f).SetEase(Ease.Linear).SetRelative(false)).OnUpdate(() =>
            {
                if (path==null|| i < 0 || i >= path.Count) return;
                //Debug.Log(transform+" "+path.Count);
                if (!path[i].GetComponent<Walkable>().dontRotate)
                {

                    if (i == path.Count - 1) dir = path[i].position - currentCube.position;
                    else dir = path[i].position - path[i + 1].position;

                    if (dir.sqrMagnitude > 0.0001f)
                    {

                        // 1. 获取 Path[i] 的 up (Y) 和 forward (Z) 方向
                        Vector3 up = path[i].up;
                        Vector3 forward = path[i].forward;
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
                            dir = projectedDir - proj;
                            //Debug.Log($"最终方向向量 = {dir}");

                        }
                        Quaternion targetRot = Quaternion.LookRotation(dir, path[i].up);

                        // 然后平滑过渡到它（局部旋转）
                        transform.DORotateQuaternion(targetRot, .2f);

                    }

                }
            });
            // 等待移动完成
            yield return s.WaitForCompletion();

        }

        // 路径完成后切换巡逻点（仅在仍处于巡逻状态时）
        if (isPatrolling)
        {
            SwitchPatrolPoint();
            FindNewPath();
        }

        // 协程结束，清空引用
        moveCoroutine = null;
    }

    // 切换巡逻点
    private void SwitchPatrolPoint()
    {
        currentPatrolIndex = (currentPatrolIndex + 1) % 2;
    }
    
    // 更新当前方块
    public void UpdateCurrentCube()
    {
        //Debug.Log("在调用敌人检测");
        Transform hitCube = RayCastDown(transform);
        if (hitCube != null)
        {
            currentCube = hitCube;
        }
        else
        {
            Debug.Log("敌人"+gameObject+"脚下方块为空"+transform.position);
        }
    }
   
    // 射线检测方法
    public Transform RayCastDown(Transform player)
    {
        if (player == null) return null;

        Vector3 origin = player.GetChild(0).position;
        Vector3 direction = -player.up;

        // 球形射线检测
        RaycastHit[] hits = Physics.SphereCastAll(origin, sphereRadius, direction, rayLength);

        // 遍历检测结果
        foreach (var hit in hits)
        {
            //Debug.Log(gameObject+" "+hit.transform.tag+" "+hit.transform);
            if (hit.transform.CompareTag(targetTag) &&
                hit.transform.GetComponent<Walkable>() != null)
            {
                return hit.transform;
            }
        }

        return null;
    }

    // 在Scene视图绘制Gizmo
    private void OnDrawGizmos()
    {
        // 绘制射线起点和球形范围
        if (transform == null) return;

        Vector3 origin = transform.GetChild(0) != null ?
            transform.GetChild(0).position : transform.position;
        Vector3 direction = -transform.up;

        // 设置Gizmo颜色
        Gizmos.color = gizmoColor;

        // 绘制球形起点（可视化射线半径）
        Gizmos.DrawWireSphere(origin, sphereRadius);

        // 绘制射线方向（长度为rayLength）
        Vector3 endPoint = origin + direction * rayLength;
        Gizmos.DrawLine(origin, endPoint);

        // 绘制射线末端的球形范围（表示射线终点的检测范围）
        Gizmos.DrawWireSphere(endPoint, sphereRadius);
    }

}
