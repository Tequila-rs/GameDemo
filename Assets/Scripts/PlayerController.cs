using System.Collections;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float runSpeed = 6.0f;
    public float rotationAngle = 90f;
    public float sideMoveSpeed = 4.0f;
    public float jumpForce = 8f;
    public float gravity = -25f;

    [Header("Animation Settings")]
    public float animationSmoothTime = 0.1f;
    public float lookBackRotationSpeed = 3f;

    [Header("First Person Settings")]
    public bool isFirstPerson = false;
    public Vector3 firstPersonCameraOffset = new Vector3(0, 1.8f, 0.05f); // Y值从1.65f提高到1.8f
    public float cameraSmoothness = 8f;

    [Header("Look Back Cooldown Settings")]
    public float lookBackCooldown = 8f; // 冷却时间
    public int maxLookBackCharges = 2; // 最大储存次数
    private int currentLookBackCharges = 2; // 当前剩余次数
    private bool isOnCooldown = false; // 是否在冷却中
    private float currentCooldownTime = 0f; // 当前冷却剩余时间

    // 组件引用
    private CharacterController controller;
    private Animator animator;
    private WatcherAI watcher;

    // 移动状态变量
    private bool isLookingBack = false;
    private bool canTurn = true;
    private bool isMovementEnabled = true;
    private Vector3 initialForward;
    private Vector3 initialRight;

    // 物理相关变量
    private Vector3 moveDirection;
    private float velocityY;
    private bool isGrounded;

    // 动画相关变量
    private float currentSpeed;
    private int speedParamHash;
    private int groundedParamHash;
    private int jumpParamHash;
    private int lookBackParamHash;

    // 摄像机相关
    private Camera mainCamera;
    private Vector3 originalCameraPosition;
    private Quaternion originalCameraRotation;
    private Transform originalCameraParent;

    // 回头看相关变量
    private Quaternion targetLookBackRotation;
    private Quaternion originalRotation;

    // 第一人称相关
    private SkinnedMeshRenderer playerMeshRenderer;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        mainCamera = Camera.main;
        watcher = FindObjectOfType<WatcherAI>();

        initialForward = transform.forward;
        initialRight = transform.right;

        // 保存摄像机的原始父物体和位置
        originalCameraParent = mainCamera.transform.parent;
        originalCameraPosition = mainCamera.transform.localPosition;
        originalCameraRotation = mainCamera.transform.localRotation;

        speedParamHash = Animator.StringToHash("Speed");
        groundedParamHash = Animator.StringToHash("IsGrounded");
        jumpParamHash = Animator.StringToHash("Jump");
        lookBackParamHash = Animator.StringToHash("LookBack");

        // 获取玩家模型渲染器
        playerMeshRenderer = GetComponentInChildren<SkinnedMeshRenderer>();

        // 初始化回头次数
        currentLookBackCharges = maxLookBackCharges;

        if (controller == null)
            Debug.LogError("CharacterController component is missing!");
        if (animator == null)
            Debug.LogWarning("Animator component is missing! Animation will not work.");
    }

    void Update()
    {
        HandleGroundCheck();
        HandleMovement();
        HandleLookBack();
        HandleTurning();
        HandleJump();
        HandleFirstPersonToggle();
        UpdateAnimations();
        HandleLookBackRotation();
        UpdateCooldown();

        // 更新第一人称摄像机位置
        UpdateFirstPersonCamera();
    }

    void UpdateCooldown()
    {
        if (isOnCooldown)
        {
            currentCooldownTime -= Time.deltaTime;

            if (currentCooldownTime <= 0)
            {
                // 冷却完成，增加一次回头机会
                currentLookBackCharges++;
                isOnCooldown = false;
                currentCooldownTime = 0f;

                // 如果次数未满，继续冷却
                if (currentLookBackCharges < maxLookBackCharges)
                {
                    StartCooldown();
                }
            }
        }
    }

    void StartCooldown()
    {
        isOnCooldown = true;
        currentCooldownTime = lookBackCooldown;
    }

    void UpdateFirstPersonCamera()
    {
        if (isFirstPerson)
        {
            // 计算目标位置和旋转
            Vector3 targetPosition = transform.position + transform.TransformDirection(firstPersonCameraOffset);
            Quaternion targetRotation = transform.rotation;

            // 平滑移动摄像机
            mainCamera.transform.position = Vector3.Lerp(mainCamera.transform.position, targetPosition, cameraSmoothness * Time.deltaTime);
            mainCamera.transform.rotation = Quaternion.Lerp(mainCamera.transform.rotation, targetRotation, cameraSmoothness * Time.deltaTime);
        }
    }

    void HandleGroundCheck()
    {
        isGrounded = controller.isGrounded;

        if (isGrounded && velocityY < 0)
        {
            velocityY = -2f;
        }
    }

    void HandleMovement()
    {
        if (!isMovementEnabled)
        {
            ApplyGravity();
            return;
        }

        Vector3 forwardMove = transform.forward * runSpeed;

        float horizontalInput = 0f;
        if (Input.GetKey(KeyCode.Q))
        {
            horizontalInput = -1f;
        }
        else if (Input.GetKey(KeyCode.E))
        {
            horizontalInput = 1f;
        }

        Vector3 sideMovement = initialRight * horizontalInput * sideMoveSpeed;

        moveDirection = forwardMove + sideMovement;
        moveDirection.y = 0;

        ApplyGravity();

        Vector3 finalMove = moveDirection + Vector3.up * velocityY;
        controller.Move(finalMove * Time.deltaTime);

        Vector3 horizontalVelocity = new Vector3(controller.velocity.x, 0, controller.velocity.z);
        currentSpeed = horizontalVelocity.magnitude;
    }

    void ApplyGravity()
    {
        velocityY += gravity * Time.deltaTime;
    }

    void HandleJump()
    {
        if (!isMovementEnabled) return;

        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocityY = Mathf.Sqrt(jumpForce * -2f * gravity);
            if (animator != null)
                animator.SetTrigger(jumpParamHash);
        }
    }

    void HandleTurning()
    {
        if (isLookingBack) return;

        if (canTurn)
        {
            if (Input.GetKeyDown(KeyCode.A))
            {
                StartCoroutine(TurnCoroutine(-rotationAngle));
            }
            else if (Input.GetKeyDown(KeyCode.D))
            {
                StartCoroutine(TurnCoroutine(rotationAngle));
            }
        }
    }

    IEnumerator TurnCoroutine(float angle)
    {
        canTurn = false;

        transform.Rotate(0, angle, 0);

        Quaternion rotation = Quaternion.Euler(0, angle, 0);
        initialForward = rotation * initialForward;
        initialRight = rotation * initialRight;

        yield return new WaitForSeconds(0.2f);
        canTurn = true;
    }

    void HandleLookBack()
    {
        // 移除第一人称禁用的限制，让空格键在两个视角下都能回头看
        if (Input.GetKeyDown(KeyCode.Space) && !isLookingBack && currentLookBackCharges > 0)
        {
            StartLookBack();
        }
        if (Input.GetKeyUp(KeyCode.Space) && isLookingBack)
        {
            StopLookBack();
        }
    }

    void StartLookBack()
    {
        isLookingBack = true;
        isMovementEnabled = false;

        // 消耗一次回头机会
        currentLookBackCharges--;

        // 只要用一次回头，就立马进入冷却cd
        if (!isOnCooldown)
        {
            StartCooldown();
        }

        // 保存原始旋转
        originalRotation = transform.rotation;

        // 计算目标旋转（转身180度）
        targetLookBackRotation = transform.rotation * Quaternion.Euler(0, 180f, 0);

        // 设置动画参数
        if (animator != null)
        {
            animator.SetBool(lookBackParamHash, true);
            animator.SetFloat(speedParamHash, 0f);
        }

        // 旋转摄像机看身后（只在第三人称时）
        if (mainCamera != null && !isFirstPerson)
        {
            mainCamera.transform.RotateAround(transform.position, Vector3.up, 180f);
        }

        // 通知Watcher停止
        if (watcher != null)
        {
            watcher.OnPlayerLookedAt(true);
        }

        Debug.Log($"Looking back - Movement stopped, Watcher stopped. Charges left: {currentLookBackCharges}");
    }

    void StopLookBack()
    {
        isLookingBack = false;
        isMovementEnabled = true;

        // 恢复原始旋转
        transform.rotation = originalRotation;

        // 取消回头看动画状态
        if (animator != null)
            animator.SetBool(lookBackParamHash, false);

        // 恢复摄像机角度（只在第三人称时）
        if (mainCamera != null && !isFirstPerson)
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

    void HandleLookBackRotation()
    {
        if (isLookingBack)
        {
            // 平滑旋转角色模型（两个视角都适用）
            transform.rotation = Quaternion.Lerp(transform.rotation, targetLookBackRotation, lookBackRotationSpeed * Time.deltaTime);

            // 确保速度参数为0，防止奔跑动画播放
            if (animator != null)
            {
                animator.SetFloat(speedParamHash, 0f);
            }
        }
    }

    void HandleFirstPersonToggle()
    {
        if (Input.GetKeyDown(KeyCode.V))
        {
            ToggleFirstPerson();
        }
    }

    void ToggleFirstPerson()
    {
        isFirstPerson = !isFirstPerson;

        if (isFirstPerson)
        {
            EnterFirstPerson();
        }
        else
        {
            ExitFirstPerson();
        }
    }

    void EnterFirstPerson()
    {
        // 隐藏玩家模型
        if (playerMeshRenderer != null)
            playerMeshRenderer.enabled = false;

        // 第一人称时如果正在回头看，则停止回头看
        if (isLookingBack)
        {
            StopLookBack();
        }

        // 第一人称：将摄像机从玩家层级中独立出来
        mainCamera.transform.SetParent(null);

        Debug.Log("切换到第一人称视角");
    }

    void ExitFirstPerson()
    {
        // 显示玩家模型
        if (playerMeshRenderer != null)
            playerMeshRenderer.enabled = true;

        // 第三人称：恢复摄像机的原始父物体和位置
        mainCamera.transform.SetParent(originalCameraParent);
        mainCamera.transform.localPosition = originalCameraPosition;
        mainCamera.transform.localRotation = originalCameraRotation;

        Debug.Log("切换回第三人称视角");
    }

    void UpdateAnimations()
    {
        if (animator == null) return;

        // 只有在不回头看时才更新速度参数
        if (!isLookingBack)
        {
            float smoothedSpeed = Mathf.Lerp(animator.GetFloat(speedParamHash), currentSpeed, animationSmoothTime);
            animator.SetFloat(speedParamHash, smoothedSpeed);
        }

        animator.SetBool(groundedParamHash, isGrounded);
    }

    // 显示回头冷却状态UI
    void OnGUI()
    {
        // 显示回头次数（字体放大）
        GUIStyle largeStyle = new GUIStyle(GUI.skin.label);
        largeStyle.fontSize = 24;
        largeStyle.fontStyle = FontStyle.Bold;

        // 根据剩余次数设置颜色
        Color textColor = currentLookBackCharges > 0 ? Color.green : Color.red;
        largeStyle.normal.textColor = textColor;

        GUI.Label(new Rect(10, 100, 400, 30), $"回头次数: {currentLookBackCharges}/{maxLookBackCharges}", largeStyle);

        // 显示冷却时间（字体放大）
        if (isOnCooldown)
        {
            // 显示进度条
            float progress = 1 - (currentCooldownTime / lookBackCooldown);
            Rect progressBarBg = new Rect(10, 140, 200, 25);
            Rect progressBarFill = new Rect(10, 140, 200 * progress, 25);

            // 绘制进度条背景
            GUI.backgroundColor = Color.gray;
            GUI.Box(progressBarBg, GUIContent.none);

            // 绘制进度条填充
            GUI.backgroundColor = Color.blue;
            GUI.Box(progressBarFill, GUIContent.none);

            // 显示进度文本
            largeStyle.normal.textColor = Color.white;
            GUI.Label(new Rect(10, 170, 400, 30), $"冷却中: {currentCooldownTime:F1}s ({progress * 100:F0}%)", largeStyle);
        }
        else
        {
            largeStyle.normal.textColor = Color.green;
            GUI.Label(new Rect(10, 140, 400, 30), "冷却时间: 可用", largeStyle);
        }

        // 添加生命值显示
        GUIStyle healthStyle = new GUIStyle(GUI.skin.label);
        healthStyle.fontSize = 24;
        healthStyle.fontStyle = FontStyle.Bold;

        // 显示生命值（临时，正式使用UI血条后可删除）
        /*PlayerHealth playerHealth = GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            float healthPercent = playerHealth.GetHealthPercentage();
            healthStyle.normal.textColor = healthPercent > 0.5f ? Color.green :
                                         healthPercent > 0.3f ? Color.yellow : Color.red;

            GUI.Label(new Rect(Screen.width - 250, 100, 240, 30),
                     $"生命值: {playerHealth.currentHealth:F0}/{playerHealth.maxHealth:F0}",
                     healthStyle);
        }*/
    }

    public void SetMovementEnabled(bool enabled)
    {
        isMovementEnabled = enabled;
    }

    public bool IsLookingBack()
    {
        return isLookingBack;
    }

    // 调试用：在Scene视图中显示第一人称摄像机位置
    void OnDrawGizmos()
    {
        if (Application.isPlaying && isFirstPerson)
        {
            Gizmos.color = Color.red;
            Vector3 cameraPos = transform.position + transform.TransformDirection(firstPersonCameraOffset);
            Gizmos.DrawWireSphere(cameraPos, 0.1f);
            Gizmos.DrawLine(transform.position, cameraPos);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (controller != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position + controller.center, controller.radius);
        }
    }
}