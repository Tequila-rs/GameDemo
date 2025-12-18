using UnityEngine;
using UnityEngine.UI;

// 无需URP，直接挂载主相机
public class ProximityWarning : MonoBehaviour
{
    [Header("警告样式设置")]
    public Color edgeColor = new Color(0.8f, 0.05f, 0.05f, 0.9f); // 红雾主色（建议A值0.7-0.9）
    public float maxFogExpand = 0.6f; // 贴脸时红雾扩展深度（0.6=边缘向中间扩展60%屏幕宽度）
    public float maxDarknessIntensity = 0.4f; // 中心暗角强度（贴脸时）
    [Header("安全距离设置")]
    public float safeDistance = 8f; // 超过此距离无红雾
    public float minDistance = 1f; // 贴脸临界距离（红雾最强）
    public float transitionSmoothness = 2.5f; // 红雾过渡顺滑度（值越大越缓）
    [Header("红边透明度强化")]
    public float fogAlphaMultiplier = 1.5f; // 红雾透明度倍增系数（1.5=原透明度的1.5倍）

    [Header("引用（拖拽根物体）")]
    public Transform player; // 人物根物体Transform
    public Transform monster; // 怪兽根物体Transform
    public bool disableAutoFind = true; // 关闭自动查找，避免覆盖手动引用

    // 内部组件
    private Image edgeFog; // 边缘红雾UI
    private Image darkVignette; // 中心暗角UI
    private Canvas warningCanvas;

    // 平滑过渡变量
    private float currentIntensity = 0f;
    private float targetIntensity = 0f;
    private float lastDistance = 0f;
    private readonly float distanceSmooth = 0.15f; // 距离防抖动系数

    void Awake()
    {
        // 创建红雾+暗角UI（全屏覆盖）
        CreateWarningUI();
        // 仅未手动赋值时自动查找
        if (!disableAutoFind) AutoFindTargets();
    }

    void Update()
    {
        // 校验引用有效性
        if (!IsReferencesValid())
        {
            currentIntensity = Mathf.Lerp(currentIntensity, 0f, Time.deltaTime * transitionSmoothness);
            UpdateWarningVisual();
            return;
        }

        // 计算人物与怪兽的实时距离（防抖动）
        float realDistance = Vector3.Distance(player.position, monster.position);
        realDistance = Mathf.Lerp(lastDistance, realDistance, distanceSmooth);
        lastDistance = realDistance;

        // 计算红雾强度：
        // 1. 距离>安全距离 → 强度0（无红雾）
        // 2. 安全距离≥距离≥最小距离 → 强度0~1渐变（越近越强）
        // 3. 距离<最小距离 → 强度1（红雾最强）
        targetIntensity = Mathf.InverseLerp(safeDistance, minDistance, realDistance);
        targetIntensity = Mathf.Clamp01(targetIntensity);

        // 平滑更新强度
        currentIntensity = Mathf.Lerp(currentIntensity, targetIntensity, Time.deltaTime * transitionSmoothness);

        // 更新红雾视觉效果
        UpdateWarningVisual();
    }

    // 校验引用是否有效
    private bool IsReferencesValid()
    {
        if (player == null || monster == null) return false;
        if (!player.gameObject.activeInHierarchy || !monster.gameObject.activeInHierarchy) return false;
        return true;
    }

    // 更新红雾+暗角视觉效果（核心修改：提高红边透明度）
    private void UpdateWarningVisual()
    {
        // 强度极低时隐藏所有效果
        if (currentIntensity < 0.01f)
        {
            edgeFog.enabled = false;
            darkVignette.enabled = false;
            return;
        }

        // 1. 边缘红雾：强度越高，从边缘向中间扩展的范围越大
        RectTransform fogRt = edgeFog.GetComponent<RectTransform>();
        // 计算红雾扩展尺寸（基于屏幕宽高，适配所有分辨率）
        float fogExpandX = maxFogExpand * currentIntensity * Screen.width;
        float fogExpandY = maxFogExpand * currentIntensity * Screen.height;
        // 关键修复：offsetMin/offsetMax设置为负数 → 红雾从边缘向中间扩展
        fogRt.offsetMin = new Vector2(-fogExpandX, -fogExpandY);
        fogRt.offsetMax = new Vector2(fogExpandX, fogExpandY);

        // 核心修改：提高红雾透明度（乘以倍增系数，且限制最大不超过1）
        float finalAlpha = Mathf.Clamp(edgeColor.a * currentIntensity * fogAlphaMultiplier, 0f, 1f);
        edgeFog.color = new Color(edgeColor.r, edgeColor.g, edgeColor.b, finalAlpha);

        // 2. 中心暗角：越近越明显（仅中心小范围透明）
        darkVignette.color = new Color(0, 0, 0, maxDarknessIntensity * currentIntensity);

        // 显示效果
        edgeFog.enabled = true;
        darkVignette.enabled = true;
    }

