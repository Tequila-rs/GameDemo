using UnityEngine;

public class ZhiZhuZMOVE : MonoBehaviour
{
    [Header("前后巡逻设置")]
    [SerializeField] private float moveSpeed = 2f;           // 移动速度
    [SerializeField] private float zPatrolDistance = 2f;     // Z轴巡逻距离（从起点到前后各多远）
    [SerializeField] private float waitTime = 1f;            // 等待时间（秒）
    [SerializeField] private float turnSpeed = 5f;           // 转身速度

    [Header("动画设置")]
    [SerializeField] private string moveAnimParameter = "MoveSpeed"; // 动画参数名

    // 私有变量
    private Vector3 startPosition;      // 起始位置
    private bool movingForward = true;  // 当前是否向前移动（Z轴正方向）
    private bool isWaiting = false;     // 是否在等待状态
    private float waitTimer = 0f;       // 等待计时器
    private Animator animator;          // 动画控制器

    void Start()
    {
        // 记录起始位置
        startPosition = transform.position;

        // 获取动画组件
        animator = GetComponentInChildren<Animator>();

        // 设置初始朝向
        SetInitialFacingDirection();
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
            HandleZMOVING();

            // 检查是否到达Z轴边界
            CheckZBoundary();
        }
    }

    void HandleZMOVING()
    {
        // 计算Z轴移动方向（向前为1，向后为-1）
        float zDirection = movingForward ? 1f : -1f;
        Vector3 zMovement = new Vector3(0, 0, zDirection); // 只在Z轴移动

        // 沿Z轴移动蜘蛛
        transform.Translate(zMovement * moveSpeed * Time.deltaTime, Space.World);

        // 更新动画
        UpdateZAnimation();
    }

    void HandleWaiting()
    {
        // 更新等待计时器
        waitTimer += Time.deltaTime;

        // 停止动画（等待时不动）
        StopZAnimation();

        // 等待时间结束
        if (waitTimer >= waitTime)
        {
            isWaiting = false;
            movingForward = !movingForward; // 改变Z轴方向

            // 转身180度（因为前后移动需要调头）
            RotateToZDirection();
        }
    }

    void CheckZBoundary()
    {
        float currentZ = transform.position.z; // 获取当前Z轴位置

        if (movingForward)
        {
            // 向前移动时检查前边界（Z轴正方向）
            if (currentZ >= startPosition.z + zPatrolDistance)
            {
                StartZWaiting();
            }
        }
        else
        {
            // 向后移动时检查后边界（Z轴负方向）
            if (currentZ <= startPosition.z - zPatrolDistance)
            {
                StartZWaiting();
            }
        }
    }

    void StartZWaiting()
    {
        isWaiting = true;
        waitTimer = 0f;
    }

    void SetInitialFacingDirection()
    {
        // 设置初始朝向，面向Z轴正方向（前）
        if (movingForward)
        {
            transform.rotation = Quaternion.Euler(0, 0, 0); // 面向前方
        }
        else
        {
            transform.rotation = Quaternion.Euler(0, 180, 0); // 转向后方
        }
    }

    void RotateToZDirection()
    {
        // 平滑转向Z轴移动方向（前后移动需要180度转身）
        float targetYRotation = movingForward ? 0f : 180f;
        Quaternion targetRotation = Quaternion.Euler(0, targetYRotation, 0);

        // 使用平滑旋转
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
    }

    void UpdateZAnimation()
    {
        if (animator != null)
        {
            // 设置移动速度参数
            animator.SetFloat(moveAnimParameter, moveSpeed);
        }
    }

    void StopZAnimation()
    {
        if (animator != null)
        {
            animator.SetFloat(moveAnimParameter, 0f);
        }
    }

    // 可视化调试（在Scene视图中显示Z轴巡逻范围）
    void OnDrawGizmosSelected()
    {
        // 只在编辑器中选择对象时显示
        if (!Application.isPlaying)
            startPosition = transform.position;

        // 绘制Z轴巡逻范围
        DrawZPatrolRange();

        // 绘制边界标记
        DrawZBoundaryMarkers();

        // 绘制当前移动方向
        DrawZMovementDirection();
    }

    void DrawZPatrolRange()
    {
        Gizmos.color = Color.blue; // 使用蓝色表示Z轴移动
        Vector3 frontBound = startPosition + Vector3.forward * zPatrolDistance; // 前边界（Z正方向）
        Vector3 backBound = startPosition + Vector3.back * zPatrolDistance;     // 后边界（Z负方向）

        // 画Z轴巡逻范围的线（前后方向）
        Gizmos.DrawLine(backBound, frontBound);

        // 添加辅助线显示方向
        Gizmos.color = new Color(0.3f, 0.3f, 1f, 0.5f); // 半透明蓝色
        Gizmos.DrawLine(backBound + Vector3.left, frontBound + Vector3.left);
        Gizmos.DrawLine(backBound + Vector3.right, frontBound + Vector3.right);
    }

    void DrawZBoundaryMarkers()
    {
        Gizmos.color = Color.white; // 使用白色边界标记
        Vector3 frontBound = startPosition + Vector3.forward * zPatrolDistance;
        Vector3 backBound = startPosition + Vector3.back * zPatrolDistance;

        // 画边界标记（长方体显示方向）
        Gizmos.DrawWireCube(frontBound, new Vector3(0.5f, 0.5f, 0.1f));
        Gizmos.DrawWireCube(backBound, new Vector3(0.5f, 0.5f, 0.1f));

        // 添加箭头指示方向
        DrawArrow(frontBound, Vector3.forward * 0.5f, Color.green);
        DrawArrow(backBound, Vector3.back * 0.5f, Color.red);
    }

    void DrawZMovementDirection()
    {
        if (Application.isPlaying)
        {
            Gizmos.color = movingForward ? Color.green : Color.red;
            Vector3 direction = movingForward ? Vector3.forward : Vector3.back;

            // 从蜘蛛位置绘制方向箭头
            DrawArrow(transform.position, direction * 1.5f, Gizmos.color);

            // 添加移动路径预览
            Gizmos.color = new Color(Gizmos.color.r, Gizmos.color.g, Gizmos.color.b, 0.3f);
            Vector3 targetPos = movingForward ?
                startPosition + Vector3.forward * zPatrolDistance :
                startPosition + Vector3.back * zPatrolDistance;
            Gizmos.DrawLine(transform.position, targetPos);
        }
    }

    // 绘制箭头辅助函数
    void DrawArrow(Vector3 position, Vector3 direction, Color color)
    {
        Gizmos.color = color;
        Gizmos.DrawRay(position, direction);

        // 箭头头部
        Vector3 right = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 30, 0) * Vector3.back * 0.25f;
        Vector3 left = Quaternion.LookRotation(direction) * Quaternion.Euler(0, -30, 0) * Vector3.back * 0.25f;

        Gizmos.DrawRay(position + direction, right);
        Gizmos.DrawRay(position + direction, left);
    }

    // 公共方法：用于外部控制
    public void SetZPatrolDistance(float newDistance)
    {
        zPatrolDistance = newDistance;
        startPosition = transform.position; // 以当前位置为新起点
    }

    public void SetZMoveSpeed(float newSpeed)
    {
        moveSpeed = newSpeed;
    }

    public void PauseZMovement()
    {
        isWaiting = true;
        StopZAnimation();
    }

    public void ResumeZMovement()
    {
        isWaiting = false;
        waitTimer = 0f;
    }

    public void ReverseZDirection()
    {
        movingForward = !movingForward;
        RotateToZDirection();
    }

    public bool IsMovingForward()
    {
        return movingForward;
    }

    public float GetCurrentZPosition()
    {
        return transform.position.z;
    }

    public float GetZPatrolProgress()
    {
        // 返回巡逻进度（0到1之间）
        float currentZ = transform.position.z;
        float totalRange = zPatrolDistance * 2;
        float progress = (currentZ - (startPosition.z - zPatrolDistance)) / totalRange;
        return Mathf.Clamp01(progress);
    }
}