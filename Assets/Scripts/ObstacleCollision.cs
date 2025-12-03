using UnityEngine;

public class ObstacleCollision : MonoBehaviour
{
    [Header("游戏失败设置")]
    public string gameOverMessage = "你撞到了障碍物！";
    public KeyCode restartKey = KeyCode.R;

    [Header("调试")]
    public bool showDebugInfo = true;
    public bool enableCollision = true;

    private bool isGameOver = false;
    private Vector3 startPosition;
    private PlayerController playerController;
    private CharacterController characterController;
    private float gameStartTime;

    void Start()
    {
        gameStartTime = Time.time;
        startPosition = transform.position;
        playerController = GetComponent<PlayerController>();
        characterController = GetComponent<CharacterController>();

        if (showDebugInfo)
        {
            Debug.Log($"玩家起始位置: {startPosition}");
            Debug.Log($"玩家碰撞体: {(characterController != null ? "存在" : "不存在")}");
            Debug.Log($"玩家控制器: {(playerController != null ? "存在" : "不存在")}");
        }
    }

    void Update()
    {
        if (isGameOver && Input.GetKeyDown(restartKey))
        {
            RestartGame();
        }

        if (!isGameOver && enableCollision && characterController != null)
        {
            SimpleForwardCheck();
        }
    }

    void SimpleForwardCheck()
    {
        if (!showDebugInfo) return;

        // 向前发射射线检测障碍物
        Vector3 rayOrigin = transform.position + Vector3.up * 0.5f;
        float rayDistance = characterController.radius + 0.5f;

        if (Physics.Raycast(rayOrigin, transform.forward, out RaycastHit hit, rayDistance))
        {
            if (hit.collider.CompareTag("Obstacle") || hit.collider.CompareTag("Trap"))
            {
                Debug.Log($"射线检测到前方障碍物: {hit.collider.name}, 距离: {hit.distance:F2}, 标签: {hit.collider.tag}");
            }
        }
    }

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (!enableCollision || isGameOver) return;

        if (Time.time - gameStartTime < 0.3f) return;

        if (hit.gameObject.CompareTag("Obstacle") || hit.gameObject.CompareTag("Trap"))
        {
            if (showDebugInfo)
            {
                Debug.Log($"控制器碰撞检测到: {hit.gameObject.name}, 标签: {hit.gameObject.tag}");
            }
            TriggerGameOver();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!enableCollision || isGameOver) return;

        if (Time.time - gameStartTime < 0.3f)
        {
            if (showDebugInfo)
            {
                Debug.Log($"忽略初始触发器: {other.name}");
            }
            return;
        }

        if (other.CompareTag("Obstacle") || other.CompareTag("Trap"))
        {
            if (showDebugInfo)
            {
                Debug.Log($"触发器进入检测到: {other.name}, 标签: {other.tag}");
                Debug.Log($"玩家位置: {transform.position}, 陷阱位置: {other.transform.position}");
                Debug.Log($"距离: {Vector3.Distance(transform.position, other.transform.position):F2}");
            }
            TriggerGameOver();
        }
    }

    // 公共方法，供陷阱调用
    public void TriggerGameOver()
    {
        if (isGameOver) return;

        isGameOver = true;

        if (showDebugInfo)
        {
            Debug.Log($"GAME OVER - {gameOverMessage} (游戏时间: {Time.time - gameStartTime:F2}秒)");
        }

        Time.timeScale = 0;

        if (playerController != null)
        {
            playerController.SetMovementEnabled(false);
        }

        WatcherAI watcher = FindObjectOfType<WatcherAI>();
        if (watcher != null)
        {
            watcher.OnPlayerLookedAt(true);
        }

        Debug.Log("=== GAME OVER ===");
        Debug.Log("按 R 键重新开始游戏");
    }

    public void RestartGame()
    {
        Debug.Log("重新开始游戏...");

        isGameOver = false;
        Time.timeScale = 1;
        gameStartTime = Time.time;

        transform.position = startPosition;
        transform.rotation = Quaternion.identity;

        if (playerController != null)
        {
            playerController.SetMovementEnabled(true);
        }

        WatcherAI watcher = FindObjectOfType<WatcherAI>();
        if (watcher != null)
        {
            watcher.OnPlayerLookedAt(false);

            var restartMethod = watcher.GetType().GetMethod("RestartGame");
            if (restartMethod != null)
            {
                restartMethod.Invoke(watcher, null);
            }
        }

        Debug.Log("游戏已重新开始");
    }

    void OnGUI()
    {
        if (isGameOver)
        {
            GUI.color = new Color(0, 0, 0, 0.7f);
            GUI.Box(new Rect(0, 0, Screen.width, Screen.height), "");
            GUI.color = Color.white;

            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.alignment = TextAnchor.MiddleCenter;
            style.normal.textColor = Color.white;

            style.fontSize = 30;
            style.fontStyle = FontStyle.Bold;
            GUI.Label(new Rect(0, Screen.height / 2 - 60, Screen.width, 50), "游戏结束", style);

            style.fontSize = 20;
            style.fontStyle = FontStyle.Normal;
            GUI.Label(new Rect(0, Screen.height / 2 - 10, Screen.width, 40), gameOverMessage, style);

            style.fontSize = 16;
            style.normal.textColor = Color.yellow;
            GUI.Label(new Rect(0, Screen.height / 2 + 30, Screen.width, 40), "按 R 键重新开始", style);
        }
    }

    void OnDrawGizmos()
    {
        if (!showDebugInfo || characterController == null) return;

        // 绘制检测射线
        Gizmos.color = Color.blue;
        Vector3 rayOrigin = transform.position + Vector3.up * 0.5f;
        float rayDistance = characterController.radius + 0.5f;
        Gizmos.DrawLine(rayOrigin, rayOrigin + transform.forward * rayDistance);

        // 绘制Character Controller范围
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position + characterController.center, characterController.radius);

        if (Application.isPlaying)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(startPosition, new Vector3(1, 2, 1));
        }
    }
}