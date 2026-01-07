using System.Collections;
using UnityEngine;

public class AutoDoor : MonoBehaviour
{
    [Header("门的核心配置")]
    public Animator doorAnimator; // 门的动画控制器（拖拽赋值）
    public string openParamName = "IsOpen"; // 动画控制器中控制开门的参数名（需和你的Animator参数一致）
    public float doorCloseDelay = 2f; // 玩家离开后，延迟几秒关门（避免频繁开关）

    [Header("调试用")]
    public bool isPlayerInRange = false; // 玩家是否在感应区
    private Coroutine closeDoorCoroutine; // 关门延迟的协程

    // 初始化：检查必要组件
    private void Awake()
    {
        if (doorAnimator == null)
        {
            doorAnimator = GetComponent<Animator>();
            if (doorAnimator == null)
            {
                Debug.LogError("门的Animator组件未赋值！", this);
            }
        }
    }

    // 玩家进入感应区 → 开门
    private void OnTriggerEnter(Collider other)
    {
        // 只检测标签为"Player"的物体（需确保玩家物体标签是Player）
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = true;

            // 停止正在执行的关门协程（如果有）
            if (closeDoorCoroutine != null)
            {
                StopCoroutine(closeDoorCoroutine);
                closeDoorCoroutine = null;
            }

            // 给Animator传参数，播放开门动画
            doorAnimator.SetBool(openParamName, true);
            Debug.Log("玩家靠近，门打开", this);
        }
    }

    // 玩家离开感应区 → 延迟关门
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = false;

            // 启动延迟关门协程
            closeDoorCoroutine = StartCoroutine(CloseDoorAfterDelay());
        }
    }

    // 延迟关门的协程
    private IEnumerator CloseDoorAfterDelay()
    {
        yield return new WaitForSeconds(doorCloseDelay);

        // 确认玩家确实不在范围内，再关门
        if (!isPlayerInRange)
        {
            doorAnimator.SetBool(openParamName, false);
            Debug.Log("玩家离开，门关闭", this);
        }
    }

    // 调试：在场景视图中绘制感应区（黄色线框）
    private void OnDrawGizmosSelected()
    {
        Collider triggerCollider = GetComponentInChildren<Collider>();
        if (triggerCollider != null && triggerCollider.isTrigger)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(triggerCollider.bounds.center, triggerCollider.bounds.size);
        }
    }
}