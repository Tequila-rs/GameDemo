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
        // 移除空格键检测，通过PlayerController获取状态
        if (playerController != null)
        {
            // 这里我们需要通过其他方式获取回头状态
            // 暂时保留原有逻辑，稍后优化
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
        // 修改：使用TransformPoint确保偏移相对于玩家方向
        Vector3 desiredPosition = player.TransformPoint(normalOffset);
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        transform.position = smoothedPosition;

        // 看着玩家
        transform.LookAt(player);
    }

    void HandleLookBackCamera()
    {
        if (watcher == null) return;

        // 修改：回头看时，摄像机在玩家前方
        Vector3 desiredPosition = player.TransformPoint(lookBackOffset);
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        transform.position = smoothedPosition;

        // 看着Watcher
        transform.LookAt(watcher);
    }

    // 新增：供PlayerController调用的方法
    public void SetLookingBack(bool lookingBack)
    {
        isLookingBack = lookingBack;
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