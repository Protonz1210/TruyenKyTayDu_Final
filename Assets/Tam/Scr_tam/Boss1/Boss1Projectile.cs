using UnityEngine;

/// <summary>
/// Projectile của Boss1.
/// Bay ngang theo hướng Boss1 đang nhìn.
/// Gây damage cho Wukong hoặc đoàn thỉnh kinh.
/// Khi trúng Wukong hoặc đoàn thỉnh kinh lần đầu, báo về Boss1Controller để bắt đầu tính thời gian Attack Window.
/// </summary>
public class Boss1Projectile : MonoBehaviour
{
    [Header("Move")]
    [Tooltip("Tốc độ bay của projectile.")]
    public float moveSpeed = 7f;

    [Tooltip("Thời gian tồn tại trước khi tự hủy.")]
    public float lifeTime = 3f;

    [Header("Damage")]
    [Tooltip("Sát thương gây ra.")]
    public int damage = 100;

    [Tooltip("Sau khi trúng target thì tự hủy.")]
    public bool destroyOnHit = true;

    [Header("Tags")]
    [Tooltip("Tag của Wukong.")]
    public string playerTag = "Player";

    [Tooltip("Tag của đoàn thỉnh kinh.")]
    public string partyTag = "Party";

    [Header("Owner")]
    [Tooltip("Boss sinh ra projectile. Dùng để tránh va chạm nhầm với chính boss.")]
    public Transform owner;

    [Header("Notify Boss")]
    [Tooltip("Khi projectile trúng Wukong hoặc đoàn thỉnh kinh, báo về Boss1 để bắt đầu đếm Attack Window.")]
    public bool notifyOwnerWhenHitValidTarget = true;

    [Header("Debug")]
    public bool enableDebugLog = false;

    private Vector2 moveDirection = Vector2.right;
    private float lifeTimer;
    private bool hasHitTarget;

    private void Start()
    {
        lifeTimer = lifeTime;
        UpdateProjectileFacing();
    }

    private void Update()
    {
        transform.position += (Vector3)(moveDirection.normalized * moveSpeed * Time.deltaTime);

        lifeTimer -= Time.deltaTime;

        if (lifeTimer <= 0f)
        {
            Destroy(gameObject);
        }
    }

    public void Init(
        Vector2 direction,
        int projectileDamage,
        float projectileMoveSpeed,
        float projectileLifeTime,
        Transform projectileOwner,
        string playerTargetTag,
        string partyTargetTag
    )
    {
        if (direction.sqrMagnitude > 0f)
        {
            moveDirection = direction.normalized;
        }

        damage = projectileDamage;
        moveSpeed = projectileMoveSpeed;
        lifeTime = projectileLifeTime;
        lifeTimer = lifeTime;
        owner = projectileOwner;
        playerTag = playerTargetTag;
        partyTag = partyTargetTag;

        UpdateProjectileFacing();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasHitTarget)
        {
            return;
        }

        if (other == null)
        {
            return;
        }

        if (IsOwnerCollider(other))
        {
            return;
        }

        bool hitPlayer = other.CompareTag(playerTag);
        bool hitParty = other.CompareTag(partyTag);

        if (!hitPlayer && !hitParty)
        {
            return;
        }

        hasHitTarget = true;

        ApplyDamageToTarget(other.gameObject);

        // Chỉ cần trúng Wukong hoặc đoàn thỉnh kinh lần đầu là Boss1 bắt đầu đếm Attack Window.
        NotifyOwnerValidTargetWasHit();

        if (enableDebugLog)
        {
            string targetType = hitPlayer ? "Wukong" : "Party";
            Debug.Log("Boss1Projectile trúng " + targetType + ": " + other.name + " | Damage = " + damage);
        }

        if (destroyOnHit)
        {
            Destroy(gameObject);
        }
    }

    private bool IsOwnerCollider(Collider2D other)
    {
        if (owner == null)
        {
            return false;
        }

        if (other.transform == owner)
        {
            return true;
        }

        if (other.transform.IsChildOf(owner))
        {
            return true;
        }

        return false;
    }

    private void NotifyOwnerValidTargetWasHit()
    {
        if (!notifyOwnerWhenHitValidTarget)
        {
            return;
        }

        if (owner == null)
        {
            return;
        }

        // Boss1Controller hiện đang dùng hàm này để bắt đầu đếm Attack Window.
        // Dù tên là WukongHit, ta dùng cho cả Wukong và Party để không phải sửa thêm Boss1Controller.
        owner.gameObject.SendMessage(
            "NotifyWukongHitByProjectile",
            SendMessageOptions.DontRequireReceiver
        );

        if (enableDebugLog)
        {
            Debug.Log("Boss1Projectile: Đã báo Boss1 bắt đầu đếm Attack Window vì projectile trúng target hợp lệ.");
        }
    }

    private void ApplyDamageToTarget(GameObject targetObject)
    {
        if (targetObject == null)
        {
            return;
        }

        // Ưu tiên gửi damage vào object bị collider chạm.
        targetObject.SendMessage(
            "TakeDamage",
            damage,
            SendMessageOptions.DontRequireReceiver
        );

        targetObject.SendMessage(
            "ReceiveDamage",
            damage,
            SendMessageOptions.DontRequireReceiver
        );

        targetObject.SendMessage(
            "ApplyDamage",
            damage,
            SendMessageOptions.DontRequireReceiver
        );

        // Nếu Health nằm ở cha, gửi thêm lên parent.
        if (targetObject.transform.parent != null)
        {
            GameObject parentObject = targetObject.transform.parent.gameObject;

            parentObject.SendMessage(
                "TakeDamage",
                damage,
                SendMessageOptions.DontRequireReceiver
            );

            parentObject.SendMessage(
                "ReceiveDamage",
                damage,
                SendMessageOptions.DontRequireReceiver
            );

            parentObject.SendMessage(
                "ApplyDamage",
                damage,
                SendMessageOptions.DontRequireReceiver
            );
        }
    }

    private void UpdateProjectileFacing()
    {
        if (moveDirection.x < 0f)
        {
            FlipProjectileToLeft();
        }
        else
        {
            FlipProjectileToRight();
        }
    }

    private void FlipProjectileToLeft()
    {
        Vector3 scale = transform.localScale;
        scale.x = -Mathf.Abs(scale.x);
        transform.localScale = scale;
    }

    private void FlipProjectileToRight()
    {
        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x);
        transform.localScale = scale;
    }
}