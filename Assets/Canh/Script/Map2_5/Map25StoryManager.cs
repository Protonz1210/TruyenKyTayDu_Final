using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

/// <summary>
/// StoryManager cho Map2.5 - Nước Chu Tử.
/// Cơ chế:
/// - Vào map: tắt UI tổng, chờ Wukong + đoàn về Idle, khóa điều khiển, hiện UI địa điểm, rồi bật UI tổng.
/// - Wukong vào trigger NPC: chỉ mở thoại khi đủ đoàn thỉnh kinh trong scene.
/// - Hết thoại NPC: mới bật tương tác hồi máu.
/// - Hồi máu xong: tắt UI, fade đen, chuyển scene.
/// </summary>
public class Map25StoryManager : MonoBehaviour
{
    public enum Map25StoryState
    {
        StartIntro,
        ExploreBeforeNPC,
        WaitingPartyForNPCDialogue,
        NPCDialogue,
        WaitHealAfterDialogue,
        LoadNextMap,
        Finished
    }

    [Header("Current State")]
    public Map25StoryState currentState = Map25StoryState.StartIntro;

    [Header("UI")]
    [Tooltip("UI tổng gameplay: máu Wukong, máu đoàn, skill, cooldown. Không kéo object chứa UIDocument fade/location vào đây.")]
    public GameObject globalHUD;

    [Tooltip("HUD controller đang quản lý Box-mask, box_text, box-image. Có thể dùng lại Map3HUDController.")]
    public Map3HUDController hudController;

    [Tooltip("UIDocument chứa UI địa điểm.")]
    public UIDocument mapHUDDocument;

    [Tooltip("Các object UI gameplay cần ẩn ở đầu map và trước khi chuyển map.")]
    public GameObject[] gameplayUIObjects;

    [Tooltip("Tên group UI boss nếu UIDocument có boss-panel. Map2.5 không có boss vẫn để nguyên cũng được.")]
    public string bossUIGroupName = "boss-panel";

    [Tooltip("Tên group bảng địa danh dọc trong UI Toolkit.")]
    public string locationBoxName = "Box-mask";

    [Tooltip("Tên label hiển thị địa danh.")]
    public string locationTextName = "box_text";

    [TextArea(3, 6)]
    [Tooltip("Text địa danh đầu map. Dùng \\n để xuống dòng.")]
    public string locationTitleText = "NƯỚC\nCHU\nTỬ";

    public float locationFadeInTime = 0.6f;
    public float locationHoldTime = 1.6f;
    public float locationFadeOutTime = 0.6f;

    public float waitBeforeLockTime = 0.25f;
    public float waitIdleAfterLockTime = 0.25f;

    [Header("Intro - Wait Idle Before Lock")]
    public bool waitCharactersIdleBeforeIntroLock = true;
    public float introIdleVelocityThreshold = 0.05f;
    public float introIdleStableTime = 0.25f;
    public float maxWaitIntroIdleTime = 5f;
    public bool requireWukongIdleStateBeforeIntro = true;
    public bool requirePartyIdleStateBeforeIntro = false;

    [Header("Movement Lock")]
    [Tooltip("Kéo Wukong, Đường Tăng, Bát Giới, Sa Tăng vào đây.")]
    public GameObject[] charactersToFreeze;

    [Tooltip("Tên các script di chuyển cần tắt khi khóa.")]
    public string[] movementScriptNamesToDisable =
    {
        "PlayerController",
        "FollowerController"
    };

    public bool freezeRigidbodyWhenLocked = true;
    public bool setAnimatorSpeedToZeroWhenLocked = true;
    public bool restoreMovementAfterIntro = true;

    [Header("Player / Party")]
    public string playerTag = "Player";
    public PlayerController wukongController;
    public Rigidbody2D wukongRigidbody;
    public Animator wukongAnimator;
    public Behaviour[] partyFollowScripts;
    public Animator[] partyAnimators;

    [Header("Wukong Animator")]
    public string wukongIdleStateName = "Wukong1Idle";
    public string wukongSpeedParameterName = "Speed";
    public string[] wukongBoolParametersToFalse;
    public string[] wukongTriggersToReset;

    [Header("Party Animator")]
    public string partyIdleStateName = "";
    public string partySpeedParameterName = "Speed";
    public string[] partyBoolParametersToFalse;
    public string[] partyTriggersToReset;

    [Header("NPC Dialogue")]
    [Tooltip("Dialogue controller dùng box thoại tổng.")]
    public Map25DialogueController dialogueController;

    [Tooltip("Các dòng thoại với NPC Chu Tử.")]
    public Map25DialogueLine[] npcDialogueLines;

    [Tooltip("Chỉ mở thoại NPC khi đủ đoàn thỉnh kinh.")]
    public bool requireFullPartyBeforeNPCDialogue = true;

    [Tooltip("Kéo Đường Tăng, Trư Bát Giới, Sa Tăng vào đây.")]
    public GameObject[] requiredPartyObjects;

