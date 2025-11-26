using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WatcherAI : MonoBehaviour
{
    [Header("Chase Settings")]
    public float baseSpeed = 5.0f;
    public float accelerationRate = 0.5f;
    public float maxSpeed = 10.0f;
    public float minDistance = 2f; // 新增：捕捉距离

    private Transform player;
    private float currentSpeed;
    private bool isHalted = false;
    private Vector3 startPosition;
    private CharacterController controller; // 新增：使用CharacterController

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        currentSpeed = baseSpeed;
        startPosition = transform.position;

        // 新增：获取或添加CharacterController
        controller = GetComponent<CharacterController>();
        if (controller == null)
        {
            controller = gameObject.AddComponent<CharacterController>();
            controller.center = new Vector3(0, 1, 0);
            controller.height = 2;
        }

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

        CheckCatchPlayer(); // 新增：检查是否抓到玩家
    }

    void ChasePlayer()
    {
        // 始终面向玩家
        transform.LookAt(player.position);

        // 修改：使用CharacterController移动
        Vector3 direction = (player.position - transform.position).normalized;
        Vector3 moveDirection = direction * currentSpeed * Time.deltaTime;
        controller.Move(moveDirection);
    }

    void Accelerate()
    {
        // 随时间加速，但有上限
        currentSpeed += accelerationRate * Time.deltaTime;
        currentSpeed = Mathf.Min(currentSpeed, maxSpeed);
    }

    // 新增：距离检测抓取
    void CheckCatchPlayer()
    {
        if (player == null) return;

        float distance = Vector3.Distance(transform.position, player.position);
        if (distance <= minDistance)
        {
            OnTriggerEnter(null); // 触发游戏结束
        }
    }

    public void OnPlayerLookedAt(bool lookedAt)
    {
        isHalted = lookedAt;

        if (lookedAt)
        {
            // 被注视时立即停止并重置速度
            currentSpeed = 0f; // 修改：速度设为0，完全停止
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
        // 修改：支持距离检测和碰撞检测
        if (other == null || other.CompareTag("Player"))
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
        GUI.Label(new Rect(10, 50, 300, 20), "Controls: Auto Run | A/D: Turn | SPACE: Look Back");
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
            player.rotation = Quaternion.identity; // 重置旋转
        }
    }
}