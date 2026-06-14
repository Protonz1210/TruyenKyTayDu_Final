using UnityEngine;

public class Map4BossPreviewGate : MonoBehaviour
{
    [Header("Story Manager")]
    [Tooltip("Map4StoryManager để kiểm tra phase hiện tại.")]
    public Map4StoryManager storyManager;

    [Header("Boss References")]
    [Tooltip("Boss3 / Thanh Sư Tinh.")]
    public Map4BossController boss3;

    [Tooltip("Boss4 / Bạch Tượng Tinh.")]
    public Map4BossController boss4;

    [Header("Preview Positions")]
    [Tooltip("Vị trí Boss3 đứng trong lúc phase đánh quái.")]
    public Transform boss3PreviewPoint;

    [Tooltip("Vị trí Boss4 đứng trong lúc phase đánh quái.")]
    public Transform boss4PreviewPoint;

    [Tooltip("Tự đưa boss về vị trí preview khi chưa tới BossFight.")]
    public bool lockBossToPreviewPoint = true;

    [Header("Boss Idle")]
    [Tooltip("Animator của Boss3.")]
    public Animator boss3Animator;

    [Tooltip("Animator của Boss4.")]
    public Animator boss4Animator;

    [Tooltip("Tên state Idle của Boss3.")]
    public string boss3IdleStateName = "Idle";

    [Tooltip("Tên state Idle của Boss4.")]
    public string boss4IdleStateName = "Idle";

    [Tooltip("Tên parameter tốc độ của boss nếu có.")]
    public string speedParameterName = "Speed";

    [Tooltip("Ép boss về Idle khi chưa tới BossFight.")]
    public bool forceBossIdleBeforeFight = true;

    [Header("Blocker")]
    [Tooltip("Collider chặn Wukong/đoàn không cho chạm boss trước phase BossFight.")]
    public Collider2D bossPreviewBlockerCollider;

    [Tooltip("Object hình ảnh cổng/tường chặn. Có thể để trống nếu muốn tường vô hình.")]
    public GameObject bossPreviewBlockerVisual;

    [Header("Boss Colliders")]
    [Tooltip("Tắt collider của boss trước BossFight để Wukong không đánh trúng boss từ xa.")]
    public bool disableBossCollidersBeforeFight = false;

    [Tooltip("Collider của Boss3 cần tắt trước khi đánh boss.")]
    public Collider2D[] boss3Colliders;

    [Tooltip("Collider của Boss4 cần tắt trước khi đánh boss.")]
    public Collider2D[] boss4Colliders;

    [Header("Debug")]
    public bool enableDebugLog = true;

    private bool hasUnlockedBossFight;

    void Awake()
    {
        AutoFindReferences();
    }

    void Start()
    {
        ApplyPreviewLockState();
    }

    void Update()
    {
        if (storyManager == null) return;

        if (IsBossFightPhase())
        {
            UnlockBossFightGate();
        }
        else
        {
            ApplyPreviewLockState();
        }
    }

    void AutoFindReferences()
    {
        if (bossPreviewBlockerCollider == null)
        {
            bossPreviewBlockerCollider = GetComponent<Collider2D>();
        }

        if (boss3 != null && boss3Animator == null)
        {
            boss3Animator = boss3.GetComponentInChildren<Animator>();
        }

        if (boss4 != null && boss4Animator == null)
        {
            boss4Animator = boss4.GetComponentInChildren<Animator>();
        }

        if (boss3 != null && (boss3Colliders == null || boss3Colliders.Length == 0))
        {
            boss3Colliders = boss3.GetComponentsInChildren<Collider2D>(true);
        }

        if (boss4 != null && (boss4Colliders == null || boss4Colliders.Length == 0))
        {
            boss4Colliders = boss4.GetComponentsInChildren<Collider2D>(true);
        }
    }

