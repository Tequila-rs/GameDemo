using UnityEngine;
using UnityEngine.UI;

// 无需URP，直接挂载主相机
public class ProximityWarning : MonoBehaviour
{
    [Header("警告样式设置")]
    public Color edgeColor = new Color(0.8f, 0.05f, 0.05f, 0.9f);
    public float maxFogSize = 0.4f;
    public float maxDarknessIntensity = 0.5f; // 中心暗角强度（用UI半透明黑实现）
    public float maxWarningDistance = 10f;
    public float minWarningDistance = 2f;
    public float transitionSmoothness = 2f;

    [Header("引用")]
    public Transform monster;
    public Transform player;

    // 纯UI组件
    private Image edgeFog;   // 边缘红雾
    private Image darkVignette; // 中心暗角（用半透明黑Image实现）
    private Canvas warningCanvas;

    private float currentIntensity = 0f;
    private float targetIntensity = 0f;

    void Awake()
    {
        CreateEdgeFogAndVignette();
        AutoFindTargets();
    }

    void Update()
    {
        if (player == null || monster == null)
        {
            targetIntensity = 0f;
            UpdateWarningEffect();
            return;
        }

        float distance = Vector3.Distance(player.position, monster.position);
        targetIntensity = Mathf.InverseLerp(maxWarningDistance, minWarningDistance, distance);
        targetIntensity = Mathf.Clamp01(targetIntensity);
        UpdateWarningEffect();
    }

    private void UpdateWarningEffect()
    {
        currentIntensity = Mathf.Lerp(currentIntensity, targetIntensity, Time.deltaTime * transitionSmoothness);

        // 边缘红雾
        RectTransform fogRt = edgeFog.GetComponent<RectTransform>();
        float fogSize = maxFogSize * currentIntensity;
        fogRt.offsetMin = new Vector2(fogSize * Screen.width, fogSize * Screen.height);
        fogRt.offsetMax = new Vector2(-fogSize * Screen.width, -fogSize * Screen.height);
        edgeFog.color = new Color(edgeColor.r, edgeColor.g, edgeColor.b, edgeColor.a * currentIntensity);

        // 中心暗角（用半透明黑UI实现）
        darkVignette.color = new Color(0, 0, 0, maxDarknessIntensity * currentIntensity);

        edgeFog.enabled = currentIntensity > 0.01f;
        darkVignette.enabled = currentIntensity > 0.01f;
    }

    // 创建红雾+暗角UI（纯UI，无URP依赖）
    private void CreateEdgeFogAndVignette()
    {
        GameObject canvasObj = new GameObject("WarningCanvas");
        canvasObj.transform.SetParent(transform);
        warningCanvas = canvasObj.AddComponent<Canvas>();
        warningCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        warningCanvas.sortingOrder = 999;

        // 1. 边缘红雾
        GameObject fogObj = new GameObject("EdgeFog");
        fogObj.transform.SetParent(warningCanvas.transform);
        edgeFog = fogObj.AddComponent<Image>();
        edgeFog.type = Image.Type.Sliced;
        edgeFog.raycastTarget = false;
        edgeFog.sprite = CreateFogTexture();
        RectTransform fogRt = fogObj.GetComponent<RectTransform>();
        fogRt.anchorMin = Vector2.zero;
        fogRt.anchorMax = Vector2.one;
        fogRt.offsetMin = Vector2.zero;
        fogRt.offsetMax = Vector2.zero;

        // 2. 中心暗角（全屏半透明黑，中间渐变透明）
        GameObject vignetteObj = new GameObject("DarkVignette");
        vignetteObj.transform.SetParent(warningCanvas.transform);
        darkVignette = vignetteObj.AddComponent<Image>();
        darkVignette.type = Image.Type.Sliced;
        darkVignette.raycastTarget = false;
        darkVignette.sprite = CreateVignetteTexture();
        RectTransform vignetteRt = vignetteObj.GetComponent<RectTransform>();
        vignetteRt.anchorMin = Vector2.zero;
        vignetteRt.anchorMax = Vector2.one;
        vignetteRt.offsetMin = Vector2.zero;
        vignetteRt.offsetMax = Vector2.zero;
    }

    // 生成红雾纹理
    private Sprite CreateFogTexture()
    {
        int size = 256;
        Texture2D tex = new Texture2D(size, size, TextureFormat.ARGB32, false);
        tex.filterMode = FilterMode.Bilinear;
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                Vector2 center = new Vector2(size / 2f, size / 2f);
                float distanceToCenter = Vector2.Distance(new Vector2(x, y), center) / (size / 2f);
                float alpha = Mathf.Clamp01((distanceToCenter - 0.8f) * 5f);
                tex.SetPixel(x, y, new Color(edgeColor.r, edgeColor.g, edgeColor.b, alpha));
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), Vector2.one * 0.5f);
    }

    // 生成中心暗角纹理（中间透明，边缘黑）
    private Sprite CreateVignetteTexture()
    {
        int size = 256;
        Texture2D tex = new Texture2D(size, size, TextureFormat.ARGB32, false);
        tex.filterMode = FilterMode.Bilinear;
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                Vector2 center = new Vector2(size / 2f, size / 2f);
                float distanceToCenter = Vector2.Distance(new Vector2(x, y), center) / (size / 2f);
                float alpha = Mathf.Clamp01(distanceToCenter * 0.8f); // 边缘黑，中心透
                tex.SetPixel(x, y, new Color(0, 0, 0, alpha));
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), Vector2.one * 0.5f);
    }

    private void AutoFindTargets()
    {
        if (player == null) player = GameObject.FindWithTag("Player")?.transform;
        if (monster == null) monster = GameObject.FindWithTag("Monster")?.transform;
    }

    void OnDestroy()
    {
        if (warningCanvas != null) Destroy(warningCanvas.gameObject);
    }
}