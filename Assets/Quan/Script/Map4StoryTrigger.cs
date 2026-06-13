using UnityEngine;

public class Map4StoryTrigger : MonoBehaviour
{
    public enum Map4TriggerType
    {
        Enemy4Intro,
        BossIntro,
        EndMap
    }

    [Header("Manager")]
    [Tooltip("Map4StoryManager điều phối toàn bộ map.")]
    public Map4StoryManager storyManager;

    [Header("Trigger")]
    [Tooltip("Loại trigger của map.")]
    public Map4TriggerType triggerType;

    [Tooltip("Tag của Wukong.")]
    public string playerTag = "Player";

    [Tooltip("Trigger chỉ chạy một lần.")]
    public bool triggerOnlyOnce = true;

    private bool hasTriggered;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (hasTriggered && triggerOnlyOnce) return;
        if (!other.CompareTag(playerTag)) return;

        hasTriggered = true;

        if (storyManager == null)
        {
            Debug.LogWarning(gameObject.name + " chưa gán Map4StoryManager.");
            return;
        }

        switch (triggerType)
        {
            case Map4TriggerType.Enemy4Intro:
                storyManager.StartEnemy4Intro();
                break;

            case Map4TriggerType.BossIntro:
                storyManager.StartBossIntro();
                break;

            case Map4TriggerType.EndMap:
                storyManager.StartEndMapByTrigger();
                break;
        }
    }
}