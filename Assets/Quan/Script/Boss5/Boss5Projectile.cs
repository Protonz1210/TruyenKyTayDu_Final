using UnityEngine;

public class Boss5Projectile : MonoBehaviour
{
    [Header("Move")]
    [Tooltip("Tốc độ bay.")]
    public float moveSpeed = 7f;

    [Tooltip("Thời gian tự hủy.")]
    public float lifeTime = 3f;

    [Header("Damage")]
    [Tooltip("Sát thương gây ra.")]
    public int damage = 100;

    [Tooltip("Chạm mục tiêu thì tự hủy.")]
    public bool destroyOnHit = true;

    [Header("Target Tags")]
    [Tooltip("Tag của Wukong.")]
    public string playerTag = "Player";

    [Tooltip("Tag của đoàn thỉnh kinh.")]
    public string partyTag = "Party";

    [Header("Visual")]
    [Tooltip("Gốc hình ảnh projectile.")]
    public Transform visualRoot;

    [Tooltip("Lật hình theo hướng bay.")]
    public bool flipVisualByDirection = true;

    [Header("Debug")]
    [Tooltip("Bật log debug.")]
    public bool enableDebugLog = false;

    Rigidbody2D rb;
    Vector2 moveDirection = Vector2.right;
    Transform ownerRoot;
    bool hasHit;

    public void Init(Vector2 direction, int newDamage, float newSpeed, float newLifeTime, Transform owner)
    {
        moveDirection = direction;

        if (moveDirection.sqrMagnitude <= 0.01f)
        {
            moveDirection = Vector2.right;
        }

        moveDirection.Normalize();

        damage = newDamage;
        moveSpeed = newSpeed;
        lifeTime = newLifeTime;
        ownerRoot = owner;

        SetupReferences();
        ApplyVisualDirection();

        Destroy(gameObject, lifeTime);
    }

    void Awake()
    {
        SetupReferences();
    }

    void Start()
    {
        ApplyVisualDirection();
        Destroy(gameObject, lifeTime);
    }

    void FixedUpdate()
    {
        if (rb != null)
        {
            rb.linearVelocity = moveDirection * moveSpeed;
        }
        else
        {
            transform.position += (Vector3)(moveDirection * moveSpeed * Time.fixedDeltaTime);
        }
    }

    void SetupReferences()
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
        }

        if (visualRoot == null)
        {
            Transform visual = transform.Find("Visual");

            if (visual != null)
            {
                visualRoot = visual;
            }
        }
    }

    void ApplyVisualDirection()
    {
        if (!flipVisualByDirection) return;
        if (visualRoot == null) return;

        Vector3 scale = visualRoot.localScale;
        scale.x = Mathf.Abs(scale.x);

        if (moveDirection.x < 0f)
        {
            scale.x *= -1f;
        }

        visualRoot.localScale = scale;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        TryHit(other);
    }

    void TryHit(Collider2D other)
    {
        if (hasHit) return;
        if (other == null) return;

        if (ownerRoot != null && other.transform.root == ownerRoot)
        {
            return;
        }

        bool hitSomething = false;

        if (other.CompareTag(playerTag) || other.GetComponentInParent<PlayerHealth>() != null)
        {
            PlayerHealth playerHealth = other.GetComponentInParent<PlayerHealth>();

            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage);
                hitSomething = true;

                if (enableDebugLog)
                {
                    Debug.Log("Boss5 projectile hit Wukong: " + damage);
                }
            }
        }

        if (!hitSomething)
        {
            PartyMemberHitReceiver partyReceiver = other.GetComponentInParent<PartyMemberHitReceiver>();

            if (partyReceiver != null)
            {
                partyReceiver.TakeDamage(damage);
                hitSomething = true;

                if (enableDebugLog)
                {
                    Debug.Log("Boss5 projectile hit PartyMemberHitReceiver: " + damage);
                }
            }
        }

        if (!hitSomething)
        {
            PartyHealth partyHealth = other.GetComponentInParent<PartyHealth>();

            if (partyHealth != null)
            {
                partyHealth.TakeDamage(damage);
                hitSomething = true;

                if (enableDebugLog)
                {
                    Debug.Log("Boss5 projectile hit PartyHealth: " + damage);
                }
            }
        }

        if (hitSomething && destroyOnHit)
        {
            hasHit = true;
            Destroy(gameObject);
        }
    }
}