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

    [Header("Ground Check")]
    [Tooltip("Chỉ kích hoạt thoại khi Wukong đang đứng dưới đất. Bật để tránh lỗi nhảy lên chạm trigger bị kẹt trên không.")]
    public bool requirePlayerGrounded = true;

    [Tooltip("Layer mặt đất của map.")]
    public LayerMask groundLayer;

    [Tooltip("Khoảng raycast kiểm tra đất bên dưới Wukong.")]
    public float groundCheckDistance = 0.25f;

    [Tooltip("Cho phép vẽ ray kiểm tra đất trong Scene view.")]
    public bool drawDebugRay = true;

    private bool hasTriggered;

    void OnTriggerEnter2D(Collider2D other)
    {
        TryTrigger(other);
    }

    void OnTriggerStay2D(Collider2D other)
    {
        TryTrigger(other);
    }

    void TryTrigger(Collider2D other)
    {
        if (hasTriggered && triggerOnlyOnce) return;
        if (!other.CompareTag(playerTag)) return;

        if (requirePlayerGrounded && !IsPlayerGrounded(other))
        {
            return;
        }

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

    bool IsPlayerGrounded(Collider2D playerCollider)
    {
        if (playerCollider == null) return false;

        Bounds bounds = playerCollider.bounds;

        Vector2 rayOrigin = new Vector2(bounds.center.x, bounds.min.y + 0.03f);
        float rayLength = groundCheckDistance;

        RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.down, rayLength, groundLayer);

        if (drawDebugRay)
        {
            Debug.DrawRay(rayOrigin, Vector2.down * rayLength, hit.collider != null ? Color.green : Color.red);
        }

        return hit.collider != null;
    }
}