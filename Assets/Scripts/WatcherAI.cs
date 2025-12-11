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

    private Transform player;
    private float currentSpeed;
    private bool isHalted = false;
    private Vector3 startPosition;

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
        }
        else
        {
            currentSpeed = baseSpeed;
            Debug.Log("Watcher CHASING");
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other == null || other.CompareTag("Player"))
        {
            Debug.Log("GAME OVER - You were caught by the Watcher!");
            Time.timeScale = 0;
            Debug.Log("=== GAME OVER ===");
            Debug.Log("Press R to restart");
        }
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
        GUI.Label(new Rect(10, 70, 300, 20), "Controls: Auto Run | A/D: Turn | SPACE: Look Back");
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
    }
}