using System.Collections;
using UnityEngine;

public class BuddhaRevealController : MonoBehaviour
{
    [Header("Phật Tổ")]
    [Tooltip("Animator của Phật Tổ.")]
    public Animator buddhaAnimator;

    [Tooltip("Trigger để Phật Tổ đưa tay ra.")]
    public string buddhaCastTriggerName = "Cast";

    [Header("Cái bát / Chiếu yêu kính")]
    [Tooltip("Object cái bát.")]
    public GameObject buddhaBowl;

    [Tooltip("Animator của cái bát.")]
    public Animator bowlAnimator;

    [Tooltip("Tên state animation tổng hợp của cái bát.")]
    public string bowlAnimationStateName = "NPC5_projectile";

    [Tooltip("Điểm xuất hiện của cái bát.")]
    public Transform bowlSpawnPoint;

    [Header("Mục tiêu")]
    [Tooltip("Điểm chiếu vào Ngộ Không giả.")]
    public Transform revealTargetPoint;

    [Header("Timing")]
    [Tooltip("Sau khi Phật Tổ đưa tay bao lâu thì cái bát hiện.")]
    public float bowlShowDelay = 0.35f;

    [Tooltip("Thời gian animation cái bát chạy trước khi gọi Ngộ Không giả Die.")]
    public float revealDuration = 1.5f;

    [Header("Debug")]
    public bool enableDebugLog = true;

    void Awake()
    {
        if (buddhaAnimator == null)
        {
            buddhaAnimator = GetComponent<Animator>();
        }

        if (buddhaBowl != null)
        {
            buddhaBowl.SetActive(false);
        }
    }

    public IEnumerator RevealRoutine()
    {
        if (enableDebugLog)
        {
            Debug.Log("Phật Tổ bắt đầu thu hồi Ngộ Không giả.");
        }

        PlayBuddhaCast();

        yield return new WaitForSeconds(bowlShowDelay);

        ShowBuddhaBowl();

        yield return new WaitForSeconds(revealDuration);
    }

    public void PlayBuddhaCast()
    {
        if (buddhaAnimator == null) return;

        buddhaAnimator.ResetTrigger(buddhaCastTriggerName);
        buddhaAnimator.SetTrigger(buddhaCastTriggerName);
    }

    public void ShowBuddhaBowl()
    {
        if (buddhaBowl == null) return;

        if (bowlSpawnPoint != null)
        {
            buddhaBowl.transform.position = bowlSpawnPoint.position;
            buddhaBowl.transform.rotation = bowlSpawnPoint.rotation;
        }

        buddhaBowl.SetActive(true);

        if (bowlAnimator == null)
        {
            bowlAnimator = buddhaBowl.GetComponent<Animator>();
        }

        if (bowlAnimator != null && !string.IsNullOrEmpty(bowlAnimationStateName))
        {
            bowlAnimator.Play(bowlAnimationStateName, 0, 0f);
            bowlAnimator.Update(0f);
        }

        if (enableDebugLog)
        {
            Debug.Log("BuddhaBowl hiện và chạy animation: " + bowlAnimationStateName);
        }
    }

    public void HideBuddhaBowl()
    {
        if (buddhaBowl != null)
        {
            buddhaBowl.SetActive(false);
        }
    }
}