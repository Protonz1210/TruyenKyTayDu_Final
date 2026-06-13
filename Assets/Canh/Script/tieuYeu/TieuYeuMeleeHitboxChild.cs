using UnityEngine;

public class TieuYeuMeleeHitboxChild : MonoBehaviour
{
    [Tooltip("Hitbox cha xử lý damage.")]
    public TieuYeuMeleeHitbox parentHitbox;

    void Awake()
    {
        if (parentHitbox == null)
        {
            parentHitbox = GetComponentInParent<TieuYeuMeleeHitbox>();
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (parentHitbox != null)
        {
            parentHitbox.ReceiveTrigger(other);
        }
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (parentHitbox != null)
        {
            parentHitbox.ReceiveTrigger(other);
        }
    }
}