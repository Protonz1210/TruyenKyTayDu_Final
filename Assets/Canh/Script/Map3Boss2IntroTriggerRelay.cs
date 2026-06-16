
using UnityEngine;

public class Map3Boss2IntroTriggerRelay : MonoBehaviour
{
    public enum TriggerType
    {
        [Tooltip("Trigger đầu tiên: Wukong chạm vào thì hiện thoại trước enemy, hết thoại enemy spawn.")]
        PreEnemyDialogue,

        [Tooltip("Trigger trước boss: Wukong chạm vào thì hiện thoại trước boss, hết thoại boss mới đánh.")]
        PreBossDialogue
    }

    [Header("Encounter")]
    [Tooltip("Story Manager riêng của Boss2 map này.")]
    public Map3Boss2StoryManager boss2StoryManager;

    [Header("Trigger Type")]
    [Tooltip("Chọn phase mà trigger này sẽ gọi.")]
    public TriggerType triggerType = TriggerType.PreEnemyDialogue;

    [Header("Detect")]
    [Tooltip("Tag của Ngộ Không / Player.")]
    public string playerTag = "Player";

    [Tooltip("Tắt collider sau khi đã kích hoạt để không bị gọi lại.")]
    public bool disableColliderAfterTriggered = true;

    [Tooltip("Tắt luôn GameObject trigger sau khi đã kích hoạt.")]
    public bool disableGameObjectAfterTriggered = false;

    [Header("Debug")]
    public bool enableDebugLog = true;

    private bool triggered;
    private Collider2D triggerCollider;

    private void Reset()
    {
        SetupCollider();
    }

    private void Awake()
    {
        SetupCollider();
    }

    private void SetupCollider()
    {
        triggerCollider = GetComponent<Collider2D>();

        if (triggerCollider != null)
        {
            triggerCollider.isTrigger = true;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (triggered)
            return;

        if (other == null)
            return;

        if (!other.CompareTag(playerTag))
            return;

        triggered = true;

        if (boss2StoryManager == null)
        {
            Debug.LogWarning("Map3Boss2IntroTriggerRelay: Chưa kéo Map3Boss2StoryManager.");
            return;
        }

        if (triggerType == TriggerType.PreEnemyDialogue)
        {
            boss2StoryManager.StartBoss2Intro();

            Log("Đã gọi Phase 2: PreEnemyDialogue.");
        }
        else if (triggerType == TriggerType.PreBossDialogue)
        {
            boss2StoryManager.StartPreBossDialogue();

            Log("Đã gọi Phase 4: PreBossDialogue.");
        }

        DisableTriggerIfNeeded();
    }

    private void DisableTriggerIfNeeded()
    {
        if (disableColliderAfterTriggered)
        {
            if (triggerCollider == null)
                triggerCollider = GetComponent<Collider2D>();

            if (triggerCollider != null)
                triggerCollider.enabled = false;
        }

        if (disableGameObjectAfterTriggered)
        {
            gameObject.SetActive(false);
        }
    }

    private void Log(string message)
    {
        if (!enableDebugLog)
            return;

        Debug.Log("[Map3Boss2IntroTriggerRelay] " + message);
    }
}