    [Tooltip("Nếu Wukong vào trigger khi chưa đủ đoàn, code sẽ chờ đủ đoàn rồi mới mở thoại.")]
    public bool waitUntilPartyReadyAfterTrigger = true;

    [Header("Heal After NPC Dialogue")]
    [Tooltip("Object hồi máu có gắn HealInteractable. Object vẫn hiện từ đầu, chỉ khóa tương tác.")]
    public HealInteractable postDialogueHealObject;

    [Tooltip("Sau thoại NPC, bắt buộc hồi máu rồi mới chuyển map.")]
    public bool requireHealBeforeLoadNextScene = true;

    [Tooltip("Tên hàm HealInteractable gọi ngược về StoryManager sau khi hồi máu.")]
    public string healCompletedNotifyMessageName = "NotifyPostDialogueHealCompleted";

    public bool refreshHealTriggerWhenEnabled = true;
    public bool lockHealInteractableOnAwake = true;
    public bool disableHealTriggerCollidersUntilUnlocked = true;
    public bool onlyDisableHealTriggerColliders = true;

    [Header("Load Next Map")]
    public bool loadNextSceneAfterHeal = true;

    [Tooltip("Tên scene tiếp theo. Phải đúng tên trong Build Settings.")]
    public string nextSceneName = "Map_3";

    public float delayBeforeLoadNextScene = 1f;
    public bool hideUIBeforeLoadNextScene = true;

    [Tooltip("Controller fade đen chuyển scene.")]
    public Map25SceneFadeController sceneFadeController;

    [Header("Debug")]
    public bool enableDebugLog = true;

    private VisualElement root;
    private VisualElement bossUIGroup;
    private VisualElement locationBox;
    private Label locationText;

    private bool introStarted;
    private bool introFinished;
    private bool npcDialogueStarted;
    private bool waitingPartyCoroutineStarted;
    private bool waitingPostDialogueHeal;
    private bool loadNextSceneFlowStarted;

    private Coroutine healRefreshCoroutine;
    private Collider2D[] postDialogueHealColliders;

    private void Awake()
    {
        FindReferencesIfNeeded();
        FindUIElements();

        CachePostDialogueHealColliders();

        if (lockHealInteractableOnAwake)
        {
            SetupPostDialogueHealObject(false);
        }

        PrepareVeryEarlyUIState();
    }

    private void Start()
    {
        StartCoroutine(BootMapIntroRoutine());
    }

    private IEnumerator BootMapIntroRoutine()
    {
        currentState = Map25StoryState.StartIntro;

        yield return null;

        FindReferencesIfNeeded();
        FindUIElements();

        PrepareMapStartState();

        if (!introStarted)
        {
            StartCoroutine(StartIntroRoutine());
        }

        Log("Map2.5 bắt đầu intro sau khi scene ổn định.");
    }

