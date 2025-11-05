using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WatcherAI : MonoBehaviour
{
    [Header("Chase Settings")]
    public float baseSpeed = 5.0f;
    public float accelerationRate = 0.5f;
    public float maxSpeed = 10.0f;

    private Transform player;
    private float currentSpeed;
    private bool isHalted = false;
    private Vector3 startPosition;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        currentSpeed = baseSpeed;
        startPosition = transform.position;

        if (player == null)
        {
            Debug.LogError("Player not found! Make sure Player has 'Player' tag.");
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
    }

    void ChasePlayer()
    {
        // 始终面向玩家
        transform.LookAt(player.position);

        // 向玩家方向移动
        Vector3 direction = (player.position - transform.position).normalized;
        transform.position += direction * currentSpeed * Time.deltaTime;
    }

    void Accelerate()
    {
        // 随时间加速，但有上限
        currentSpeed += accelerationRate * Time.deltaTime;
        currentSpeed = Mathf.Min(currentSpeed, maxSpeed);
    }

    public void OnPlayerLookedAt(bool lookedAt)
    {
        isHalted = lookedAt;

        if (lookedAt)
        {
            // 被注视时立即停止并重置速度
            currentSpeed = baseSpeed;
            Debug.Log("Watcher HALTED");
        }
        else
        {
            // 恢复注视时从基础速度开始
            currentSpeed = baseSpeed;
            Debug.Log("Watcher CHASING");
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("GAME OVER - You were caught by the Watcher!");

            // 简单游戏结束处理
            Time.timeScale = 0; // 暂停游戏

            // 在Console和屏幕上显示游戏结束信息
            Debug.Log("=== GAME OVER ===");
            Debug.Log("Press R to restart");
        }
    }

    // 调试显示
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

        // 显示状态信息
        GUI.Label(new Rect(10, 10, 300, 20), $"Watcher State: {(isHalted ? "STOPPED" : "CHASING")}");
        GUI.Label(new Rect(10, 30, 300, 20), $"Watcher Speed: {currentSpeed:F1}");
        GUI.Label(new Rect(10, 50, 300, 20), "Controls: Auto Run | SPACE: Look Back");
    }

    void RestartGame()
    {
        Time.timeScale = 1;
        // 重置位置
        transform.position = startPosition;
        currentSpeed = baseSpeed;
        isHalted = false;

        // 重置玩家位置
        if (player != null)
        {
            player.position = new Vector3(0, 1, 0);
        }
    }
}