using UnityEngine;

public class ObstacleCollision : MonoBehaviour
{
    [Header("碰撞触发设置")]
    public string gameOverMessage = "你被鬼抓住了！";
    public KeyCode restartKey = KeyCode.R;

    [Header("起点设置")]
    public Vector3 restartPosition = new Vector3(0, 1, -4.19f); // 根据你的Transform设置
    public Quaternion restartRotation = Quaternion.identity;

    [Header("调试")]
    public bool showDebugInfo = true;
    public bool enableCollision = true;

    private bool isGameOver = false;
    private CharacterController characterController;
    private PlayerController playerController;
    private float gameStartTime;

    void Start()
    {
        gameStartTime = Time.time;
        playerController = GetComponent<PlayerController>();
        characterController = GetComponent<CharacterController>();

        // 在Start时记录当前位置作为起点（只记录一次）
        if (!HasCustomRestartPosition())
        {
            restartPosition = transform.position;
            restartRotation = transform.rotation;
            Debug.Log($"自动记录起点位置: {restartPosition}");
        }

        if (showDebugInfo)
        {
            Debug.Log($"玩家起始位置: {restartPosition}");
            Debug.Log($"玩家旋转: {restartRotation.eulerAngles}");
            Debug.Log($"玩家控制器: {(characterController != null ? "存在" : "不存在")}");
            Debug.Log($"玩家控制脚本: {(playerController != null ? "存在" : "不存在")}");
        }
    }

    void Update()
    {
        // 统一处理R键重新开始（无论游戏是否结束）
        if (Input.GetKeyDown(restartKey))
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

        // 简单的向前射线检测碰撞
        Vector3 rayOrigin = transform.position + Vector3.up * 0.5f;
        float rayDistance = characterController.radius + 0.5f;

        if (Physics.Raycast(rayOrigin, transform.forward, out RaycastHit hit, rayDistance))
        {
            if (hit.collider.CompareTag("Obstacle") || hit.collider.CompareTag("Trap"))
            {
                Debug.Log($"射线检测到前方障碍: {hit.collider.name}, 距离: {hit.distance:F2}, 标签: {hit.collider.tag}");
            }
        }
    }

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (!enableCollision || isGameOver) return;
        if (Time.time - gameStartTime < 0.3f) return;

        if (hit.gameObject.CompareTag("Obstacle") || hit.gameObject.CompareTag("Trap"))
        {
            // 优先使用玩家的生命值系统
            PlayerHealth playerHealth = GetComponent<PlayerHealth>();
            if (playerHealth != null && playerHealth.IsAlive())
            {
                if (showDebugInfo)
                {
                    Debug.Log($"控制器碰撞: {hit.gameObject.name}, 受到30点伤害");
                }

                // 控制器碰撞受到伤害
                playerHealth.TakeDamage(30f);
            }
            else
            {
                // 备用：当没有生命值系统时使用
                if (showDebugInfo)
                {
                    Debug.Log($"控制器碰撞检测到: {hit.gameObject.name}，触发游戏结束");
                }
                TriggerGameOver();
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!enableCollision || isGameOver) return;

        if (Time.time - gameStartTime < 0.3f)
        {
            if (showDebugInfo)
            {
                Debug.Log($"游戏开始后跳过碰撞: {other.name}");
            }
            return;
        }

        if (other.CompareTag("Obstacle") || other.CompareTag("Trap"))
        {
            // 优先使用玩家的生命值系统
            PlayerHealth playerHealth = GetComponent<PlayerHealth>();
            if (playerHealth != null && playerHealth.IsAlive())
            {
                if (showDebugInfo)
                {
                    Debug.Log($"触发器进入: {other.name}，受到25点伤害");
                }

                // 触发器碰撞受到伤害
                playerHealth.TakeDamage(25f);
            }
            else
            {
                // 备用
                if (showDebugInfo)
                {
                    Debug.Log($"触发器进入检测到: {other.name}");
                }
                TriggerGameOver();
            }
        }
    }

    // 公开方法，确保其他脚本可以调用
    public void TriggerGameOver()
    {
        if (isGameOver) return;

        isGameOver = true;

        if (showDebugInfo)
        {
            Debug.Log($"GAME OVER - {gameOverMessage} (游戏时长: {Time.time - gameStartTime:F2}秒)");
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

        // 关键修复：先禁用CharacterController，再设置位置
        if (characterController != null)
        {
            characterController.enabled = false;
            Debug.Log("已禁用CharacterController");
        }

        // 重置玩家位置到预设的起点
        Vector3 targetPosition = restartPosition;
        Quaternion targetRotation = restartRotation;

        Debug.Log($"正在重置玩家到: {targetPosition}, 旋转: {targetRotation.eulerAngles}");

        transform.position = targetPosition;
        transform.rotation = targetRotation;

        // 重新启用CharacterController
        if (characterController != null)
        {
            characterController.enabled = true;
            Debug.Log("已启用CharacterController");
        }

        // 重置玩家生命值（如果存在）
        PlayerHealth playerHealth = GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.RestartGame();
        }

        // 重置玩家的回头次数和方向系统 - 使用公开方法而不是反射
        if (playerController != null)
        {
            // 重置回头次数
            ResetPlayerLookBackCharges();

            // 重置移动方向系统
            ResetPlayerDirectionSystem();

            // 重置回头状态
            ResetPlayerLookBackState();

            // 启用玩家移动
            playerController.SetMovementEnabled(true);
            Debug.Log("已启用玩家移动");
        }

        // 重置Watcher
        WatcherAI watcher = FindObjectOfType<WatcherAI>();
        if (watcher != null)
        {
            watcher.OnPlayerLookedAt(false);
            watcher.RestartGame();
            Debug.Log("已重置Watcher");
        }

        // 重新开始背景音乐
        if (BackgroundMusicManager.Instance != null)
        {
            BackgroundMusicManager.Instance.OnGameRestart();
            Debug.Log("已重新开始背景音乐");
        }

        Debug.Log($"玩家已成功重置到起点: {targetPosition}");

        // 确保玩家完全重置
        StartCoroutine(EnsurePlayerReset());
    }

