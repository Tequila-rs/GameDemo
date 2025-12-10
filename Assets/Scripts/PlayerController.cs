using System.Collections;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    // ===== 新增生命值相关 =====
    [Header("生命值设置")]
    public int maxHealth = 100;
    private int currentHealth;

    [Header("移动设置")]
    public float runSpeed = 6.0f;
    public float rotationAngle = 90f;
    public float sideMoveSpeed = 4.0f;
    public float jumpForce = 8f;
    public float gravity = -25f;

    [Header("第一人称设置")] // 补全缺失的第一人称相关变量
    public bool isFirstPerson = false; // 修复"当前上下文中不存在isFirstPerson"错误
    public Vector3 firstPersonCameraOffset = new Vector3(0, 1.8f, 0.05f);
    public float cameraSmoothness = 8f;

    [Header("回头冷却设置")]
    public float lookBackCooldown = 8f;
    public int maxLookBackCharges = 2;
    private int currentLookBackCharges = 2;
    private bool isOnCooldown = false;
    private float currentCooldownTime = 0f;

    // 核心组件引用
    private CharacterController controller;
    private Animator animator;
    private WatcherAI watcher;

    // 状态变量
    private bool isLookingBack = false;
    private bool canTurn = true;
    private bool isMovementEnabled = true;
    private Vector3 initialForward;
    private Vector3 initialRight;

    // 移动相关
    private Vector3 moveDirection;
    private float velocityY;
    private bool isGrounded;

    // 动画相关
    private float currentSpeed;
    private int speedParamHash;
    private int groundedParamHash;
    private int jumpParamHash;
    private int lookBackParamHash;

    // 相机相关
    private Camera mainCamera;
    private Vector3 originalCameraPosition;
    private Quaternion originalCameraRotation;
    private Transform originalCameraParent;

    // 回头旋转相关
    private Quaternion targetLookBackRotation;
    private Quaternion originalRotation;

    // 渲染相关
    private SkinnedMeshRenderer playerMeshRenderer;

    void Start()
    {
        // 初始化生命值
        currentHealth = maxHealth;

        // 获取核心组件
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        mainCamera = Camera.main;
        watcher = FindObjectOfType<WatcherAI>();

        // 确保CharacterController启用（碰撞检测关键）
        if (controller != null)
        {
            controller.enabled = true;
            controller.detectCollisions = true;
        }
        else
        {
            Debug.LogError("Player缺少CharacterController组件！碰撞检测将失效");
        }

        // 初始化方向向量
        initialForward = transform.forward;
        initialRight = transform.right;

        // 保存相机初始状态
        if (mainCamera != null)
        {
            originalCameraParent = mainCamera.transform.parent;
            originalCameraPosition = mainCamera.transform.localPosition;
            originalCameraRotation = mainCamera.transform.localRotation;
        }

        // 动画参数哈希（优化性能）
        speedParamHash = Animator.StringToHash("Speed");
        groundedParamHash = Animator.StringToHash("IsGrounded");
        jumpParamHash = Animator.StringToHash("Jump");
        lookBackParamHash = Animator.StringToHash("LookBack");

        // 获取玩家模型渲染器
        playerMeshRenderer = GetComponentInChildren<SkinnedMeshRenderer>();

        // 初始化回头次数
        currentLookBackCharges = maxLookBackCharges;

        // 动画组件检查
        if (animator == null)
            Debug.LogWarning("缺少Animator组件！动画将无法工作。");
    }

    // ===== 新增：添加生命值方法（兼容治疗药水）=====
    public void AddHealth(int amount)
    {
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        Debug.Log($"生命值+{amount}，当前生命值: {currentHealth}/{maxHealth}");
    }

    // ===== 速度提升方法 =====
    public void StartSpeedBoost(float multiplier, float duration)
    {
        StartCoroutine(SpeedBoostCoroutine(multiplier, duration));
    }

    private IEnumerator SpeedBoostCoroutine(float multiplier, float duration)
    {
        float originalRunSpeed = runSpeed;
        float originalSideSpeed = sideMoveSpeed;

        // 应用速度倍率
        runSpeed *= multiplier;
        sideMoveSpeed *= multiplier;

        Debug.Log($"速度提升生效！原速度: {originalRunSpeed}, 新速度: {runSpeed}");

        // 计时并显示剩余时间
        float endTime = Time.time + duration;
        while (Time.time < endTime)
        {
            yield return null;
        }

        // 恢复原速度
        runSpeed = originalRunSpeed;
        sideMoveSpeed = originalSideSpeed;
        Debug.Log("速度提升效果结束，恢复原速度");
    }

    // ===== 回头次数增加方法 =====
    public void AddLookbackCharge(int amount = 1)
    {
        currentLookBackCharges = Mathf.Min(currentLookBackCharges + amount, maxLookBackCharges);
        Debug.Log($"回头次数+{amount}，当前: {currentLookBackCharges}/{maxLookBackCharges}");
    }

    // ===== 核心更新逻辑 =====
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
        UpdateFirstPersonCamera();
    }

    // ===== 地面检测 =====
    void HandleGroundCheck()
    {
        isGrounded = controller.isGrounded;

        if (isGrounded && velocityY < 0)
        {
            velocityY = -2f; // 轻微的地面吸附
        }
    }

    // ===== 移动处理 =====
    void HandleMovement()
    {
        if (!isMovementEnabled)
        {
            ApplyGravity();
            return;
        }

        // 自动向前移动
        Vector3 forwardMove = transform.forward * runSpeed;

        // Q/E 左右侧移
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

        // 合并移动方向（仅水平）
        moveDirection = forwardMove + sideMovement;
        moveDirection.y = 0;

        // 应用重力
        ApplyGravity();

        // 最终移动向量（包含重力）
        Vector3 finalMove = moveDirection + Vector3.up * velocityY;
        controller.Move(finalMove * Time.deltaTime);

        // 计算当前移动速度（仅水平）
        Vector3 horizontalVelocity = new Vector3(controller.velocity.x, 0, controller.velocity.z);
        currentSpeed = horizontalVelocity.magnitude;
    }

    // ===== 重力应用 =====
    void ApplyGravity()
    {
        velocityY += gravity * Time.deltaTime;
    }

    // ===== 跳跃处理 =====
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

    // ===== 转向处理（A/D）=====
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

    // ===== 转向协程 =====
    IEnumerator TurnCoroutine(float angle)
    {
        canTurn = false;

        // 立即转向指定角度
        transform.Rotate(0, angle, 0);

        // 更新初始方向向量
        Quaternion rotation = Quaternion.Euler(0, angle, 0);
        initialForward = rotation * initialForward;
        initialRight = rotation * initialRight;

        // 转向冷却
        yield return new WaitForSeconds(0.2f);
        canTurn = true;
    }

    // ===== 回头处理（空格）=====
    void HandleLookBack()
    {
        if (Input.GetKeyDown(KeyCode.Space) && !isLookingBack && currentLookBackCharges > 0)
        {
            StartLookBack();
        }
        if (Input.GetKeyUp(KeyCode.Space) && isLookingBack)
        {
            StopLookBack();
        }
    }

    // ===== 开始回头 =====
    void StartLookBack()
    {
        isLookingBack = true;
        isMovementEnabled = false;

        // 消耗回头次数
        currentLookBackCharges--;

        // 启动冷却
        if (!isOnCooldown)
        {
            StartCooldown();
        }

        // 保存初始旋转，目标旋转为180度
        originalRotation = transform.rotation;
        targetLookBackRotation = transform.rotation * Quaternion.Euler(0, 180f, 0);

        // 动画更新
        if (animator != null)
        {
            animator.SetBool(lookBackParamHash, true);
            animator.SetFloat(speedParamHash, 0f);
        }

        // 相机旋转（第三人称）
        if (mainCamera != null && !isFirstPerson)
        {
            mainCamera.transform.RotateAround(transform.position, Vector3.up, 180f);
        }

        // 通知Watcher停止追逐
        if (watcher != null)
        {
            watcher.OnPlayerLookedAt(true);
        }

        Debug.Log($"开始回头 - 剩余次数: {currentLookBackCharges}");
    }

    // ===== 停止回头 =====
    void StopLookBack()
    {
        isLookingBack = false;
        isMovementEnabled = true;

        // 恢复初始旋转
        transform.rotation = originalRotation;

        // 动画更新
        if (animator != null)
            animator.SetBool(lookBackParamHash, false);

        // 恢复相机位置（第三人称）
        if (mainCamera != null && !isFirstPerson)
        {
            mainCamera.transform.localPosition = originalCameraPosition;
            mainCamera.transform.localRotation = originalCameraRotation;
        }

        // 通知Watcher继续追逐
        if (watcher != null)
        {
            watcher.OnPlayerLookedAt(false);
        }
    }

    // ===== 回头旋转平滑过渡 =====
    void HandleLookBackRotation()
    {
        if (isLookingBack)
        {
            transform.rotation = Quaternion.Lerp(transform.rotation, targetLookBackRotation, 3f * Time.deltaTime);

            if (animator != null)
            {
                animator.SetFloat(speedParamHash, 0f);
            }
        }
    }

    // ===== 第一人称切换（V键）=====
    void HandleFirstPersonToggle()
    {
        if (Input.GetKeyDown(KeyCode.V))
        {
            ToggleFirstPerson();
        }
    }

    // ===== 切换第一/第三人称 =====
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

    // ===== 进入第一人称 =====
    void EnterFirstPerson()
    {
        // 隐藏玩家模型
        if (playerMeshRenderer != null)
            playerMeshRenderer.enabled = false;

        // 如果正在回头，停止回头
        if (isLookingBack)
        {
            StopLookBack();
        }

        // 相机脱离父物体，跟随玩家
        if (mainCamera != null)
        {
            mainCamera.transform.SetParent(null);
        }

        Debug.Log("切换到第一人称视角");
    }

    // ===== 退出第一人称 =====
    void ExitFirstPerson()
    {
        // 显示玩家模型
        if (playerMeshRenderer != null)
            playerMeshRenderer.enabled = true;

        // 恢复相机初始状态
        if (mainCamera != null)
        {
            mainCamera.transform.SetParent(originalCameraParent);
            mainCamera.transform.localPosition = originalCameraPosition;
            mainCamera.transform.localRotation = originalCameraRotation;
        }

        Debug.Log("切换回第三人称视角");
    }

    // ===== 更新第一人称相机位置 =====
    void UpdateFirstPersonCamera()
    {
        if (isFirstPerson && mainCamera != null)
        {
            Vector3 targetPosition = transform.position + transform.TransformDirection(firstPersonCameraOffset);
            Quaternion targetRotation = transform.rotation;

            // 平滑过渡相机位置和旋转
            mainCamera.transform.position = Vector3.Lerp(mainCamera.transform.position, targetPosition, cameraSmoothness * Time.deltaTime);
            mainCamera.transform.rotation = Quaternion.Lerp(mainCamera.transform.rotation, targetRotation, cameraSmoothness * Time.deltaTime);
        }
    }

    // ===== 更新动画 =====
    void UpdateAnimations()
    {
        if (animator == null) return;

        animator.SetFloat(speedParamHash, currentSpeed, 0.1f, Time.deltaTime);
        animator.SetBool(groundedParamHash, isGrounded);
    }

    // ===== 更新冷却时间 =====
    void UpdateCooldown()
    {
        if (isOnCooldown)
        {
            currentCooldownTime -= Time.deltaTime;

            if (currentCooldownTime <= 0)
            {
                // 冷却结束，恢复一次回头次数
                currentLookBackCharges++;
                isOnCooldown = false;
                currentCooldownTime = 0f;

                // 如果还有次数未恢复，继续冷却
                if (currentLookBackCharges < maxLookBackCharges)
                {
                    StartCooldown();
                }
            }
        }
    }

    // ===== 启动冷却 =====
    void StartCooldown()
    {
        isOnCooldown = true;
        currentCooldownTime = lookBackCooldown;
    }

    // ===== GUI显示（生命值、回头次数、操作说明）=====
    void OnGUI()
    {
        GUIStyle largeStyle = new GUIStyle(GUI.skin.label);
        largeStyle.fontSize = 24;
        largeStyle.fontStyle = FontStyle.Bold;

        // 显示生命值
        largeStyle.normal.textColor = Color.red;
        GUI.Label(new Rect(10, 20, 400, 30), $"生命值: {currentHealth}/{maxHealth}", largeStyle);

        // 显示回头次数
        Color textColor = currentLookBackCharges > 0 ? Color.green : Color.red;
        largeStyle.normal.textColor = textColor;
        GUI.Label(new Rect(10, 60, 400, 30), $"回头次数: {currentLookBackCharges}/{maxLookBackCharges}", largeStyle);

        // 显示冷却状态
        if (isOnCooldown)
        {
            float progress = 1 - (currentCooldownTime / lookBackCooldown);
            Rect progressBarBg = new Rect(10, 100, 200, 25);
            Rect progressBarFill = new Rect(10, 100, 200 * progress, 25);

            // 冷却进度条背景
            GUI.backgroundColor = Color.gray;
            GUI.Box(progressBarBg, GUIContent.none);

            // 冷却进度条填充
            GUI.backgroundColor = Color.blue;
            GUI.Box(progressBarFill, GUIContent.none);

            // 冷却时间文本
            largeStyle.normal.textColor = Color.white;
            GUI.Label(new Rect(10, 130, 400, 30), $"冷却中: {currentCooldownTime:F1}s", largeStyle);
        }
        else
        {
            largeStyle.normal.textColor = Color.green;
            GUI.Label(new Rect(10, 100, 400, 30), "冷却就绪", largeStyle);
        }

        // 操作说明
        GUIStyle hintStyle = new GUIStyle(GUI.skin.label);
        hintStyle.fontSize = 16;
        hintStyle.normal.textColor = Color.yellow;

        string controls = "操作说明:\n" +
                         "自动前进 | A/D: 转向 | 空格: 回头\n" +
                         "V: 切换视角 | 触碰药水获得效果";

        GUI.Label(new Rect(10, 160, 300, 100), controls, hintStyle);
    }

    // ===== 公共方法 =====
    public void SetMovementEnabled(bool enabled)
    {
        isMovementEnabled = enabled;
    }

    public bool IsLookingBack()
    {
        return isLookingBack;
    }

    // ===== Gizmos调试 =====
    void OnDrawGizmos()
    {
        if (Application.isPlaying && isFirstPerson)
        {
            // 第一人称相机位置调试
            Gizmos.color = Color.red;
            Vector3 cameraPos = transform.position + transform.TransformDirection(firstPersonCameraOffset);
            Gizmos.DrawWireSphere(cameraPos, 0.1f);
            Gizmos.DrawLine(transform.position, cameraPos);
        }
    }

    void OnDrawGizmosSelected()
    {
        // CharacterController碰撞范围调试
        if (controller != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position + controller.center, controller.radius);
        }
    }
}