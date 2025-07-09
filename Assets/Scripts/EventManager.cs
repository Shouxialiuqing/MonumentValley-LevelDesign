using System;
using UnityEngine;

public static class EventManager
{
    // 定义场景跳转事件
    public static event Action OnSceneTransitionTriggered;

    // 触发场景跳转事件的方法
    public static void TriggerSceneTransition()
    {
        OnSceneTransitionTriggered?.Invoke();
    }
}