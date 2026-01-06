using UnityEngine;

public class ZhiZhuAutoMove : MonoBehaviour
{
    [Header("巡逻设置")]
    [SerializeField] private float moveSpeed = 2f;           // 移动速度
    [SerializeField] private float patrolDistance = 2f;      // 巡逻距离（从起点到左右各多远）
    [SerializeField] private float waitTime = 1f;            // 等待时间（秒）
    [SerializeField] private float turnSpeed = 5f;           // 转身速度

    [Header("动画设置")]
    [SerializeField] private string moveAnimParameter = "MoveSpeed"; // 动画参数名

    // 私有变量
    private Vector3 startPosition;      // 起始位置
    private bool movingRight = true;    // 当前是否向右移动
    private bool isWaiting = false;     // 是否在等待状态
    private float waitTimer = 0f;       // 等待计时器
    private Animator animator;          // 动画控制器

    void Start()
    {
        // 记录起始位置
        startPosition = transform.position;

        // 获取动画组件
        animator = GetComponentInChildren<Animator>();

        // 初始朝向右
        if (movingRight)
        {
            transform.rotation = Quaternion.Euler(0, 90, 0); // 面向右
        }
        else
        {
            transform.rotation = Quaternion.Euler(0, -90, 0); // 面向左
        }
    }

    void Update()
    {
        if (isWaiting)
        {
            // 等待状态处理
            HandleWaiting();
        }
        else
        {
            // 移动状态处理
            HandleMoving();

            // 检查是否到达边界
            CheckBoundary();
        }
    }

    void HandleMoving()
    {
        // 计算移动方向（向右为1，向左为-1）
        float direction = movingRight ? 1f : -1f;
        Vector3 movement = new Vector3(direction, 0, 0);

        // 移动蜘蛛
        transform.Translate(movement * moveSpeed * Time.deltaTime, Space.World);

        // 更新动画
        if (animator != null)
        {
            animator.SetFloat(moveAnimParameter, moveSpeed);
        }
    }

    void HandleWaiting()
    {
        // 更新等待计时器
        waitTimer += Time.deltaTime;

        // 停止动画（等待时不动）
        if (animator != null)
        {
            animator.SetFloat(moveAnimParameter, 0f);
        }

        // 等待时间结束
        if (waitTimer >= waitTime)
        {
            isWaiting = false;
            movingRight = !movingRight; // 改变方向

            // 转身
            RotateToDirection();
        }
    }

    void CheckBoundary()
    {
        float currentX = transform.position.x;

        if (movingRight)
        {
            // 向右移动时检查右边界
            if (currentX >= startPosition.x + patrolDistance)
            {
                StartWaiting();
            }
        }
        else
        {
            // 向左移动时检查左边界
            if (currentX <= startPosition.x - patrolDistance)
            {
                StartWaiting();
            }
        }
    }

    void StartWaiting()
    {
        isWaiting = true;
        waitTimer = 0f;
    }

    void RotateToDirection()
    {
        // 设置面向方向
        float targetYRotation = movingRight ? 90f : -90f;
        Quaternion targetRotation = Quaternion.Euler(0, targetYRotation, 0);
        transform.rotation = targetRotation;
    }

    // 可视化调试（在Scene视图中显示巡逻范围）
    void OnDrawGizmosSelected()
    {
        // 只在编辑器中选择对象时显示
        if (!Application.isPlaying)
            startPosition = transform.position;

        Gizmos.color = Color.green;
        Vector3 leftBound = startPosition + Vector3.left * patrolDistance;
        Vector3 rightBound = startPosition + Vector3.right * patrolDistance;

        // 画巡逻范围的线
        Gizmos.DrawLine(leftBound, rightBound);

        // 画边界标记
        Gizmos.color = Color.red;
        Gizmos.DrawCube(leftBound, new Vector3(0.2f, 1f, 0.2f));
        Gizmos.DrawCube(rightBound, new Vector3(0.2f, 1f, 0.2f));

        // 画当前巡逻方向
        if (Application.isPlaying)
        {
            Gizmos.color = movingRight ? Color.blue : Color.yellow;
            Vector3 direction = movingRight ? Vector3.right : Vector3.left;
            Gizmos.DrawRay(transform.position, direction * 2f);
        }
    }
}