    private void FindReferencesIfNeeded()
    {
        FindHUDDocumentIfNeeded();

#if UNITY_2023_1_OR_NEWER
        if (hudController == null)
            hudController = FindFirstObjectByType<Map3HUDController>();

        if (dialogueController == null)
            dialogueController = FindFirstObjectByType<Map25DialogueController>();

        if (sceneFadeController == null)
            sceneFadeController = FindFirstObjectByType<Map25SceneFadeController>();
#else
        if (hudController == null)
            hudController = FindObjectOfType<Map3HUDController>();

        if (dialogueController == null)
            dialogueController = FindObjectOfType<Map25DialogueController>();

        if (sceneFadeController == null)
            sceneFadeController = FindObjectOfType<Map25SceneFadeController>();
#endif

        if (wukongController == null && !string.IsNullOrEmpty(playerTag))
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag(playerTag);

            if (playerObject != null)
            {
                wukongController = playerObject.GetComponent<PlayerController>();
                wukongRigidbody = playerObject.GetComponent<Rigidbody2D>();
                wukongAnimator = playerObject.GetComponentInChildren<Animator>(true);
            }
        }
    }

    // ======================================================
    // INTRO
    // ======================================================

    private void PrepareVeryEarlyUIState()
    {
        currentState = Map25StoryState.StartIntro;

        HideGameplayUI();
        HideBossUI();
        HideDialogueUI();
        HideLocationTitleImmediate();

        Log("Đã tắt UI tổng ngay từ Awake.");
    }

    private void PrepareMapStartState()
    {
        currentState = Map25StoryState.StartIntro;

        HideGameplayUI();
        HideBossUI();
        HideDialogueUI();
        HideLocationTitleImmediate();

        SetupPostDialogueHealObject(false);

        Log("Đã chuẩn bị trạng thái đầu Map2.5.");
    }

    private IEnumerator StartIntroRoutine()
    {
        introStarted = true;
        introFinished = false;
        currentState = Map25StoryState.StartIntro;

        FindReferencesIfNeeded();
        FindUIElements();

        HideGameplayUI();
        HideBossUI();
        HideDialogueUI();
        HideLocationTitleImmediate();

        if (waitBeforeLockTime > 0f)
        {
            yield return new WaitForSeconds(waitBeforeLockTime);
        }

        yield return StartCoroutine(WaitCharactersIdleBeforeIntroLockRoutine());

        LockWukongAndParty();
        LockCharacters(true);

        if (waitIdleAfterLockTime > 0f)
        {
            yield return new WaitForSeconds(waitIdleAfterLockTime);
        }

        HideGameplayUI();
        HideBossUI();
        HideDialogueUI();
        HideLocationTitleImmediate();

        yield return StartCoroutine(ShowLocationTitleRoutine());

        ShowGameplayUI();

        HideBossUI();
        HideDialogueUI();
        HideLocationTitleImmediate();

        if (restoreMovementAfterIntro)
        {
            LockCharacters(false);
            UnlockWukongAndParty();
        }

        currentState = Map25StoryState.ExploreBeforeNPC;
        introFinished = true;

        Log("Intro kết thúc. UI tổng đã bật lại, nhân vật đã mở khóa.");
    }

    private IEnumerator WaitCharactersIdleBeforeIntroLockRoutine()
    {
        if (!waitCharactersIdleBeforeIntroLock)
        {
            yield break;
        }

        float waitTimer = 0f;
        float stableTimer = 0f;

        while (true)
        {
            waitTimer += Time.deltaTime;

            bool allIdleReady = AreIntroCharactersIdleReady();

            if (allIdleReady)
            {
                stableTimer += Time.deltaTime;

                if (stableTimer >= introIdleStableTime)
                {
                    Log("Wukong và đoàn đã về Idle ổn định. Bắt đầu khóa di chuyển.");
                    yield break;
                }
            }
            else
            {
                stableTimer = 0f;
            }

            if (waitTimer >= maxWaitIntroIdleTime)
            {
                Debug.LogWarning("Map25StoryManager: Chờ Wukong/đoàn về Idle quá lâu. Tiếp tục intro để tránh kẹt.");
                yield break;
            }

            yield return null;
        }
    }

    private bool AreIntroCharactersIdleReady()
    {
        if (charactersToFreeze == null || charactersToFreeze.Length == 0)
        {
            return true;
        }

        for (int i = 0; i < charactersToFreeze.Length; i++)
        {
            GameObject character = charactersToFreeze[i];

            if (character == null)
            {
                continue;
            }

            if (!IsCharacterIdleReadyForIntro(character))
            {
                return false;
            }
        }

        return true;
    }

    private bool IsCharacterIdleReadyForIntro(GameObject character)
    {
        if (character == null)
        {
            return true;
        }

        Rigidbody2D[] bodies = character.GetComponentsInChildren<Rigidbody2D>(true);

        for (int i = 0; i < bodies.Length; i++)
        {
            Rigidbody2D rb = bodies[i];

            if (rb == null)
            {
                continue;
            }

            if (Mathf.Abs(rb.linearVelocity.x) > introIdleVelocityThreshold)
            {
                return false;
            }

            if (Mathf.Abs(rb.linearVelocity.y) > introIdleVelocityThreshold)
            {
                return false;
            }
        }

        Animator animator = character.GetComponentInChildren<Animator>(true);

        if (animator == null)
        {
            return true;
        }

        if (animator.IsInTransition(0))
        {
            return false;
        }

        bool isWukong = IsWukongCharacter(character);

        if (isWukong && requireWukongIdleStateBeforeIntro && !string.IsNullOrEmpty(wukongIdleStateName))
        {
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

            if (!stateInfo.IsName(wukongIdleStateName))
            {
                return false;
            }
        }

        if (!isWukong && requirePartyIdleStateBeforeIntro && !string.IsNullOrEmpty(partyIdleStateName))
        {
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

            if (!stateInfo.IsName(partyIdleStateName))
            {
                return false;
            }
        }

        float speedValue;

        if (TryGetAnimatorFloat(animator, "Speed", out speedValue))
        {
            if (Mathf.Abs(speedValue) > introIdleVelocityThreshold)
            {
                return false;
            }
        }

        return true;
    }

    private bool IsWukongCharacter(GameObject character)
    {
        if (character == null)
        {
            return false;
        }

        if (wukongAnimator != null && wukongAnimator.transform.IsChildOf(character.transform))
        {
            return true;
        }

        if (wukongController != null && wukongController.transform.IsChildOf(character.transform))
        {
            return true;
        }

        if (wukongController != null && wukongController.gameObject == character)
        {
            return true;
        }

        return false;
    }

    private bool TryGetAnimatorFloat(Animator animator, string parameterName, out float value)
    {
        value = 0f;

        if (animator == null || string.IsNullOrEmpty(parameterName))
        {
            return false;
        }

        AnimatorControllerParameter[] parameters = animator.parameters;

        for (int i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].name == parameterName &&
                parameters[i].type == AnimatorControllerParameterType.Float)
            {
                value = animator.GetFloat(parameterName);
                return true;
            }
        }

        return false;
    }

    private IEnumerator ShowLocationTitleRoutine()
    {
        if (hudController != null)
        {
            hudController.locationFadeInTime = locationFadeInTime;
            hudController.locationHoldTime = locationHoldTime;
            hudController.locationFadeOutTime = locationFadeOutTime;

            yield return StartCoroutine(hudController.PlayLocationTitle(locationTitleText));
            yield break;
        }

        if (locationBox == null || locationText == null)
        {
            FindUIElements();
        }

        if (locationBox == null)
        {
            Debug.LogWarning("Map25StoryManager: Không tìm thấy UI địa điểm và chưa gán HudController.");
            yield break;
        }

        if (locationText != null)
        {
            locationText.text = locationTitleText;
        }

        locationBox.style.display = DisplayStyle.Flex;
        locationBox.style.visibility = Visibility.Visible;
        locationBox.style.opacity = 0f;

        yield return StartCoroutine(FadeVisualElement(locationBox, 0f, 1f, locationFadeInTime));

        if (locationHoldTime > 0f)
        {
            yield return new WaitForSeconds(locationHoldTime);
        }

        yield return StartCoroutine(FadeVisualElement(locationBox, 1f, 0f, locationFadeOutTime));

        locationBox.style.opacity = 0f;
        locationBox.style.display = DisplayStyle.None;
    }

    private IEnumerator FadeVisualElement(VisualElement element, float from, float to, float duration)
    {
        if (element == null)
        {
            yield break;
        }

        if (duration <= 0f)
        {
            element.style.opacity = to;
            yield break;
        }

        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;

            float t = Mathf.Clamp01(timer / duration);
            element.style.opacity = Mathf.Lerp(from, to, t);

            yield return null;
        }

        element.style.opacity = to;
    }

    // ======================================================
    // NPC TRIGGER / DIALOGUE
    // ======================================================

    public void TryStartNPCDialogueFromTrigger(GameObject triggerObject)
    {
        if (!introFinished || currentState == Map25StoryState.StartIntro)
        {
            return;
        }

        if (npcDialogueStarted)
        {
            return;
        }

        if (!IsPlayerObject(triggerObject))
        {
            return;
        }

        if (requireFullPartyBeforeNPCDialogue && !IsFullPartyReady())
        {
            currentState = Map25StoryState.WaitingPartyForNPCDialogue;

            if (waitUntilPartyReadyAfterTrigger && !waitingPartyCoroutineStarted)
            {
                StartCoroutine(WaitFullPartyThenStartNPCDialogue());
            }

            Log("Wukong vào trigger NPC nhưng chưa đủ đoàn. Đang chờ đoàn.");
            return;
        }

        StartNPCDialogue();
    }

    private IEnumerator WaitFullPartyThenStartNPCDialogue()
    {
        waitingPartyCoroutineStarted = true;

        while (!npcDialogueStarted)
        {
            if (IsFullPartyReady())
            {
                StartNPCDialogue();
                yield break;
            }

            yield return null;
        }
    }

    private bool IsPlayerObject(GameObject targetObject)
    {
        if (targetObject == null)
        {
            return false;
        }

        if (!string.IsNullOrEmpty(playerTag) && targetObject.CompareTag(playerTag))
        {
            return true;
        }

        if (wukongController != null)
        {
            if (targetObject == wukongController.gameObject)
            {
                return true;
            }

            if (targetObject.transform.IsChildOf(wukongController.transform))
            {
                return true;
            }

            if (wukongController.transform.IsChildOf(targetObject.transform))
            {
                return true;
            }
        }

        return targetObject.GetComponentInParent<PlayerController>() != null;
    }

    private bool IsFullPartyReady()
    {
        if (!requireFullPartyBeforeNPCDialogue)
        {
            return true;
        }

        if (requiredPartyObjects == null || requiredPartyObjects.Length == 0)
        {
            // Chưa khai báo thì coi như đủ để tránh kẹt map.
            // Muốn bắt buộc đủ đoàn thì phải kéo NPC1/NPC2/NPC3 vào Required Party Objects.
            return true;
        }

        for (int i = 0; i < requiredPartyObjects.Length; i++)
        {
            GameObject member = requiredPartyObjects[i];

            if (member == null || !member.activeInHierarchy)
            {
                return false;
            }
        }

        return true;
    }

    private void StartNPCDialogue()
    {
        if (npcDialogueStarted)
        {
            return;
        }

        npcDialogueStarted = true;
        currentState = Map25StoryState.NPCDialogue;

        FindReferencesIfNeeded();

        LockWukongAndParty();
        LockCharacters(true);

        HideBossUI();
        HideLocationTitleImmediate();

        if (dialogueController != null && npcDialogueLines != null && npcDialogueLines.Length > 0)
        {
            dialogueController.StartDialogue(npcDialogueLines, OnNPCDialogueFinished);
        }
        else
        {
            OnNPCDialogueFinished();
        }

        Log("Bắt đầu thoại với NPC Nước Chu Tử.");
    }

    private void OnNPCDialogueFinished()
    {
        currentState = Map25StoryState.WaitHealAfterDialogue;

        UnlockWukongAndParty();
        LockCharacters(false);
        HideDialogueUI();
        HideBossUI();

        if (requireHealBeforeLoadNextScene && postDialogueHealObject != null)
        {
            waitingPostDialogueHeal = true;
            SetupPostDialogueHealObject(true);

            Log("Hết thoại NPC. Đã bật tương tác hồi máu.");
            return;
        }

        StartLoadNextSceneFlow();
    }

    // ======================================================
    // HEAL / LOAD NEXT MAP
    // ======================================================

    public void NotifyPostDialogueHealCompleted()
    {
        if (!waitingPostDialogueHeal)
        {
            Log("NotifyPostDialogueHealCompleted được gọi nhưng StoryManager không chờ hồi máu.");
            return;
        }

        waitingPostDialogueHeal = false;

        SetupPostDialogueHealObject(false);

        Log("Wukong đã hồi máu xong. Bắt đầu flow chuyển map.");
        StartLoadNextSceneFlow();
    }

    public void NotifyHealUsed()
    {
        NotifyPostDialogueHealCompleted();
    }

    private void StartLoadNextSceneFlow()
    {
        if (loadNextSceneFlowStarted)
        {
            return;
        }

        loadNextSceneFlowStarted = true;
        currentState = Map25StoryState.LoadNextMap;

        if (loadNextSceneAfterHeal)
        {
            StartCoroutine(LoadNextSceneAfterDelayRoutine());
            return;
        }

        FinishStory();
    }

    private IEnumerator LoadNextSceneAfterDelayRoutine()
    {
        if (delayBeforeLoadNextScene > 0f)
        {
            yield return new WaitForSeconds(delayBeforeLoadNextScene);
        }

        if (string.IsNullOrEmpty(nextSceneName))
        {
            Debug.LogWarning("Map25StoryManager: Chưa nhập Next Scene Name.");
            yield break;
        }

        if (hideUIBeforeLoadNextScene)
        {
            HideGameplayUI();
            HideBossUI();
            HideDialogueUI();
            HideLocationTitleImmediate();
        }

        yield return null;

        if (sceneFadeController != null)
        {
            sceneFadeController.FadeOutThenLoadScene(nextSceneName);
        }
        else
        {
            Debug.LogWarning("Map25StoryManager: Chưa gán Map25SceneFadeController, chuyển scene trực tiếp.");
            SceneManager.LoadScene(nextSceneName);
        }
    }

    private void CachePostDialogueHealColliders()
    {
        if (postDialogueHealObject == null)
        {
            postDialogueHealColliders = null;
            return;
        }

        postDialogueHealColliders = postDialogueHealObject.GetComponentsInChildren<Collider2D>(true);
    }

    private void SetPostDialogueHealCollidersActive(bool active)
    {
        if (!disableHealTriggerCollidersUntilUnlocked)
        {
            return;
        }

        if (postDialogueHealObject == null)
        {
            return;
        }

        if (postDialogueHealColliders == null || postDialogueHealColliders.Length == 0)
        {
            CachePostDialogueHealColliders();
        }

        if (postDialogueHealColliders == null)
        {
            return;
        }

        for (int i = 0; i < postDialogueHealColliders.Length; i++)
        {
            Collider2D targetCollider = postDialogueHealColliders[i];

            if (targetCollider == null)
            {
                continue;
            }

            if (onlyDisableHealTriggerColliders && !targetCollider.isTrigger)
            {
                continue;
            }

            targetCollider.enabled = active;
        }
    }

    private void SetupPostDialogueHealObject(bool interactionEnabled)
    {
        if (postDialogueHealObject == null)
        {
            return;
        }

        postDialogueHealObject.notifyObject = gameObject;
        postDialogueHealObject.notifyMessageName = healCompletedNotifyMessageName;

        if (postDialogueHealObject.interactHintObject != null)
        {
            postDialogueHealObject.interactHintObject.SetActive(false);
        }

        if (interactionEnabled)
        {
            SetPostDialogueHealCollidersActive(true);

            postDialogueHealObject.enabled = true;
            postDialogueHealObject.SendMessage("EnableInteraction", SendMessageOptions.DontRequireReceiver);

            if (refreshHealTriggerWhenEnabled)
            {
                if (healRefreshCoroutine != null)
                {
                    StopCoroutine(healRefreshCoroutine);
                }

                healRefreshCoroutine = StartCoroutine(RefreshHealTriggerAfterEnable());
            }

            Log("Đã bật tương tác HealInteractable sau thoại NPC.");
        }
        else
        {
            postDialogueHealObject.SendMessage("DisableInteraction", SendMessageOptions.DontRequireReceiver);
            postDialogueHealObject.enabled = false;

            SetPostDialogueHealCollidersActive(false);

            Log("Đã khóa HealInteractable và trigger hồi máu.");
        }
    }

    private IEnumerator RefreshHealTriggerAfterEnable()
    {
        yield return new WaitForFixedUpdate();

        if (postDialogueHealObject == null)
        {
            yield break;
        }

        Collider2D[] colliders = postDialogueHealObject.GetComponentsInChildren<Collider2D>(true);

        if (colliders == null || colliders.Length == 0)
        {
            yield break;
        }

        for (int i = 0; i < colliders.Length; i++)
        {
            Collider2D targetCollider = colliders[i];

            if (targetCollider == null)
            {
                continue;
            }

            if (onlyDisableHealTriggerColliders && !targetCollider.isTrigger)
            {
                continue;
            }

            bool wasEnabled = targetCollider.enabled;
            bool wasTrigger = targetCollider.isTrigger;

            targetCollider.isTrigger = true;
            targetCollider.enabled = false;

            yield return null;

            targetCollider.enabled = wasEnabled;
            targetCollider.isTrigger = wasTrigger;
        }
    }

    private void FinishStory()
    {
        currentState = Map25StoryState.Finished;

        UnlockWukongAndParty();
        LockCharacters(false);
        HideBossUI();

        Log("Map2.5 Story Finished.");
    }

    // ======================================================
    // UI HELPERS
    // ======================================================

    private void FindHUDDocumentIfNeeded()
    {
        if (mapHUDDocument != null)
        {
            return;
        }

#if UNITY_2023_1_OR_NEWER
        UIDocument[] documents = FindObjectsByType<UIDocument>(FindObjectsSortMode.None);
#else
        UIDocument[] documents = FindObjectsOfType<UIDocument>();
#endif

        for (int i = 0; i < documents.Length; i++)
        {
            if (documents[i] == null || documents[i].rootVisualElement == null)
            {
                continue;
            }

            VisualElement documentRoot = documents[i].rootVisualElement;

            bool hasLocationBox =
                FindVisualElementByNames(documentRoot, locationBoxName, "Box-mask", "box_mask", "box-mask", "Box_mask") != null;

            bool hasBossGroup =
                FindVisualElementByNames(documentRoot, bossUIGroupName, "boss-panel", "boss-1-group", "boss_1_group") != null;

            if (hasLocationBox || hasBossGroup)
            {
                mapHUDDocument = documents[i];
                break;
            }
        }
    }

    private void FindUIElements()
    {
        ForceMapHUDDocumentActive();

        if (mapHUDDocument == null)
        {
            FindHUDDocumentIfNeeded();
        }

        if (mapHUDDocument == null)
        {
            if (enableDebugLog)
            {
                Debug.LogWarning("Map25StoryManager: Chưa gán UIDocument chứa UI địa điểm.");
            }

            return;
        }

        root = mapHUDDocument.rootVisualElement;

        if (root == null)
        {
            if (enableDebugLog)
            {
                Debug.LogWarning("Map25StoryManager: UIDocument chưa có rootVisualElement.");
            }

            return;
        }

        bossUIGroup = FindVisualElementByNames(root, bossUIGroupName, "boss-panel", "boss-1-group", "boss_1_group");
        locationBox = FindVisualElementByNames(root, locationBoxName, "Box-mask", "box_mask", "box-mask", "Box_mask");
        locationText = FindLabelByNames(root, locationTextName, "box_text", "Box-text", "box-text", "Box_text");

        if (enableDebugLog)
        {
            Debug.Log(
                "Map25StoryManager FindUIElements | BossUIGroup: " + (bossUIGroup != null) +
                " | LocationBox: " + (locationBox != null) +
                " | LocationText: " + (locationText != null) +
                " | HudController: " + (hudController != null)
            );
        }
    }

    private void ForceMapHUDDocumentActive()
    {
        if (mapHUDDocument == null)
        {
            return;
        }

        GameObject documentObject = mapHUDDocument.gameObject;

        if (documentObject != null && !documentObject.activeSelf)
        {
            documentObject.SetActive(true);
        }
    }

    private VisualElement FindVisualElementByNames(VisualElement searchRoot, params string[] names)
    {
        if (searchRoot == null || names == null)
        {
            return null;
        }

        for (int i = 0; i < names.Length; i++)
        {
            string elementName = names[i];

            if (string.IsNullOrEmpty(elementName))
            {
                continue;
            }

            VisualElement result = searchRoot.Q<VisualElement>(elementName);

            if (result != null)
            {
                return result;
            }
        }

        return null;
    }

    private Label FindLabelByNames(VisualElement searchRoot, params string[] names)
    {
        if (searchRoot == null || names == null)
        {
            return null;
        }

        for (int i = 0; i < names.Length; i++)
        {
            string elementName = names[i];

            if (string.IsNullOrEmpty(elementName))
            {
                continue;
            }

            Label result = searchRoot.Q<Label>(elementName);

            if (result != null)
            {
                return result;
            }
        }

        return null;
    }

    private void HideLocationTitleImmediate()
    {
        if (hudController != null)
        {
            hudController.HideBoxInstant();
            return;
        }

        if (locationBox == null)
        {
            FindUIElements();
        }

        if (locationBox == null)
        {
            return;
        }

        locationBox.style.opacity = 0f;
        locationBox.style.display = DisplayStyle.None;
    }

    private void HideBossUI()
    {
        if (bossUIGroup == null)
        {
            FindUIElements();
        }

        if (bossUIGroup != null)
        {
            bossUIGroup.style.display = DisplayStyle.None;
            bossUIGroup.style.opacity = 0f;
        }
    }

    private void HideDialogueUI()
    {
        if (dialogueController != null)
        {
            dialogueController.HideDialogue();
        }
    }

    private void HideGameplayUI()
    {
        SetGameplayUIActive(false);
    }

    private void ShowGameplayUI()
    {
        SetGameplayUIActive(true);
    }

    private void SetGameplayUIActive(bool active)
    {
        if (globalHUD != null && !IsObjectContainingProtectedUIDocument(globalHUD))
        {
            globalHUD.SetActive(active);
        }

        if (gameplayUIObjects == null)
        {
            return;
        }

        for (int i = 0; i < gameplayUIObjects.Length; i++)
        {
            GameObject uiObject = gameplayUIObjects[i];

            if (uiObject == null)
            {
                continue;
            }

            if (IsObjectContainingProtectedUIDocument(uiObject))
            {
                if (enableDebugLog)
                {
                    Debug.LogWarning("Map25StoryManager: Không tắt " + uiObject.name + " vì object này chứa UIDocument địa điểm/fade.");
                }

                continue;
            }

            uiObject.SetActive(active);
        }
    }

    private bool IsObjectContainingProtectedUIDocument(GameObject targetObject)
    {
        if (targetObject == null)
        {
            return false;
        }

        if (mapHUDDocument != null)
        {
            GameObject documentObject = mapHUDDocument.gameObject;

            if (documentObject == targetObject)
            {
                return true;
            }

            if (documentObject != null && documentObject.transform.IsChildOf(targetObject.transform))
            {
                return true;
            }
        }

        if (sceneFadeController != null && sceneFadeController.uiDocument != null)
        {
            GameObject fadeDocumentObject = sceneFadeController.uiDocument.gameObject;

            if (fadeDocumentObject == targetObject)
            {
                return true;
            }

            if (fadeDocumentObject != null && fadeDocumentObject.transform.IsChildOf(targetObject.transform))
            {
                return true;
            }
        }

        return false;
    }

    // ======================================================
    // LOCK HELPERS
    // ======================================================

    private void LockCharacters(bool locked)
    {
        if (charactersToFreeze == null)
        {
            return;
        }

        for (int i = 0; i < charactersToFreeze.Length; i++)
        {
            GameObject character = charactersToFreeze[i];

            if (character == null)
            {
                continue;
            }

            if (freezeRigidbodyWhenLocked)
            {
                FreezeRigidbody(character);
            }

            if (setAnimatorSpeedToZeroWhenLocked && locked)
            {
                SetAnimatorSpeedToZero(character);
            }

            character.SendMessage("SetControlLocked", locked, SendMessageOptions.DontRequireReceiver);
            character.SendMessage("LockMovement", locked, SendMessageOptions.DontRequireReceiver);
            character.SendMessage("SetMovementLocked", locked, SendMessageOptions.DontRequireReceiver);
            character.SendMessage("SetCanMove", !locked, SendMessageOptions.DontRequireReceiver);

            SetMovementScriptsEnabled(character, !locked);
        }

        Log("LockCharacters = " + locked);
    }

    private void FreezeRigidbody(GameObject character)
    {
        if (character == null)
        {
            return;
        }

        Rigidbody2D[] bodies = character.GetComponentsInChildren<Rigidbody2D>(true);

        for (int i = 0; i < bodies.Length; i++)
        {
            Rigidbody2D rb = bodies[i];

            if (rb == null)
            {
                continue;
            }

            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
    }

    private void SetAnimatorSpeedToZero(GameObject character)
    {
        if (character == null)
        {
            return;
        }

        Animator[] animators = character.GetComponentsInChildren<Animator>(true);

        for (int i = 0; i < animators.Length; i++)
        {
            SetAnimatorFloatIfExists(animators[i], "Speed", 0f);
        }
    }

    private void SetMovementScriptsEnabled(GameObject character, bool enabled)
    {
        if (character == null || movementScriptNamesToDisable == null)
        {
            return;
        }

        MonoBehaviour[] behaviours = character.GetComponentsInChildren<MonoBehaviour>(true);

        for (int i = 0; i < behaviours.Length; i++)
        {
            MonoBehaviour behaviour = behaviours[i];

            if (behaviour == null)
            {
                continue;
            }

            string scriptName = behaviour.GetType().Name;

            for (int n = 0; n < movementScriptNamesToDisable.Length; n++)
            {
                if (scriptName == movementScriptNamesToDisable[n])
                {
                    behaviour.enabled = enabled;
                    break;
                }
            }
        }
    }

    private void LockWukongAndParty()
    {
        LockWukong();
        LockParty();
    }

    private void UnlockWukongAndParty()
    {
        UnlockWukong();
        UnlockParty();
    }

    private void LockWukong()
    {
        if (wukongRigidbody != null)
        {
            wukongRigidbody.linearVelocity = Vector2.zero;
            wukongRigidbody.angularVelocity = 0f;
        }

        if (wukongController != null)
        {
            wukongController.enabled = false;
        }

        ForceWukongIdle();
    }

    private void UnlockWukong()
    {
        if (wukongRigidbody != null)
        {
            wukongRigidbody.linearVelocity = Vector2.zero;
            wukongRigidbody.angularVelocity = 0f;
        }

        if (wukongController != null)
        {
            wukongController.enabled = true;
        }
    }

    private void LockParty()
    {
        if (partyFollowScripts != null)
        {
            for (int i = 0; i < partyFollowScripts.Length; i++)
            {
                if (partyFollowScripts[i] != null)
                {
                    partyFollowScripts[i].enabled = false;
                }
            }
        }

        ForcePartyIdle();
    }

    private void UnlockParty()
    {
        if (partyFollowScripts != null)
        {
            for (int i = 0; i < partyFollowScripts.Length; i++)
            {
                if (partyFollowScripts[i] != null)
                {
                    partyFollowScripts[i].enabled = true;
                }
            }
        }
    }

    private void ForceWukongIdle()
    {
        if (wukongAnimator == null)
        {
            return;
        }

        SetAnimatorFloatIfExists(wukongAnimator, wukongSpeedParameterName, 0f);

        if (wukongBoolParametersToFalse != null)
        {
            for (int i = 0; i < wukongBoolParametersToFalse.Length; i++)
            {
                SetAnimatorBoolIfExists(wukongAnimator, wukongBoolParametersToFalse[i], false);
            }
        }

        if (wukongTriggersToReset != null)
        {
            for (int i = 0; i < wukongTriggersToReset.Length; i++)
            {
                ResetAnimatorTriggerIfExists(wukongAnimator, wukongTriggersToReset[i]);
            }
        }

        if (!string.IsNullOrEmpty(wukongIdleStateName) && HasAnimatorState(wukongAnimator, wukongIdleStateName))
        {
            wukongAnimator.Play(wukongIdleStateName, 0, 0f);
            wukongAnimator.Update(0f);
        }
    }

    private void ForcePartyIdle()
    {
        if (partyAnimators == null)
        {
            return;
        }

        for (int i = 0; i < partyAnimators.Length; i++)
        {
            Animator targetAnimator = partyAnimators[i];

            if (targetAnimator == null)
            {
                continue;
            }

            SetAnimatorFloatIfExists(targetAnimator, partySpeedParameterName, 0f);

            if (partyBoolParametersToFalse != null)
            {
                for (int j = 0; j < partyBoolParametersToFalse.Length; j++)
                {
                    SetAnimatorBoolIfExists(targetAnimator, partyBoolParametersToFalse[j], false);
                }
            }

            if (partyTriggersToReset != null)
            {
                for (int j = 0; j < partyTriggersToReset.Length; j++)
                {
                    ResetAnimatorTriggerIfExists(targetAnimator, partyTriggersToReset[j]);
                }
            }

            if (!string.IsNullOrEmpty(partyIdleStateName) && HasAnimatorState(targetAnimator, partyIdleStateName))
            {
                targetAnimator.Play(partyIdleStateName, 0, 0f);
                targetAnimator.Update(0f);
            }
        }
    }

    // ======================================================
    // ANIMATOR HELPERS
    // ======================================================

    private void SetAnimatorFloatIfExists(Animator targetAnimator, string parameterName, float value)
    {
        if (targetAnimator == null || string.IsNullOrEmpty(parameterName))
        {
            return;
        }

        AnimatorControllerParameter[] parameters = targetAnimator.parameters;

        for (int i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].name == parameterName &&
                parameters[i].type == AnimatorControllerParameterType.Float)
            {
                targetAnimator.SetFloat(parameterName, value);
                return;
            }
        }
    }

    private void SetAnimatorBoolIfExists(Animator targetAnimator, string parameterName, bool value)
    {
        if (targetAnimator == null || string.IsNullOrEmpty(parameterName))
        {
            return;
        }

        AnimatorControllerParameter[] parameters = targetAnimator.parameters;

        for (int i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].name == parameterName &&
                parameters[i].type == AnimatorControllerParameterType.Bool)
            {
                targetAnimator.SetBool(parameterName, value);
                return;
            }
        }
    }

    private void ResetAnimatorTriggerIfExists(Animator targetAnimator, string parameterName)
    {
        if (targetAnimator == null || string.IsNullOrEmpty(parameterName))
        {
            return;
        }

        AnimatorControllerParameter[] parameters = targetAnimator.parameters;

        for (int i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].name == parameterName &&
                parameters[i].type == AnimatorControllerParameterType.Trigger)
            {
                targetAnimator.ResetTrigger(parameterName);
                return;
            }
        }
    }

    private bool HasAnimatorState(Animator anim, string stateName)
    {
        if (anim == null || string.IsNullOrEmpty(stateName))
        {
            return false;
        }

        return anim.HasState(0, Animator.StringToHash(stateName));
    }

    private void Log(string message)
    {
        if (!enableDebugLog)
        {
            return;
        }

        Debug.Log("[Map25StoryManager] " + message + " Current State = " + currentState);
    }
}
