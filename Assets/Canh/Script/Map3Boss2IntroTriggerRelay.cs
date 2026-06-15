
using UnityEngine;

public class Map3Boss2IntroTriggerRelay : MonoBehaviour
{
    [Header("Encounter")]
    [Tooltip("Story Manager riêng của Boss2 map bạn.")]
    public Map3Boss2StoryManager boss2StoryManager;

    [Header("Detect")]
    [Tooltip("Tag của Ngộ Không.")]
    public string playerTag = "Player";

    [Tooltip("Tắt trigger sau khi đã kích hoạt.")]
    public bool disableAfterTriggered = true;

    private bool triggered;

    void Reset()
    {
        Collider2D col = GetComponent<Collider2D>();

        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    void Awake()
    {
        Collider2D col = GetComponent<Collider2D>();

        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (triggered)
            return;

        if (other == null)
            return;

        if (!other.CompareTag(playerTag))
            return;

        triggered = true;

        if (boss2StoryManager != null)
        {
            boss2StoryManager.StartBoss2Intro();
        }
        else
        {
            Debug.LogWarning("Map3Boss2IntroTriggerRelay chưa kéo Map3Boss2StoryManager.");
        }

        if (disableAfterTriggered)
        {
            Collider2D col = GetComponent<Collider2D>();

            if (col != null)
            {
                col.enabled = false;
            }
        }
    }
}