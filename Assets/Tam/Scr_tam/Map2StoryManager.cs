using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

/// <summary>
/// Quản lý cốt truyện Map2.
/// 
/// Phase 0: StartIntro
/// - Vào map.
/// - Ẩn UI tổng.
/// - Ẩn UI Boss1.
/// - Ẩn box trò chuyện.
/// - Chờ Wukong + đoàn về Idle.
/// - Khóa di chuyển.
/// - Hiện bảng địa danh dọc fade in / giữ / fade out.
/// 
/// Phase 1: ExploreBeforeBoss
/// - Hiện lại UI tổng.
/// - Vẫn ẩn box trò chuyện.
/// - Mở khóa di chuyển.
/// - Boss1 chưa active.
/// 
/// Phase 2: PreBossDialogue
/// - Wukong chạm trigger trước boss.
/// - Chờ Wukong về Idle.
/// - Khóa Wukong + đoàn.
/// - Hiện hội thoại trước boss.
/// 
/// Phase 3: BossFight
/// - Hết thoại trước boss.
/// - Hiện UI máu Boss1.
/// - Boss1 ActivateCombat().
/// 
/// Phase 4: PostBossDialogue
/// - Boss1 chết.
/// - Không khóa Wukong + đoàn.
/// - Hiện hội thoại sau boss.
/// - Hết thoại thì fade đen rồi chuyển map.
/// </summary>
public class Map2StoryManager : MonoBehaviour
{
    public enum Map2Phase
    {
        StartIntro,
        ExploreBeforeBoss,
        PreBossDialogue,
        BossFight,
        PostBossDialogue,
        Finished
    }

    [Header("Phase")]
    [Tooltip("Phase hiện tại của Map2.")]
    public Map2Phase currentPhase = Map2Phase.StartIntro;

    [Header("UI Document")]
    [Tooltip("UIDocument chứa Map2HUD.")]
    public UIDocument map2HUDDocument;

    [Header("Game Over")]
    [Tooltip("UI GameOver riêng của Map2. Kéo object OVER có GameOverMenuController vào đây.")]
    public GameOverMenuController gameOverMenuController;

    [Tooltip("Bật lên để khi Wukong chết xong animation thì hiện GameOver.")]
    public bool gameOverWhenWukongDead = true;

    [Tooltip("Bật lên để khi máu đoàn thỉnh kinh về 0 thì hiện GameOver.")]
    public bool gameOverWhenPartyDead = true;

    [Header("Party Health Notify")]
    [Tooltip("Script máu tổng của đoàn thỉnh kinh. Kéo PartyManager có PartyHealth vào đây.")]
    public PartyHealth partyHealth;

    [Tooltip("Nếu chưa gán PartyHealth, tự tìm PartyHealth trong scene.")]
    public bool autoFindPartyHealthIfMissing = true;

    private bool gameOverStarted;

    [Header("Gameplay UI Objects")]
    [Tooltip("Các object UI tổng cần ẩn ở đầu map và hiện lại sau intro. Ví dụ: GlobalHUD.")]
    public GameObject[] gameplayUIObjects;

    [Header("Dialogue")]
    [Tooltip("Controller quản lý box trò chuyện tổng của Map2.")]
    public Map2GlobalDialogueController dialogueController;

    [Tooltip("Tự động tìm Map2GlobalDialogueController nếu chưa gán.")]
    public bool autoFindDialogueController = true;

    [Header("Boss UI")]
    [Tooltip("Tên group UI máu Boss1 trong Map2HUD.")]
    public string boss1GroupName = "boss-1-group";

    [Header("Location Title UI")]
    [Tooltip("Tên group bảng địa danh dọc.")]
    public string locationBoxName = "box_mask";

    [Tooltip("Tên Label hiển thị địa danh.")]
    public string locationTextName = "box_text";

    [TextArea(3, 6)]
    [Tooltip("Text địa danh hiện đầu map. Dùng \\n để xuống dòng.")]
    public string locationTitleText = "THÁC\nLINH\nVÂN";

    [Tooltip("Thời gian fade in bảng địa danh.")]
    public float locationFadeInTime = 0.6f;

    [Tooltip("Thời gian giữ bảng địa danh sau khi fade in xong.")]
    public float locationHoldTime = 1.6f;

