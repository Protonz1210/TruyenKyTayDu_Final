using System.Collections;
using UnityEngine;

/// <summary>
/// Trigger NPC cho Map2.5 - Nước Chu Tử.
/// Wukong đi vào trigger trước, nhưng chưa kích hoạt thoại ngay.
/// Script sẽ chờ đủ đoàn thỉnh kinh vào trong màn hình camera rồi mới gọi Map25StoryManager.
/// 
/// Cách dùng:
/// - Gắn script này vào box trigger trước mặt NPC.
/// - BoxCollider2D bật Is Trigger.
/// - Kéo Map25StoryManager vào Story Manager.
/// - Kéo Đường Tăng, Bát Giới, Sa Tăng vào Party Objects.
/// - Khi cả đoàn đã vào màn hình, script gọi:
///   storyManager.TryStartNPCDialogueFromTrigger(cachedPlayerObject);
/// </summary>
public class Map25PartyCameraGateTrigger : MonoBehaviour
{
    [Header("Manager")]
    [Tooltip("Map25StoryManager điều phối toàn bộ Map2.5.")]
    public Map25StoryManager storyManager;

    [Header("Trigger")]
    [Tooltip("Tag của Wukong.")]
    public string playerTag = "Player";

    [Tooltip("Trigger chỉ chạy một lần.")]
    public bool triggerOnlyOnce = true;

    [Header("Party Objects")]
    [Tooltip("Kéo các thành viên đoàn thỉnh kinh vào đây: Đường Tăng, Bát Giới, Sa Tăng.")]
    public Transform[] partyObjects;

    [Header("Camera Check")]
    [Tooltip("Camera dùng để kiểm tra nhân vật có trong màn hình không. Nếu bỏ trống sẽ tự lấy Camera.main.")]
    public Camera targetCamera;

    [Tooltip("Chừa mép màn hình. 0.05 nghĩa là nhân vật phải nằm trong vùng 5% đến 95% của khung hình.")]
    [Range(0f, 0.3f)]
    public float viewportPadding = 0.05f;

    [Tooltip("Bao lâu kiểm tra một lần.")]
    public float checkInterval = 0.1f;

    [Tooltip("Sau khi cả đoàn đã vào màn hình, chờ ổn định thêm một chút rồi mới kích hoạt thoại.")]
    public float stableTimeBeforeTrigger = 0.25f;

    [Header("Debug")]
    public bool enableDebugLog = true;

    private bool hasTriggered;
    private bool isWaitingParty;
    private Coroutine waitRoutine;

    private GameObject cachedPlayerObject;

