using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    [Header("生命值设置")]
    public float maxHealth = 100f;
    public float currentHealth = 100f;
    public float damageCooldown = 0.5f; // 伤害冷却时间，防止连续受伤

    [Header("UI引用")]
    public Slider healthSlider;
    public Text healthText;
    public GameObject deathScreenUI; // 可选：死亡界面

    [Header("视觉反馈")]
    public Color fullHealthColor = Color.green;
    public Color lowHealthColor = Color.red;
    public Image healthFillImage; // 血条填充图片（如果使用图片血条）

    [Header("音效/特效")]
    public AudioClip damageSound;
    public AudioClip healSound;
    public AudioClip deathSound;
    public ParticleSystem healEffect;
    public ParticleSystem damageEffect;

    private AudioSource audioSource;
    private bool isDead = false;
    private bool canTakeDamage = true;
    private float lastDamageTime;

    void Start()
    {
        // 初始化生命值
        currentHealth = maxHealth;
        isDead = false;

        // 获取音频源
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // 初始化UI
        UpdateHealthUI();

        // 初始化血条颜色
        if (healthFillImage != null)
        {
            healthFillImage.color = fullHealthColor;
        }
    }

    void Update()
    {
        // 测试代码：按H键加血，按J键扣血（开发完成后可删除）
        if (Input.GetKeyDown(KeyCode.H))
        {
            Heal(20);
        }

        if (Input.GetKeyDown(KeyCode.J) && canTakeDamage)
        {
            TakeDamage(10);
        }

        // 更新伤害冷却
        if (!canTakeDamage && Time.time - lastDamageTime > damageCooldown)
        {
            canTakeDamage = true;
        }
    }

    // 受到伤害
    // 受到伤害
    public void TakeDamage(float damageAmount)
    {
        if (isDead || !canTakeDamage)
        {
            Debug.Log($"伤害被忽略: 死亡状态={isDead}, 可受伤害={canTakeDamage}");
            return;
        }

        // 应用伤害（删除重复的扣血代码）
        currentHealth -= damageAmount;
        lastDamageTime = Time.time;
        canTakeDamage = false;

        Debug.Log($"玩家受到 {damageAmount} 点伤害，剩余生命: {currentHealth}");

        // 播放伤害音效
        if (damageSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(damageSound);
        }

        // 播放伤害特效
        if (damageEffect != null)
        {
            Instantiate(damageEffect, transform.position, Quaternion.identity);
        }

        // 更新UI
        UpdateHealthUI();

        // 检查是否死亡
        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }
    }

    // 治疗
    public void Heal(float healAmount)
    {
        if (isDead) return;

        // 应用治疗
        currentHealth += healAmount;
        if (currentHealth > maxHealth)
        {
            currentHealth = maxHealth;
        }

        // 播放治疗音效
        if (healSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(healSound);
        }

        // 播放治疗特效
        if (healEffect != null)
        {
            Instantiate(healEffect, transform.position, Quaternion.identity);
        }

        // 更新UI
        UpdateHealthUI();

        Debug.Log($"玩家恢复 {healAmount} 点生命，当前生命: {currentHealth}");
    }

    // 更新生命值UI
    void UpdateHealthUI()
    {
        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth;

            // 动态调整血条颜色
            float healthPercent = currentHealth / maxHealth;
            if (healthFillImage != null)
            {
                healthFillImage.color = Color.Lerp(lowHealthColor, fullHealthColor, healthPercent);
            }
        }

        if (healthText != null)
        {
            healthText.text = $"{currentHealth:F0}/{maxHealth:F0}";

            // 根据生命值百分比改变文本颜色
            if (currentHealth <= maxHealth * 0.3f)
            {
                healthText.color = Color.red;
            }
            else if (currentHealth <= maxHealth * 0.5f)
            {
                healthText.color = Color.yellow;
            }
            else
            {
                healthText.color = Color.green;
            }
        }
    }

    // 死亡处理
    void Die()
    {
        if (isDead) return;

        isDead = true;

        // 播放死亡音效
        if (deathSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(deathSound);
        }

        // 显示死亡界面
        if (deathScreenUI != null)
        {
            deathScreenUI.SetActive(true);
        }

        // 禁用玩家控制
        PlayerController playerController = GetComponent<PlayerController>();
        if (playerController != null)
        {
            playerController.SetMovementEnabled(false);
        }

        // 停止Watcher
        WatcherAI watcher = FindObjectOfType<WatcherAI>();
        if (watcher != null)
        {
            watcher.OnPlayerLookedAt(true);
        }

        // 停止游戏时间
        Time.timeScale = 0.3f; // 慢动作效果

        Debug.Log("玩家死亡！");

        // 3秒后重启游戏
        Invoke("RestartGame", 3f);
    }

    // 重启游戏
    void RestartGame()
    {
        Time.timeScale = 1f;

        // 重置生命值
        currentHealth = maxHealth;
        isDead = false;

        // 隐藏死亡界面
        if (deathScreenUI != null)
        {
            deathScreenUI.SetActive(false);
        }

        // 启用玩家控制
        PlayerController playerController = GetComponent<PlayerController>();
        if (playerController != null)
        {
            playerController.SetMovementEnabled(true);
        }

        // 重置Watcher
        WatcherAI watcher = FindObjectOfType<WatcherAI>();
        if (watcher != null)
        {
            watcher.OnPlayerLookedAt(false);

            // 调用Watcher的RestartGame方法
            var restartMethod = watcher.GetType().GetMethod("RestartGame");
            if (restartMethod != null)
            {
                restartMethod.Invoke(watcher, null);
            }
        }

        // 重置玩家位置
        ObstacleCollision obstacleCollision = GetComponent<ObstacleCollision>();
        if (obstacleCollision != null)
        {
            // 调用RestartGame方法
            obstacleCollision.SendMessage("RestartGame", SendMessageOptions.DontRequireReceiver);
        }

        UpdateHealthUI();

        Debug.Log("游戏重新开始");
    }

    // 检查是否存活
    public bool IsAlive()
    {
        return !isDead;
    }

    // 获取当前生命值百分比
    public float GetHealthPercentage()
    {
        return currentHealth / maxHealth;
    }

    // 供陷阱脚本调用的接口
    public void ApplyTrapDamage(float damage)
    {
        TakeDamage(damage);
    }
}