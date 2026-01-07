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

    // 改为public以便其他脚本访问
    [HideInInspector] public Transform player;
    [HideInInspector] public float currentSpeed;
    [HideInInspector] public bool isHalted = false;

    private Vector3 startPosition;
    private WatcherFootsteps footstepsComponent; // 添加引用

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        currentSpeed = baseSpeed;
        startPosition = transform.position;

        // 直接设置正确的高度位置
        float groundY = -1.5f;
        float watcherHeight = 2.0f; // Transform Scale Y = 1.5
        float targetY = groundY + (watcherHeight * 0.5f); // 模型中心的高度

        // 调整位置
        Vector3 pos = transform.position;
        pos.y = targetY;
        transform.position = pos;

        if (player == null)
        {
            Debug.LogError("Player not found! Make sure Player has 'Player' tag.");
        }

        // 添加或获取脚步声组件
        footstepsComponent = GetComponent<WatcherFootsteps>();
        if (footstepsComponent == null)
        {
            footstepsComponent = gameObject.AddComponent<WatcherFootsteps>();
            Debug.Log("已添加WatcherFootsteps组件到Watcher");
        }

        // 如果还没有音频剪辑，尝试加载一个默认的
        if (footstepsComponent.proximitySound == null)
        {
            Debug.LogWarning("请为WatcherFootsteps组件添加Proximity Sound音频剪辑");
        }
    }

    void Update()
    {
        if (player == null) return;

        if (!isHalted)
        {
            ChasePlayer();
            Accelerate();
        }

        CheckCatchPlayer();

        // 强制保持在地面上
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

    void ChasePlayer()
    {
        // 始终面向玩家
        transform.LookAt(new Vector3(player.position.x, transform.position.y, player.position.z));

        // 直接使用Transform移动，不要任何物理组件
        Vector3 direction = (player.position - transform.position).normalized;
        direction.y = 0; // 确保不会向上/下移动

        transform.position += direction * currentSpeed * Time.deltaTime;
    }

    void Accelerate()
    {
        currentSpeed += accelerationRate * Time.deltaTime;
        currentSpeed = Mathf.Min(currentSpeed, maxSpeed);
    }

    void CheckCatchPlayer()
    {
        if (player == null) return;

        float distance = Vector3.Distance(transform.position, player.position);
        if (distance <= minDistance)
        {
            OnTriggerEnter(null);
        }
    }

    public void OnPlayerLookedAt(bool lookedAt)
    {
        isHalted = lookedAt;

        if (lookedAt)
        {
            currentSpeed = 0f;
            Debug.Log("Watcher HALTED");

            // 玩家回头时，立即停止声音
            if (footstepsComponent != null)
            {
                footstepsComponent.SetSoundEnabled(false);
                Debug.Log("玩家回头：Watcher声音已停止");
            }
        }
        else
        {
            currentSpeed = baseSpeed;
            Debug.Log("Watcher CHASING");

            // 玩家停止回头时，恢复声音
            if (footstepsComponent != null)
            {
                footstepsComponent.SetSoundEnabled(true);
                Debug.Log("玩家停止回头：Watcher声音已恢复");
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other == null || other.CompareTag("Player"))
        {
            TriggerGameOver();
        }
    }

    // 新增：游戏结束方法
    void TriggerGameOver()
    {
        Debug.Log("GAME OVER - You were caught by the Watcher!");
        Time.timeScale = 0;
        Debug.Log("=== GAME OVER ===");
        Debug.Log("Press R to restart");

        // 怪物追上玩家时，立即停止声音
        if (footstepsComponent != null)
        {
            footstepsComponent.StopImmediately(); // 使用立即停止方法
            Debug.Log("怪物追上玩家：Watcher声音已立即停止");
        }

        // 同时禁用Watcher的移动
        isHalted = true;
        currentSpeed = 0f;
    }

    void OnGUI()
    {
        if (Time.timeScale == 0)
        {
            GUI.Label(new Rect(Screen.width / 2 - 100, Screen.height / 2 - 25, 200, 50),
                     "GAME OVER\nYou were caught!\nPress R to restart");

            if (Input.GetKeyDown(KeyCode.R))
            {
                RestartGame();
            }
        }

        GUI.Label(new Rect(10, 10, 300, 20), $"Watcher State: {(isHalted ? "STOPPED" : "CHASING")}");
        GUI.Label(new Rect(10, 30, 300, 20), $"Watcher Speed: {currentSpeed:F1}");
        GUI.Label(new Rect(10, 50, 300, 20), $"Y Position: {transform.position.y:F2}");

        // 添加距离信息
        if (player != null)
        {
            float distance = Vector3.Distance(transform.position, player.position);
            GUI.Label(new Rect(10, 70, 300, 20), $"Distance to Player: {distance:F1}m");
        }

        // 添加声音状态信息
        if (footstepsComponent != null)
        {
            GUI.Label(new Rect(10, 90, 300, 20), $"Watcher Sound: {(footstepsComponent.isPlaying ? "ON" : "OFF")}");
        }

        GUI.Label(new Rect(10, 110, 300, 40), "Controls: Auto Run | A/D: Turn | SPACE: Look Back");
    }

    void RestartGame()
    {
        Time.timeScale = 1;

        // 重置位置到正确的高度
        float groundY = -1.5f;
        float watcherHeight = 2.0f;
        float targetY = groundY + (watcherHeight * 0.5f);

        Vector3 newPos = startPosition;
        newPos.y = targetY;
        transform.position = newPos;

        currentSpeed = baseSpeed;
        isHalted = false;

        if (player != null)
        {
            player.position = new Vector3(0, 1, 0);
            player.rotation = Quaternion.identity;
        }

        // 重启时重置声音
        if (footstepsComponent != null)
        {
            footstepsComponent.SetSoundEnabled(true);
        }
    }

    // 获取当前音量（供其他脚本使用）
    public float GetCurrentSoundVolume()
    {
        if (footstepsComponent != null)
        {
            // 使用反射获取当前音量
            System.Reflection.FieldInfo currentVolumeField = typeof(WatcherFootsteps).GetField(
                "currentVolume",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
            );

            if (currentVolumeField != null)
            {
                return (float)currentVolumeField.GetValue(footstepsComponent);
            }
        }
        return 0f;
    }
}