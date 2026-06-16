
using System.Reflection;
using UnityEngine;

public class Boss2StopWhenTargetDead : MonoBehaviour
{
    [Header("Boss2")]
    [Tooltip("Boss2 cần dừng đánh.")]
    public Boss2Controller boss2;

    [Header("Targets To Check")]
    [Tooltip("Wukong cần kiểm tra chết.")]
    public Transform wukongTarget;

    [Tooltip("Các thành viên đoàn thỉnh kinh cần kiểm tra chết.")]
    public Transform[] partyTargets;

    [Header("Tags")]
    [Tooltip("Tag của Wukong.")]
    public string playerTag = "Player";

    [Tooltip("Tag của đoàn thỉnh kinh.")]
    public string partyTag = "Party";

    [Header("Stop Rule")]
    [Tooltip("Wukong chết thì Boss2 dừng đánh.")]
    public bool stopWhenWukongDead = true;

    [Tooltip("Bất kỳ thành viên đoàn nào chết thì Boss2 dừng đánh.")]
    public bool stopWhenAnyPartyDead = true;

    [Tooltip("Nếu không kéo Party Targets thì tự tìm object tag Party.")]
    public bool autoFindPartyByTag = true;

    [Tooltip("Dừng xong thì disable luôn Boss2Controller để chắc chắn boss không đánh nữa.")]
    public bool disableBoss2ControllerAfterStop = true;

    [Header("Animator")]
    [Tooltip("Animator của Boss2.")]
    public Animator bossAnimator;

    [Tooltip("Tên state Idle của Boss2.")]
    public string idleStateName = "Boss2_idle";

    [Tooltip("Tên trigger đánh của Boss2.")]
    public string meleeTriggerName = "MeleeAttack";

    [Tooltip("Tên trigger chết của Boss2.")]
    public string dieTriggerName = "Die";

    [Tooltip("Tên parameter Speed.")]
    public string speedParameterName = "Speed";

    [Header("Debug")]
    public bool enableDebugLog = true;

    bool hasStoppedBoss;

    void Awake()
    {
        FindRefsIfNeeded();
    }

    void Update()
    {
        if (hasStoppedBoss) return;

        FindRefsIfNeeded();

        if (stopWhenWukongDead && IsTargetDead(wukongTarget))
        {
            StopBossNow("Wukong đã chết");
            return;
        }

        if (stopWhenAnyPartyDead)
        {
            if (partyTargets != null && partyTargets.Length > 0)
            {
                for (int i = 0; i < partyTargets.Length; i++)
                {
                    if (IsTargetDead(partyTargets[i]))
                    {
                        StopBossNow("Một thành viên đoàn thỉnh kinh đã chết: " + partyTargets[i].name);
                        return;
                    }
                }
            }
            else if (autoFindPartyByTag)
            {
                GameObject[] partyObjects = GameObject.FindGameObjectsWithTag(partyTag);

                for (int i = 0; i < partyObjects.Length; i++)
                {
                    if (partyObjects[i] == null) continue;

                    if (IsTargetDead(partyObjects[i].transform))
                    {
                        StopBossNow("Một object tag Party đã chết: " + partyObjects[i].name);
                        return;
                    }
                }
            }
        }
    }

    void FindRefsIfNeeded()
    {
        if (boss2 == null)
        {
            boss2 = GetComponent<Boss2Controller>();
        }

        if (bossAnimator == null)
        {
            bossAnimator = GetComponent<Animator>();
        }

        if (wukongTarget == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag(playerTag);

            if (playerObject != null)
            {
                wukongTarget = playerObject.transform;
            }
        }
    }

    bool IsTargetDead(Transform target)
    {
        if (target == null)
        {
            return false;
        }

        if (!target.gameObject.activeInHierarchy)
        {
            return true;
        }

        MonoBehaviour[] scripts = target.GetComponentsInChildren<MonoBehaviour>(true);

        for (int i = 0; i < scripts.Length; i++)
        {
            MonoBehaviour script = scripts[i];
            if (script == null) continue;

            if (CheckIsDeadMethod(script)) return true;
            if (CheckCurrentHealthMethod(script)) return true;
            if (CheckCurrentHealthField(script)) return true;
            if (CheckIsDeadField(script)) return true;
        }

        return false;
    }

    bool CheckIsDeadMethod(MonoBehaviour script)
    {
        MethodInfo method = script.GetType().GetMethod(
            "IsDead",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
        );

        if (method == null) return false;
        if (method.ReturnType != typeof(bool)) return false;

        bool result = (bool)method.Invoke(script, null);
        return result;
    }

    bool CheckCurrentHealthMethod(MonoBehaviour script)
    {
        MethodInfo method = script.GetType().GetMethod(
            "GetCurrentHealth",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
        );

        if (method == null) return false;
        if (method.ReturnType != typeof(int)) return false;

        int hp = (int)method.Invoke(script, null);
        return hp <= 0;
    }

    bool CheckCurrentHealthField(MonoBehaviour script)
    {
        FieldInfo field = script.GetType().GetField(
            "currentHealth",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
        );

        if (field == null) return false;
        if (field.FieldType != typeof(int)) return false;

        int hp = (int)field.GetValue(script);
        return hp <= 0;
    }

    bool CheckIsDeadField(MonoBehaviour script)
    {
        FieldInfo field = script.GetType().GetField(
            "isDead",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
        );

        if (field == null) return false;
        if (field.FieldType != typeof(bool)) return false;

        bool dead = (bool)field.GetValue(script);
        return dead;
    }

    void StopBossNow(string reason)
    {
        hasStoppedBoss = true;

        if (enableDebugLog)
        {
            Debug.Log("Boss2StopWhenTargetDead: Dừng Boss2 vì " + reason);
        }

        if (boss2 != null)
        {
            boss2.combatStoppedByDeath = true;
            boss2.combatActivated = false;
            boss2.canMove = false;
            boss2.canAttack = false;
            boss2.canReceiveDamage = false;
            boss2.canShowBossUI = false;

            boss2.CloseMeleeHitbox();
            boss2.StopBossCombat();
        }

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        ForceIdle();

        if (boss2 != null && disableBoss2ControllerAfterStop)
        {
            boss2.enabled = false;
        }
    }

    void ForceIdle()
    {
        if (bossAnimator == null) return;

        bossAnimator.enabled = true;

        if (!string.IsNullOrEmpty(meleeTriggerName))
        {
            bossAnimator.ResetTrigger(meleeTriggerName);
        }

        if (!string.IsNullOrEmpty(dieTriggerName))
        {
            bossAnimator.ResetTrigger(dieTriggerName);
        }

        if (!string.IsNullOrEmpty(speedParameterName))
        {
            bossAnimator.SetFloat(speedParameterName, 0f);
        }

        if (!string.IsNullOrEmpty(idleStateName))
        {
            bossAnimator.Play(idleStateName, 0, 0f);
            bossAnimator.Update(0f);
        }
    }
}