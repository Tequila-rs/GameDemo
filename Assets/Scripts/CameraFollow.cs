using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("跟随设置")]
    public Transform player;
    public Transform watcher;
    public Vector3 normalOffset = new Vector3(0f, 4f, -6f); // 正常跟随的偏移
    public Vector3 lookBackOffset = new Vector3(0f, 3f, 3f); // 回头看时的偏移
    public float smoothSpeed = 8f;

    private bool isLookingBack = false;
    private PlayerController playerController;

    void Start()
    {
        // 自动查找玩家和Watcher
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
                playerController = playerObj.GetComponent<PlayerController>();
            }
        }

        if (watcher == null)
        {
            GameObject watcherObj = GameObject.Find("Watcher");
            if (watcherObj != null)
            {
                watcher = watcherObj.transform;
            }
        }
    }

    void Update()
    {
        // 检测空格键输入
        if (Input.GetKeyDown(KeyCode.Space) && !isLookingBack)
        {
            StartLookBack();
        }
        if (Input.GetKeyUp(KeyCode.Space) && isLookingBack)
        {
            StopLookBack();
        }
    }

    void LateUpdate()
    {
        if (player == null) return;

        if (isLookingBack)
        {
            // 回头看模式：摄像机在玩家前方，看着Watcher
            HandleLookBackCamera();
        }
        else
        {
            // 正常模式：摄像机在玩家后方，看着玩家
            HandleNormalCamera();
        }
    }

    void HandleNormalCamera()
    {
        // 计算正常跟随位置
        Vector3 desiredPosition = player.position + normalOffset;
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        transform.position = smoothedPosition;

        // 看着玩家
        transform.LookAt(player);
    }

    void HandleLookBackCamera()
    {
        if (watcher == null) return;

        // 回头看时，摄像机在玩家前方，看着Watcher方向
        Vector3 lookDirection = (watcher.position - player.position).normalized;
        Vector3 desiredPosition = player.position + lookBackOffset;

        // 平滑移动
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        transform.position = smoothedPosition;

        // 看着Watcher
        transform.LookAt(watcher);
    }

    void StartLookBack()
    {
        isLookingBack = true;

        // 通知Watcher停止
        if (watcher != null)
        {
            WatcherAI watcherAI = watcher.GetComponent<WatcherAI>();
            if (watcherAI != null)
            {
                watcherAI.OnPlayerLookedAt(true);
            }
        }

        Debug.Log("Looking back at Watcher");
    }

    void StopLookBack()
    {
        isLookingBack = false;

        // 通知Watcher继续追击
        if (watcher != null)
        {
            WatcherAI watcherAI = watcher.GetComponent<WatcherAI>();
            if (watcherAI != null)
            {
                watcherAI.OnPlayerLookedAt(false);
            }
        }

        Debug.Log("Looking forward, resuming chase");
    }
}