using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    public EnemyController targetEnemy;
    public PlayerController player;
    private Coroutine delayCoroutine=null;
    [Space]
    [Header("场景过渡设置")]
    [Tooltip("场景淡入淡出控制器")]
    public ScreenFader startFader; 
    public ScreenFader screenFader;
    [Tooltip("下一个场景的索引")]
    public int nextSceneIndex = -1;
    public GameObject pressAnyKeyText;//开场界面的提示文字

    [Space]
    [Header("按键输入的响应设置")]
    [Tooltip("定义规则旋转角度：当旋转到规定的角度时控制路径连接")]
    public List<PathCondition> pathConditions = new List<PathCondition>();//当物体旋转到正确的角度时，路径启用

    [Tooltip("定义高度条件：当物体移动到规定高度时控制路径连接")]
    public List<HeightCondition> heightConditions = new List<HeightCondition>();

    [Tooltip("每次输入按键时旋转轴的变化情况")]
    public List<PivotData> pivots = new List<PivotData>();//旋转轴心类数组


    [Space]
    [Header("按钮触发的旋转设置")]
    [Tooltip("要旋转的Pivot索引")]
    public int pivotIndex = 0;  // 可在编辑器中设置的pivot索引
    [Tooltip("按钮按下后旋转到的目标向量（局部坐标系）")]
    public Vector3 rotationVector = new Vector3(0, 0, 90);  // 直接设置旋转向量
    [Tooltip("旋转动画持续时间（秒）")]
    public float rotateDuration = 0.6f;  // 动画时长
    [Tooltip("动画缓动类型")]
    public Ease rotateEase = Ease.OutBack;  // 缓动效果

    [Space]
    [Header("高度控制设置")]
    [Tooltip("控制高度的Pivot索引")]
    public int heightPivotIndex = 0;  // 用于控制高度的轴心索引
    [Tooltip("每次上下键移动的高度增量")]
    public float heightStep = 0.5f;   // 每次抬高/降低的距离
    [Tooltip("高度动画持续时间")]
    public float heightDuration = 0.3f;  // 高度变化的动画时间
    [Tooltip("最高高度限制（世界坐标Y值）")]
    public float maxHeight = 3f;      // 最大抬高高度
    [Tooltip("最低高度限制（世界坐标Y值）")]
    public float minHeight = -3f;      // 最低降低高度

    [Space]
    [Tooltip("根据条件需要隐藏的物体")]
    public Transform[] objectsToHide;
    private void Awake()
    {
        instance = this;
    }
    private void OnEnable()
    {
        // 订阅事件
        EventManager.OnSceneTransitionTriggered += TransitionToNextScene;
    }

    private void OnDisable()
    {
        // 取消订阅事件
        EventManager.OnSceneTransitionTriggered -= TransitionToNextScene;
    }

    private void Start()
    {
        player.enabled = false;
        StartCoroutine(SceneStartFadeIn());
    }
    private IEnumerator SceneStartFadeIn()
    {
        //float time = 1f;
        //if (SceneManager.GetActiveScene().buildIndex == 0) time = 2f;
        //if (screenFader != null)
        //{
        //    StartCoroutine(screenFader.FadeIn(time));
        //    yield return new WaitForSeconds(time);
        //}
        // 如果是第一个场景（索引0）
        if (SceneManager.GetActiveScene().buildIndex == 0)
        {
            // 显示"按任意键继续"提示（如果有）
            if (pressAnyKeyText != null)
                pressAnyKeyText.SetActive(true);

            // 等待任意按键输入
            while (!Input.anyKeyDown)
            {
                yield return null;
            }

            // 隐藏提示（如果有）
            if (pressAnyKeyText != null)
                pressAnyKeyText.SetActive(false);

            // 开始场景image淡出
            if (startFader != null&&screenFader!=null)
            {
                StartCoroutine(startFader.FadeIn(1f)); // 假设FadeOut是淡出方法
                yield return new WaitForSeconds(1.5f); // 淡出后短暂等待
                startFader.gameObject.SetActive(false);
                StartCoroutine(screenFader.FadeIn(1f));
                yield return new WaitForSeconds(1f);
                player.enabled = true;
            }
        }
        else
        {
            if (screenFader != null)
            {
                StartCoroutine(screenFader.FadeIn(1f));
                yield return new WaitForSeconds(1f);
                player.enabled = true;
            }
        }
        
    }
    void Update()
    {
        foreach(PathCondition pc in pathConditions)
        {
           //旋转角度如果符合要求就激活路径
            int count = 0;
            //检查所有的轴心有没有旋转到理想的位置
            for (int i = 0; i < pc.conditions.Count; i++)
            {
                Vector3 angle1 = pc.conditions[i].conditionObject.eulerAngles;
                Vector3 angle2 = pc.conditions[i].eulerAngle;

                float xDiff = Mathf.DeltaAngle(angle1.x, angle2.x);
                float yDiff = Mathf.DeltaAngle(angle1.y, angle2.y);
                float zDiff = Mathf.DeltaAngle(angle1.z, angle2.z);

                if (Mathf.Abs(xDiff) < 0.1f && Mathf.Abs(yDiff) < 0.1f && Mathf.Abs(zDiff) < 0.1f)
                {
                    count++;
                }
                
            }
            foreach(SinglePath sp in pc.paths)
                sp.block.possiblePaths[sp.index].active = (count == pc.conditions.Count);//根据轴心有没有旋转到理想的位置来判断物体路径是否激活
        }

        // 新增的高度条件检查
        foreach (HeightCondition hc in heightConditions)
        {
            int count = 0;
            for (int i = 0; i < hc.conditions.Count; i++)
            {
                float currentHeight = hc.conditions[i].conditionObject.position.y;
                if (Mathf.Abs(currentHeight - hc.conditions[i].targetHeight) < 0.1f)
                {
                    count++;
                }
            }
            foreach (SinglePath sp in hc.paths)
                sp.block.possiblePaths[sp.index].active = (count == hc.conditions.Count);
        }

        if (player.walking)//玩家如果在移动，就不处理输入
            return;

        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.LeftArrow))
        {
            AudioManager.Instance.PlayOneShot("Rotate");
            int multiplier = Input.GetKey(KeyCode.RightArrow) ? 1 : -1;

            foreach (PivotData pivot in pivots)
            {
                if (pivot.pivotTransform == null) continue;
                if(!pivot.dontRotate)
                {
                    pivot.pivotTransform.DOComplete();
                    pivot.pivotTransform.DORotate(
                        pivot.rotationAxis * pivot.rotationAngle * multiplier,
                        pivot.rotationDuration,
                        RotateMode.WorldAxisAdd
                    ).SetEase(Ease.OutBack);
                    PlayerController.instance.UpdateCurrentCube();//更新脚下方块的transform信息;
                }
            }
        }
        // 上下键高度控制
        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.DownArrow))
        {
            AudioManager.Instance.PlayOneShot("Rotate");
            // 安全检查
            if (!(pivots == null || pivots.Count <= heightPivotIndex))
            {
                var targetPivot = pivots[heightPivotIndex].pivotTransform;
                float currentY = targetPivot.position.y;
                float targetY = Input.GetKey(KeyCode.UpArrow) ?
                               currentY + heightStep :  // 上键抬高
                               currentY - heightStep;   // 下键降低

                // 限制在最大和最小高度之间
                targetY = Mathf.Clamp(targetY, minHeight, maxHeight);

                // 如果目标高度与当前高度有差异才执行动画
                if (Mathf.Abs(targetY - currentY) > 0.01f)
                {
                    targetPivot.DOComplete();
                    // 只修改Y轴位置，保持X和Z轴不变
                    Vector3 targetPos = new Vector3(
                        targetPivot.position.x,
                        targetY,
                        targetPivot.position.z
                    );
                    targetPivot.DOMove(targetPos, heightDuration).SetEase(Ease.OutQuad);
                    PlayerController.instance.UpdateCurrentCube();
                    //敌人重新寻路
                   if(targetEnemy!=null&&delayCoroutine==null)  delayCoroutine=StartCoroutine(DelayedRestartPatrol());
                }
            }
        }
           
        foreach (Transform t in objectsToHide)
        {
            t.gameObject.SetActive(pivots[1].pivotTransform.eulerAngles.y > 45 && pivots[1].pivotTransform.eulerAngles.y < 90 + 45);
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().name);
        }//r键重新加载场景

    }
    private IEnumerator DelayedRestartPatrol()
    {
        targetEnemy.StopPatrol();
        Debug.Log("停止寻路");
        
        // 3. 等待0.5秒
        yield return new WaitForSeconds(.8f);
        Debug.Log("等完了.6s");
        targetEnemy.StartPatrol();
        delayCoroutine = null;
    }

    //public void RotateRightPivot()
    //{
    //    pivots[0].pivotTransform.DOComplete();//快速完成当前未完成的动画
    //    pivots[0].pivotTransform.DORotate(new Vector3(0, 0, 90), .6f).SetEase(Ease.OutBack);//0.6秒内带回弹效果的动画选择
    //}

    public void RotatePivot()
    {
        // 安全检查
        if (pivots == null || pivots.Count <= pivotIndex || pivots[pivotIndex].pivotTransform == null)
        {
            //Debug.LogWarning($"Pivot索引 {pivotIndex} 无效或未设置pivotTransform");
            return;
        }

        // 停止当前动画并开始新的旋转动画
        var targetPivot = pivots[pivotIndex].pivotTransform;
        targetPivot.DOComplete();
        targetPivot.DORotate(rotationVector, rotateDuration)
                   .SetEase(rotateEase);
    }
    
    public void TransitionToNextScene()
    {
        nextSceneIndex = SceneManager.GetActiveScene().buildIndex+1;
        StartCoroutine(FadeOutAndLoadScene(nextSceneIndex));      
    }

    private IEnumerator FadeOutAndLoadScene(int sceneIndex)
    {
        if (screenFader != null)
        {
            StartCoroutine(screenFader.FadeOut(1f));
            yield return new WaitForSeconds(1.5f);
        }
        if (sceneIndex < 3)
        {
            AsyncOperation asyncOp = SceneManager.LoadSceneAsync(sceneIndex);
            asyncOp.allowSceneActivation = false;

            // 等待加载进度达到90%
            while (asyncOp.progress < 0.9f)
            {
                yield return null;
            }

            asyncOp.allowSceneActivation = true;

            // 等待场景完全激活
            while (!asyncOp.isDone)
            {
                yield return null;
            }
        }
        else Application.Quit();//游戏结束
    }

}
//普通条件类:判断旋转
[System.Serializable]
public class PathCondition
{
    public string pathConditionName;
    public List<Condition> conditions;//目标旋转情况集合
    public List<SinglePath> paths;//要连接的路径集合
}
[System.Serializable]
public class Condition//目标的旋转情况
{
    public Transform conditionObject;
    public Vector3 eulerAngle;

}

// 新增的高度条件类
[System.Serializable]
public class HeightCondition
{
    public string conditionName;
    public List<HeightConditionData> conditions;
    public List<SinglePath> paths;
}

[System.Serializable]
public class HeightConditionData
{
    public Transform conditionObject;
    public float targetHeight;
}
[System.Serializable]
public class SinglePath//每个walkable中想要动态设置是否激活的索引
{
    public Walkable block;
    public int index;
}

[System.Serializable]
public class PivotData//旋转轴数据类，储存每次按下键后的旋转情况
{
    public Transform pivotTransform;
    [Tooltip("旋转向量:绕哪个向量旋转")]
    public Vector3 rotationAxis = Vector3.up;
    [Tooltip("旋转角度:每次按键输入旋转多少度")]
    public float rotationAngle = 90f;
    [Tooltip("旋转时长:每次旋转需要多久")]
    public float rotationDuration = 0.6f;
    [Tooltip("是否由按钮控制旋转")]
    public bool dontRotate=false;
}