    [Tooltip("Thời gian fade out bảng địa danh.")]
    public float locationFadeOutTime = 0.6f;

    [Header("Wait Idle Before Intro")]
    [Tooltip("Chờ một khoảng ngắn để Wukong và đoàn kịp ổn định sau khi load map.")]
    public float waitBeforeLockTime = 0.25f;

    [Tooltip("Sau khi khóa di chuyển, chờ thêm một chút cho animation về Idle.")]
    public float waitIdleAfterLockTime = 0.25f;

    [Header("Pre Boss Dialogue")]
    [Tooltip("Danh sách thoại trước Boss1.")]
    public Map2GlobalDialogueLine[] preBossDialogueLines;

    [Tooltip("Khóa Wukong và đoàn trong lúc hội thoại trước Boss.")]
    public bool lockCharactersDuringPreBossDialogue = true;

    [Tooltip("Chờ Wukong về Idle rồi mới mở thoại trước Boss.")]
    public bool waitWukongIdleBeforePreBossDialogue = true;

    [Tooltip("Object Wukong. Dùng để kiểm tra Animator/Rigidbody khi chờ Idle.")]
    public GameObject wukongObject;

    [Tooltip("Animator của Wukong.")]
    public Animator wukongAnimator;

    [Tooltip("Rigidbody2D của Wukong.")]
    public Rigidbody2D wukongRigidbody;

    [Tooltip("Tên state Idle thật của Wukong trong Animator.")]
    public string wukongIdleStateNameForDialogueWait = "Wukong Idle";

    [Tooltip("Tốc độ Rigidbody nhỏ hơn ngưỡng này thì coi như Wukong đứng yên.")]
    public float wukongIdleVelocityThreshold = 0.05f;

    [Tooltip("Wukong phải Idle ổn định bao lâu mới mở thoại.")]
    public float wukongIdleStableTime = 0.25f;

    [Tooltip("Thời gian chờ tối đa. Quá thời gian này vẫn mở thoại để tránh kẹt.")]
    public float maxWaitWukongIdleTime = 5f;

    [Header("Movement Lock")]
    [Tooltip("Các object nhân vật cần khóa. Chỉ cần kéo GameObject vào đây.")]
    public GameObject[] charactersToFreeze;

    [Tooltip("Tên các script di chuyển cần tắt khi khóa điều khiển. Không cần kéo component script.")]
    public string[] movementScriptNamesToDisable =
    {
        "PlayerController",
        "FollowerController"
    };

    [Tooltip("Khi khóa, có dừng Rigidbody2D của object và object con không.")]
    public bool freezeRigidbodyWhenLocked = true;

    [Tooltip("Khi khóa, có set Animator Speed = 0 không.")]
    public bool setAnimatorSpeedToZeroWhenLocked = true;

    [Tooltip("Có khôi phục di chuyển sau intro không.")]
    public bool restoreMovementAfterIntro = true;

    [Header("Post Boss Dialogue")]
    [Tooltip("Danh sách thoại sau khi Boss1 chết.")]
    public Map2GlobalDialogueLine[] postBossDialogueLines;

    [Tooltip("Sau khi hết thoại sau boss thì chuyển map.")]
    public bool loadNextSceneAfterPostBossDialogue = true;

    [Tooltip("Tên scene tiếp theo cần chuyển sang. Phải đúng tên trong Build Settings.")]
    public string nextSceneName = "Map3";

    [Tooltip("Delay nhẹ trước khi chuyển scene sau khi hết thoại.")]
    public float delayBeforeLoadNextScene = 0.5f;

    [Tooltip("Ẩn UI máu Boss1 khi bắt đầu thoại sau boss.")]
    public bool hideBoss1UIWhenPostDialogueStart = true;

    [Header("Scene Fade")]
    [Tooltip("Controller fade đen khi vào map và kết thúc map.")]
    public Map2SceneFadeController sceneFadeController;

    [Tooltip("Tự động tìm Map2SceneFadeController nếu chưa gán.")]
    public bool autoFindSceneFadeController = true;

    [Header("Boss1")]
    [Tooltip("Boss1 trong Map2. Đầu map boss chưa active.")]
    public Boss1Controller boss1;

    [Header("Debug")]
    public bool enableDebugLog = true;

