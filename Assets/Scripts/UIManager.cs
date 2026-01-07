using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    [Header("GUI样式设置")]
    public GUISkin customGuiSkin;
    public int largeFontSize = 24;
    public int hintFontSize = 16;
    public Color healthColor = Color.red;
    public Color chargeColor = Color.green;
    public Color noChargeColor = Color.red;
    public Color coolDownColor = Color.blue;
    public Color coolDownBgColor = Color.gray;
    public Color readyColor = Color.green;
    public Color hintColor = Color.yellow;

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

    // 特效提示
    private string currentEffectTip = "";
    private float effectTipShowTimer = 0f;
    private bool isSpeedBoostActive = false;
    private float speedBoostRemainingTime = 0f;
    private float speedBoostTotalTime = 0f;

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

    private void Update()
    {
        if (effectTipShowTimer > 0)
        {
            effectTipShowTimer -= Time.deltaTime;
        }
    }
}