    // 重置玩家回头次数
    private void ResetPlayerLookBackCharges()
    {
        // 通过反射获取并重置回头次数
        var lookbackField = typeof(PlayerController).GetField("currentLookBackCharges",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var maxLookbackField = typeof(PlayerController).GetField("maxLookBackCharges",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

        if (lookbackField != null && maxLookbackField != null)
        {
            int maxCharges = (int)maxLookbackField.GetValue(playerController);
            lookbackField.SetValue(playerController, maxCharges);
            Debug.Log($"重置回头次数: {maxCharges}/{maxCharges}");
        }
    }

    // 重置玩家方向系统
    private void ResetPlayerDirectionSystem()
    {
        // 重置初始方向向量
        var initialForwardField = typeof(PlayerController).GetField("initialForward",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var initialRightField = typeof(PlayerController).GetField("initialRight",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var canTurnField = typeof(PlayerController).GetField("canTurn",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (initialForwardField != null && initialRightField != null && canTurnField != null)
        {
            // 重置为当前transform的方向
            initialForwardField.SetValue(playerController, transform.forward);
            initialRightField.SetValue(playerController, transform.right);
            canTurnField.SetValue(playerController, true);

            Debug.Log($"重置方向系统: 前向={transform.forward}, 右向={transform.right}, 可转向=true");
        }
    }

    // 重置玩家回头状态
    private void ResetPlayerLookBackState()
    {
        // 重置回头相关状态
        var isLookingBackField = typeof(PlayerController).GetField("isLookingBack",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var targetLookBackRotationField = typeof(PlayerController).GetField("targetLookBackRotation",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var originalRotationField = typeof(PlayerController).GetField("originalRotation",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (isLookingBackField != null && targetLookBackRotationField != null && originalRotationField != null)
        {
            isLookingBackField.SetValue(playerController, false);
            targetLookBackRotationField.SetValue(playerController, transform.rotation);
            originalRotationField.SetValue(playerController, transform.rotation);

            Debug.Log($"重置回头状态: 不再回头, 目标旋转={transform.rotation.eulerAngles}");
        }
    }

    private System.Collections.IEnumerator EnsurePlayerReset()
    {
        yield return new WaitForSeconds(0.1f);

        // 再次检查位置
        Debug.Log($"重置后确认位置: {transform.position}, 期望位置: {restartPosition}");

        // 如果位置不对，强制修正
        if (Vector3.Distance(transform.position, restartPosition) > 0.5f)
        {
            Debug.LogWarning("玩家位置未正确重置，强制修正...");
            if (characterController != null) characterController.enabled = false;
            transform.position = restartPosition;
            transform.rotation = restartRotation;
            if (characterController != null) characterController.enabled = true;
        }

        // 额外：确保方向系统正确
        yield return new WaitForSeconds(0.1f);
        Debug.Log($"重置后方向: 前向={transform.forward}, 右向={transform.right}");
    }

    // 检查是否设置了自定义起点位置
    private bool HasCustomRestartPosition()
    {
        // 如果restartPosition不是默认值，说明在Inspector中设置了自定义值
        return restartPosition != new Vector3(0, 1, 0);
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
        if (!showDebugInfo) return;

        // 绘制射线检测
        if (characterController != null)
        {
            Gizmos.color = Color.blue;
            Vector3 rayOrigin = transform.position + Vector3.up * 0.5f;
            float rayDistance = characterController.radius + 0.5f;
            Gizmos.DrawLine(rayOrigin, rayOrigin + transform.forward * rayDistance);

            // 绘制Character Controller范围
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position + characterController.center, characterController.radius);
        }

        // 绘制重新开始位置
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(restartPosition, new Vector3(1, 2, 1));
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(restartPosition, restartPosition + Vector3.forward * 2);

        // 显示文本
#if UNITY_EDITOR
        UnityEditor.Handles.Label(restartPosition + Vector3.up * 2,
            $"重新开始位置\nX:{restartPosition.x:F2}\nY:{restartPosition.y:F2}\nZ:{restartPosition.z:F2}");
#endif
    }
}