    private VisualElement root;
    private VisualElement boss1Group;
    private VisualElement locationBox;
    private Label locationText;

    private bool preBossDialogueStarted;
    private bool bossFightStarted;
    private Coroutine preBossDialogueCoroutine;

    private bool introStarted;
    private bool introFinished;

    private bool postBossDialogueStarted;
    private Coroutine postBossDialogueCoroutine;

    private void Awake()
    {
        AutoBindReferences();
        FindUIElements();
        SetupDeathNotifyTargets();
    }

    private void Start()
    {
        AutoBindReferences();
        FindUIElements();
        SetupDeathNotifyTargets();

        PrepareMapStartState();

        if (!introStarted)
        {
            StartCoroutine(StartIntroRoutine());
        }
    }

    private void Update()
    {
        if (PauseMenuController.IsPausedGlobal)
        {
            return;
        }

        CheckBoss1DeadForPostDialogue();
    }

    private void AutoBindReferences()
    {
#if UNITY_2023_1_OR_NEWER
        if (map2HUDDocument == null)
        {
            UIDocument[] documents = FindObjectsByType<UIDocument>(FindObjectsSortMode.None);

            for (int i = 0; i < documents.Length; i++)
            {
                if (documents[i] == null)
                {
                    continue;
                }

                VisualElement documentRoot = documents[i].rootVisualElement;

                if (documentRoot == null)
                {
                    continue;
                }

                bool hasLocationBox = documentRoot.Q<VisualElement>(locationBoxName) != null;
                bool hasBossGroup = documentRoot.Q<VisualElement>(boss1GroupName) != null;

                if (hasLocationBox || hasBossGroup)
                {
                    map2HUDDocument = documents[i];
                    break;
                }
            }
        }

        if (boss1 == null)
        {
            boss1 = FindFirstObjectByType<Boss1Controller>();
        }

        if (autoFindDialogueController && dialogueController == null)
        {
            dialogueController = FindFirstObjectByType<Map2GlobalDialogueController>();
        }
#else
        if (map2HUDDocument == null)
        {
            UIDocument[] documents = FindObjectsOfType<UIDocument>();

            for (int i = 0; i < documents.Length; i++)
            {
                if (documents[i] == null)
                {
                    continue;
                }

                VisualElement documentRoot = documents[i].rootVisualElement;

                if (documentRoot == null)
                {
                    continue;
                }

                bool hasLocationBox = documentRoot.Q<VisualElement>(locationBoxName) != null;
                bool hasBossGroup = documentRoot.Q<VisualElement>(boss1GroupName) != null;

                if (hasLocationBox || hasBossGroup)
                {
                    map2HUDDocument = documents[i];
                    break;
                }
            }
        }

        if (boss1 == null)
        {
            boss1 = FindObjectOfType<Boss1Controller>();
        }

        if (autoFindDialogueController && dialogueController == null)
        {
            dialogueController = FindObjectOfType<Map2GlobalDialogueController>();
        }
#endif

        AutoBindWukongReferences();
        AutoBindSceneFadeController();
    }
    private void SetupDeathNotifyTargets()
    {
        SetupWukongDeathNotifyTarget();
        SetupPartyDeathNotifyTargets();
    }

    private void SetupWukongDeathNotifyTarget()
    {
        if (wukongObject == null)
        {
            AutoBindWukongReferences();
        }

        if (wukongObject == null)
        {
            Debug.LogWarning("Map2StoryManager: Chưa gán Wukong Object nên không thể setup Death Notify cho Wukong.");
            return;
        }

        PlayerController playerController = wukongObject.GetComponent<PlayerController>();

        if (playerController == null)
        {
            playerController = wukongObject.GetComponentInChildren<PlayerController>(true);
        }

        if (playerController == null)
        {
            Debug.LogWarning("Map2StoryManager: Không tìm thấy PlayerController để setup Death Notify cho Wukong.");
            return;
        }

        playerController.SetDeathNotifyTarget(gameObject);
        playerController.SetDeathNotifyMessageName("NotifyWukongDeathFinished");

        if (enableDebugLog)
        {
            Debug.Log("Map2StoryManager: Đã setup Death Notify Target cho Wukong.");
        }
    }

