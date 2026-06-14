using UnityEngine;

public class Boss1Projectile : MonoBehaviour
{
    [Header("Move")]
    [Tooltip("Tốc độ bay.")]
    public float moveSpeed = 7f;

    [Tooltip("Thời gian tự hủy.")]
    public float lifeTime = 3f;

    [Header("Damage")]
    [Tooltip("Sát thương.")]
    public int damage = 80;

    [Tooltip("Tự hủy khi trúng mục tiêu.")]
    public bool destroyOnHit = true;

    [Header("Debug")]
    [Tooltip("Bật log debug.")]
    public bool enableDebugLog = true;

    private Vector2 moveDirection = Vector2.right;
    private Transform ownerRoot;
    private Rigidbody2D rb;
    private bool hasHit;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    void FixedUpdate()
    {
        if (rb != null)
        {
            rb.linearVelocity = moveDirection.normalized * moveSpeed;
        }
        else
        {
            transform.position += (Vector3)(moveDirection.normalized * moveSpeed * Time.fixedDeltaTime);
        }
    }

    public void Init(Vector2 direction, float speed, int projectileDamage, float projectileLifeTime, Transform projectileOwnerRoot)
    {
        moveDirection = direction.normalized;
        moveSpeed = speed;
        damage = projectileDamage;
        lifeTime = projectileLifeTime;
        ownerRoot = projectileOwnerRoot;
        hasHit = false;

        if (moveDirection.x < 0f)
        {
            Vector3 scale = transform.localScale;
            scale.x = -Mathf.Abs(scale.x);
            transform.localScale = scale;
        }
        else
        {
            Vector3 scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x);
            transform.localScale = scale;
        }

        CancelInvoke();
        Destroy(gameObject, lifeTime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        TryHit(other);
    }

    void OnTriggerStay2D(Collider2D other)
    {
        TryHit(other);
    }

    void TryHit(Collider2D other)
    {
        if (hasHit)
            return;

        if (other == null)
            return;

        if (ownerRoot != null && other.transform.root == ownerRoot)
            return;

        PlayerHealth playerHealth = other.GetComponentInParent<PlayerHealth>();

        if (playerHealth != null)
        {
            hasHit = true;
            playerHealth.TakeDamage(damage);

            if (enableDebugLog)
            {
                Debug.Log("Mãng Xà Tinh projectile gây damage Wukong: -" + damage);
            }

            if (destroyOnHit)
            {
                Destroy(gameObject);
            }

            return;
        }

        PartyMemberHitReceiver partyMember = other.GetComponentInParent<PartyMemberHitReceiver>();

        if (partyMember != null)
        {
            hasHit = true;
            partyMember.TakeDamage(damage);

            if (enableDebugLog)
            {
                Debug.Log("Mãng Xà Tinh projectile gây damage đoàn: -" + damage);
            }

            if (destroyOnHit)
            {
                Destroy(gameObject);
            }

            return;
        }
    }
}