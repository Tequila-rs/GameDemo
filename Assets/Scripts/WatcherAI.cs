using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WatcherAI : MonoBehaviour
{
    [Header("Chase Settings")]
    public float baseSpeed = 5.0f;
    public float accelerationRate = 0.5f;
    public float maxSpeed = 10.0f;
    public float minDistance = 2f;
    [Header("Player Path Tracking")]
    public float pathRecordInterval = 0.2f; // 记录玩家路径的时间间隔
    public int maxPathPoints = 100; // 最大记录的路径点数量（避免内存溢出）
    public float pathFollowSmoothTime = 0.1f; // 跟随路径的平滑时间
    [Header("Pursuit Settings")]
    public float directPursuitRange = 10f; // 距离玩家足够近时直接追击，不沿路径

    private Transform player;
    private float currentSpeed;
    private bool isHalted = false;
    private Vector3 startPosition;
    private float turnSmoothVelocity;

    // 玩家路径相关
    private List<Vector3> playerPath = new List<Vector3>();
    private int currentPathIndex = 0;
    private float lastRecordTime;

    void Start()
    {
        // 找到玩家
        player = GameObject.FindGameObjectWithTag("Player").transform;
        currentSpeed = baseSpeed;
        startPosition = transform.position;

        // 初始化高度
        AdjustHeightPosition();

        if (player == null)
        {
            Debug.LogError("Player not found! Make sure Player has 'Player' tag.");
        }

        // 初始化路径记录时间
        lastRecordTime = Time.time;
    }

    void Update()
    {
        if (player == null) return;

        // 持续记录玩家路径
        RecordPlayerPath();

        if (!isHalted)
        {
            // 核心逻辑：距离近则直接追玩家，否则沿玩家路径追
            FollowPlayerPathOrDirect();
            Accelerate();
        }

        // 检查是否抓到玩家
        CheckCatchPlayer();
        // 维持高度
        MaintainHeight();
    }

    /// <summary>
    /// 记录玩家的移动路径
    /// </summary>
    void RecordPlayerPath()
    {
        // 按固定时间间隔记录，避免路径点过多
        if (Time.time - lastRecordTime >= pathRecordInterval)
        {
            Vector3 playerPos = player.position;
            playerPos.y = transform.position.y; // 统一高度，避免Y轴偏差

            // 避免记录重复位置（玩家静止时）
            if (playerPath.Count == 0 || Vector3.Distance(playerPath[playerPath.Count - 1], playerPos) > 0.1f)
            {
                playerPath.Add(playerPos);

                // 限制路径点数量，超出则移除最旧的
                if (playerPath.Count > maxPathPoints)
                {
                    playerPath.RemoveAt(0);
                }
            }

            lastRecordTime = Time.time;
        }
    }

    /// <summary>
    /// 核心追击逻辑：近距直接追玩家，远距沿玩家路径追
    /// </summary>
    void FollowPlayerPathOrDirect()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // 情况1：距离玩家足够近，直接朝向玩家追击
        if (distanceToPlayer <= directPursuitRange && playerPath.Count > 0)
        {
            DirectPursuitPlayer();
        }
        // 情况2：距离较远，沿玩家的路径追击
        else if (playerPath.Count > currentPathIndex)
        {
            FollowPlayerPath();
        }
    }

    /// <summary>
    /// 直接朝向玩家追击（近距离）
    /// </summary>
    void DirectPursuitPlayer()
    {
        Vector3 direction = (player.position - transform.position).normalized;
        direction.y = 0; // 忽略Y轴

        // 平滑转向玩家
        if (direction.magnitude >= 0.1f)
        {
            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle,
                                                ref turnSmoothVelocity, pathFollowSmoothTime);
            transform.rotation = Quaternion.Euler(0f, angle, 0f);
        }

        // 向玩家移动
        transform.position += transform.forward * currentSpeed * Time.deltaTime;

        // 调试绘制：直接追击的射线（红色）
        Debug.DrawRay(transform.position, transform.forward * 3f, Color.red);
        Debug.DrawLine(transform.position, player.position, Color.yellow);
    }

    /// <summary>
    /// 沿玩家的历史路径追击（远距离）
    /// </summary>
    void FollowPlayerPath()
    {
        Vector3 targetPos = playerPath[currentPathIndex];
        Vector3 direction = (targetPos - transform.position).normalized;
        direction.y = 0; // 忽略Y轴

        // 平滑转向目标路径点
        if (direction.magnitude >= 0.1f)
        {
            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle,
                                                ref turnSmoothVelocity, pathFollowSmoothTime);
            transform.rotation = Quaternion.Euler(0f, angle, 0f);
        }

        // 向路径点移动
        transform.position += transform.forward * currentSpeed * Time.deltaTime;

        // 检查是否到达当前路径点，到达则切换下一个
        float distanceToPathPoint = Vector3.Distance(transform.position, targetPos);
        if (distanceToPathPoint <= 0.5f)
        {
            currentPathIndex++;
            // 防止索引越界
            currentPathIndex = Mathf.Min(currentPathIndex, playerPath.Count - 1);
        }

        // 调试绘制：路径追击的射线（绿色）和路径线（蓝色）
        Debug.DrawRay(transform.position, transform.forward * 3f, Color.green);
        Debug.DrawLine(transform.position, targetPos, Color.blue);

        // 绘制玩家的完整路径
        for (int i = 0; i < playerPath.Count - 1; i++)
        {
            Debug.DrawLine(playerPath[i], playerPath[i + 1], Color.cyan);
        }
    }

    /// <summary>
    /// 调整Watcher的高度，防止Y轴偏移
    /// </summary>
    void AdjustHeightPosition()
    {
        float groundY = -1.5f;
        float watcherHeight = 2.0f;
        float targetY = groundY + (watcherHeight * 0.5f);

        Vector3 pos = transform.position;
        pos.y = targetY;
        transform.position = pos;
    }

    /// <summary>
    /// 加速逻辑（持续加速直到最大速度）
    /// </summary>
    void Accelerate()
    {
        currentSpeed += accelerationRate * Time.deltaTime;
        currentSpeed = Mathf.Min(currentSpeed, maxSpeed);
    }

    /// <summary>
    /// 维持高度，防止Watcher掉下去
    /// </summary>
    void MaintainHeight()
    {
        float groundY = -1.5f;
        float watcherHeight = 2.0f;
        float minY = groundY + (watcherHeight * 0.5f);

        if (transform.position.y < minY)
        {
            Vector3 pos = transform.position;
            pos.y = minY;
            transform.position = pos;
        }
    }

    /// <summary>
    /// 检查是否抓到玩家
    /// </summary>
    void CheckCatchPlayer()
    {
        if (player == null) return;

        float directDistance = Vector3.Distance(transform.position, player.position);

        // 距离足够近，且玩家在视野范围内则捕获
        if (directDistance <= minDistance * 1.5f)
        {
            Vector3 toPlayer = player.position - transform.position;
            float angle = Vector3.Angle(transform.forward, toPlayer);

            if (angle < 60f || directDistance <= minDistance)
            {
                OnCatchPlayer();
            }
        }
    }

    /// <summary>
    /// 抓到玩家后的游戏结束逻辑
    /// </summary>
    void OnCatchPlayer()
    {
        Debug.Log("GAME OVER - You were caught by the Watcher!");
        Time.timeScale = 0;
    }

    /// <summary>
    /// 被玩家注视时暂停，移开视线后恢复
    /// </summary>
    /// <param name="lookedAt">是否被玩家注视</param>
    public void OnPlayerLookedAt(bool lookedAt)
    {
        isHalted = lookedAt;

        if (lookedAt)
        {
            currentSpeed = 0f;
            Debug.Log("Watcher HALTED - Player is looking at it!");
        }
        else
        {
            currentSpeed = baseSpeed;
            Debug.Log("Watcher CHASING - Player looked away!");
        }
    }

    /// <summary>
    /// 碰撞检测（玩家直接撞到Watcher）
    /// </summary>
    void OnTriggerEnter(Collider other)
    {
        if (other != null && other.CompareTag("Player"))
        {
            OnCatchPlayer();
        }
    }

    /// <summary>
    /// GUI显示状态（调试用）
    /// </summary>
    void OnGUI()
    {
        if (Time.timeScale == 0)
        {
            GUIStyle gameOverStyle = new GUIStyle(GUI.skin.label);
            gameOverStyle.alignment = TextAnchor.MiddleCenter;
            gameOverStyle.fontSize = 20;
            gameOverStyle.fontStyle = FontStyle.Bold;
            gameOverStyle.normal.textColor = Color.red;

            GUI.Label(new Rect(Screen.width / 2 - 150, Screen.height / 2 - 60, 300, 120),
                     "GAME OVER\nYou were caught by the Watcher!\n\nPress R to restart", gameOverStyle);

            if (Input.GetKeyDown(KeyCode.R))
            {
                RestartGame();
            }
        }

        // 调试信息显示
        GUIStyle statusStyle = new GUIStyle(GUI.skin.label);
        statusStyle.normal.textColor = Color.white;
        statusStyle.fontSize = 14;

        GUI.Label(new Rect(10, 10, 400, 20), $"Watcher State: {(isHalted ? "STOPPED" : "CHASING")}", statusStyle);
        GUI.Label(new Rect(10, 35, 400, 20), $"Watcher Speed: {currentSpeed:F1}", statusStyle);
        GUI.Label(new Rect(10, 60, 400, 20), $"Player Path Points: {playerPath.Count}", statusStyle);
        GUI.Label(new Rect(10, 85, 400, 20), $"Distance to Player: {Vector3.Distance(transform.position, player.position):F1}", statusStyle);
        GUI.Label(new Rect(10, 110, 400, 20), $"Pursuit Mode: {(Vector3.Distance(transform.position, player.position) <= directPursuitRange ? "DIRECT" : "PATH FOLLOW")}", statusStyle);
    }

    /// <summary>
    /// 重启游戏
    /// </summary>
    void RestartGame()
    {
        Time.timeScale = 1;

        // 重置Watcher状态
        currentSpeed = baseSpeed;
        isHalted = false;
        currentPathIndex = 0;
        playerPath.Clear(); // 清空玩家路径记录

        // 重置Watcher位置
        Vector3 newPos = startPosition;
        AdjustHeightPosition();
        transform.position = newPos;
        transform.rotation = Quaternion.identity;

        // 重置玩家位置（在Watcher前方10个单位）
        if (player != null)
        {
            Vector3 playerStartPos = newPos;
            playerStartPos.z += 10f;
            playerStartPos.y = 1f;
            player.position = playerStartPos;
            player.rotation = Quaternion.identity;
        }

        Debug.Log("Game Restarted!");
    }

    /// <summary>
    /// Scene视图绘制调试Gizmos
    /// </summary>
    void OnDrawGizmos()
    {
        // 绘制玩家路径
        Gizmos.color = Color.cyan;
        for (int i = 0; i < playerPath.Count - 1; i++)
        {
            Gizmos.DrawLine(playerPath[i], playerPath[i + 1]);
        }

        // 绘制当前追击的路径点
        if (currentPathIndex < playerPath.Count)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(playerPath[currentPathIndex], 0.4f);
        }

        // 绘制Watcher的前进方向
        Gizmos.color = Color.yellow;
        Vector3 forwardPos = transform.position + transform.forward * 2f;
        Gizmos.DrawLine(transform.position, forwardPos);
        Gizmos.DrawLine(forwardPos, forwardPos + (transform.right * 0.3f - transform.forward * 0.3f));
        Gizmos.DrawLine(forwardPos, forwardPos + (-transform.right * 0.3f - transform.forward * 0.3f));

        // 绘制Watcher位置
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, 0.5f);

        // 绘制直接追击范围
        Gizmos.color = new Color(1, 0.5f, 0, 0.2f);
        Gizmos.DrawSphere(transform.position, directPursuitRange);
    }
}