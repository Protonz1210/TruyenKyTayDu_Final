using UnityEngine;

public class Boss4UltimateProjectile : MonoBehaviour
{
    [Header("Life")]
    [Tooltip("Thời gian tồn tại của projectile.")]
    public float lifeTime = 2.5f;

    [Tooltip("Thời gian hitbox nở rộng.")]
    public float expandDuration = 1.2f;

    [Header("References")]
    [Tooltip("Object hình ảnh của projectile.")]
    public Transform visualRoot;

    [Tooltip("Collider gây sát thương của projectile.")]
    public BoxCollider2D hitboxCollider;

    [Tooltip("Vùng xử lý sát thương của projectile.")]
    public Boss4UltimateDamageZone damageZone;

    [Header("Follow Fire Point")]
    [Tooltip("Projectile bám theo Fire Point.")]
    public bool followFirePoint = true;

    [Tooltip("Dừng bám Fire Point sau một thời gian.")]
    public bool stopFollowingAfterTime = false;

    [Tooltip("Thời gian bám Fire Point.")]
    public float followDuration = 0.4f;

    private Transform followAnchor;

    [Header("Hitbox Size")]
    [Tooltip("Chiều rộng hitbox ban đầu.")]
    public float startWidth = 0.5f;

    [Tooltip("Chiều rộng hitbox tối đa.")]
    public float maxWidth = 6f;

    [Tooltip("Chiều cao hitbox ban đầu.")]
    public float startHeight = 1f;

    [Tooltip("Chiều cao hitbox tối đa.")]
    public float maxHeight = 2.5f;

    [Tooltip("Hitbox nở về phía trước.")]
    public bool expandForward = true;

    [Header("Visual")]
    [Tooltip("Lật hình ảnh theo hướng bắn.")]
    public bool flipVisualByDirection = true;
    [Header("Visual Anchor")]
    [Tooltip("Giữ VisualRoot ở đúng tâm projectile khi lật hướng.")]
    public bool keepVisualRootCentered = true;

    [Tooltip("Vị trí local cố định của VisualRoot.")]
    public Vector3 visualRootCenterLocalPosition = Vector3.zero;
    [Header("Debug")]
    [Tooltip("Bật log debug.")]
    public bool enableDebugLog = true;

    private Vector2 direction = Vector2.right;
    private float spawnTime;
    private bool isDestroyed;


public void Init(Vector2 shootDirection, int projectileDamage, Transform ownerRoot, Transform firePointAnchor)
    {
        if (shootDirection.sqrMagnitude <= 0.001f)
        {
            shootDirection = Vector2.right;
        }

        direction = shootDirection.normalized;
        spawnTime = Time.time;
        followAnchor = firePointAnchor;
        isDestroyed = false;

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
            damageZone.Init(projectileDamage, ownerRoot, this);
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
                direction +
                " | Follow Anchor: " +
                (followAnchor != null ? followAnchor.name : "None")
            );
        }
    }

    void Update()
    {
        if (isDestroyed)
            return;

        float elapsed = Time.time - spawnTime;

        float progress = 0f;

        if (expandDuration > 0f)
        {
            progress = Mathf.Clamp01(elapsed / expandDuration);
        }
        else
        {
            progress = 1f;
        }

        UpdateHitbox(progress);
    }

    void LateUpdate()
    {
        if (isDestroyed)
            return;

        FollowFirePointIfNeeded();
    }

    void FollowFirePointIfNeeded()
    {
        if (!followFirePoint)
            return;

        if (followAnchor == null)
            return;

        if (stopFollowingAfterTime)
        {
            float elapsed = Time.time - spawnTime;

            if (elapsed > followDuration)
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

    public void DestroyProjectile()
    {
        if (isDestroyed)
            return;

        isDestroyed = true;

        if (hitboxCollider != null)
        {
            hitboxCollider.enabled = false;
        }

        Destroy(gameObject);
    }

    void OnDrawGizmosSelected()
    {
        if (hitboxCollider == null)
        {
            hitboxCollider = GetComponentInChildren<BoxCollider2D>();
        }

        if (hitboxCollider == null)
            return;

        Gizmos.color = Color.cyan;

        Vector3 center = hitboxCollider.transform.TransformPoint(hitboxCollider.offset);

        Vector3 size = new Vector3(
            hitboxCollider.size.x * Mathf.Abs(hitboxCollider.transform.lossyScale.x),
            hitboxCollider.size.y * Mathf.Abs(hitboxCollider.transform.lossyScale.y),
            0f
        );

        Gizmos.DrawWireCube(center, size);
    }
}