    private void SetupPartyDeathNotifyTargets()
    {
        bool hasSetupAnyPartyHealth = false;

        if (partyHealth != null)
        {
            SetupOnePartyHealthDeathNotify(partyHealth);
            hasSetupAnyPartyHealth = true;
        }

        if (charactersToFreeze != null && charactersToFreeze.Length > 0)
        {
            for (int i = 0; i < charactersToFreeze.Length; i++)
            {
                GameObject character = charactersToFreeze[i];

                if (character == null)
                {
                    continue;
                }

                PartyHealth foundPartyHealth = character.GetComponent<PartyHealth>();

                if (foundPartyHealth == null)
                {
                    foundPartyHealth = character.GetComponentInChildren<PartyHealth>(true);
                }

                if (foundPartyHealth == null)
                {
                    foundPartyHealth = character.GetComponentInParent<PartyHealth>();
                }

                if (foundPartyHealth == null)
                {
                    continue;
                }

                SetupOnePartyHealthDeathNotify(foundPartyHealth);
                hasSetupAnyPartyHealth = true;
            }
        }

        if (!hasSetupAnyPartyHealth && autoFindPartyHealthIfMissing)
        {
#if UNITY_2023_1_OR_NEWER
        PartyHealth[] allPartyHealth = FindObjectsByType<PartyHealth>(FindObjectsSortMode.None);
#else
            PartyHealth[] allPartyHealth = FindObjectsOfType<PartyHealth>();
#endif

            for (int i = 0; i < allPartyHealth.Length; i++)
            {
                if (allPartyHealth[i] == null)
                {
                    continue;
                }

                SetupOnePartyHealthDeathNotify(allPartyHealth[i]);
                hasSetupAnyPartyHealth = true;
            }
        }

        if (!hasSetupAnyPartyHealth)
        {
            Debug.LogWarning("Map2StoryManager: Không tìm thấy PartyHealth nào để setup Death Notify cho đoàn. Hãy kéo PartyManager vào field Party Health.");
        }
    }

    private void SetupOnePartyHealthDeathNotify(PartyHealth targetPartyHealth)
    {
        if (targetPartyHealth == null)
        {
            return;
        }

        targetPartyHealth.SetDeathNotifyTarget(gameObject);
        targetPartyHealth.SetDeathNotifyMessageName("NotifyPartyDead");

        if (enableDebugLog)
        {
            Debug.Log("Map2StoryManager: Đã setup Death Notify Target cho PartyHealth trên object: " + targetPartyHealth.gameObject.name);
        }
    }

    private void AutoBindWukongReferences()
    {
        if (wukongObject == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

            if (playerObject != null)
            {
                wukongObject = playerObject;
            }
        }

        if (wukongObject == null)
        {
            return;
        }

        if (wukongAnimator == null)
        {
            wukongAnimator = wukongObject.GetComponentInChildren<Animator>();
        }

        if (wukongRigidbody == null)
        {
            wukongRigidbody = wukongObject.GetComponent<Rigidbody2D>();
        }
    }

    private void AutoBindSceneFadeController()
    {
        if (!autoFindSceneFadeController)
        {
            return;
        }

        if (sceneFadeController != null)
        {
            return;
        }

#if UNITY_2023_1_OR_NEWER
        sceneFadeController = FindFirstObjectByType<Map2SceneFadeController>();
#else
        sceneFadeController = FindObjectOfType<Map2SceneFadeController>();
#endif

        if (sceneFadeController == null && enableDebugLog)
        {
            Debug.LogWarning("Map2StoryManager: Chưa tìm thấy Map2SceneFadeController trong scene.");
        }
    }

    private void FindUIElements()
    {
        if (map2HUDDocument == null)
        {
            if (enableDebugLog)
            {
                Debug.LogWarning("Map2StoryManager: Chưa gán Map2 HUD UIDocument.");
            }

            return;
        }

        root = map2HUDDocument.rootVisualElement;

        if (root == null)
        {
            if (enableDebugLog)
            {
                Debug.LogWarning("Map2StoryManager: UIDocument chưa có rootVisualElement.");
            }

            return;
        }

        boss1Group = root.Q<VisualElement>(boss1GroupName);
        locationBox = root.Q<VisualElement>(locationBoxName);
        locationText = root.Q<Label>(locationTextName);

        if (enableDebugLog)
        {
            Debug.Log(
                "Map2StoryManager FindUIElements | Boss1Group: " + (boss1Group != null) +
                " | LocationBox: " + (locationBox != null) +
                " | LocationText: " + (locationText != null) +
                " | DialogueController: " + (dialogueController != null) +
                " | SceneFadeController: " + (sceneFadeController != null)
            );
        }
    }

