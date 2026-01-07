using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class WatcherFootsteps : MonoBehaviour
{
    [Header("声音设置")]
    public AudioClip proximitySound; // 靠近时的持续声音
    public float minVolume = 0.5f; // 最远距离时的最小音量
    public float maxVolume = 5.0f; // 最近距离时的最大音量

    [Header("距离设置")]
    public float startDistance = 10f; // 开始听到声音的距离
    public float maxVolumeDistance = 2f; // 声音达到最大音量的距离

    [Header("淡入淡出")]
    public float fadeSpeed = 3f; // 音量变化速度

    [Header("调试")]
    public bool showDebugInfo = false;

    private AudioSource audioSource;
    private Transform player;
    private WatcherAI watcherAI;
    private float targetVolume = 0f;
    private float currentVolume = 0f;
    private bool soundEnabled = true; // 声音是否启用

    // 公开属性，供其他脚本访问
    [HideInInspector] public bool isPlaying = false;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        watcherAI = GetComponent<WatcherAI>();

        // 自动查找玩家
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }

        // 配置音频源
        if (audioSource != null && proximitySound != null)
        {
            audioSource.clip = proximitySound;
            audioSource.loop = true; // 循环播放
            audioSource.spatialBlend = 1.0f; // 3D音效
            audioSource.volume = 0f; // 初始音量为0
            audioSource.playOnAwake = false;
        }
        else if (proximitySound == null)
        {
            Debug.LogWarning("请为WatcherFootsteps添加Proximity Sound音频剪辑");
        }
    }

    void Update()
    {
        if (player == null || audioSource == null || proximitySound == null) return;

        // 如果声音被禁用，强制音量为0
        if (!soundEnabled)
        {
            targetVolume = 0f;
            currentVolume = Mathf.Lerp(currentVolume, targetVolume, Time.deltaTime * fadeSpeed * 2f); // 禁用时淡出更快
            audioSource.volume = currentVolume;

            if (currentVolume < 0.01f && audioSource.isPlaying)
            {
                audioSource.Stop();
                isPlaying = false;
            }
            return;
        }

        // 检查Watcher是否停止
        bool isHalted = false;
        if (watcherAI != null)
        {
            System.Reflection.FieldInfo isHaltedField = typeof(WatcherAI).GetField(
                "isHalted",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
            );

            if (isHaltedField != null)
            {
                isHalted = (bool)isHaltedField.GetValue(watcherAI);
            }
        }

        // 计算与玩家的距离
        float distance = Vector3.Distance(transform.position, player.position);

        // 根据距离计算目标音量
        if (isHalted || distance > startDistance)
        {
            targetVolume = 0f; // Watcher停止或距离太远，音量为0
        }
        else
        {
            // 计算音量：距离越近，音量越大
            if (distance <= maxVolumeDistance)
            {
                targetVolume = maxVolume; // 最近距离，最大音量
            }
            else
            {
                // 在maxVolumeDistance和startDistance之间线性插值
                float t = 1f - (distance - maxVolumeDistance) / (startDistance - maxVolumeDistance);
                targetVolume = Mathf.Lerp(minVolume, maxVolume, t);
            }
        }

        // 平滑过渡到目标音量
        currentVolume = Mathf.Lerp(currentVolume, targetVolume, Time.deltaTime * fadeSpeed);

        // 设置音频源音量
        audioSource.volume = currentVolume;

        // 根据音量控制音频源播放/停止
        if (currentVolume > 0.01f)
        {
            if (!audioSource.isPlaying)
            {
                audioSource.Play();
                isPlaying = true;
            }
        }
        else
        {
            if (audioSource.isPlaying)
            {
                audioSource.Stop();
                isPlaying = false;
            }
        }

        // 调试信息
        if (showDebugInfo && Time.frameCount % 60 == 0)
        {
            Debug.Log($"Watcher声音 - 距离: {distance:F2}m, 当前音量: {currentVolume:F2}, 目标音量: {targetVolume:F2}, 启用: {soundEnabled}");
        }
    }

    // 手动控制声音启用/禁用
    public void SetSoundEnabled(bool enabled)
    {
        soundEnabled = enabled;

        if (!enabled)
        {
            // 禁用声音时，立即将目标音量设为0
            targetVolume = 0f;

            if (showDebugInfo)
            {
                Debug.Log("Watcher声音已禁用");
            }
        }
        else
        {
            if (showDebugInfo)
            {
                Debug.Log("Watcher声音已启用");
            }
        }
    }

    // 立即停止声音（用于紧急情况，如游戏结束）
    public void StopImmediately()
    {
        soundEnabled = false;
        targetVolume = 0f;
        currentVolume = 0f;

        if (audioSource != null)
        {
            audioSource.volume = 0f;
            audioSource.Stop();
            isPlaying = false;
        }

        if (showDebugInfo)
        {
            Debug.Log("Watcher声音已立即停止");
        }
    }

    // 立即开始声音
    public void StartImmediately()
    {
        soundEnabled = true;

        if (audioSource != null && proximitySound != null && !audioSource.isPlaying)
        {
            audioSource.Play();
            isPlaying = true;
        }
    }

    // 调试用：在Scene视图中显示声音范围
    void OnDrawGizmosSelected()
    {
        if (!showDebugInfo) return;

        // 绘制声音开始距离（绿色）
        Gizmos.color = new Color(0f, 1f, 0f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, startDistance);

        // 绘制最大音量距离（红色）
        Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, maxVolumeDistance);

        // 绘制音量梯度环
        for (int i = 1; i < 4; i++)
        {
            float distance = Mathf.Lerp(maxVolumeDistance, startDistance, i * 0.25f);
            Gizmos.color = new Color(1f, i * 0.25f, 0f, 0.1f);
            Gizmos.DrawWireSphere(transform.position, distance);
        }

        // 如果玩家存在，绘制连接线并显示信息
        if (player != null)
        {
            float distance = Vector3.Distance(transform.position, player.position);
            float calculatedVolume = 0f;

            if (distance <= maxVolumeDistance)
            {
                calculatedVolume = maxVolume;
            }
            else if (distance <= startDistance)
            {
                float t = 1f - (distance - maxVolumeDistance) / (startDistance - maxVolumeDistance);
                calculatedVolume = Mathf.Lerp(minVolume, maxVolume, t);
            }

            // 根据音量和是否启用设置线条颜色
            if (!soundEnabled)
            {
                Gizmos.color = Color.gray; // 禁用时为灰色
            }
            else if (calculatedVolume > 0)
            {
                Gizmos.color = Color.Lerp(Color.green, Color.red, calculatedVolume);
            }
            else
            {
                Gizmos.color = Color.blue;
            }

            Gizmos.DrawLine(transform.position, player.position);

            // 在连线中点显示信息
            Vector3 midPoint = (transform.position + player.position) / 2;
#if UNITY_EDITOR
            string statusText = soundEnabled ? "启用" : "禁用";
            UnityEditor.Handles.Label(midPoint,
                $"距离: {distance:F1}m\n音量: {calculatedVolume:F2}\n状态: {statusText}");
#endif
        }
    }

    // 在Inspector中重置时自动添加AudioSource
    void Reset()
    {
        AudioSource existingSource = GetComponent<AudioSource>();
        if (existingSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        else
        {
            audioSource = existingSource;
        }

        // 设置默认值
        startDistance = 10f;
        maxVolumeDistance = 2f;
        minVolume = 0.1f;
        maxVolume = 1.0f;
        fadeSpeed = 3f;
    }
}