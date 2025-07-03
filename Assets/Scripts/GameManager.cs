using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    public PlayerController player;//玩家
    public List<PathCondition> pathConditions = new List<PathCondition>();//当物体旋转到正确的角度时，路径启用
    public List<Transform> pivots;//旋转轴心

    public Transform[] objectsToHide;//根据条件需要隐藏的物体

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
            pivots[0].DOComplete();
            pivots[0].DORotate(new Vector3(0, 90 * multiplier, 0), .6f, RotateMode.WorldAxisAdd).SetEase(Ease.OutBack);
        }//根据输入左右箭头判断移动方向

        foreach(Transform t in objectsToHide)
        {
            t.gameObject.SetActive(pivots[0].eulerAngles.y > 45 && pivots[0].eulerAngles.y < 90 + 45);
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().name);
        }//r键重新加载场景

    }

    public void RotateRightPivot()
    {
        pivots[1].DOComplete();//快速完成当前未完成的动画
        pivots[1].DORotate(new Vector3(0, 0, 90), .6f).SetEase(Ease.OutBack);//0.6秒内带回弹效果的动画选择
    }
}

[System.Serializable]
public class PathCondition
{
    public string pathConditionName;
    public List<Condition> conditions;//理想旋转状态
    public List<SinglePath> paths;//要连接的路径
}
[System.Serializable]
public class Condition
{
    public Transform conditionObject;
    public Vector3 eulerAngle;

}
[System.Serializable]
public class SinglePath
{
    public Walkable block;
    public int index;
}
