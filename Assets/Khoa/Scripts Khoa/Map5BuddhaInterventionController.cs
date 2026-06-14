using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class Map5BuddhaInterventionController : MonoBehaviour
{
    [Header("Buddha")]
    [Tooltip("Object Phật Tổ. Có thể là NPC5_Buddha.")]
    public GameObject buddhaObject;

    [Tooltip("Animator của Phật Tổ. Nếu chưa có animation đưa tay thì có thể để trống.")]
    public Animator buddhaAnimator;

    [Tooltip("Tên Trigger animation Phật Tổ đưa tay. Nếu Animator chưa có trigger này thì để trống.")]
    public string buddhaHandTriggerName = "Hand";

    [Tooltip("Thời gian chờ sau khi Phật Tổ bắt đầu đưa tay rồi mới hiện bát.")]
    public float delayBeforeBowlAppear = 0.8f;

    [Header("Buddha Bowl")]
    [Tooltip("Object bát Phật Tổ sẽ hiện ra.")]
    public GameObject buddhaBowl;

    [Tooltip("Điểm spawn/điểm đặt bát.")]
    public Transform buddhaBowlSpawnPoint;

    [Tooltip("Bật: khi hiện bát sẽ đặt bát đúng vị trí BuddhaBowlSpawnPoint.")]
    public bool moveBowlToSpawnPoint = true;

    [Tooltip("Bật: khi hiện bát sẽ xoay bát theo rotation của BuddhaBowlSpawnPoint.")]
    public bool useSpawnPointRotation = false;

    [Header("Fake Wukong Die")]
    [Tooltip("Object FakeWukong.")]
    public GameObject fakeWukongObject;

    [Tooltip("Animator của FakeWukong.")]
    public Animator fakeWukongAnimator;

    [Tooltip("Tên Trigger Die trong Animator của FakeWukong.")]
    public string fakeDieTriggerName = "Die";

    [Tooltip("Thời gian chờ sau khi bát hiện ra rồi FakeWukong mới Die.")]
    public float delayBeforeFakeDie = 0.35f;

    [Header("Test")]
    [Tooltip("Bật để bấm phím B test riêng Phật Tổ can thiệp.")]
    public bool enableTestKey = true;

    [Header("State")]
    [Tooltip("Đang chạy đoạn Phật Tổ can thiệp hay không.")]
    public bool isInterventionRunning;

    private Action onFinishedCallback;

    private void Start()
    {
        CacheReferences();
        HideBowlAtStart();
    }

    private void Update()
    {
        if (!enableTestKey)
        {
            return;
        }

        if (isInterventionRunning)
        {
            return;
        }

        if (Keyboard.current != null && Keyboard.current.bKey.wasPressedThisFrame)
        {
            PlayInterventionOnce(() =>
            {
                Debug.Log("[Map5BuddhaInterventionController] Test intervention finished.");
            });
        }
    }

    public void PlayInterventionOnce(Action onFinished = null)
    {
        if (isInterventionRunning)
        {
            Debug.LogWarning("[Map5BuddhaInterventionController] Intervention đang chạy, không gọi lại.");
            return;
        }

        onFinishedCallback = onFinished;
        StartCoroutine(InterventionRoutine());
    }

    private IEnumerator InterventionRoutine()
    {
        isInterventionRunning = true;

        PlayBuddhaHandAnimation();

        yield return new WaitForSeconds(delayBeforeBowlAppear);

        ShowBuddhaBowl();

        yield return new WaitForSeconds(delayBeforeFakeDie);

        PlayFakeWukongDie();

        yield return new WaitForSeconds(0.5f);

        isInterventionRunning = false;

        Action callback = onFinishedCallback;
        onFinishedCallback = null;
        callback?.Invoke();
    }

    private void CacheReferences()
    {
        if (buddhaAnimator == null && buddhaObject != null)
        {
            buddhaAnimator = buddhaObject.GetComponent<Animator>();
        }

        if (fakeWukongAnimator == null && fakeWukongObject != null)
        {
            fakeWukongAnimator = fakeWukongObject.GetComponent<Animator>();
        }
    }

    private void HideBowlAtStart()
    {
        if (buddhaBowl != null)
        {
            buddhaBowl.SetActive(false);
        }
    }

    private void PlayBuddhaHandAnimation()
    {
        if (buddhaAnimator == null)
        {
            Debug.LogWarning("[Map5BuddhaInterventionController] Chưa có Buddha Animator. Bỏ qua animation đưa tay.");
            return;
        }

        if (string.IsNullOrEmpty(buddhaHandTriggerName))
        {
            Debug.LogWarning("[Map5BuddhaInterventionController] Chưa nhập Buddha Hand Trigger Name. Bỏ qua animation đưa tay.");
            return;
        }

        buddhaAnimator.SetTrigger(buddhaHandTriggerName);
    }

    private void ShowBuddhaBowl()
    {
        if (buddhaBowl == null)
        {
            Debug.LogError("[Map5BuddhaInterventionController] Chưa gán BuddhaBowl.");
            return;
        }

        if (moveBowlToSpawnPoint && buddhaBowlSpawnPoint != null)
        {
            buddhaBowl.transform.position = buddhaBowlSpawnPoint.position;

            if (useSpawnPointRotation)
            {
                buddhaBowl.transform.rotation = buddhaBowlSpawnPoint.rotation;
            }
        }

        buddhaBowl.SetActive(true);

        Debug.Log("[Map5BuddhaInterventionController] BuddhaBowl appeared.");
    }

    private void PlayFakeWukongDie()
    {
        if (fakeWukongAnimator == null)
        {
            Debug.LogError("[Map5BuddhaInterventionController] Chưa gán Animator của FakeWukong.");
            return;
        }

        if (string.IsNullOrEmpty(fakeDieTriggerName))
        {
            Debug.LogError("[Map5BuddhaInterventionController] Fake Die Trigger Name đang trống.");
            return;
        }

        fakeWukongAnimator.SetTrigger(fakeDieTriggerName);

        Debug.Log("[Map5BuddhaInterventionController] FakeWukong Die triggered.");
    }
}