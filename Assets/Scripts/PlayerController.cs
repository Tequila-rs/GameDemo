using System.Collections;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    // ===== 生命值设置 =====
    [Header("生命值设置")]
    public int maxHealth = 100;
    private int currentHealth;

    [Header("移动设置")]
    public float runSpeed = 6.0f;
    public float rotationAngle = 90f;
    public float sideMoveSpeed = 4.0f;
    public float jumpForce = 8f;
    public float gravity = -25f;

    [Header("第一人称设置")]
    public Vector3 firstPersonCameraOffset = new Vector3(0, 1.8f, 0.05f);
    public float cameraSmoothness = 8f;

    [Header("回头冷却设置")]
    public float lookBackCooldown = 8f;
    public int maxLookBackCharges = 2;
    private int currentLookBackCharges = 2;
    private bool isOnCooldown = false;
    private float currentCooldownTime = 0f;

    // 组件引用
    private CharacterController controller;
    private Animator animator;
    private WatcherAI watcher;
    private Camera mainCamera;

    // 状态变量
    private bool isLookingBack = false;
    private bool canTurn = true;
    private bool isMovementEnabled = true;
    private Vector3 initialForward;
    private Vector3 initialRight;

    // 移动变量
    private Vector3 moveDirection;
    private float velocityY;
    private bool isGrounded;
    private float currentSpeed;

    // 动画参数
    private int speedParamHash;
    private int groundedParamHash;
    private int jumpParamHash;
    private int lookBackParamHash;

    // 回头旋转变量
    private Quaternion targetLookBackRotation;
    private Quaternion originalRotation;

    // 游戏状态
    private bool isGameOver = false;
    private bool isVictory = false;

    void Start()
    {
        // 初始化生命值
        currentHealth = maxHealth;

        // 获取组件引用
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        mainCamera = Camera.main;
        watcher = FindObjectOfType<WatcherAI>();

        // 确保CharacterController可用
        if (controller != null)
        {
            controller.enabled = true;
            controller.detectCollisions = true;
        }
        else
        {
            Debug.LogError("Player缺少CharacterController组件，移动系统会失效");
        }

        // 初始化方向向量
        initialForward = transform.forward;
        initialRight = transform.right;

        // 动画参数哈希
        speedParamHash = Animator.StringToHash("Speed");
        groundedParamHash = Animator.StringToHash("IsGrounded");
        jumpParamHash = Animator.StringToHash("Jump");
        lookBackParamHash = Animator.StringToHash("LookBack");

        // 初始化回头次数
        currentLookBackCharges = maxLookBackCharges;

        // 动画组件检查
        if (animator == null)
            Debug.LogWarning("缺少Animator组件，动画系统无法工作");

        // 隐藏玩家模型（第一人称视角）
        HidePlayerModel();

        // 初始化UI状态
        UpdateAllUI();

        // 初始化游戏状态
        isGameOver = false;
        isVictory = false;
    }

    // 隐藏玩家模型（第一人称视角）
    void HidePlayerModel()
    {
        // 获取所有渲染器并隐藏
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            renderer.enabled = false;
        }

        // 获取所有SkinnedMeshRenderer并隐藏
        SkinnedMeshRenderer[] skinnedRenderers = GetComponentsInChildren<SkinnedMeshRenderer>();
        foreach (SkinnedMeshRenderer skinnedRenderer in skinnedRenderers)
        {
            skinnedRenderer.enabled = false;
        }

        Debug.Log("玩家模型已隐藏（第一人称视角）");
    }

    // ===== 生命值系统 =====
    public void AddHealth(int amount)
    {
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        Debug.Log($"生命值+{amount}, 当前: {currentHealth}/{maxHealth}");
        UpdateHealthUI(); // 更新UI
    }

    // ===== 速度加成系统 =====
    public void StartSpeedBoost(float multiplier, float duration)
    {
        StartCoroutine(SpeedBoostCoroutine(multiplier, duration));
    }

    private IEnumerator SpeedBoostCoroutine(float multiplier, float duration)
    {
        float originalRunSpeed = runSpeed;
        float originalSideSpeed = sideMoveSpeed;

        // 提升速度
        runSpeed *= multiplier;
        sideMoveSpeed *= multiplier;
        Debug.Log($"速度加成生效，原速度: {originalRunSpeed}, 新速度: {runSpeed}");

        // 加速期间持续更新倒计时UI
        float endTime = Time.time + duration;
        while (Time.time < endTime)
        {
            float remainingTime = endTime - Time.time;
            if (UIManager.Instance != null)
            {
                UIManager.Instance.UpdateSpeedBoostUI(true, remainingTime, duration);
            }
            yield return null;
        }

        // 恢复原始速度
        runSpeed = originalRunSpeed;
        sideMoveSpeed = originalSideSpeed;

        // 加速结束，更新UI
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateSpeedBoostUI(false, 0, duration);
        }
        Debug.Log("速度加成效果结束，恢复原速度");
    }

    // ===== 回头次数增加系统 =====
    public void AddLookbackCharge(int amount = 1)
    {
        currentLookBackCharges = Mathf.Min(currentLookBackCharges + amount, maxLookBackCharges);
        Debug.Log($"回头次数+{amount}, 当前: {currentLookBackCharges}/{maxLookBackCharges}");
        UpdateLookBackChargeUI(); // 更新UI
    }

    // ===== 核心循环逻辑 =====
    void Update()
    {
        // 如果游戏结束或胜利，不处理输入和移动
        if (isGameOver || isVictory) return;

        HandleGroundCheck();
        HandleMovement();
        HandleLookBack();
        HandleTurning();
        HandleJump();
        UpdateAnimations();
        HandleLookBackRotation();
        UpdateCooldown();

        // 实时同步UI状态
        UpdateAllUI();
    }

    // ===== 地面检测 =====
    void HandleGroundCheck()
    {
        isGrounded = controller.isGrounded;
        if (isGrounded && velocityY < 0)
        {
            velocityY = -2f;
        }
    }

    // ===== 移动控制 =====
    void HandleMovement()
    {
        if (!isMovementEnabled || isVictory || isGameOver)
        {
            ApplyGravity();
            return;
        }

        Vector3 forwardMove = transform.forward * runSpeed;
        float horizontalInput = 0f;
        if (Input.GetKey(KeyCode.Q))
            horizontalInput = -1f;
        else if (Input.GetKey(KeyCode.E))
            horizontalInput = 1f;

        Vector3 sideMovement = initialRight * horizontalInput * sideMoveSpeed;
        moveDirection = forwardMove + sideMovement;
        moveDirection.y = 0;

        ApplyGravity();
        Vector3 finalMove = moveDirection + Vector3.up * velocityY;
        controller.Move(finalMove * Time.deltaTime);

        Vector3 horizontalVelocity = new Vector3(controller.velocity.x, 0, controller.velocity.z);
        currentSpeed = horizontalVelocity.magnitude;
    }

    // ===== 重力应用 =====
    void ApplyGravity()
    {
        velocityY += gravity * Time.deltaTime;
    }

    // ===== 跳跃控制 =====
    void HandleJump()
    {
        if (!isMovementEnabled || isVictory || isGameOver) return;
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocityY = Mathf.Sqrt(jumpForce * -2f * gravity);
            if (animator != null)
                animator.SetTrigger(jumpParamHash);
        }
    }

    // ===== 转向控制 =====
    void HandleTurning()
    {
        if (isLookingBack || isVictory || isGameOver) return;
        if (canTurn)
        {
            if (Input.GetKeyDown(KeyCode.A))
                StartCoroutine(TurnCoroutine(-rotationAngle));
            else if (Input.GetKeyDown(KeyCode.D))
                StartCoroutine(TurnCoroutine(rotationAngle));
        }
    }

    // ===== 转向协程 =====
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

    // ===== 回头控制 =====
    void HandleLookBack()
    {
        if (isVictory || isGameOver) return;

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
        currentLookBackCharges--;

        if (!isOnCooldown)
            StartCooldown();

        originalRotation = transform.rotation;
        targetLookBackRotation = transform.rotation * Quaternion.Euler(0, 180f, 0);

        if (animator != null)
        {
            animator.SetBool(lookBackParamHash, true);
            animator.SetFloat(speedParamHash, 0f);
        }

        if (watcher != null)
            watcher.OnPlayerLookedAt(true);

        Debug.Log($"开始回头 - 剩余次数: {currentLookBackCharges}");
        UpdateLookBackChargeUI(); // 更新UI
    }

    // ===== 停止回头 =====
    void StopLookBack()
    {
        isLookingBack = false;
        isMovementEnabled = true;
        transform.rotation = originalRotation;

        if (animator != null)
            animator.SetBool(lookBackParamHash, false);

        if (watcher != null)
            watcher.OnPlayerLookedAt(false);
    }

    // ===== 回头旋转平滑 =====
    void HandleLookBackRotation()
    {
        if (isLookingBack)
        {
            transform.rotation = Quaternion.Lerp(transform.rotation, targetLookBackRotation, 3f * Time.deltaTime);
            if (animator != null)
                animator.SetFloat(speedParamHash, 0f);
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
                currentLookBackCharges++;
                isOnCooldown = false;
                currentCooldownTime = 0f;

                if (currentLookBackCharges < maxLookBackCharges)
                    StartCooldown();

                UpdateLookBackChargeUI(); // 更新UI
            }
        }
    }

    // ===== 开始冷却 =====
    void StartCooldown()
    {
        isOnCooldown = true;
        currentCooldownTime = lookBackCooldown;
    }

    // ===== UI更新接口（同步到UIManager）=====
    /// <summary>
    /// 更新所有UI状态
    /// </summary>
    private void UpdateAllUI()
    {
        if (UIManager.Instance == null) return;
        UpdateHealthUI();
        UpdateLookBackChargeUI();
        UpdateCoolDownUI();
    }

    /// <summary>
    /// 更新生命值UI
    /// </summary>
    private void UpdateHealthUI()
    {
        UIManager.Instance?.UpdateHealthUI(currentHealth, maxHealth);
    }

    /// <summary>
    /// 更新回头次数UI
    /// </summary>
    private void UpdateLookBackChargeUI()
    {
        UIManager.Instance?.UpdateLookBackChargeUI(currentLookBackCharges, maxLookBackCharges);
    }

    /// <summary>
    /// 更新冷却状态UI
    /// </summary>
    private void UpdateCoolDownUI()
    {
        UIManager.Instance?.UpdateCoolDownUI(isOnCooldown, currentCooldownTime, lookBackCooldown);
    }

    // ===== 公开的重置方法 =====
    /// <summary>
    /// 重置玩家方向系统和所有状态
    /// </summary>
    public void ResetDirectionSystem()
    {
        // 重置初始方向向量
        initialForward = transform.forward;
        initialRight = transform.right;
        canTurn = true;

        // 重置回头状态
        isLookingBack = false;
        originalRotation = transform.rotation;
        targetLookBackRotation = transform.rotation;

        // 重置移动状态
        isMovementEnabled = true;

        // 重置回头次数
        currentLookBackCharges = maxLookBackCharges;
        isOnCooldown = false;
        currentCooldownTime = 0f;

        // 重置移动变量
        moveDirection = Vector3.zero;
        velocityY = 0f;
        currentSpeed = 0f;

        // 重置动画状态
        if (animator != null)
        {
            animator.SetBool(lookBackParamHash, false);
            animator.SetFloat(speedParamHash, 0f);
            animator.SetBool(groundedParamHash, true);
        }

        // 重置UI
        UpdateAllUI();

        Debug.Log("玩家方向系统已完全重置");
    }

    // ===== 触发器检测（用于终点检测）=====
    void OnTriggerEnter(Collider other)
    {
        if (isVictory || isGameOver) return;

        // 检测Capsule终点
        if (other.CompareTag("Finish"))
        {
            TriggerVictory();
        }
    }

    // ===== 胜利触发 =====
    void TriggerVictory()
    {
        isVictory = true;
        isMovementEnabled = false;

        Debug.Log("恭喜！顺利通关！");

        // 通知UIManager显示胜利界面
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowVictoryScreen();
        }

        // 停止所有移动
        currentSpeed = 0;

        // 停止时间（但保留UI响应）
        Time.timeScale = 0;

        // 播放胜利音效（如果需要）
        // if (AudioManager.Instance != null) AudioManager.Instance.PlayVictorySound();

        Debug.Log("=== VICTORY ===");
        Debug.Log("Press R to restart or ESC to quit");
    }

    // ===== 游戏结束处理 =====
    public void TriggerGameOver()
    {
        isGameOver = true;
        isMovementEnabled = false;
    }

    // ===== 重置游戏状态 =====
    public void ResetGameState()
    {
        isGameOver = false;
        isVictory = false;
        isMovementEnabled = true;
        Time.timeScale = 1;

        // 重置生命值
        currentHealth = maxHealth;

        // 重置回头次数
        currentLookBackCharges = maxLookBackCharges;
        isOnCooldown = false;
        currentCooldownTime = 0f;

        // 重置方向系统
        ResetDirectionSystem();

        // 更新UI
        UpdateAllUI();

        // 新增：重置药水（确保胜利重新开始时也能重置）
        Potion.ResetAllPotions();

        Debug.Log("玩家游戏状态已完全重置");
    }

    // ===== 外部接口 =====
    public void SetMovementEnabled(bool enabled)
    {
        if (!isVictory && !isGameOver)
            isMovementEnabled = enabled;
    }

    public bool IsLookingBack()
    {
        return isLookingBack;
    }

    public bool IsGameOver()
    {
        return isGameOver;
    }

    public bool IsVictory()
    {
        return isVictory;
    }

    // ===== 获取当前状态（供调试用）=====
    public string GetPlayerState()
    {
        return $"移动: {(isMovementEnabled ? "启用" : "禁用")}, " +
               $"回头: {(isLookingBack ? "是" : "否")}, " +
               $"可转向: {(canTurn ? "是" : "否")}, " +
               $"前向: {initialForward}, " +
               $"右向: {initialRight}, " +
               $"回头次数: {currentLookBackCharges}/{maxLookBackCharges}, " +
               $"游戏状态: {(isVictory ? "胜利" : isGameOver ? "失败" : "进行中")}";
    }

    // ===== Gizmos绘制 =====
    void OnDrawGizmos()
    {
        if (Application.isPlaying)
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