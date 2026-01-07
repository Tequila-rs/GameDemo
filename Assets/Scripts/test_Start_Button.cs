using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class StartUIController : MonoBehaviour
{
    public Button startBtn;   // 开始按钮
    public Button exitBtn;    // 新增：退出按钮
    public string gameSceneName = "SampleScene"; // 游戏场景名

    void Start()
    {
        // 显示鼠标
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // 开始按钮绑定场景跳转
        startBtn.onClick.AddListener(OnStartButtonClick);
        // 退出按钮绑定退出逻辑
        exitBtn.onClick.AddListener(OnExitButtonClick);
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

    // 新增：退出按钮点击事件
    private void OnExitButtonClick()
    {
        Debug.Log("=====退出按钮已点击=====");
        // 编辑器中退出播放模式，打包后退出程序
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
    }
}