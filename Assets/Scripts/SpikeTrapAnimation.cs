using UnityEngine;
using System.Collections.Generic;

public class SpikeTrapAnimation : MonoBehaviour
{
    [Header("动画设置")]
    public List<Transform> spikeMeshes = new List<Transform>(); // 所有刺的列表
    public float hideSpeed = 3.0f;
    public float showSpeed = 2.0f;
    public float hideDistance = 0.3f;

    [Header("状态")]
    public bool startHidden = false;
    public bool isHidden = false;

    private List<Vector3> shownPositions = new List<Vector3>();
    private List<Vector3> hiddenPositions = new List<Vector3>();

    void Start()
    {
        // 如果没有指定刺，自动查找所有spike_开头的物体
        if (spikeMeshes.Count == 0)
        {
            FindAllSpikes();
        }

        if (spikeMeshes.Count > 0)
        {
            // 初始化所有刺的位置
            foreach (Transform spike in spikeMeshes)
            {
                if (spike != null)
                {
                    shownPositions.Add(spike.localPosition);
                    hiddenPositions.Add(spike.localPosition - new Vector3(0, hideDistance, 0));
                }
            }

            // 设置初始状态
            isHidden = startHidden;
            SetInitialPosition();
        }
        else
        {
            Debug.LogError("没有找到任何刺的网格！");
        }
    }

    void Update()
    {
        if (spikeMeshes.Count == 0) return;

        // 为所有刺执行平滑动画
        for (int i = 0; i < spikeMeshes.Count; i++)
        {
            if (spikeMeshes[i] != null)
            {
                Vector3 targetPosition = isHidden ? hiddenPositions[i] : shownPositions[i];
                float currentSpeed = isHidden ? hideSpeed : showSpeed;

                spikeMeshes[i].localPosition = Vector3.Lerp(
                    spikeMeshes[i].localPosition,
                    targetPosition,
                    Time.deltaTime * currentSpeed
                );
            }
        }

        // 测试用：按空格键切换状态
        if (Input.GetKeyDown(KeyCode.Space))
        {
            ToggleSpike();
        }
    }

    // 自动查找所有刺
    private void FindAllSpikes()
    {
        foreach (Transform child in GetComponentsInChildren<Transform>())
        {
            if (child.name.StartsWith("spike_"))
            {
                spikeMeshes.Add(child);
                Debug.Log("找到刺: " + child.name);
            }
        }
    }

    // 设置初始位置
    private void SetInitialPosition()
    {
        for (int i = 0; i < spikeMeshes.Count; i++)
        {
            if (spikeMeshes[i] != null)
            {
                spikeMeshes[i].localPosition = isHidden ? hiddenPositions[i] : shownPositions[i];
            }
        }
    }

    public void ToggleSpike()
    {
        isHidden = !isHidden;
        Debug.Log("刺陷阱状态: " + (isHidden ? "隐藏" : "显示"));
    }

    public void HideSpike()
    {
        isHidden = true;
    }

    public void ShowSpike()
    {
        isHidden = false;
    }
}