    private void Awake()
    {
        if (storyManager == null)
        {
#if UNITY_2023_1_OR_NEWER
            storyManager = FindFirstObjectByType<Map25StoryManager>();
#else
            storyManager = FindObjectOfType<Map25StoryManager>();
#endif
        }

        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryStartWaiting(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryStartWaiting(other);
    }

    private void TryStartWaiting(Collider2D other)
    {
        if (hasTriggered && triggerOnlyOnce)
        {
            return;
        }

        if (isWaitingParty)
        {
            return;
        }

        if (other == null)
        {
            return;
        }

        if (!IsPlayerCollider(other))
        {
            return;
        }

        if (storyManager == null)
        {
            Debug.LogWarning(gameObject.name + ": Chưa gán Map25StoryManager.");
            return;
        }

        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (targetCamera == null)
        {
            Debug.LogWarning(gameObject.name + ": Không tìm thấy Camera để kiểm tra party trong màn hình.");
            return;
        }

        cachedPlayerObject = GetPlayerObjectFromCollider(other);

        if (cachedPlayerObject == null)
        {
            cachedPlayerObject = other.gameObject;
        }

        isWaitingParty = true;

        if (enableDebugLog)
        {
            Debug.Log(gameObject.name + ": Wukong đã vào vùng NPC Map2.5. Đang chờ cả đoàn thỉnh kinh vào màn hình.");
        }

        if (waitRoutine != null)
        {
            StopCoroutine(waitRoutine);
        }

        waitRoutine = StartCoroutine(WaitPartyVisibleThenTriggerRoutine());
    }

    private bool IsPlayerCollider(Collider2D other)
    {
        if (other == null)
        {
            return false;
        }

        if (!string.IsNullOrEmpty(playerTag) && other.CompareTag(playerTag))
        {
            return true;
        }

        if (other.GetComponentInParent<PlayerController>() != null)
        {
            return true;
        }

        return false;
    }

    private GameObject GetPlayerObjectFromCollider(Collider2D other)
    {
        if (other == null)
        {
            return null;
        }

        PlayerController playerController = other.GetComponentInParent<PlayerController>();

        if (playerController != null)
        {
            return playerController.gameObject;
        }

        return other.gameObject;
    }

    private IEnumerator WaitPartyVisibleThenTriggerRoutine()
    {
        float stableTimer = 0f;

        while (true)
        {
            bool allPartyVisible = AreAllPartyObjectsVisibleInCamera();

            if (allPartyVisible)
            {
                stableTimer += checkInterval;

                if (stableTimer >= stableTimeBeforeTrigger)
                {
                    ActivateNpcDialogueTrigger();
                    yield break;
                }
            }
            else
            {
                stableTimer = 0f;
            }

            yield return new WaitForSeconds(checkInterval);
        }
    }

    private bool AreAllPartyObjectsVisibleInCamera()
    {
        if (partyObjects == null || partyObjects.Length == 0)
        {
            Debug.LogWarning(gameObject.name + ": Chưa gán Party Objects. Script sẽ coi như đã đủ đoàn.");
            return true;
        }

        for (int i = 0; i < partyObjects.Length; i++)
        {
            Transform party = partyObjects[i];

            if (party == null)
            {
                continue;
            }

            if (!party.gameObject.activeInHierarchy)
            {
                return false;
            }

            if (!IsTransformVisibleInCamera(party))
            {
                return false;
            }
        }

        return true;
    }

    private bool IsTransformVisibleInCamera(Transform target)
    {
        if (target == null || targetCamera == null)
        {
            return false;
        }

        Vector3 viewportPosition = targetCamera.WorldToViewportPoint(target.position);

        if (viewportPosition.z < 0f)
        {
            return false;
        }

        bool insideX =
            viewportPosition.x >= viewportPadding &&
            viewportPosition.x <= 1f - viewportPadding;

        bool insideY =
            viewportPosition.y >= viewportPadding &&
            viewportPosition.y <= 1f - viewportPadding;

        return insideX && insideY;
    }

    private void ActivateNpcDialogueTrigger()
    {
        if (hasTriggered && triggerOnlyOnce)
        {
            return;
        }

        hasTriggered = true;
        isWaitingParty = false;
        waitRoutine = null;

        if (enableDebugLog)
        {
            Debug.Log(gameObject.name + ": Cả đoàn đã vào màn hình. Kích hoạt hội thoại NPC Map2.5.");
        }

        if (storyManager == null)
        {
            Debug.LogWarning(gameObject.name + ": StoryManager null, không thể kích hoạt thoại.");
            return;
        }

        if (cachedPlayerObject == null)
        {
            Debug.LogWarning(gameObject.name + ": Không có cachedPlayerObject, dùng chính object trigger gọi thử.");
            storyManager.TryStartNPCDialogueFromTrigger(gameObject);
            return;
        }

        storyManager.TryStartNPCDialogueFromTrigger(cachedPlayerObject);
    }

    public void ResetTrigger()
    {
        hasTriggered = false;
        isWaitingParty = false;
        cachedPlayerObject = null;

        if (waitRoutine != null)
        {
            StopCoroutine(waitRoutine);
            waitRoutine = null;
        }
    }
}
