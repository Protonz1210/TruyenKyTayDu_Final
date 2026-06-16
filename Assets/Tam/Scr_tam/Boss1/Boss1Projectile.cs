using UnityEngine;

/// <summary>
/// Projectile của Boss1.
/// Bay ngang theo hướng Boss1 đang nhìn.
/// Gây damage cho Wukong hoặc đoàn thỉnh kinh.
/// </summary>
public class Boss1Projectile : MonoBehaviour
{
    [Header("Move")]
    public float moveSpeed = 7f;

    public float lifeTime = 3f;

    [Header("Damage")]
    public int damage = 100;

    [Tooltip("Sau khi trúng target thì tự hủy.")]
    public bool destroyOnHit = true;

    [Header("Tags")]
    public string playerTag = "Player";
    public string partyTag = "Party";

    [Header("Owner")]
    [Tooltip("Boss sinh ra projectile. Dùng để tránh va chạm nhầm với chính boss.")]
    public Transform owner;

    [Header("Debug")]
    public bool enableDebugLog = false;

    private Vector2 moveDirection = Vector2.right;
    private float lifeTimer;

    private void Start()
    {
        lifeTimer = lifeTime;

        if (moveDirection.x < 0f)
        {
            FlipProjectileToLeft();
        }
        else
        {
            FlipProjectileToRight();
        }
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

        if (moveDirection.x < 0f)
        {
            FlipProjectileToLeft();
        }
        else
        {
            FlipProjectileToRight();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other == null)
        {
            return;
        }

        if (owner != null && other.transform == owner)
        {
            return;
        }

        bool hitPlayer = other.CompareTag(playerTag);
        bool hitParty = other.CompareTag(partyTag);

        if (!hitPlayer && !hitParty)
        {
            return;
        }

        ApplyDamageToTarget(other.gameObject);

        if (enableDebugLog)
        {
            Debug.Log("Boss1Projectile trúng: " + other.name + " | Damage = " + damage);
        }

        if (destroyOnHit)
        {
            Destroy(gameObject);
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