    bool IsBossFightPhase()
    {
        if (storyManager == null) return false;

        return storyManager.currentPhase == Map4StoryManager.Map4Phase.BossFight
            || storyManager.currentPhase == Map4StoryManager.Map4Phase.Boss5Appear
            || storyManager.currentPhase == Map4StoryManager.Map4Phase.Boss5StoryDialogue
            || storyManager.currentPhase == Map4StoryManager.Map4Phase.WukongTransform
            || storyManager.currentPhase == Map4StoryManager.Map4Phase.EndMap;
    }

    void ApplyPreviewLockState()
    {
        if (hasUnlockedBossFight) return;

        SetBlockerActive(true);

        if (disableBossCollidersBeforeFight)
        {
            SetBossCollidersEnabled(false);
        }

        ForceBossesNonCombat();

        if (lockBossToPreviewPoint)
        {
            MoveBossesToPreviewPoints();
        }

        if (forceBossIdleBeforeFight)
        {
            ForceBossesIdle();
        }
    }

    void UnlockBossFightGate()
    {
        if (hasUnlockedBossFight) return;

        hasUnlockedBossFight = true;

        SetBlockerActive(false);
        SetBossCollidersEnabled(true);

        if (enableDebugLog)
        {
            Debug.Log("Map4BossPreviewGate: đã mở cổng boss, Boss3/Boss4 có thể bắt đầu BossFight.");
        }
    }

    void SetBlockerActive(bool active)
    {
        if (bossPreviewBlockerCollider != null)
        {
            bossPreviewBlockerCollider.enabled = active;
        }

        if (bossPreviewBlockerVisual != null)
        {
            bossPreviewBlockerVisual.SetActive(active);
        }
    }

    void SetBossCollidersEnabled(bool enabled)
    {
        SetCollidersEnabled(boss3Colliders, enabled);
        SetCollidersEnabled(boss4Colliders, enabled);
    }

    void SetCollidersEnabled(Collider2D[] colliders, bool enabled)
    {
        if (colliders == null) return;

        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null)
            {
                colliders[i].enabled = enabled;
            }
        }
    }

    void ForceBossesNonCombat()
    {
        if (boss3 != null)
        {
            boss3.SendMessage("DeactivateCombat", SendMessageOptions.DontRequireReceiver);
            boss3.SendMessage("StopCombatAndReturnIdle", SendMessageOptions.DontRequireReceiver);
        }

        if (boss4 != null)
        {
            boss4.SendMessage("DeactivateCombat", SendMessageOptions.DontRequireReceiver);
            boss4.SendMessage("StopCombatAndReturnIdle", SendMessageOptions.DontRequireReceiver);
        }
    }

    void MoveBossesToPreviewPoints()
    {
        if (boss3 != null && boss3PreviewPoint != null)
        {
            boss3.transform.position = boss3PreviewPoint.position;
        }

        if (boss4 != null && boss4PreviewPoint != null)
        {
            boss4.transform.position = boss4PreviewPoint.position;
        }
    }

    void ForceBossesIdle()
    {
        ForceAnimatorIdle(boss3Animator, boss3IdleStateName);
        ForceAnimatorIdle(boss4Animator, boss4IdleStateName);
    }

    void ForceAnimatorIdle(Animator targetAnimator, string idleStateName)
    {
        if (targetAnimator == null) return;

        SetAnimatorFloatIfExists(targetAnimator, speedParameterName, 0f);

        if (string.IsNullOrEmpty(idleStateName)) return;

        AnimatorStateInfo stateInfo = targetAnimator.GetCurrentAnimatorStateInfo(0);

        if (!stateInfo.IsName(idleStateName))
        {
            targetAnimator.Play(idleStateName, 0, 0f);
            targetAnimator.Update(0f);
        }
    }

    void SetAnimatorFloatIfExists(Animator targetAnimator, string parameterName, float value)
    {
        if (targetAnimator == null) return;
        if (string.IsNullOrEmpty(parameterName)) return;

        AnimatorControllerParameter[] parameters = targetAnimator.parameters;

        for (int i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].name == parameterName && parameters[i].type == AnimatorControllerParameterType.Float)
            {
                targetAnimator.SetFloat(parameterName, value);
                return;
            }
        }
    }
}