using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float runSpeed = 6.0f;

    private CharacterController controller;
    private bool isLookingBack = false;
    private WatcherAI watcher;

    // 摄像机相关
    private Camera mainCamera;
    private Vector3 originalCameraPosition;
    private Quaternion originalCameraRotation;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        mainCamera = Camera.main;
        watcher = FindObjectOfType<WatcherAI>();

        // 保存摄像机原始位置
        originalCameraPosition = mainCamera.transform.localPosition;
        originalCameraRotation = mainCamera.transform.localRotation;
    }

    void Update()
    {
        HandleMovement();
        HandleLookBack();
    }

    void HandleMovement()
    {
        // 只有不回头时才能向前跑
        if (!isLookingBack)
        {
            Vector3 moveDirection = Vector3.forward * runSpeed;
            // 转换为世界空间方向
            moveDirection = transform.TransformDirection(moveDirection);
            controller.SimpleMove(moveDirection);
        }
    }

    void HandleLookBack()
    {
        // 按下空格键开始回头看
        if (Input.GetKeyDown(KeyCode.Space) && !isLookingBack)
        {
            StartLookBack();
        }
        // 松开空格键转回前方
        if (Input.GetKeyUp(KeyCode.Space) && isLookingBack)
        {
            StopLookBack();
        }
    }

    void StartLookBack()
    {
        isLookingBack = true;

        // 旋转摄像机看身后
        if (mainCamera != null)
        {
            mainCamera.transform.RotateAround(transform.position, Vector3.up, 180f);
        }

        // 通知Watcher停止
        if (watcher != null)
        {
            watcher.OnPlayerLookedAt(true);
        }

        Debug.Log("Looking back - Watcher should stop");
    }

    void StopLookBack()
    {
        isLookingBack = false;

        // 恢复摄像机角度
        if (mainCamera != null)
        {
            mainCamera.transform.localPosition = originalCameraPosition;
            mainCamera.transform.localRotation = originalCameraRotation;
        }

        // 通知Watcher继续追击
        if (watcher != null)
        {
            watcher.OnPlayerLookedAt(false);
        }

        Debug.Log("Looking forward - Watcher should chase");
    }
}
