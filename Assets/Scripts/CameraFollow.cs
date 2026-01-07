using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("第一人称设置")]
    public Vector3 firstPersonOffset = new Vector3(0f, 1.7f, 1.1f); // 第一人称摄像机位置
    public float firstPersonFOV = 75f; // 第一人称视野
    public float smoothSpeed = 8f;

    private Transform player;
    private Camera cam;
    private float originalFOV;

    void Start()
    {
        // 自动查找玩家
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
            }
        }

        // 获取摄像机组件
        cam = GetComponent<Camera>();
        if (cam != null)
        {
            originalFOV = cam.fieldOfView;
        }

        // 立即设置为第一人称视角
        SetFirstPersonView();
    }

    void LateUpdate()
    {
        if (player == null) return;

        // 计算目标位置和旋转
        CalculateFirstPersonTransform();

        // 平滑移动到目标位置和旋转
        transform.position = Vector3.Lerp(transform.position, targetPosition, smoothSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, smoothSpeed * Time.deltaTime);

        // 处理FOV变化
        HandleFOV();
    }

    private Vector3 targetPosition;
    private Quaternion targetRotation;

    void CalculateFirstPersonTransform()
    {
        // 第一人称模式：摄像机在玩家头部位置
        targetPosition = player.TransformPoint(firstPersonOffset);
        targetRotation = player.rotation;
    }

    void HandleFOV()
    {
        if (cam != null)
        {
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, firstPersonFOV, smoothSpeed * Time.deltaTime);
        }
    }

    // 设置为第一人称视角
    public void SetFirstPersonView()
    {
        if (cam != null)
        {
            cam.fieldOfView = firstPersonFOV;
        }
    }

    // 获取玩家引用
    public void SetPlayer(Transform playerTransform)
    {
        player = playerTransform;
    }
}