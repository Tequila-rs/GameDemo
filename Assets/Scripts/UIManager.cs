using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    [Header("GUI样式设置")]
    public GUISkin customGuiSkin;
    public int largeFontSize = 24;
    public int hintFontSize = 16;
    public int victoryFontSize = 48;
    public int gameOverFontSize = 48;
    public Color healthColor = Color.red;
    public Color chargeColor = Color.green;
    public Color noChargeColor = Color.red;
    public Color coolDownColor = Color.blue;
    public Color coolDownBgColor = Color.gray;
    public Color readyColor = Color.green;
    public Color hintColor = Color.yellow;
    public Color victoryColor = new Color(1f, 0.8f, 0f, 1f); // 金色
    public Color gameOverColor = new Color(1f, 0.2f, 0.2f, 1f); // 红色
    public Color restartButtonColor = new Color(0.2f, 0.6f, 1f, 1f); // 蓝色
    public Color quitButtonColor = new Color(1f, 0.3f, 0.3f, 1f); // 红色

    [Header("特效UI")]
    public Color speedBoostColor = Color.cyan;
    public Color healColor = Color.green;
    public Color lookBackChargeColor = Color.yellow;
    public float effectTipShowTime = 2f;
    public float speedTipPosX = Screen.width / 2 - 150;
    public float speedTipPosY = 50;

    // UI样式
    private GUIStyle largeStyle;
    private GUIStyle hintStyle;
    private GUIStyle victoryStyle;
    private GUIStyle gameOverStyle;
    private GUIStyle buttonStyle;

    // 特效提示
    private string currentEffectTip = "";
    private float effectTipShowTimer = 0f;
    private bool isSpeedBoostActive = false;
    private float speedBoostRemainingTime = 0f;
    private float speedBoostTotalTime = 0f;

    // 游戏状态界面
    private bool showVictoryScreen = false;
    private bool showGameOverScreen = false;
    private float screenAlpha = 0f;
    private float fadeSpeed = 2f;
    private string currentScreenText = "";
    private string currentSubtitleText = "";
    private Color currentScreenColor;

    // 单例
    public static UIManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
        InitStyles();
    }

    private void InitStyles()
    {
        largeStyle = new GUIStyle();
        if (customGuiSkin != null && customGuiSkin.label != null)
        {
            largeStyle = new GUIStyle(customGuiSkin.label);
        }
        largeStyle.fontSize = largeFontSize;
        largeStyle.fontStyle = FontStyle.Bold;
        largeStyle.alignment = TextAnchor.MiddleLeft;
        largeStyle.padding = new RectOffset(10, 10, 5, 5);
        largeStyle.wordWrap = false;

        hintStyle = new GUIStyle();
        if (customGuiSkin != null && customGuiSkin.label != null)
        {
            hintStyle = new GUIStyle(customGuiSkin.label);
        }
        hintStyle.fontSize = hintFontSize;
        hintStyle.normal.textColor = hintColor;
        hintStyle.alignment = TextAnchor.UpperLeft;
        hintStyle.wordWrap = true;
        hintStyle.padding = new RectOffset(10, 10, 5, 5);

        // 胜利样式
        victoryStyle = new GUIStyle();
        if (customGuiSkin != null && customGuiSkin.label != null)
        {
            victoryStyle = new GUIStyle(customGuiSkin.label);
        }
        victoryStyle.fontSize = victoryFontSize;
        victoryStyle.fontStyle = FontStyle.Bold;
        victoryStyle.alignment = TextAnchor.MiddleCenter;
        victoryStyle.normal.textColor = victoryColor;

        // 失败样式
        gameOverStyle = new GUIStyle();
        if (customGuiSkin != null && customGuiSkin.label != null)
        {
            gameOverStyle = new GUIStyle(customGuiSkin.label);
        }
        gameOverStyle.fontSize = gameOverFontSize;
        gameOverStyle.fontStyle = FontStyle.Bold;
        gameOverStyle.alignment = TextAnchor.MiddleCenter;
        gameOverStyle.normal.textColor = gameOverColor;

        // 按钮样式
        buttonStyle = new GUIStyle();
        if (customGuiSkin != null && customGuiSkin.button != null)
        {
            buttonStyle = new GUIStyle(customGuiSkin.button);
        }
        buttonStyle.fontSize = 28;
        buttonStyle.fontStyle = FontStyle.Bold;
        buttonStyle.alignment = TextAnchor.MiddleCenter;
        buttonStyle.padding = new RectOffset(20, 20, 10, 10);
    }

    private void Update()
    {
        if (effectTipShowTimer > 0)
        {
            effectTipShowTimer -= Time.unscaledDeltaTime;
        }

        // 界面淡入效果
        if ((showVictoryScreen || showGameOverScreen) && screenAlpha < 1f)
        {
            screenAlpha += fadeSpeed * Time.unscaledDeltaTime;
            screenAlpha = Mathf.Clamp01(screenAlpha);
        }

        // 处理界面的R键重启和ESC键退出
        if (showVictoryScreen || showGameOverScreen)
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                RestartGame();
            }
            else if (Input.GetKeyDown(KeyCode.Escape))
            {
                QuitGame();
            }
        }
    }

    private void OnGUI()
    {
        if (customGuiSkin != null)
        {
            GUI.skin = customGuiSkin;
        }

        // 关键修改：访问公共属性 IsSettingOpen（首字母大写）
        if (SettingUIManager.Instance != null && !SettingUIManager.Instance.IsSettingOpen)
        {
            DrawHealthUI();
            DrawLookBackChargeUI();
            DrawCoolDownUI();
            DrawControlTipsUI();
            DrawEffectTipUI();
            DrawSpeedBoostCountdownUI();
        }

        // 绘制胜利界面（覆盖在其他UI之上）
        if (showVictoryScreen)
        {
            DrawGameStateScreen("恭喜！顺利通关！", "你成功逃脱了追捕！", victoryColor);
        }

        // 绘制失败界面（覆盖在其他UI之上）
        if (showGameOverScreen)
        {
            DrawGameStateScreen("游戏失败", "你被追击者抓住了！", gameOverColor);
        }
    }

    #region 基础UI绘制
    private int currentHealth;
    private int maxHealth;
    public void UpdateHealthUI(int current, int max)
    {
        currentHealth = current;
        maxHealth = max;
    }
    private void DrawHealthUI()
    {
        largeStyle.normal.textColor = healthColor;
        //GUI.Label(new Rect(20, 30, 300, 40), $"生命值: {currentHealth}/{maxHealth}", largeStyle);
    }

    private int currentLookBackCharges;
    private int maxLookBackCharges;
    public void UpdateLookBackChargeUI(int current, int max)
    {
        currentLookBackCharges = current;
        maxLookBackCharges = max;
    }
    private void DrawLookBackChargeUI()
    {
        largeStyle.normal.textColor = currentLookBackCharges > 0 ? chargeColor : noChargeColor;
        GUI.Label(new Rect(20, 80, 300, 40), $"回头次数: {currentLookBackCharges}/{maxLookBackCharges}", largeStyle);
    }

    private bool isOnCooldown;
    private float currentCooldownTime;
    private float totalCooldownTime;
    public void UpdateCoolDownUI(bool isCooling, float currentTime, float totalTime)
    {
        isOnCooldown = isCooling;
        currentCooldownTime = currentTime;
        totalCooldownTime = totalTime;
    }
    private void DrawCoolDownUI()
    {
        if (isOnCooldown)
        {
            Rect bgRect = new Rect(20, 130, 250, 30);
            GUI.backgroundColor = coolDownBgColor;
            GUI.Box(bgRect, GUIContent.none);

            float progress = 1 - (currentCooldownTime / totalCooldownTime);
            Rect fillRect = new Rect(20, 130, 250 * progress, 30);
            GUI.backgroundColor = coolDownColor;
            GUI.Box(fillRect, GUIContent.none);

            largeStyle.normal.textColor = Color.white;
            GUI.Label(bgRect, $"冷却中: {currentCooldownTime:F1}s", largeStyle);
        }
        else
        {
            largeStyle.normal.textColor = readyColor;
            GUI.Label(new Rect(20, 130, 300, 40), "冷却完成", largeStyle);
        }
    }

    private void DrawControlTipsUI()
    {
        // 新增：提示Ctrl键打开设置
        string tips = "操作说明:\n" +
                     "自动前进 | A/D: 转向 | 空格: 回头\n" +
                     "V: 切换视角 | 拾取药水触发特效\n" +
                     "Ctrl: 打开/关闭设置面板"; // 新增Ctrl按键提示
        //GUI.Label(new Rect(20, Screen.height - 140, 300, 120), tips, hintStyle);
    }
    #endregion

    #region 特效UI绘制
    public void ShowEffectTip(string tip, Color color)
    {
        currentEffectTip = tip;
        effectTipShowTimer = effectTipShowTime;
        largeStyle.normal.textColor = color;
    }
    private void DrawEffectTipUI()
    {
        if (effectTipShowTimer > 0 && !string.IsNullOrEmpty(currentEffectTip))
        {
            Rect tipRect = new Rect(speedTipPosX, speedTipPosY, 300, 40);
            GUI.Label(tipRect, currentEffectTip, largeStyle);
            effectTipShowTimer -= Time.deltaTime;
        }
    }

    public void UpdateSpeedBoostUI(bool isActive, float remainingTime, float totalTime)
    {
        isSpeedBoostActive = isActive;
        speedBoostRemainingTime = remainingTime;
        speedBoostTotalTime = totalTime;
    }
    private void DrawSpeedBoostCountdownUI()
    {
        if (isSpeedBoostActive)
        {
            Rect bgRect = new Rect(Screen.width - 270, 30, 250, 30);
            GUI.backgroundColor = coolDownBgColor;
            GUI.Box(bgRect, GUIContent.none);

            float progress = speedBoostRemainingTime / speedBoostTotalTime;
            Rect fillRect = new Rect(Screen.width - 270, 30, 250 * progress, 30);
            GUI.backgroundColor = speedBoostColor;
            GUI.Box(fillRect, GUIContent.none);

            largeStyle.normal.textColor = Color.white;
            GUI.Label(bgRect, $"加速持续: {speedBoostRemainingTime:F1}s", largeStyle);
        }
    }
    #endregion

    #region 游戏状态界面
    public void ShowVictoryScreen()
    {
        showVictoryScreen = true;
        showGameOverScreen = false;
        screenAlpha = 0f; // 从0开始淡入
    }

    public void ShowGameOverScreen()
    {
        showGameOverScreen = true;
        showVictoryScreen = false;
        screenAlpha = 0f; // 从0开始淡入
    }

    private void DrawGameStateScreen(string title, string subtitle, Color titleColor)
    {
        // 设置透明度
        GUI.color = new Color(1, 1, 1, screenAlpha);

        // 绘制半透明黑色背景
        Texture2D blackTexture = new Texture2D(1, 1);
        blackTexture.SetPixel(0, 0, new Color(0, 0, 0, 0.8f));
        blackTexture.Apply();

        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), blackTexture);

        // 标题文字
        GUIStyle titleStyle = showVictoryScreen ? victoryStyle : gameOverStyle;
        titleStyle.normal.textColor = Color.Lerp(new Color(titleColor.r, titleColor.g, titleColor.b, 0),
                                                   titleColor, screenAlpha);

        Vector2 textSize = titleStyle.CalcSize(new GUIContent(title));
        Rect textRect = new Rect(Screen.width / 2 - textSize.x / 2,
                                Screen.height / 2 - 150,
                                textSize.x, textSize.y);

        GUI.Label(textRect, title, titleStyle);

        // 副标题
        string subtitleText = subtitle;
        GUIStyle subtitleStyle = new GUIStyle(titleStyle);
        subtitleStyle.fontSize = 32;
        subtitleStyle.normal.textColor = Color.Lerp(new Color(1, 1, 1, 0), Color.white, screenAlpha);

        Vector2 subtitleSize = subtitleStyle.CalcSize(new GUIContent(subtitleText));
        Rect subtitleRect = new Rect(Screen.width / 2 - subtitleSize.x / 2,
                                    textRect.y + textSize.y + 20,
                                    subtitleSize.x, subtitleSize.y);

        GUI.Label(subtitleRect, subtitleText, subtitleStyle);

        // 按钮组
        float buttonY = subtitleRect.y + subtitleSize.y + 80;
        float buttonWidth = 200;
        float buttonHeight = 60;
        float buttonSpacing = 30;

        // 重启按钮
        buttonStyle.normal.textColor = Color.Lerp(new Color(1, 1, 1, 0), Color.white, screenAlpha);
        GUI.backgroundColor = restartButtonColor * screenAlpha;

        Rect restartButtonRect = new Rect(Screen.width / 2 - buttonWidth - buttonSpacing / 2,
                                         buttonY,
                                         buttonWidth, buttonHeight);

        if (GUI.Button(restartButtonRect, "重新开始 (R)", buttonStyle))
        {
            RestartGame();
        }

        // 退出按钮
        GUI.backgroundColor = quitButtonColor * screenAlpha;

        Rect quitButtonRect = new Rect(Screen.width / 2 + buttonSpacing / 2,
                                      buttonY,
                                      buttonWidth, buttonHeight);

        if (GUI.Button(quitButtonRect, "退出游戏 (ESC)", buttonStyle))
        {
            QuitGame();
        }

        // 操作提示
        string hint = "提示：按 R 键重新开始游戏，按 ESC 键退出游戏";
        GUIStyle hintStyle = new GUIStyle();
        hintStyle.fontSize = 20;
        hintStyle.normal.textColor = Color.Lerp(new Color(1, 1, 1, 0), new Color(1, 1, 1, 0.7f), screenAlpha);
        hintStyle.alignment = TextAnchor.MiddleCenter;

        Vector2 hintSize = hintStyle.CalcSize(new GUIContent(hint));
        Rect hintRect = new Rect(Screen.width / 2 - hintSize.x / 2,
                                buttonY + buttonHeight + 30,
                                hintSize.x, hintSize.y);

        GUI.Label(hintRect, hint, hintStyle);

        // 重置颜色
        GUI.color = Color.white;
        GUI.backgroundColor = Color.white;
    }

    private void RestartGame()
    {
        // 重置时间
        Time.timeScale = 1;

        // 隐藏所有游戏状态界面
        showVictoryScreen = false;
        showGameOverScreen = false;
        screenAlpha = 0f;

        // 重置玩家状态
        PlayerController player = FindObjectOfType<PlayerController>();
        if (player != null)
        {
            player.ResetGameState();
        }

        // 重置Watcher状态
        WatcherAI watcher = FindObjectOfType<WatcherAI>();
        if (watcher != null)
        {
            watcher.RestartGame();
        }

        // 重置ObstacleCollision状态
        ObstacleCollision obstacleCollision = FindObjectOfType<ObstacleCollision>();
        if (obstacleCollision != null)
        {
            obstacleCollision.RestartGame();
        }

        Debug.Log("游戏已重新开始");
    }

    private void QuitGame()
    {
        // 在编辑器中停止运行，在构建版本中退出应用
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
    }
    #endregion
}