    private void PrepareMapStartState()
    {
        currentPhase = Map2Phase.StartIntro;

        HideGameplayUI();
        HideBoss1UI();
        HideDialogueUI();
        HideLocationTitleImmediate();

        PrepareBoss1StartState();

        if (enableDebugLog)
        {
            Debug.Log("Map2StoryManager: Đã chuẩn bị trạng thái đầu map.");
        }
    }

    private void PrepareBoss1StartState()
    {
        if (boss1 == null)
        {
            return;
        }

        boss1.activeOnStart = false;
        boss1.forceCombatOnStart = false;
        boss1.isActive = false;

        Rigidbody2D bossRb = boss1.GetComponent<Rigidbody2D>();

        if (bossRb != null)
        {
            bossRb.linearVelocity = Vector2.zero;
        }
    }

    private IEnumerator StartIntroRoutine()
    {
        introStarted = true;
        introFinished = false;
        currentPhase = Map2Phase.StartIntro;

        HideGameplayUI();
        HideBoss1UI();
        HideDialogueUI();

        yield return new WaitForSeconds(waitBeforeLockTime);

        LockCharacters(true);

        yield return new WaitForSeconds(waitIdleAfterLockTime);

        yield return StartCoroutine(ShowLocationTitleRoutine());

        ShowGameplayUI();
        HideBoss1UI();
        HideDialogueUI();

        if (restoreMovementAfterIntro)
        {
            LockCharacters(false);
        }

        currentPhase = Map2Phase.ExploreBeforeBoss;
        introFinished = true;

        if (enableDebugLog)
        {
            Debug.Log("Map2StoryManager: Kết thúc Phase 0, chuyển sang Phase 1 ExploreBeforeBoss.");
        }
    }

    private IEnumerator ShowLocationTitleRoutine()
    {
        if (locationBox == null || locationText == null)
        {
            FindUIElements();
        }

        if (locationBox == null)
        {
            Debug.LogWarning("Map2StoryManager: Không tìm thấy #box_mask để hiện địa danh.");
            yield break;
        }

        if (locationText != null)
        {
            locationText.text = locationTitleText;
        }

        locationBox.style.display = DisplayStyle.Flex;
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
            float alpha = Mathf.Lerp(from, to, t);

            element.style.opacity = alpha;

            yield return null;
        }

