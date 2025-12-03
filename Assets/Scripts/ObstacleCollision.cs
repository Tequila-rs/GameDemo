using UnityEngine;

public class ObstacleCollision : MonoBehaviour
{
    [Header("游戏失败设置")]
    public string gameOverMessage = "你撞到了障碍物！";
    public KeyCode restartKey = KeyCode.R;

    private bool isGameOver = false;
    private Vector3 startPosition;
    private PlayerController playerController;
    private WatcherAI watcher;

    void Start()
    {
        startPosition = transform.position;
        playerController = GetComponent<PlayerController>();

        // 查找Watcher
        GameObject watcherObj = GameObject.FindGameObjectWithTag("Watcher");
        if (watcherObj != null)
        {
            watcher = watcherObj.GetComponent<WatcherAI>();
        }
    }

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        // 检查是否碰撞到障碍物
        if (hit.gameObject.CompareTag("Obstacle") && !isGameOver)
        {
            TriggerGameOver();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // 处理触发器碰撞（如果需要）
        if (other.CompareTag("Obstacle") && !isGameOver)
        {
            TriggerGameOver();
        }
    }

    void TriggerGameOver()
    {
        isGameOver = true;

        Debug.Log("GAME OVER - " + gameOverMessage);

        // 暂停游戏
        Time.timeScale = 0;

        // 禁用玩家移动
        if (playerController != null)
        {
            playerController.SetMovementEnabled(false);
        }

        // 停止Watcher
        if (watcher != null)
        {
            watcher.OnPlayerLookedAt(true);
        }

        Debug.Log("=== GAME OVER ===");
        Debug.Log("你撞到了障碍物！");
        Debug.Log("按 R 键重新开始游戏");
    }

    void RestartGame()
    {
        isGameOver = false;
        Time.timeScale = 1;

        // 重置玩家位置
        transform.position = startPosition;
        transform.rotation = Quaternion.identity;

        // 重新启用玩家移动
        if (playerController != null)
        {
            playerController.SetMovementEnabled(true);
        }

        // 重置Watcher
        if (watcher != null)
        {
            watcher.OnPlayerLookedAt(false);

            // 调用Watcher的RestartGame方法（如果存在）
            var restartMethod = watcher.GetType().GetMethod("RestartGame");
            if (restartMethod != null)
            {
                restartMethod.Invoke(watcher, null);
            }
        }

        Debug.Log("游戏重新开始");
    }

    void OnGUI()
    {
        if (isGameOver)
        {
            // 显示游戏结束界面
            GUIStyle gameOverStyle = new GUIStyle(GUI.skin.label);
            gameOverStyle.alignment = TextAnchor.MiddleCenter;
            gameOverStyle.fontSize = 24;
            gameOverStyle.normal.textColor = Color.red;

            GUIStyle messageStyle = new GUIStyle(GUI.skin.label);
            messageStyle.alignment = TextAnchor.MiddleCenter;
            messageStyle.fontSize = 16;
            messageStyle.normal.textColor = Color.white;

            // 半透明背景
            GUI.Box(new Rect(0, 0, Screen.width, Screen.height), "");

            // 游戏结束文字
            GUI.Label(new Rect(0, Screen.height / 2 - 60, Screen.width, 40), "游戏结束", gameOverStyle);
            GUI.Label(new Rect(0, Screen.height / 2 - 20, Screen.width, 40), gameOverMessage, messageStyle);
            GUI.Label(new Rect(0, Screen.height / 2 + 20, Screen.width, 40), "按 R 键重新开始", messageStyle);

            // 检测重新开始按键
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == restartKey)
            {
                RestartGame();
            }
        }
    }

    // 调试用：在Scene视图中显示起始位置
    void OnDrawGizmosSelected()
    {
        if (Application.isPlaying)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(startPosition, new Vector3(1, 2, 1));
            Gizmos.DrawIcon(startPosition, "PlayerStart");
        }
    }
}