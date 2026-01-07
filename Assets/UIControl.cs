using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class UIControl : MonoBehaviour
{
    // 你已手动拖入的按钮和UI物体
    public Button startBtn;
    public Button settingsBtn;
    public Button exitBtn;

    public GameObject startUIRoot;
    public string gameSceneName = "SampleScene";


    void Start()
    {
        // 清除所有旧的事件监听，避免重复绑定/冲突
        startBtn.onClick.RemoveAllListeners();
        settingsBtn.onClick.RemoveAllListeners();
        exitBtn.onClick.RemoveAllListeners();

        // 重新绑定事件
        BindButtonEvents();
        Debug.Log("按钮事件绑定完成，可点击测试");
    }


    private void BindButtonEvents()
    {
        // 开始游戏按钮
        startBtn.onClick.AddListener(() =>
        {
            Debug.Log("开始游戏按钮被点击！");
            StartGame();
        });

        // 设置按钮
        settingsBtn.onClick.AddListener(() =>
        {
            Debug.Log("设置按钮被点击！");
            OpenSettings();
        });

        // 退出按钮
        exitBtn.onClick.AddListener(() =>
        {
            Debug.Log("退出按钮被点击！");
            ExitGame();
        });
    }


    public void StartGame()
    {
        // 新增：打印日志+改变按钮颜色，肉眼确认点击触发
        Debug.Log("=====开始游戏按钮已触发=====");
        startBtn.image.color = Color.red; // 按钮变红，肉眼能看到

        if (startUIRoot != null)
            startUIRoot.SetActive(false);

        if (IsSceneInBuildSettings(gameSceneName))
        {
            Debug.Log($"开始加载场景：{gameSceneName}");
            SceneManager.LoadScene(gameSceneName);
        }
        else
        {
            Debug.LogError($"场景 {gameSceneName} 不在Build列表！");
        }
    }


    // 检查场景是否在Build列表中（修复版）
    private bool IsSceneInBuildSettings(string sceneName)
    {
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
            string sceneNameInBuild = System.IO.Path.GetFileNameWithoutExtension(scenePath);
            if (sceneNameInBuild == sceneName)
                return true;
        }
        return false;
    }


    private void OpenSettings()
    {
        Debug.Log("设置面板待实现");
    }


    private void ExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}