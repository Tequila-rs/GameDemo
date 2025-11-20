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
    public float lookBackRotationSpeed = 3f; // 新增：回头看时的旋转速度

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

    // 新增：回头看相关变量
    private Quaternion targetLookBackRotation;
    private Quaternion originalRotation;
    private Coroutine lookBackCoroutine;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        mainCamera = Camera.main;
        watcher = FindObjectOfType<WatcherAI>();

        initialForward = transform.forward;
        initialRight = transform.right;

        originalCameraPosition = mainCamera.transform.localPosition;
        originalCameraRotation = mainCamera.transform.localRotation;

        speedParamHash = Animator.StringToHash("Speed");
        groundedParamHash = Animator.StringToHash("IsGrounded");
        jumpParamHash = Animator.StringToHash("Jump");
        lookBackParamHash = Animator.StringToHash("LookBack");

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
        UpdateAnimations();

        // 新增：处理回头看时的平滑旋转
        HandleLookBackRotation();
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
        if (Input.GetKey(KeyCode.LeftArrow))
        {
            horizontalInput = -1f;
        }
        else if (Input.GetKey(KeyCode.RightArrow))
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
        if (Input.GetKeyDown(KeyCode.Space) && !isLookingBack)
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

        // 保存原始旋转
        originalRotation = transform.rotation;

        // 计算目标旋转（转身180度）
        targetLookBackRotation = transform.rotation * Quaternion.Euler(0, 180f, 0);

        // 设置动画参数
        if (animator != null)
        {
            animator.SetBool(lookBackParamHash, true);
            animator.SetFloat(speedParamHash, 0f); // 强制设置速度为0，停止奔跑动画
        }

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
        isMovementEnabled = true;

        // 恢复原始旋转
        transform.rotation = originalRotation;

        // 取消回头看动画状态
        if (animator != null)
            animator.SetBool(lookBackParamHash, false);

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

    // 新增：处理回头看时的平滑旋转
    void HandleLookBackRotation()
    {
        if (isLookingBack)
        {
            // 平滑旋转角色模型
            transform.rotation = Quaternion.Lerp(transform.rotation, targetLookBackRotation, lookBackRotationSpeed * Time.deltaTime);

            // 确保速度参数为0，防止奔跑动画播放
            if (animator != null)
            {
                animator.SetFloat(speedParamHash, 0f);
            }
        }
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

    public void SetMovementEnabled(bool enabled)
    {
        isMovementEnabled = enabled;
    }

    public bool IsLookingBack()
    {
        return isLookingBack;
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