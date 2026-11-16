using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemySight : MonoBehaviour
{
    [Header("感知参数")]
    public Transform player;                 // 拖玩家进来
    public float sightDistance = 12f;        // 视线长度（米）
    public float fieldOfView = 120f;         // 视野角（度），360 就是全向感知
    public float eyeHeight = 0.6f;           // 眼睛高度，从球心往上抬一点

    [Header("节奏")]
    public float checkInterval = 0.15f;      // 每 0.15 秒打一次射线，不必每帧打
    public float loseSightDelay = 2f;        // 丢失视线后还追多久

    [Header("移动参数")]
    public Transform[] patrolPoints;         // 巡逻点，可以为空（那就站着不动）
    public float patrolSpeed = 1.6f;
    public float chaseSpeed = 3.2f;          // 必须小于玩家 4 m/s，否则玩家跑不掉
    [Tooltip("射线检测层：勾选Default、Player，不要勾选Enemy！")]
    public LayerMask raycastMask;
    // 供 UI 读取：这一帧到底看没看见玩家
    public bool SeesPlayer { get { return seesPlayer; } }

    private bool seesPlayer;
    public bool chasing;                    // false=巡逻，true=追击
    private int patrolIndex;
    private float nextCheckTime;
    private float lastSeenTime = -999f;
    private Vector3 lastKnownPosition;       // 最后一次看见玩家的位置
    private NavMeshAgent agent;              // 可能为空：这一层还没加
    private bool navWarningLogged;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        if (player == null)
        {
            Debug.LogWarning("[敌人] 没有指定 Player，AI 不会动。请在 Inspector 里拖一下");
        }

        if (agent != null && !agent.isOnNavMesh)
        {
            Debug.LogWarning("[敌人] 有 NavMeshAgent 但没烘焙 NavMesh：先按直线移动");
        }
    }

    void Update()
    {
        if (player == null)
        {
            return;
        }

        // ---- 1. 每隔一段时间做一次"感知" ----
        if (Time.time >= nextCheckTime)
        {
            nextCheckTime = Time.time + Mathf.Max(0.02f, checkInterval);
            CheckSight();
        }

        // ---- 2. 按状态行动 ----
        if (chasing)
        {
            DoChase();
        }
        else
        {
            DoPatrol();
        }
    }

    /// <summary>核心：一条射线决定"看见 / 没看见"。</summary>
    void CheckSight()
    {
        Vector3 eye = transform.position + Vector3.up * eyeHeight;
        Vector3 toPlayer = player.position + Vector3.up * 0.9f - eye;   // 瞄玩家胸口，不是脚底
        float distance = toPlayer.magnitude;

        bool visible = false;

        // 条件一：距离在视线范围内
        if (distance <= sightDistance && distance > 0.01f)
        {
            Vector3 direction = toPlayer / distance;

            // 条件二：在视野角内（正前方左右各一半）
            float angle = Vector3.Angle(transform.forward, direction);

            if (angle <= fieldOfView * 0.5f)
            {
                RaycastHit hit;

                // 条件三：射线第一个打到的是玩家本人，而不是墙
                // 注意：这里用 Ignore，敌人不该被触发器（开关、终点区）干扰
                //int mask = ~(1 << LayerMask.NameToLayer("Enemy"));
                if (Physics.Raycast(eye, direction, out hit, sightDistance, raycastMask, QueryTriggerInteraction.Ignore))
                {
                    visible = hit.collider.CompareTag("Player");

                    if (!visible && !seesPlayer)
                    {
                        // 这条日志就是课堂上"躲到墙后面"的证据
                        Debug.Log("[敌人] 视线被 " + hit.collider.name + " 挡住了 —— 看不见玩家");
                    }
                }
            }
        }

        // ---- 状态切换 ----
        if (visible && !seesPlayer)
        {
            Debug.Log("[敌人] 射线命中玩家 —— 进入追击");
            chasing = true;
        }
        else if (!visible && seesPlayer && chasing)
        {
            // 刚丢失视线：不清空 lastKnownPosition。
            // 它会保留"最后一次真正看见玩家"的位置，敌人会去那儿找一圈 ——
            // 这就是游戏里"怪物走到你刚才藏身的墙角"的原理。
            Debug.Log("[敌人] 丢失视线，前往最后已知位置 " + lastKnownPosition.ToString("F1"));
        }

        seesPlayer = visible;

        if (visible)
        {
            lastSeenTime = Time.time;
            lastKnownPosition = player.position;
            chasing = true;
        }
    }

    void DoPatrol()
    {
        if (patrolPoints == null || patrolPoints.Length == 0)
        {
            return;
        }

        Vector3 target = patrolPoints[patrolIndex % patrolPoints.Length].position;

        // 到了就换下一个点
        if (FlatDistance(transform.position, target) < 0.8f)
        {
            patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
            target = patrolPoints[patrolIndex].position;
            Debug.Log("[敌人] 到达巡逻点，前往下一个：" + target.ToString("F1"));
        }

        MoveTo(target, patrolSpeed);
    }

    void DoChase()
    {
        // 超过 loseSightDelay 没再看见，就放弃
        if (Time.time - lastSeenTime > loseSightDelay)
        {
            chasing = false;
            Debug.Log("[敌人] 连续 " + loseSightDelay + " 秒没看见玩家 —— 回到巡逻");

            if (agent != null && agent.isOnNavMesh)
            {
                agent.ResetPath();
            }

            return;
        }

        // 看得见就追本人，刚丢视线就追"最后看见的位置"
        Vector3 target = seesPlayer ? player.position : lastKnownPosition;
        MoveTo(target, chaseSpeed);
    }

    /// <summary>
    /// 移动：有 NavMeshAgent 就用寻路，没有就直线走。
    /// 下一层的全部改动，就在这个方法的第一行。
    /// </summary>
    void MoveTo(Vector3 target, float speed)
    {
        if (agent != null && agent.isOnNavMesh)
        {
            agent.speed = speed;
            agent.SetDestination(target);
            return;
        }

        // ---- 降级版：直线追击（会穿墙，先让学生看到问题）----
        if (!navWarningLogged && agent != null)
        {
            navWarningLogged = true;
            Debug.LogWarning("[敌人] NavMesh 未烘焙，先用直线移动");
        }

        Vector3 direction = target - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.01f)
        {
            return;
        }

        direction.Normalize();
        transform.position += direction * speed * Time.deltaTime;   // 注意：这里直接改 position，所以墙挡不住
        transform.rotation = Quaternion.Slerp(transform.rotation,
            Quaternion.LookRotation(direction, Vector3.up), 6f * Time.deltaTime);
    }

    static float FlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    /// <summary>课堂可视化：绿线=没看见，红线=看见；青色圈是巡逻点。</summary>
    void OnDrawGizmos()
    {
        Vector3 eye = transform.position + Vector3.up * eyeHeight;

        Gizmos.color = seesPlayer ? Color.red : Color.green;
        Gizmos.DrawLine(eye, eye + transform.forward * sightDistance);

        // 视野锥的两条边
        Vector3 left = Quaternion.Euler(0f, -fieldOfView * 0.5f, 0f) * transform.forward;
        Vector3 right = Quaternion.Euler(0f, fieldOfView * 0.5f, 0f) * transform.forward;
        Gizmos.DrawLine(eye, eye + left * sightDistance);
        Gizmos.DrawLine(eye, eye + right * sightDistance);

        // 敌人到玩家的连线：被墙挡住时，学生能直接看到"线穿不过去"
        if (player != null)
        {
            Gizmos.color = seesPlayer ? Color.red : new Color(1f, 1f, 1f, 0.3f);
            Gizmos.DrawLine(eye, player.position + Vector3.up * 0.9f);
        }

        if (patrolPoints != null && patrolPoints.Length > 0)
        {
            Gizmos.color = Color.cyan;
            for (int i = 0; i < patrolPoints.Length; i++)
            {
                if (patrolPoints[i] == null)
                {
                    continue;
                }

                Gizmos.DrawWireSphere(patrolPoints[i].position, 0.35f);

                int next = (i + 1) % patrolPoints.Length;
                if (patrolPoints[next] != null)
                {
                    Gizmos.DrawLine(patrolPoints[i].position, patrolPoints[next].position);
                }
            }
        }
    }
}
