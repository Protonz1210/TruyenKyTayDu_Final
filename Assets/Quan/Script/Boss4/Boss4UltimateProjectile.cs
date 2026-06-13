using UnityEngine;

public class Boss4UltimateProjectile : MonoBehaviour
{
    [Header("Life")]
    [Tooltip("Thời gian tồn tại của projectile.")]
    public float lifeTime = 2.9f;

    [Tooltip("Thời gian projectile mở rộng hitbox.")]
    public float expandDuration = 2.9f;

    [Header("References")]
    [Tooltip("Gốc hình ảnh của projectile.")]
    public Transform visualRoot;

    [Tooltip("BoxCollider2D gây damage.")]
    public BoxCollider2D hitboxCollider;

    [Tooltip("Vùng gây damage.")]
    public Boss4UltimateDamageZone damageZone;

    [Header("Follow Fire Point")]
    [Tooltip("Projectile bám theo fire point khi mới sinh ra.")]
    public bool followFirePoint = false;

    [Tooltip("Dừng bám fire point sau một thời gian.")]
    public bool stopFollowingAfterTime = false;

    [Tooltip("Thời gian bám fire point.")]
    public float followDuration = 0.4f;

    [Header("Hitbox Size")]
    [Tooltip("Chiều rộng ban đầu.")]
    public float startWidth = 1f;

    [Tooltip("Chiều rộng tối đa.")]
    public float maxWidth = 3.5f;

    [Tooltip("Chiều cao ban đầu.")]
    public float startHeight = 1f;

    [Tooltip("Chiều cao tối đa.")]
    public float maxHeight = 3.5f;

    [Tooltip("Hitbox nở về phía trước.")]
    public bool expandForward = true;

    [Header("Visual")]
    [Tooltip("Lật hình theo hướng bắn.")]
    public bool flipVisualByDirection = true;

    [Header("Visual Anchor")]
    [Tooltip("Giữ VisualRoot ở đúng tâm projectile khi lật hướng.")]
    public bool keepVisualRootCentered = true;

    [Tooltip("Vị trí local cố định của VisualRoot.")]
    public Vector3 visualRootCenterLocalPosition = Vector3.zero;

    [Header("Debug")]
    [Tooltip("Bật log debug.")]
    public bool enableDebugLog = false;

    Vector2 direction = Vector2.right;
    Transform ownerRoot;
    Transform followAnchor;
    int projectileDamage = 100;

    float timer;
    bool initialized;

    public void Init(Vector2 shootDirection, int damage, Transform owner, Transform anchor)
    {
        direction = shootDirection;

        if (direction.sqrMagnitude <= 0.01f)
        {
            direction = Vector2.right;
        }

        direction.Normalize();

        projectileDamage = damage;
        ownerRoot = owner;
        followAnchor = anchor;

        initialized = true;
        timer = 0f;

        if (hitboxCollider == null)
        {
            hitboxCollider = GetComponentInChildren<BoxCollider2D>();
        }

        if (damageZone == null)
        {
            damageZone = GetComponentInChildren<Boss4UltimateDamageZone>();
        }

        if (visualRoot == null)
        {
            Transform visual = transform.Find("Visual");

            if (visual != null)
            {
                visualRoot = visual;
            }
        }

        if (followAnchor != null && followFirePoint)
        {
            transform.position = followAnchor.position;
        }

        if (hitboxCollider != null)
        {
            hitboxCollider.isTrigger = true;
            hitboxCollider.enabled = true;
            UpdateHitbox(0f);
        }
        else
        {
            Debug.LogWarning("Boss4UltimateProjectile thiếu BoxCollider2D ở object Hitbox.");
        }

        if (damageZone != null)
        {
            damageZone.Init(projectileDamage, ownerRoot);
        }
        else
        {
            Debug.LogWarning("Boss4UltimateProjectile thiếu Boss4UltimateDamageZone ở object Hitbox.");
        }

        ApplyVisualDirection();

        Destroy(gameObject, lifeTime);

        if (enableDebugLog)
        {
            Debug.Log(
                "Boss4UltimateProjectile Init thành công | Damage: " +
                projectileDamage +
                " | Direction: " +
                direction
            );
        }
    }

    void Start()
    {
        if (!initialized)
        {
            Init(direction, projectileDamage, ownerRoot, followAnchor);
        }
    }

    void Update()
    {
        timer += Time.deltaTime;

        UpdateFollowFirePoint();

        float progress = 1f;

        if (expandDuration > 0f)
        {
            progress = Mathf.Clamp01(timer / expandDuration);
        }

        UpdateHitbox(progress);
    }

    void UpdateFollowFirePoint()
    {
        if (!followFirePoint) return;
        if (followAnchor == null) return;

        if (stopFollowingAfterTime && timer >= followDuration)
        {
            return;
        }

        transform.position = followAnchor.position;
    }

    void UpdateHitbox(float progress)
    {
        if (hitboxCollider == null) return;

        float width = Mathf.Lerp(startWidth, maxWidth, progress);
        float height = Mathf.Lerp(startHeight, maxHeight, progress);

        hitboxCollider.size = new Vector2(width, height);

        if (expandForward)
        {
            float offsetX = width * 0.5f * direction.x;
            hitboxCollider.offset = new Vector2(offsetX, 0f);
        }
        else
        {
            hitboxCollider.offset = Vector2.zero;
        }
    }

    void ApplyVisualDirection()
    {
        if (visualRoot == null) return;

        if (keepVisualRootCentered)
        {
            visualRoot.localPosition = visualRootCenterLocalPosition;
        }

        if (!flipVisualByDirection) return;

        Vector3 visualScale = visualRoot.localScale;
        visualScale.x = Mathf.Abs(visualScale.x);

        if (direction.x < 0f)
        {
            visualScale.x *= -1f;
        }

        visualRoot.localScale = visualScale;

        transform.rotation = Quaternion.identity;
    }
}