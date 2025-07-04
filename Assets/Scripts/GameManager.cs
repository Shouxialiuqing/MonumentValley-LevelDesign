using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    public PlayerController player;

    [Tooltip("定义规则旋转角度：当旋转到规定的角度时控制路径连接")]
    public List<PathCondition> pathConditions = new List<PathCondition>();//当物体旋转到正确的角度时，路径启用
    
    [Tooltip("每次输入按键时旋转轴的旋转情况")]
    public List<PivotData> pivots = new List<PivotData>();//旋转轴心类

    [Tooltip("根据条件需要隐藏的物体")]
    public Transform[] objectsToHide;
   
    private void Awake()
    {
        instance = this;
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
                if (pc.conditions[i].conditionObject.eulerAngles == pc.conditions[i].eulerAngle)
                {
                    count++;
                }
            }
            foreach(SinglePath sp in pc.paths)
                sp.block.possiblePaths[sp.index].active = (count == pc.conditions.Count);//根据轴心有没有旋转到理想的位置来判断物体路径是否激活
        }

        if (player.walking)//玩家如果在移动，就不处理输入
            return;

        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.LeftArrow))
        {
            int multiplier = Input.GetKey(KeyCode.RightArrow) ? 1 : -1;

            foreach (PivotData pivot in pivots)
            {
                if (pivot.pivotTransform == null) continue;
                if(!pivot.isButtonControl)
                {
                    pivot.pivotTransform.DOComplete();
                    pivot.pivotTransform.DORotate(
                        pivot.rotationAxis * pivot.rotationAngle * multiplier,
                        pivot.rotationDuration,
                        RotateMode.WorldAxisAdd
                    ).SetEase(pivot.rotationEase);
                    PlayerController.instance.RayCastDown();//更新脚下方块的transform信息;
                }
               
            }
        }
            foreach (Transform t in objectsToHide)
        {
            t.gameObject.SetActive(pivots[0].pivotTransform.eulerAngles.y > 45 && pivots[0].pivotTransform.eulerAngles.y < 90 + 45);
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().name);
        }//r键重新加载场景

    }

    public void RotateRightPivot()
    {
        pivots[1].pivotTransform.DOComplete();//快速完成当前未完成的动画
        pivots[1].pivotTransform.DORotate(new Vector3(0, 0, 90), .6f).SetEase(Ease.OutBack);//0.6秒内带回弹效果的动画选择
    }
}

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
    public Vector3 rotationAxis = Vector3.up;
    public float rotationAngle = 90f;
    public float rotationDuration = 0.6f;
    public Ease rotationEase = Ease.OutBack;
    public bool isButtonControl=false;
}