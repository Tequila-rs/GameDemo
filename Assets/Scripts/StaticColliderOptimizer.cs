using UnityEngine;
using System.Collections.Generic;

public class StaticColliderOptimizer : MonoBehaviour
{
    [Header("合并碰撞器设置")]
    public bool combineColliders = true; // 合并多个小碰撞器为大碰撞器
    public float combineDistance = 2f; // 合并距离阈值
    public bool generateNavMeshObstacle = false; // 为AI导航生成障碍物

    [Header("碰撞器类型")]
    public ColliderGenerationMode generationMode = ColliderGenerationMode.Adaptive;

    private List<GameObject> colliderHolders = new List<GameObject>();

    public enum ColliderGenerationMode
    {
        Box,        // 所有用BoxCollider
        Mesh,       // 所有用MeshCollider
        Adaptive    // 根据形状自动选择
    }

    void Start()
    {
        GenerateOptimizedColliders();
    }

    [ContextMenu("生成优化碰撞器")]
    public void GenerateOptimizedColliders()
    {
        // 清理旧的碰撞器容器
        CleanupOldColliders();

        if (combineColliders)
        {
            GenerateCombinedColliders();
        }
        else
        {
            GenerateIndividualColliders();
        }

        if (generateNavMeshObstacle)
        {
            AddNavMeshObstacles();
        }
    }

    private void GenerateIndividualColliders()
    {
        MeshFilter[] meshFilters = GetComponentsInChildren<MeshFilter>();

        foreach (MeshFilter filter in meshFilters)
        {
            if (filter.sharedMesh == null) continue;

            GameObject obj = filter.gameObject;

            // 跳过已经有碰撞器的物体
            if (obj.GetComponent<Collider>() != null) continue;

            Collider collider = null;

            switch (generationMode)
            {
                case ColliderGenerationMode.Box:
                    collider = obj.AddComponent<BoxCollider>();
                    break;

                case ColliderGenerationMode.Mesh:
                    MeshCollider meshCollider = obj.AddComponent<MeshCollider>();
                    meshCollider.convex = false; // 静态物体用非凸体
                    collider = meshCollider;
                    break;

                case ColliderGenerationMode.Adaptive:
                    collider = AddAdaptiveCollider(obj);
                    break;
            }

            if (collider != null)
            {
                collider.isTrigger = false;
                colliderHolders.Add(obj);
            }
        }
    }

    private void GenerateCombinedColliders()
    {
        // 收集所有网格
        List<MeshFilter> meshFilters = new List<MeshFilter>(GetComponentsInChildren<MeshFilter>());

        // 按距离分组
        List<List<MeshFilter>> groups = new List<List<MeshFilter>>();

        while (meshFilters.Count > 0)
        {
            MeshFilter seed = meshFilters[0];
            List<MeshFilter> group = new List<MeshFilter> { seed };
            meshFilters.RemoveAt(0);

            // 查找附近的网格
            for (int i = meshFilters.Count - 1; i >= 0; i--)
            {
                MeshFilter other = meshFilters[i];
                float distance = Vector3.Distance(seed.transform.position, other.transform.position);

                if (distance <= combineDistance)
                {
                    group.Add(other);
                    meshFilters.RemoveAt(i);
                }
            }

            groups.Add(group);
        }

        // 为每个组创建合并的碰撞器
        foreach (List<MeshFilter> group in groups)
        {
            if (group.Count == 0) continue;

            // 创建容器对象
            GameObject colliderHolder = new GameObject($"CombinedCollider_Group_{colliderHolders.Count}");
            colliderHolder.transform.SetParent(transform);
            colliderHolder.transform.position = CalculateGroupCenter(group);

            // 计算合并的边界
            Bounds combinedBounds = new Bounds();
            bool first = true;

            foreach (MeshFilter filter in group)
            {
                Renderer renderer = filter.GetComponent<Renderer>();
                if (renderer == null) continue;

                if (first)
                {
                    combinedBounds = renderer.bounds;
                    first = false;
                }
                else
                {
                    combinedBounds.Encapsulate(renderer.bounds);
                }
            }

            // 添加BoxCollider
            BoxCollider boxCollider = colliderHolder.AddComponent<BoxCollider>();
            boxCollider.center = colliderHolder.transform.InverseTransformPoint(combinedBounds.center);
            boxCollider.size = combinedBounds.size;

            colliderHolders.Add(colliderHolder);
        }
    }

    private Collider AddAdaptiveCollider(GameObject obj)
    {
        MeshFilter meshFilter = obj.GetComponent<MeshFilter>();
        if (meshFilter == null || meshFilter.sharedMesh == null)
            return obj.AddComponent<BoxCollider>();

        Mesh mesh = meshFilter.sharedMesh;
        Vector3 size = mesh.bounds.size;

        // 根据形状选择碰撞器类型
        float aspectRatio = Mathf.Max(size.x, size.y, size.z) / Mathf.Min(size.x, size.y, size.z);

        if (aspectRatio > 5f) // 细长物体用胶囊碰撞器
        {
            CapsuleCollider capsule = obj.AddComponent<CapsuleCollider>();
            capsule.height = size.y;
            capsule.radius = Mathf.Max(size.x, size.z) * 0.5f;
            return capsule;
        }
        else if (IsRoundObject(mesh)) // 圆形物体用球体碰撞器
        {
            SphereCollider sphere = obj.AddComponent<SphereCollider>();
            sphere.radius = size.magnitude * 0.5f;
            return sphere;
        }
        else // 其他用BoxCollider
        {
            return obj.AddComponent<BoxCollider>();
        }
    }

    private bool IsRoundObject(Mesh mesh)
    {
        // 简单判断是否接近圆形
        Vector3 size = mesh.bounds.size;
        return Mathf.Abs(size.x - size.z) < 0.1f && Mathf.Abs(size.y - size.x) > 2f;
    }

    private Vector3 CalculateGroupCenter(List<MeshFilter> group)
    {
        Vector3 center = Vector3.zero;
        foreach (MeshFilter filter in group)
        {
            center += filter.transform.position;
        }
        return center / group.Count;
    }

    private void AddNavMeshObstacles()
    {
        foreach (GameObject holder in colliderHolders)
        {
            if (holder.GetComponent<UnityEngine.AI.NavMeshObstacle>() == null)
            {
                var obstacle = holder.AddComponent<UnityEngine.AI.NavMeshObstacle>();
                obstacle.carving = true;
                obstacle.shape = UnityEngine.AI.NavMeshObstacleShape.Box;
            }
        }
    }

    private void CleanupOldColliders()
    {
        foreach (GameObject holder in colliderHolders)
        {
            if (holder != null)
            {
                if (Application.isPlaying)
                    Destroy(holder);
                else
                    DestroyImmediate(holder);
            }
        }
        colliderHolders.Clear();
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0, 1, 1, 0.3f);
        foreach (GameObject holder in colliderHolders)
        {
            if (holder != null)
            {
                Collider col = holder.GetComponent<Collider>();
                if (col != null)
                {
                    if (col is BoxCollider box)
                    {
                        Gizmos.DrawWireCube(holder.transform.TransformPoint(box.center), box.size);
                    }
                }
            }
        }
    }
}