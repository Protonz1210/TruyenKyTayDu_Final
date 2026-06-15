using System.Collections;
using UnityEngine;

/// <summary>
/// Trigger dùng cho phase NPC Map1.
/// Wukong đi vào trigger trước, nhưng chưa kích hoạt ngay.
/// Script sẽ chờ toàn bộ đoàn thỉnh kinh vào trong màn hình camera rồi mới gọi Map1StoryManager.
/// </summary>
public class Map1PartyCameraGateTrigger : MonoBehaviour
{
    [Header("Manager")]
    [Tooltip("Map1StoryManager điều phối toàn bộ Map1.")]
    public Map1StoryManager storyManager;

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

        if (!other.CompareTag(playerTag))
        {
            return;
        }

        if (storyManager == null)
        {
            Debug.LogWarning(gameObject.name + ": Chưa gán Map1StoryManager.");
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

        isWaitingParty = true;

        if (enableDebugLog)
        {
            Debug.Log(gameObject.name + ": Wukong đã vào vùng NPC. Đang chờ cả đoàn thỉnh kinh vào màn hình.");
        }

        if (waitRoutine != null)
        {
            StopCoroutine(waitRoutine);
        }

        waitRoutine = StartCoroutine(WaitPartyVisibleThenTriggerRoutine());
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

            if (!IsTransformVisibleInCamera(party))
            {
                return false;
            }
        }

        return true;
    }

    private bool IsTransformVisibleInCamera(Transform target)
    {
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
            Debug.Log(gameObject.name + ": Cả đoàn đã vào màn hình. Kích hoạt hội thoại NPC.");
        }

        storyManager.StartSupplyPointByTrigger();
    }
}