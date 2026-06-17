using UnityEngine;

/// <summary>
/// Trigger trước Boss1 Map2.
/// Khi Wukong đi vào vùng này:
/// - Chỉ kích hoạt một lần.
/// - Gọi Map2StoryManager bắt đầu hội thoại trước Boss.
/// - Map2StoryManager sẽ tự chờ Wukong về Idle rồi mới mở thoại.
/// </summary>
public class Map2PreBossDialogueTrigger : MonoBehaviour
{
    [Header("Story Manager")]
    [Tooltip("Map2StoryManager trong scene.")]
    public Map2StoryManager storyManager;

    [Header("Detect")]
    [Tooltip("Tag của Wukong.")]
    public string playerTag = "Player";

    [Tooltip("Chỉ kích hoạt một lần.")]
    public bool triggerOnlyOnce = true;

    [Tooltip("Sau khi kích hoạt thì tắt object trigger.")]
    public bool disableTriggerObjectAfterTriggered = true;

    [Header("Debug")]
    public bool enableDebugLog = true;

    private bool triggered;

    private void Awake()
    {
        if (storyManager == null)
        {
#if UNITY_2023_1_OR_NEWER
            storyManager = FindFirstObjectByType<Map2StoryManager>();
#else
            storyManager = FindObjectOfType<Map2StoryManager>();
#endif
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (triggerOnlyOnce && triggered)
        {
            return;
        }

        if (other == null)
        {
            return;
        }

        if (!other.CompareTag(playerTag))
        {
            return;
        }

        triggered = true;

        if (enableDebugLog)
        {
            Debug.Log("Map2PreBossDialogueTrigger: Wukong đã vào trigger trước Boss1.");
        }

        if (storyManager != null)
        {
            storyManager.StartPreBossDialogueByTrigger();
        }
        else
        {
            Debug.LogWarning("Map2PreBossDialogueTrigger: Chưa gán Map2StoryManager.");
        }

        if (disableTriggerObjectAfterTriggered)
        {
            gameObject.SetActive(false);
        }
    }

    private void Reset()
    {
        Collider2D col = GetComponent<Collider2D>();

        if (col != null)
        {
            col.isTrigger = true;
        }
    }
}