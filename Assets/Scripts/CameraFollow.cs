using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("跟随设置")]
    public Transform player;
    public Transform watcher;
    public Vector3 normalOffset = new Vector3(0f, 4f, -6f); // 正常第三人称偏移
    public Vector3 lookBackOffset = new Vector3(0f, 3f, 6f); // 回头看时的第三人称偏移（在玩家前方）
    public float smoothSpeed = 8f;

    [Header("第一人称设置")]
    public Vector3 firstPersonOffset = new Vector3(0f, 1.7f, 0.3f); // 第一人称摄像机位置
    public Vector3 firstPersonLookBackOffset = new Vector3(0f, 1.7f, -0.3f); // 第一人称回头看位置
    public float firstPersonFOV = 75f; // 第一人称视野

    private bool isLookingBack = false;
    private bool isFirstPerson = false; // 视角模式
    private PlayerController playerController;
    private Camera cam;
    private float originalFOV;

    // 新增：用于稳定镜头的变量
    private Vector3 targetPosition;
    private Quaternion targetRotation;

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

        // 获取摄像机组件
        cam = GetComponent<Camera>();
        if (cam != null)
        {
            originalFOV = cam.fieldOfView;
        }
    }

    void Update()
    {
        // 处理视角切换
        HandleViewSwitch();
    }

    void LateUpdate()
    {
        if (player == null) return;

        // 计算目标位置和旋转
        CalculateTargetTransform();

        // 平滑移动到目标位置和旋转
        transform.position = Vector3.Lerp(transform.position, targetPosition, smoothSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, smoothSpeed * Time.deltaTime);

        // 处理FOV变化
        HandleFOV();
    }

    void CalculateTargetTransform()
    {
        if (isLookingBack)
        {
            // 回头看模式
            if (isFirstPerson)
            {
                // 第一人称回头看模式（暂时简单处理）
                CalculateFirstPersonLookBack();
            }
            else
            {
                // 第三人称回头看模式
                CalculateThirdPersonLookBack();
            }
        }
        else
        {
            // 正常模式
            if (isFirstPerson)
            {
                // 第一人称正常模式
                CalculateFirstPersonNormal();
            }
            else
            {
                // 第三人称正常模式
                CalculateThirdPersonNormal();
            }
        }
    }

    void CalculateThirdPersonNormal()
    {
        // 第三人称正常跟随
        targetPosition = player.TransformPoint(normalOffset);
        targetRotation = Quaternion.LookRotation(player.position - targetPosition);
    }

    void CalculateThirdPersonLookBack()
    {
        if (watcher == null)
        {
            // 如果没有Watcher，使用备用方案
            targetPosition = player.TransformPoint(lookBackOffset);
            targetRotation = Quaternion.LookRotation(player.position - targetPosition) * Quaternion.Euler(0, 180f, 0);
            return;
        }

        // 第三人称回头看：摄像机移动到玩家前方，看着Watcher
        targetPosition = player.TransformPoint(lookBackOffset);

        // 稳定地看着Watcher，使用固定位置而不是实时跟踪
        Vector3 lookDirection = watcher.position - targetPosition;
        targetRotation = Quaternion.LookRotation(lookDirection);
    }

    void CalculateFirstPersonNormal()
    {
        // 第一人称正常模式：摄像机在玩家头部位置
        targetPosition = player.TransformPoint(firstPersonOffset);
        targetRotation = player.rotation;
    }

    void CalculateFirstPersonLookBack()
    {
        // 第一人称回头看：暂时使用简单处理（后续修复）
        CalculateFirstPersonNormal();
    }

    void HandleFOV()
    {
        if (cam != null)
        {
            float targetFOV = isFirstPerson ? firstPersonFOV : originalFOV;
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFOV, smoothSpeed * Time.deltaTime);
        }
    }

    void HandleViewSwitch()
    {
        // 按下V键切换视角
        if (Input.GetKeyDown(KeyCode.V))
        {
            ToggleViewMode();
        }
    }

    void ToggleViewMode()
    {
        isFirstPerson = !isFirstPerson;
        Debug.Log($"Switched to {(isFirstPerson ? "First Person" : "Third Person")} View");
    }

    // 供PlayerController调用的方法
    public void SetLookingBack(bool lookingBack)
    {
        isLookingBack = lookingBack;
        // 保持当前视角模式，不自动切换
    }

    // 公开方法供其他脚本访问当前视角模式
    public bool IsFirstPerson()
    {
        return isFirstPerson;
    }

    public bool IsLookingBackMode()
    {
        return isLookingBack;
    }
}