using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float runSpeed = 6.0f;
    public float rotationAngle = 90f; // 每次旋转的角度
    public float sideMoveSpeed = 4.0f; // 新增：左右移动速度

    private CharacterController controller;
    private bool isLookingBack = false;
    private WatcherAI watcher;
    private bool canTurn = true; // 防止连续旋转
    private bool isMovementEnabled = true; // 控制移动是否启用
    private Vector3 initialForward; // 初始面向方向
    private Vector3 initialRight; // 初始右侧方向

    // 摄像机相关
    private Camera mainCamera;
    private Vector3 originalCameraPosition;
    private Quaternion originalCameraRotation;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        mainCamera = Camera.main;
        watcher = FindObjectOfType<WatcherAI>();

        // 保存初始方向
        initialForward = transform.forward;
        initialRight = transform.right;

        // 保存摄像机原始位置
        originalCameraPosition = mainCamera.transform.localPosition;
        originalCameraRotation = mainCamera.transform.localRotation;
    }

    void Update()
    {
        HandleMovement();
        HandleLookBack();
        HandleTurning(); // 处理转向
    }

    void HandleMovement()
    {
        if (!isMovementEnabled) return; // 如果移动被禁用，直接返回

        // 基础向前移动（使用当前面向方向）
        Vector3 moveDirection = transform.forward * runSpeed;

        // 新增：左右移动输入（使用初始方向为基准）
        float horizontalInput = 0f;
        if (Input.GetKey(KeyCode.LeftArrow))
        {
            horizontalInput = -1f;
        }
        else if (Input.GetKey(KeyCode.RightArrow))
        {
            horizontalInput = 1f;
        }

        // 左右移动（以初始方向为基准）
        Vector3 sideMovement = initialRight * horizontalInput * sideMoveSpeed;

        // 合并移动方向
        moveDirection += sideMovement;

        // 转换为世界空间方向
        controller.SimpleMove(moveDirection);
    }

    // 修改：离散的90度转向
    void HandleTurning()
    {
        if (isLookingBack) return; // 回头看时不能转向

        if (canTurn)
        {
            if (Input.GetKeyDown(KeyCode.A)) // 按下A键立即左转90度
            {
                StartCoroutine(TurnCoroutine(-rotationAngle));
            }
            else if (Input.GetKeyDown(KeyCode.D)) // 按下D键立即右转90度
            {
                StartCoroutine(TurnCoroutine(rotationAngle));
            }
        }
    }

    // 新增：转向协程，防止连续旋转
    IEnumerator TurnCoroutine(float angle)
    {
        canTurn = false;

        // 立即旋转90度
        transform.Rotate(0, angle, 0);

        // 更新初始方向
        Quaternion rotation = Quaternion.Euler(0, angle, 0);
        initialForward = rotation * initialForward;
        initialRight = rotation * initialRight;

        // 短暂延迟防止连续旋转
        yield return new WaitForSeconds(0.2f);
        canTurn = true;
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
        isMovementEnabled = false; // 禁用玩家移动

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

        Debug.Log("Looking back - Movement stopped, Watcher stopped");
    }

    void StopLookBack()
    {
        isLookingBack = false;
        isMovementEnabled = true; // 启用玩家移动

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

        Debug.Log("Looking forward - Movement resumed, Watcher chasing");
    }
}