    // 创建全屏红雾+暗角UI（确保边缘红雾样式正确）
    private void CreateWarningUI()
    {
        // 1. 创建全屏画布（最顶层显示）
        GameObject canvasObj = new GameObject("WarningCanvas");
        canvasObj.transform.SetParent(transform);
        warningCanvas = canvasObj.AddComponent<Canvas>();
        warningCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        warningCanvas.sortingOrder = 9999; // 避免被其他UI遮挡

        // 2. 创建边缘红雾UI（核心：中间透明、边缘红的纹理）
        GameObject fogObj = new GameObject("EdgeFog");
        fogObj.transform.SetParent(warningCanvas.transform);
        edgeFog = fogObj.AddComponent<Image>();
        edgeFog.type = Image.Type.Sliced;
        edgeFog.raycastTarget = false; // 不阻挡按钮交互
        edgeFog.sprite = CreateEdgeFogTexture(); // 生成边缘红雾纹理
        // 设置红雾UI为全屏
        RectTransform fogRt = fogObj.GetComponent<RectTransform>();
        fogRt.anchorMin = Vector2.zero;
        fogRt.anchorMax = Vector2.one;
        fogRt.offsetMin = Vector2.zero;
        fogRt.offsetMax = Vector2.zero;

        // 3. 创建中心暗角UI（强化中心透明效果）
        GameObject vignetteObj = new GameObject("CenterVignette");
        vignetteObj.transform.SetParent(warningCanvas.transform);
        darkVignette = vignetteObj.AddComponent<Image>();
        darkVignette.type = Image.Type.Sliced;
        darkVignette.raycastTarget = false;
        darkVignette.sprite = CreateVignetteTexture(); // 生成中心暗角纹理
        // 设置暗角UI为全屏
        RectTransform vignetteRt = vignetteObj.GetComponent<RectTransform>();
        vignetteRt.anchorMin = Vector2.zero;
        vignetteRt.anchorMax = Vector2.one;
        vignetteRt.offsetMin = Vector2.zero;
        vignetteRt.offsetMax = Vector2.zero;
    }

    // 生成边缘红雾纹理（核心：中间完全透明，边缘渐变红）
    private Sprite CreateEdgeFogTexture()
    {
        int texSize = 512; // 更高分辨率，红雾渐变更平滑
        Texture2D fogTex = new Texture2D(texSize, texSize, TextureFormat.ARGB32, false);
        fogTex.filterMode = FilterMode.Bilinear; // 纹理插值更平滑

        for (int x = 0; x < texSize; x++)
        {
            for (int y = 0; y < texSize; y++)
            {
                // 计算像素到纹理中心的距离（归一化0-1）
                Vector2 center = new Vector2(texSize / 2f, texSize / 2f);
                float distanceToCenter = Vector2.Distance(new Vector2(x, y), center) / (texSize / 2f);

                // 纹理逻辑：
                // - 距离<0.5 → 完全透明（中心区域）
                // - 0.5≤距离≤1 → 从透明渐变到红色（边缘区域）
                float alpha = 0f;
                if (distanceToCenter > 0.5f)
                {
                    alpha = Mathf.Lerp(0f, 1f, (distanceToCenter - 0.5f) / 0.5f);
                }
                fogTex.SetPixel(x, y, new Color(edgeColor.r, edgeColor.g, edgeColor.b, alpha));
            }
        }
        fogTex.Apply();
        // 创建Sprite（中心点为纹理中心）
        return Sprite.Create(fogTex, new Rect(0, 0, texSize, texSize), new Vector2(0.5f, 0.5f));
    }

    // 生成中心暗角纹理（中间透明，边缘黑，强化中心可视区域）
    private Sprite CreateVignetteTexture()
    {
        int texSize = 512;
        Texture2D vignetteTex = new Texture2D(texSize, texSize, TextureFormat.ARGB32, false);
        vignetteTex.filterMode = FilterMode.Bilinear;

        for (int x = 0; x < texSize; x++)
        {
            for (int y = 0; y < texSize; y++)
            {
                Vector2 center = new Vector2(texSize / 2f, texSize / 2f);
                float distanceToCenter = Vector2.Distance(new Vector2(x, y), center) / (texSize / 2f);
                // 暗角逻辑：边缘黑，中心透明
                float alpha = Mathf.Clamp01(distanceToCenter * 0.7f);
                vignetteTex.SetPixel(x, y, new Color(0, 0, 0, alpha));
            }
        }
        vignetteTex.Apply();
        return Sprite.Create(vignetteTex, new Rect(0, 0, texSize, texSize), new Vector2(0.5f, 0.5f));
    }

    // 自动查找人物/怪兽（备用）
    private void AutoFindTargets()
    {
        if (player == null) player = GameObject.FindWithTag("Player")?.transform;
        if (monster == null) monster = GameObject.FindWithTag("Monster")?.transform;
    }

    // Scene视图调试（可视化安全距离和物体位置）
    void OnDrawGizmosSelected()
    {
        if (IsReferencesValid())
        {
            // 人物位置（蓝色球）
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(player.position, 0.3f);
            // 怪兽位置（红色球）
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(monster.position, 0.3f);
            // 安全距离（黄色线框）
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(player.position, safeDistance);
            // 贴脸距离（橙色线框）
            Gizmos.color = new Color(1f, 0.5f, 0f);
            Gizmos.DrawWireSphere(player.position, minDistance);
            // 人物-怪兽连线（白色）
            Gizmos.color = Color.white;
            Gizmos.DrawLine(player.position, monster.position);
        }
    }

    // 销毁时清理UI，避免内存泄漏
    void OnDestroy()
    {
        if (warningCanvas != null) Destroy(warningCanvas.gameObject);
    }
}