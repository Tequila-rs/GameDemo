using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WatcherAI : MonoBehaviour
{
    [Header("Chase Settings")]
    public float baseSpeed = 5.0f;
    public float accelerationRate = 0.5f;
    public float maxSpeed = 10.0f;
    public float minDistance = 2f;
    [Header("Player Path Tracking")]
    public float pathRecordInterval = 0.2f; // ��¼���·����ʱ����
    public int maxPathPoints = 100; // ����¼��·���������������ڴ������
    public float pathFollowSmoothTime = 0.1f; // ����·����ƽ��ʱ��
    [Header("Pursuit Settings")]
    public float directPursuitRange = 10f; // ��������㹻��ʱֱ��׷��������·��

    // ��Ϊpublic�Ա������ű�����
    [HideInInspector] public Transform player;
    [HideInInspector] public float currentSpeed;
    [HideInInspector] public bool isHalted = false;

    private Vector3 startPosition;
    private float turnSmoothVelocity;

    // ���·�����
    private List<Vector3> playerPath = new List<Vector3>();
    private int currentPathIndex = 0;
    private float lastRecordTime;

    void Start()
    {
        // �ҵ����
        player = GameObject.FindGameObjectWithTag("Player").transform;
        currentSpeed = baseSpeed;
        startPosition = transform.position;

        // ��ʼ���߶�
        AdjustHeightPosition();

        if (player == null)
        {
            Debug.LogError("Player not found! Make sure Player has 'Player' tag.");
        }

        // ��ʼ��·����¼ʱ��
        lastRecordTime = Time.time;
    }

    void Update()
    {
        if (player == null) return;

        // ������¼���·��
        RecordPlayerPath();

        if (!isHalted)
        {
            // �����߼����������ֱ��׷��ң����������·��׷
            FollowPlayerPathOrDirect();
            Accelerate();
        }

        // ����Ƿ�ץ�����
        CheckCatchPlayer();
        // ά�ָ߶�
        MaintainHeight();
    }

    /// <summary>
    /// ��¼��ҵ��ƶ�·��
    /// </summary>
    void RecordPlayerPath()
    {
        // ���̶�ʱ������¼������·�������
        if (Time.time - lastRecordTime >= pathRecordInterval)
        {
            Vector3 playerPos = player.position;
            playerPos.y = transform.position.y; // ͳһ�߶ȣ�����Y��ƫ��

            // �����¼�ظ�λ�ã���Ҿ�ֹʱ��
            if (playerPath.Count == 0 || Vector3.Distance(playerPath[playerPath.Count - 1], playerPos) > 0.1f)
            {
                playerPath.Add(playerPos);

                // ����·�����������������Ƴ���ɵ�
                if (playerPath.Count > maxPathPoints)
                {
                    playerPath.RemoveAt(0);
                }
            }

            lastRecordTime = Time.time;
        }
    }

    /// <summary>
    /// ����׷���߼�������ֱ��׷��ң�Զ�������·��׷
    /// </summary>
    void FollowPlayerPathOrDirect()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // ���1����������㹻����ֱ�ӳ������׷��
        if (distanceToPlayer <= directPursuitRange && playerPath.Count > 0)
        {
            DirectPursuitPlayer();
        }
        // ���2�������Զ������ҵ�·��׷��
        else if (playerPath.Count > currentPathIndex)
        {
            FollowPlayerPath();
        }
    }

    /// <summary>
    /// ֱ�ӳ������׷���������룩
    /// </summary>
    void DirectPursuitPlayer()
    {
        Vector3 direction = (player.position - transform.position).normalized;
        direction.y = 0; // ����Y��

        // ƽ��ת�����
        if (direction.magnitude >= 0.1f)
        {
            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle,
                                                ref turnSmoothVelocity, pathFollowSmoothTime);
            transform.rotation = Quaternion.Euler(0f, angle, 0f);
        }

        // ������ƶ�
        transform.position += transform.forward * currentSpeed * Time.deltaTime;

        // ���Ի��ƣ�ֱ��׷�������ߣ���ɫ��
        Debug.DrawRay(transform.position, transform.forward * 3f, Color.red);
        Debug.DrawLine(transform.position, player.position, Color.yellow);
    }

    /// <summary>
    /// ����ҵ���ʷ·��׷����Զ���룩
    /// </summary>
    void FollowPlayerPath()
    {
        Vector3 targetPos = playerPath[currentPathIndex];
        Vector3 direction = (targetPos - transform.position).normalized;
        direction.y = 0; // ����Y��

        // ƽ��ת��Ŀ��·����
        if (direction.magnitude >= 0.1f)
        {
            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle,
                                                ref turnSmoothVelocity, pathFollowSmoothTime);
            transform.rotation = Quaternion.Euler(0f, angle, 0f);
        }

        // ��·�����ƶ�
        transform.position += transform.forward * currentSpeed * Time.deltaTime;

        // ����Ƿ񵽴ﵱǰ·���㣬�������л���һ��
        float distanceToPathPoint = Vector3.Distance(transform.position, targetPos);
        if (distanceToPathPoint <= 0.5f)
        {
            currentPathIndex++;
            // ��ֹ����Խ��
            currentPathIndex = Mathf.Min(currentPathIndex, playerPath.Count - 1);
        }

        // ���Ի��ƣ�·��׷�������ߣ���ɫ����·���ߣ���ɫ��
        Debug.DrawRay(transform.position, transform.forward * 3f, Color.green);
        Debug.DrawLine(transform.position, targetPos, Color.blue);

        // ������ҵ�����·��
        for (int i = 0; i < playerPath.Count - 1; i++)
        {
            Debug.DrawLine(playerPath[i], playerPath[i + 1], Color.cyan);
        }
    }

    /// <summary>
    /// ����Watcher�ĸ߶ȣ���ֹY��ƫ��
    /// </summary>
    void AdjustHeightPosition()
    {
        float groundY = -1.5f;
        float watcherHeight = 2.0f;
        float targetY = groundY + (watcherHeight * 0.5f);

        Vector3 pos = transform.position;
        pos.y = targetY;
        transform.position = pos;
    }

    /// <summary>
    /// �����߼�����������ֱ������ٶȣ�
    /// </summary>
    void Accelerate()
    {
        currentSpeed += accelerationRate * Time.deltaTime;
        currentSpeed = Mathf.Min(currentSpeed, maxSpeed);
    }

    /// <summary>
    /// ά�ָ߶ȣ���ֹWatcher����ȥ
    /// </summary>
    void MaintainHeight()
    {
        float groundY = -1.5f;
        float watcherHeight = 2.0f;
        float minY = groundY + (watcherHeight * 0.5f);

        if (transform.position.y < minY)
        {
            Vector3 pos = transform.position;
            pos.y = minY;
            transform.position = pos;
        }
    }

    /// <summary>
    /// ����Ƿ�ץ�����
    /// </summary>
    void CheckCatchPlayer()
    {
        if (player == null) return;

        float directDistance = Vector3.Distance(transform.position, player.position);

        // �����㹻�������������Ұ��Χ���򲶻�
        if (directDistance <= minDistance * 1.5f)
        {
            Vector3 toPlayer = player.position - transform.position;
            float angle = Vector3.Angle(transform.forward, toPlayer);

            if (angle < 60f || directDistance <= minDistance)
            {
                OnCatchPlayer();
            }
        }
    }

    /// <summary>
    /// ץ����Һ����Ϸ�����߼�
    /// </summary>
    void OnCatchPlayer()
    {
        Debug.Log("GAME OVER - You were caught by the Watcher!");
        Time.timeScale = 0;
    }

    /// <summary>
    /// �����ע��ʱ��ͣ���ƿ����ߺ�ָ�
    /// </summary>
    /// <param name="lookedAt">�Ƿ����ע��</param>
    public void OnPlayerLookedAt(bool lookedAt)
    {
        isHalted = lookedAt;

        if (lookedAt)
        {
            currentSpeed = 0f;
            Debug.Log("Watcher HALTED - Player is looking at it!");
        }
        else
        {
            currentSpeed = baseSpeed;
            Debug.Log("Watcher CHASING - Player looked away!");
        }
    }

    /// <summary>
    /// ��ײ��⣨���ֱ��ײ��Watcher��
    /// </summary>
    void OnTriggerEnter(Collider other)
    {
        if (other != null && other.CompareTag("Player"))
        {
            OnCatchPlayer();
        }
    }

    /// <summary>
    /// GUI��ʾ״̬�������ã�
    /// </summary>
    void OnGUI()
    {
        if (Time.timeScale == 0)
        {
            GUIStyle gameOverStyle = new GUIStyle(GUI.skin.label);
            gameOverStyle.alignment = TextAnchor.MiddleCenter;
            gameOverStyle.fontSize = 20;
            gameOverStyle.fontStyle = FontStyle.Bold;
            gameOverStyle.normal.textColor = Color.red;

            GUI.Label(new Rect(Screen.width / 2 - 150, Screen.height / 2 - 60, 300, 120),
                     "GAME OVER\nYou were caught by the Watcher!\n\nPress R to restart", gameOverStyle);

            if (Input.GetKeyDown(KeyCode.R))
            {
                RestartGame();
            }
        }

        // ������Ϣ��ʾ
        GUIStyle statusStyle = new GUIStyle(GUI.skin.label);
        statusStyle.normal.textColor = Color.white;
        statusStyle.fontSize = 14;

        GUI.Label(new Rect(10, 10, 400, 20), $"Watcher State: {(isHalted ? "STOPPED" : "CHASING")}", statusStyle);
        GUI.Label(new Rect(10, 35, 400, 20), $"Watcher Speed: {currentSpeed:F1}", statusStyle);
        GUI.Label(new Rect(10, 60, 400, 20), $"Player Path Points: {playerPath.Count}", statusStyle);
        GUI.Label(new Rect(10, 85, 400, 20), $"Distance to Player: {Vector3.Distance(transform.position, player.position):F1}", statusStyle);
        GUI.Label(new Rect(10, 110, 400, 20), $"Pursuit Mode: {(Vector3.Distance(transform.position, player.position) <= directPursuitRange ? "DIRECT" : "PATH FOLLOW")}", statusStyle);
    }

    /// <summary>
    /// ������Ϸ
    /// </summary>
    void RestartGame()
    {
        Time.timeScale = 1;

        // ����Watcher״̬
        currentSpeed = baseSpeed;
        isHalted = false;
        currentPathIndex = 0;
        playerPath.Clear(); // ������·����¼

        // ����Watcherλ��
        Vector3 newPos = startPosition;
        AdjustHeightPosition();
        transform.position = newPos;
        transform.rotation = Quaternion.identity;

        // �������λ�ã���Watcherǰ��10����λ��
        if (player != null)
        {
            Vector3 playerStartPos = newPos;
            playerStartPos.z += 10f;
            playerStartPos.y = 1f;
            player.position = playerStartPos;
            player.rotation = Quaternion.identity;
        }

        Debug.Log("Game Restarted!");
    }

    /// <summary>
    /// Scene��ͼ���Ƶ���Gizmos
    /// </summary>
    void OnDrawGizmos()
    {
        // �������·��
        Gizmos.color = Color.cyan;
        for (int i = 0; i < playerPath.Count - 1; i++)
        {
            Gizmos.DrawLine(playerPath[i], playerPath[i + 1]);
        }

        // ���Ƶ�ǰ׷����·����
        if (currentPathIndex < playerPath.Count)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(playerPath[currentPathIndex], 0.4f);
        }

        // ����Watcher��ǰ������
        Gizmos.color = Color.yellow;
        Vector3 forwardPos = transform.position + transform.forward * 2f;
        Gizmos.DrawLine(transform.position, forwardPos);
        Gizmos.DrawLine(forwardPos, forwardPos + (transform.right * 0.3f - transform.forward * 0.3f));
        Gizmos.DrawLine(forwardPos, forwardPos + (-transform.right * 0.3f - transform.forward * 0.3f));

        // ����Watcherλ��
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, 0.5f);

        // ����ֱ��׷����Χ
        Gizmos.color = new Color(1, 0.5f, 0, 0.2f);
        Gizmos.DrawSphere(transform.position, directPursuitRange);
    }
}