// BackgroundMusicManager.cs - 完整优化版
using UnityEngine;
using UnityEngine.SceneManagement;

public class BackgroundMusicManager : MonoBehaviour
{
    [Header("背景音乐设置")]
    public AudioClip bgmClip;            // 背景音乐音频剪辑
    public bool playOnAwake = true;      // 游戏启动时自动播放
    public bool loop = true;             // 循环播放
    public float fadeInTime = 2.0f;      // 淡入时间
    public float fadeOutTime = 2.0f;     // 淡出时间

    [Header("音量设置")]
    [Range(0f, 1f)]
    public float baseVolume = 0.3f;      // 基础音量（降低默认值）
    public bool useMasterVolume = true;  // 是否使用主音量设置

    [Header("动态音量调节")]
    public bool enableDynamicVolume = true; // 启用动态音量调节
    public float minDynamicVolume = 0.1f;   // 鬼靠近时的最小音量
    public float maxDynamicVolume = 0.3f;   // 鬼远离时的最大音量
    public float watcherCloseDistance = 5f; // 鬼靠近的判定距离
    public float volumeSmoothTime = 1.0f;   // 音量平滑过渡时间

    private AudioSource audioSource;
    private float targetVolume;
    private bool isFading = false;
    private float fadeSpeed = 0f;
    private WatcherAI watcher;           // 鬼的引用
    private Transform player;            // 玩家的引用
    private float dynamicVolumeModifier = 1f; // 动态音量调节器
    private float currentDynamicVolume = 1f;  // 当前动态音量
    private WatcherFootsteps watcherFootsteps; // 鬼的脚步声组件

    // 单例模式
    public static BackgroundMusicManager Instance { get; private set; }

    void Awake()
    {
        // 单例初始化
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 跨场景不销毁
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // 创建音频源
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.clip = bgmClip;
        audioSource.loop = loop;
        audioSource.playOnAwake = false; // 我们自己控制播放
        audioSource.priority = 256;      // 降低优先级，让脚步声更清晰

        // 设置初始音量
        targetVolume = baseVolume;
        audioSource.volume = 0f; // 初始为0，用于淡入效果

        // 如果设置中启用了声音，则播放
        if (playOnAwake && ShouldPlay())
        {
            Play();
        }
    }

    void Start()
    {
        // 查找鬼和玩家
        FindWatcherAndPlayer();

        // 关联设置管理器
        if (SettingUIManager.Instance != null)
        {
            UpdateVolumeFromSettings();
        }
    }

    void Update()
    {
        // 处理淡入淡出
        if (isFading && audioSource.isPlaying)
        {
            audioSource.volume = Mathf.MoveTowards(audioSource.volume, targetVolume, fadeSpeed * Time.deltaTime);

            if (Mathf.Abs(audioSource.volume - targetVolume) < 0.01f)
            {
                isFading = false;
                audioSource.volume = targetVolume;

                // 如果淡出结束且目标音量为0，则停止播放
                if (targetVolume <= 0f && audioSource.volume <= 0f)
                {
                    audioSource.Stop();
                }
            }
        }

        // 动态音量调节
        if (enableDynamicVolume && watcher != null && player != null && audioSource.isPlaying)
        {
            UpdateDynamicVolume();
        }
    }

    // 查找鬼和玩家
    void FindWatcherAndPlayer()
    {
        // 查找鬼
        GameObject watcherObj = GameObject.FindGameObjectWithTag("Watcher");
        if (watcherObj != null)
        {
            watcher = watcherObj.GetComponent<WatcherAI>();
            watcherFootsteps = watcherObj.GetComponent<WatcherFootsteps>();
        }

        // 查找玩家
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }

        // 如果没有找到，尝试其他查找方式
        if (watcher == null)
        {
            watcher = FindObjectOfType<WatcherAI>();
            if (watcher != null)
            {
                watcherFootsteps = watcher.GetComponent<WatcherFootsteps>();
            }
        }

        if (player == null)
        {
            GameObject foundPlayer = GameObject.Find("Player");
            if (foundPlayer != null) player = foundPlayer.transform;
        }