        element.style.opacity = to;
    }

    private void HideLocationTitleImmediate()
    {
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

    private void HideBoss1UI()
    {
        if (boss1Group == null)
        {
            FindUIElements();
        }

        if (boss1Group != null)
        {
            boss1Group.style.display = DisplayStyle.None;
        }
    }

    private void ShowBoss1UI()
    {
        if (boss1Group == null)
        {
            FindUIElements();
        }

        if (boss1Group != null)
        {
            boss1Group.style.display = DisplayStyle.Flex;
        }
    }

    private void HideDialogueUI()
    {
        if (dialogueController == null && autoFindDialogueController)
        {
            AutoBindReferences();
        }

        if (dialogueController != null)
        {
            dialogueController.HideDialogue();
        }
    }

    private void HideGameplayUI()
    {
        if (gameplayUIObjects == null)
        {
            return;
        }

        for (int i = 0; i < gameplayUIObjects.Length; i++)
        {
            if (gameplayUIObjects[i] != null)
            {
                gameplayUIObjects[i].SetActive(false);
            }
        }
    }

    private void ShowGameplayUI()
    {
        if (gameplayUIObjects == null)
        {
            return;
        }

        for (int i = 0; i < gameplayUIObjects.Length; i++)
        {
            if (gameplayUIObjects[i] != null)
            {
                gameplayUIObjects[i].SetActive(true);
            }
        }
    }

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

        if (enableDebugLog)
        {
            Debug.Log("Map2StoryManager: LockCharacters = " + locked);
        }
    }

    private void FreezeRigidbody(GameObject character)
    {
        if (character == null)
        {
            return;
        }

        Rigidbody2D rb = character.GetComponent<Rigidbody2D>();

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        Rigidbody2D[] childBodies = character.GetComponentsInChildren<Rigidbody2D>(true);

        for (int i = 0; i < childBodies.Length; i++)
        {
            if (childBodies[i] != null)
            {
                childBodies[i].linearVelocity = Vector2.zero;
            }
        }
    }

    private void SetAnimatorSpeedToZero(GameObject character)
    {
        if (character == null)
        {
            return;
        }

        Animator animator = character.GetComponent<Animator>();

        if (animator != null)
        {
            SetAnimatorFloatIfExists(animator, "Speed", 0f);
        }

        Animator[] childAnimators = character.GetComponentsInChildren<Animator>(true);

        for (int i = 0; i < childAnimators.Length; i++)
        {
            if (childAnimators[i] != null)
            {
                SetAnimatorFloatIfExists(childAnimators[i], "Speed", 0f);
            }
        }
    }

    private void SetMovementScriptsEnabled(GameObject character, bool enabled)
    {
        if (character == null)
        {
            return;
        }

        if (movementScriptNamesToDisable == null || movementScriptNamesToDisable.Length == 0)
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

                    if (enableDebugLog)
                    {
                        Debug.Log(
                            "Map2StoryManager: " +
                            (enabled ? "Bật lại " : "Tắt ") +
                            scriptName +
                            " trên " +
                            behaviour.gameObject.name
                        );
                    }

                    break;
                }
            }
        }
    }

    private void SetAnimatorFloatIfExists(Animator animator, string parameterName, float value)
    {
        if (animator == null)
        {
            return;
        }

        if (string.IsNullOrEmpty(parameterName))
        {
            return;
        }

        AnimatorControllerParameter[] parameters = animator.parameters;

        for (int i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].name == parameterName)
            {
                animator.SetFloat(parameterName, value);
                return;
            }
        }
    }

    public void StartPreBossDialogueByTrigger()
    {
        if (PauseMenuController.IsPausedGlobal)
        {
            if (enableDebugLog)
            {
                Debug.Log("Map2StoryManager: Đang Pause nên không kích hoạt PreBossDialogue.");
            }

            return;
        }

        if (preBossDialogueStarted)
        {
            if (enableDebugLog)
            {
                Debug.Log("Map2StoryManager: PreBossDialogue đã chạy rồi, không kích hoạt lại.");
            }

            return;
        }

        if (currentPhase != Map2Phase.ExploreBeforeBoss)
        {
            if (enableDebugLog)
            {
                Debug.Log("Map2StoryManager: Chưa thể bắt đầu PreBossDialogue vì phase hiện tại là " + currentPhase);
            }

            return;
        }

        preBossDialogueStarted = true;

        if (preBossDialogueCoroutine != null)
        {
            StopCoroutine(preBossDialogueCoroutine);
        }

        preBossDialogueCoroutine = StartCoroutine(WaitWukongIdleThenStartPreBossDialogueRoutine());
    }

    private IEnumerator WaitWukongIdleThenStartPreBossDialogueRoutine()
    {
       

        if (enableDebugLog)
        {
            Debug.Log("Map2StoryManager: Trigger trước Boss1 đã kích hoạt. Bắt đầu chờ Wukong về Idle.");
        }

        float waitTimer = 0f;
        float idleTimer = 0f;

        while (waitWukongIdleBeforePreBossDialogue)
        {
            if (PauseMenuController.IsPausedGlobal)
            {
                yield return null;
                continue;
            }

            waitTimer += Time.deltaTime;

            bool idleReady = IsWukongIdleReadyForDialogue();
            if (idleReady)
            {
                idleTimer += Time.deltaTime;

                if (idleTimer >= wukongIdleStableTime)
                {
                    break;
                }
            }
            else
            {
                idleTimer = 0f;
            }

            if (waitTimer >= maxWaitWukongIdleTime)
            {
                Debug.LogWarning("Map2StoryManager: Chờ Wukong về Idle quá lâu. Tự mở thoại trước Boss để tránh kẹt phase.");
                break;
            }

            yield return null;
        }

        StartPreBossDialogue();
    }

    private bool IsWukongIdleReadyForDialogue()
    {
        if (wukongAnimator == null)
        {
            return true;
        }

        if (wukongAnimator.IsInTransition(0))
        {
            return false;
        }

        AnimatorStateInfo stateInfo = wukongAnimator.GetCurrentAnimatorStateInfo(0);

        bool isIdleState = false;

        if (!string.IsNullOrEmpty(wukongIdleStateNameForDialogueWait))
        {
            isIdleState = stateInfo.IsName(wukongIdleStateNameForDialogueWait);
        }

        if (!isIdleState)
        {
            return false;
        }

        if (wukongRigidbody != null)
        {
            if (Mathf.Abs(wukongRigidbody.linearVelocity.x) > wukongIdleVelocityThreshold)
            {
                return false;
            }

            if (Mathf.Abs(wukongRigidbody.linearVelocity.y) > wukongIdleVelocityThreshold)
            {
                return false;
            }
        }

        return true;
    }

    private void StartPreBossDialogue()
    {
        if (PauseMenuController.IsPausedGlobal)
        {
            if (enableDebugLog)
            {
                Debug.Log("Map2StoryManager: Đang Pause nên chưa mở PreBossDialogue.");
            }

            return;
        }
        currentPhase = Map2Phase.PreBossDialogue;

        if (lockCharactersDuringPreBossDialogue)
        {
            LockCharacters(true);
        }

        HideBoss1UI();
        HideLocationTitleImmediate();

        if (dialogueController != null && preBossDialogueLines != null && preBossDialogueLines.Length > 0)
        {
            dialogueController.dialogueMode = Map2GlobalDialogueController.DialogueMode.ConversationNextKey;
            dialogueController.StartDialogue(preBossDialogueLines, OnPreBossDialogueFinished);
        }
        else
        {
            Debug.LogWarning("Map2StoryManager: Chưa gán dialogueController hoặc preBossDialogueLines. Chuyển thẳng sang BossFight.");
            OnPreBossDialogueFinished();
        }

        if (enableDebugLog)
        {
            Debug.Log("Map2StoryManager: Bắt đầu hội thoại trước Boss1.");
        }
    }

    private void OnPreBossDialogueFinished()
    {
        StartBossFight();
    }

    private void StartBossFight()
    {
        if (PauseMenuController.IsPausedGlobal)
        {
            if (enableDebugLog)
            {
                Debug.Log("Map2StoryManager: Đang Pause nên chưa bắt đầu BossFight.");
            }

            return;
        }

        if (bossFightStarted)
        {
            return;
        }

        bossFightStarted = true;
        currentPhase = Map2Phase.BossFight;

        if (dialogueController != null)
        {
            dialogueController.HideDialogue();
        }

        ShowBoss1UI();

        if (lockCharactersDuringPreBossDialogue)
        {
            LockCharacters(false);
        }

        if (boss1 != null)
        {
            boss1.combatStoppedByDeath = false;
            boss1.activeOnStart = false;
            boss1.forceCombatOnStart = false;
            boss1.ActivateCombat();
        }
        else
        {
            Debug.LogWarning("Map2StoryManager: Chưa gán Boss1Controller.");
        }

        if (enableDebugLog)
        {
            Debug.Log("Map2StoryManager: Kết thúc hội thoại trước Boss. Bắt đầu BossFight.");
        }
    }

    private void CheckBoss1DeadForPostDialogue()
    {
        if (currentPhase != Map2Phase.BossFight)
        {
            return;
        }

        if (postBossDialogueStarted)
        {
            return;
        }

        if (boss1 == null)
        {
            return;
        }

        if (!boss1.IsDead())
        {
            return;
        }

        StartPostBossDialogue();
    }

    private void StartPostBossDialogue()
    {
        if (postBossDialogueStarted)
        {
            return;
        }

        postBossDialogueStarted = true;
        currentPhase = Map2Phase.PostBossDialogue;

        if (hideBoss1UIWhenPostDialogueStart)
        {
            HideBoss1UI();
        }

        if (dialogueController != null && postBossDialogueLines != null && postBossDialogueLines.Length > 0)
        {
            dialogueController.dialogueMode = Map2GlobalDialogueController.DialogueMode.ConversationNextKey;
            dialogueController.StartDialogue(postBossDialogueLines, OnPostBossDialogueFinished);
        }
        else
        {
            Debug.LogWarning("Map2StoryManager: Chưa có postBossDialogueLines hoặc dialogueController. Chuyển map luôn.");
            OnPostBossDialogueFinished();
        }

        if (enableDebugLog)
        {
            Debug.Log("Map2StoryManager: Boss1 đã chết. Bắt đầu hội thoại sau Boss.");
        }
    }

    private void OnPostBossDialogueFinished()
    {
        currentPhase = Map2Phase.Finished;

        if (dialogueController != null)
        {
            dialogueController.HideDialogue();
        }

        if (loadNextSceneAfterPostBossDialogue)
        {
            if (postBossDialogueCoroutine != null)
            {
                StopCoroutine(postBossDialogueCoroutine);
            }

            postBossDialogueCoroutine = StartCoroutine(LoadNextSceneAfterDelayRoutine());
        }

        if (enableDebugLog)
        {
            Debug.Log("Map2StoryManager: Kết thúc hội thoại sau Boss.");
        }
    }

    private IEnumerator LoadNextSceneAfterDelayRoutine()
    {
        if (delayBeforeLoadNextScene > 0f)
        {
            yield return new WaitForSeconds(delayBeforeLoadNextScene);
        }

        if (string.IsNullOrEmpty(nextSceneName))
        {
            Debug.LogWarning("Map2StoryManager: Chưa nhập Next Scene Name.");
            yield break;
        }

        // Khi bắt đầu chuyển cảnh, tắt UI tổng để màn hình sạch trước khi fade đen.
        HideGameplayUI();
        HideBoss1UI();
        HideDialogueUI();
        HideLocationTitleImmediate();

        // Chờ 1 frame để UI kịp tắt rồi mới fade.
        yield return null;

        AutoBindSceneFadeController();

        if (sceneFadeController != null)
        {
            sceneFadeController.FadeOutThenLoadScene(nextSceneName);
        }
        else
        {
            Debug.LogWarning("Map2StoryManager: Không có Map2SceneFadeController, chuyển scene trực tiếp.");
            SceneManager.LoadScene(nextSceneName);
        }
    }
    public void NotifyWukongDeathFinished()
    {
        if (!gameOverWhenWukongDead)
        {
            return;
        }

        if (enableDebugLog)
        {
            Debug.Log("Map2StoryManager: Đã nhận báo Wukong chết xong animation.");
        }

        ShowGameOver("Wukong chết xong animation.");
    }

    public void NotifyPartyDead()
    {
        if (!gameOverWhenPartyDead)
        {
            return;
        }

        if (enableDebugLog)
        {
            Debug.Log("Map2StoryManager: Đã nhận báo đoàn thỉnh kinh hết máu.");
        }

        ShowGameOver("Đoàn thỉnh kinh hết máu.");
    }

    private void ShowGameOver(string reason)
    {
        if (gameOverStarted)
        {
            return;
        }

        gameOverStarted = true;

        if (enableDebugLog)
        {
            Debug.Log("Map2StoryManager: GAME OVER. Lý do: " + reason);
        }

        HideUIBeforeGameOver();

        LockCharacters(true);

        if (boss1 != null)
        {
            boss1.NotifyWukongDead();
            boss1.NotifyPartyDead();
        }

        if (gameOverMenuController != null)
        {
            gameOverMenuController.ShowGameOver();
        }
        else
        {
            Debug.LogWarning("Map2StoryManager: Chưa gán GameOverMenuController.");
        }
    }

    private void HideUIBeforeGameOver()
    {
        HideBoss1UI();
        HideDialogueUI();
        HideLocationTitleImmediate();

        if (gameplayUIObjects != null)
        {
            for (int i = 0; i < gameplayUIObjects.Length; i++)
            {
                if (gameplayUIObjects[i] != null)
                {
                    gameplayUIObjects[i].SetActive(false);
                }
            }
        }
    }

    public bool IsIntroFinished()
    {
        return introFinished;
    }

    public bool IsExploreBeforeBoss()
    {
        return currentPhase == Map2Phase.ExploreBeforeBoss;
    }
}