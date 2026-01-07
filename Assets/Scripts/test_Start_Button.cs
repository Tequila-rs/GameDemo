using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class StartUIController : MonoBehaviour
{
    public Button startBtn;         // 开始按钮
    public Button exitBtn;          // 退出按钮
    public Button controlsBtn;      // 新增：Controls按钮
    public GameObject controlsPanel;// 新增：操作说明面板
    public string gameSceneName = "SampleScene"; // 游戏场景名

    void Start()
    {
        // 显示鼠标
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // 初始隐藏操作说明面板
        if (controlsPanel != null)
        {
            controlsPanel.SetActive(false);
        }

        // 按钮事件绑定
        startBtn.onClick.AddListener(OnStartButtonClick);
        exitBtn.onClick.AddListener(OnExitButtonClick);
        controlsBtn.onClick.AddListener(ToggleControlsPanel); // 新增：Controls按钮绑定
    }

    // 开始按钮点击事件
    private void OnStartButtonClick()
    {
        Debug.Log("=====开始按钮已点击，加载场景=====");
        try
        {
            SceneManager.LoadScene(gameSceneName);
            Debug.Log($"场景 {gameSceneName} 加载指令已发送");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"加载失败：{e.Message}");
        }
    }

    // 退出按钮点击事件
    private void OnExitButtonClick()
    {
        Debug.Log("=====退出按钮已点击=====");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // 新增：Controls按钮点击事件（切换操作说明面板显隐）
    private void ToggleControlsPanel()
    {
        if (controlsPanel != null)
        {
            // 切换面板的“显示/隐藏”状态
            controlsPanel.SetActive(!controlsPanel.activeSelf);
            Debug.Log(controlsPanel.activeSelf ? "操作说明面板已显示" : "操作说明面板已隐藏");
        }
        else
        {
            Debug.LogWarning("请先在Inspector中拖入ControlsPanel对象！");
        }
    }
}