        Debug.Log($"背景音乐初始化: 鬼={(watcher != null ? "找到" : "未找到")}, 玩家={(player != null ? "找到" : "未找到")}");
    }

    // 更新动态音量（根据鬼的距离调节）
    void UpdateDynamicVolume()
    {
        if (watcher == null || player == null) return;

        // 计算鬼和玩家的距离
        float distance = Vector3.Distance(watcher.transform.position, player.position);

        // 计算音量调节因子：距离越近，音量越低
        float volumeFactor = 1f;
        if (distance <= watcherCloseDistance)
        {
            // 距离在0到watcherCloseDistance之间，音量从minDynamicVolume到maxDynamicVolume
            float t = Mathf.Clamp01(distance / watcherCloseDistance);
            volumeFactor = Mathf.Lerp(minDynamicVolume / maxDynamicVolume, 1f, t);
        }

        // 平滑过渡到新的音量因子
        currentDynamicVolume = Mathf.Lerp(currentDynamicVolume, volumeFactor, Time.deltaTime / volumeSmoothTime);

        // 获取鬼的脚步声音量（如果脚步声很大，进一步降低背景音乐）
        float watcherSoundVolume = GetWatcherSoundVolume();
        float additionalReduction = 1f;

        if (watcherSoundVolume > 0.3f) // 脚步声较大时
        {
            // 脚步声越大，背景音乐降低越多
            float reductionAmount = Mathf.Lerp(0f, 0.5f, (watcherSoundVolume - 0.3f) / 0.7f);
            additionalReduction = 1f - reductionAmount;
        }

        // 应用动态音量
        dynamicVolumeModifier = currentDynamicVolume * additionalReduction;
        ApplyCurrentVolume();
    }

    // 获取鬼的脚步声音量
    float GetWatcherSoundVolume()
    {
        if (watcherFootsteps != null)
        {
            // 直接获取WatcherFootsteps的当前音量
            // 使用反射获取私有字段
            System.Reflection.FieldInfo currentVolumeField = typeof(WatcherFootsteps).GetField(
                "currentVolume",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
            );

            if (currentVolumeField != null)
            {
                return (float)currentVolumeField.GetValue(watcherFootsteps);
            }

            // 如果无法通过反射获取，使用一个估算值
            if (watcherFootsteps.isPlaying)
            {
                // 根据距离估算音量
                float distance = Vector3.Distance(watcher.transform.position, player.position);
                float maxDist = 10f; // WatcherFootsteps的startDistance
                float minDist = 2f;  // WatcherFootsteps的maxVolumeDistance

                if (distance <= minDist) return 0.7f;
                if (distance >= maxDist) return 0f;

                float t = 1f - (distance - minDist) / (maxDist - minDist);
                return Mathf.Lerp(0.1f, 0.7f, t);
            }
        }

        return 0f;
    }

    // 应用当前音量（考虑所有因素）
    void ApplyCurrentVolume()
    {
        if (audioSource == null) return;

        float finalVolume = baseVolume * dynamicVolumeModifier;

        // 考虑主音量设置
        if (useMasterVolume && SettingUIManager.Instance != null)
        {
            float masterVolume = PlayerPrefs.GetFloat("Volume", 1f);
            bool soundOn = PlayerPrefs.GetInt("SoundOn", 1) == 1;
            finalVolume *= (soundOn ? masterVolume : 0f);
        }

        // 确保音量在合理范围内
        finalVolume = Mathf.Clamp(finalVolume, 0f, 1f);

        // 直接设置音量（不经过淡入淡出系统）
        audioSource.volume = finalVolume;
        targetVolume = finalVolume; // 更新目标音量，以便淡入淡出系统知道正确的目标
    }

    // 播放背景音乐
    public void Play()
    {
        if (bgmClip == null)
        {
            Debug.LogWarning("背景音乐剪辑未设置！");
            return;
        }

        if (!audioSource.isPlaying)
        {
            audioSource.Play();
        }

        // 重新查找鬼和玩家（确保引用有效）
        FindWatcherAndPlayer();

        // 淡入效果
        StartFade(baseVolume, fadeInTime);

        Debug.Log("开始播放背景音乐");
    }

    // 停止背景音乐
    public void Stop()
    {
        StartFade(0f, fadeOutTime);
        Debug.Log("停止播放背景音乐（淡出中）");
    }

    // 立即停止（无淡出效果）
    public void StopImmediately()
    {
        audioSource.Stop();
        audioSource.volume = 0f;
        isFading = false;

        Debug.Log("立即停止背景音乐");
    }

    // 暂停背景音乐
    public void Pause()
    {
        if (audioSource.isPlaying)
        {
            audioSource.Pause();
            Debug.Log("暂停背景音乐");
        }
    }

    // 恢复播放
    public void UnPause()
    {
        if (!audioSource.isPlaying && audioSource.time > 0)
        {
            audioSource.UnPause();
            Debug.Log("恢复播放背景音乐");
        }
    }

    // 设置基础音量
    public void SetBaseVolume(float newVolume, bool fade = false, float fadeTime = 1.0f)
    {
        newVolume = Mathf.Clamp01(newVolume);
        baseVolume = newVolume;

        if (useMasterVolume)
        {
            UpdateVolumeFromSettings();
        }
        else
        {
            if (fade)
            {
                StartFade(newVolume, fadeTime);
            }
            else
            {
                ApplyCurrentVolume();
            }
        }
    }

    // 从设置管理器更新音量
    public void UpdateVolumeFromSettings()
    {
        if (SettingUIManager.Instance != null && useMasterVolume)
        {
            ApplyCurrentVolume();
        }
    }

    // 开启/关闭动态音量调节
    public void SetDynamicVolumeEnabled(bool enabled)
    {
        enableDynamicVolume = enabled;
        if (!enabled)
        {
            dynamicVolumeModifier = 1f;
            ApplyCurrentVolume();
        }
        Debug.Log($"动态音量调节: {(enabled ? "启用" : "禁用")}");
    }

    // 设置动态音量范围
    public void SetDynamicVolumeRange(float minVolume, float maxVolume)
    {
        minDynamicVolume = Mathf.Clamp01(minVolume);
        maxDynamicVolume = Mathf.Clamp01(maxVolume);
        baseVolume = maxDynamicVolume; // 更新基础音量为最大音量
        ApplyCurrentVolume();
        Debug.Log($"动态音量范围: {minDynamicVolume:F2} - {maxDynamicVolume:F2}");
    }

    // 设置鬼的接近距离
    public void SetWatcherCloseDistance(float distance)
    {
        watcherCloseDistance = Mathf.Max(1f, distance);
        Debug.Log($"鬼接近距离: {watcherCloseDistance:F1}米");
    }

    // 检查是否应该播放（考虑设置）
    private bool ShouldPlay()
    {
        if (useMasterVolume && SettingUIManager.Instance != null)
        {
            bool soundOn = PlayerPrefs.GetInt("SoundOn", 1) == 1;
            return soundOn;
        }
        return true;
    }

    // 开始淡入淡出
    private void StartFade(float targetVol, float duration)
    {
        targetVolume = targetVol * dynamicVolumeModifier;
        fadeSpeed = Mathf.Abs(audioSource.volume - targetVolume) / Mathf.Max(duration, 0.1f);
        isFading = true;
    }

    // 游戏结束时调用
    public void OnGameOver()
    {
        Stop();
        Debug.Log("游戏结束，背景音乐停止");
    }

    // 游戏重新开始时调用
    public void OnGameRestart()
    {
        if (ShouldPlay())
        {
            Play();
        }
        Debug.Log("游戏重新开始，背景音乐播放");
    }

    // 场景切换时处理
    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 重新查找鬼和玩家
        FindWatcherAndPlayer();

        // 如果新场景中已有音乐管理器，销毁自己
        var existingManager = FindObjectOfType<BackgroundMusicManager>();
        if (existingManager != null && existingManager != this)
        {
            Destroy(gameObject);
        }
    }

    // 获取当前音量信息（供调试用）
    public string GetVolumeInfo()
    {
        return $"音量: {audioSource.volume:F2}\n动态调节: {dynamicVolumeModifier:F2}\n状态: {(audioSource.isPlaying ? "播放中" : "停止")}";
    }

    // 调试信息
    void OnGUI()
    {
        if (SettingUIManager.Instance != null && !SettingUIManager.Instance.IsSettingOpen)
        {
            string dynamicInfo = enableDynamicVolume ?
                $"动态音量: {dynamicVolumeModifier:F2} ({currentDynamicVolume:F2})" : "动态音量: 关闭";

            GUI.Label(new Rect(Screen.width - 250, 150, 250, 30),
                     $"背景音乐: {(audioSource.isPlaying ? "播放中" : "停止")}");
            GUI.Label(new Rect(Screen.width - 250, 170, 250, 30),
                     $"音量: {audioSource.volume:F2} / {baseVolume:F2}");
            GUI.Label(new Rect(Screen.width - 250, 190, 250, 30),
                     dynamicInfo);

            if (watcher != null && player != null)
            {
                float distance = Vector3.Distance(watcher.transform.position, player.position);
                GUI.Label(new Rect(Screen.width - 250, 210, 250, 30),
                         $"鬼距离: {distance:F1}m");
            }
        }
    }

    // 调试用：在Scene视图中显示音量调节范围
    void OnDrawGizmosSelected()
    {
        if (player != null && enableDynamicVolume)
        {
            // 绘制动态音量调节范围
            Gizmos.color = new Color(0.5f, 0.5f, 1f, 0.3f); // 半透明蓝色
            Gizmos.DrawWireSphere(player.position, watcherCloseDistance);

            if (watcher != null)
            {
                // 绘制连线
                float distance = Vector3.Distance(player.position, watcher.transform.position);
                float volumeFactor = distance <= watcherCloseDistance ?
                    Mathf.Lerp(minDynamicVolume / maxDynamicVolume, 1f, distance / watcherCloseDistance) : 1f;

                // 根据音量因子设置线条颜色
                Gizmos.color = Color.Lerp(Color.red, Color.green, volumeFactor);
                Gizmos.DrawLine(player.position, watcher.transform.position);

#if UNITY_EDITOR
                // 显示信息
                Vector3 midPoint = (player.position + watcher.transform.position) / 2;
                UnityEditor.Handles.Label(midPoint,
                    $"距离: {distance:F1}m\n音量因子: {volumeFactor:F2}");
#endif
            }
        }
    }
}