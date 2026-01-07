using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class StartUIController : MonoBehaviour
{
    public Button startBtn;
    // 新增：指定游戏场景名（和Build Settings里的一致！）
    public string gameSceneName = "SampleScene";

    void Start()
    {
        // 核心：强制显示鼠标，避免点击后消失
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        startBtn.onClick.AddListener(() =>
        {
            Debug.Log("=====按钮已点击，开始加载场景====="); // 打印日志验证
            // 加异常捕获，看加载失败原因
            try
            {
                SceneManager.LoadScene(gameSceneName);
                Debug.Log($"场景 {gameSceneName} 加载指令已发送");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"加载失败：{e.Message}");
            }
        });
    }
}