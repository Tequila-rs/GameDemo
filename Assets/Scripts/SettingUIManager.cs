using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SettingUIManager : MonoBehaviour
{
    [Header("UI组件")]
    public GameObject settingPanel; // 设置面板根节点
    public Button openSettingBtn;   // 打开设置按钮（游戏中显示在左上角）
    public Button resumeBtn;        // 继续按钮
    public Button exitBtn;          // 退出按钮
    public Toggle soundToggle;      // 声音开关
    public Slider volumeSlider;     // 音量滑块

    [Header("音效设置")]
    public AudioSource bgmSource;   // 背景音乐源（可选）

    // 新增：对外暴露的只读属性（供UIManager访问）
    public bool IsSettingOpen => isSettingOpen;
    private bool isSettingOpen = false;
    private float originalTimeScale; // 原始时间缩放
    private static SettingUIManager instance;
    private PlayerController playerController; // 玩家控制器引用
    // 新增：按键防抖标记（避免长按重复触发）
    private bool isCtrlPressed = false;

    // 单例模式
    public static SettingUIManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<SettingUIManager>();
            }
            return instance;
        }
    }

    void Awake()
    {
        // 单例初始化
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);

        // 获取玩家控制器
        playerController = FindObjectOfType<PlayerController>();
    }

    void Start()
    {
        // 初始化UI状态，确保按钮默认显示
        if (settingPanel != null)
            settingPanel.SetActive(false);
        if (openSettingBtn != null)
            openSettingBtn.gameObject.SetActive(true); // 强制显示打开按钮

        // 绑定按钮事件
        if (openSettingBtn != null)
            openSettingBtn.onClick.AddListener(OpenSettingPanel);
        if (resumeBtn != null)
            resumeBtn.onClick.AddListener(CloseSettingPanel);
        if (exitBtn != null)
            exitBtn.onClick.AddListener(ExitGame);

        // 绑定音效事件
        if (soundToggle != null)
            soundToggle.onValueChanged.AddListener(OnSoundToggle);
        if (volumeSlider != null)
            volumeSlider.onValueChanged.AddListener(OnVolumeChange);

        // 初始化音效状态（从本地存档读取）
        if (soundToggle != null)
            soundToggle.isOn = PlayerPrefs.GetInt("SoundOn", 1) == 1;
        if (volumeSlider != null)
            volumeSlider.value = PlayerPrefs.GetFloat("Volume", 1f);

        OnSoundToggle(soundToggle.isOn);
        OnVolumeChange(volumeSlider.value);
    }

    // 新增：每帧检测Ctrl按键
    void Update()
    {
        CheckCtrlKeyInput();
    }

    /// <summary>
    /// 检测Ctrl按键，触发设置面板的打开/关闭
    /// </summary>
    private void CheckCtrlKeyInput()
    {
        // 检测左Ctrl或右Ctrl按键按下（按下瞬间触发，避免长按重复）
        if (Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.RightControl))
        {
            if (!isCtrlPressed)
            {
                isCtrlPressed = true;
                // 根据当前状态切换面板：打开→关闭，关闭→打开
                if (isSettingOpen)
                {
                    CloseSettingPanel();
                }
                else
                {
                    OpenSettingPanel();
                }
            }
        }

        // 按键抬起时重置防抖标记
        if (Input.GetKeyUp(KeyCode.LeftControl) || Input.GetKeyUp(KeyCode.RightControl))
        {
            isCtrlPressed = false;
        }
    }

    /// <summary>
    /// 打开设置面板（暂停游戏）
    /// </summary>
    void OpenSettingPanel()
    {
        if (!isSettingOpen)
        {
            // 暂停游戏
            originalTimeScale = Time.timeScale;
            Time.timeScale = 0;

            // 禁用玩家移动
            if (playerController != null)
                playerController.SetMovementEnabled(false);

            // 显示设置面板，隐藏打开按钮
            settingPanel.SetActive(true);
            openSettingBtn.gameObject.SetActive(false);

            isSettingOpen = true;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }

    /// <summary>
    /// 关闭设置面板（恢复游戏）
    /// </summary>
    void CloseSettingPanel()
    {
        if (isSettingOpen)
        {
            // 恢复游戏时间
            Time.timeScale = originalTimeScale;

            // 启用玩家移动
            if (playerController != null)
                playerController.SetMovementEnabled(true);

            // 隐藏设置面板，显示打开按钮
            settingPanel.SetActive(false);
            openSettingBtn.gameObject.SetActive(true);

            isSettingOpen = false;
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    /// <summary>
    /// 退出游戏
    /// </summary>
    void ExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    /// <summary>
    /// 音效开关切换
    /// </summary>
    void OnSoundToggle(bool isOn)
    {
        PlayerPrefs.SetInt("SoundOn", isOn ? 1 : 0);
        if (bgmSource != null)
        {
            bgmSource.mute = !isOn;
        }
        // 全局音效开关（可选）
        AudioListener.pause = !isOn;
    }

    /// <summary>
    /// 音量调整
    /// </summary>
    void OnVolumeChange(float value)
    {
        PlayerPrefs.SetFloat("Volume", value);
        AudioListener.volume = value;
    }

    /// <summary>
    /// 防止暂停时退出游戏导致时间缩放异常
    /// </summary>
    void OnDestroy()
    {
        Time.timeScale = 1;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    /// <summary>
    /// 强制关闭设置面板（恢复游戏状态）
    /// </summary>
    public void ForceCloseSetting()
    {
        if (isSettingOpen)
        {
            CloseSettingPanel();
        }
    }
}