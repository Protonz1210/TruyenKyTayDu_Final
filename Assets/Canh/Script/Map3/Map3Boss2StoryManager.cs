using System;
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

public class Map3Boss2StoryManager : MonoBehaviour
{
    public enum Map3Boss2StoryState
    {
        StartIntro,
        ExploreBeforeBoss,

        Waiting,
        PreEnemyDialogue,
        NormalEnemyWave,
        PreBossDialogue,
        BossFight,
        PostBossDialogue,
        Finished
    }

    [Header("Current State")]
    public Map3Boss2StoryState currentState = Map3Boss2StoryState.StartIntro;

    [Header("UI")]
    [Tooltip("GlobalHUD chứa máu Wukong, máu đoàn, 3 nút chiêu, cooldown. Không kéo object chứa box địa điểm vào đây nếu box địa điểm nằm chung UIDocument.")]
    public GameObject globalHUD;

    [Tooltip("Map3HUDController nằm trên Map3BossHUD.")]
    public Map3HUDController hudController;

    [Header("Game Over")]
    [Tooltip("UI GameOver riêng của Map3. Kéo object OVER có GameOverMenuController vào đây.")]
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

    [Header("Phase 0 - UI Địa Điểm Đầu Map")]
    [Tooltip("UIDocument chứa Box-mask / box_mask và box_text. Không được tắt GameObject này ở đầu map.")]
    public UIDocument mapHUDDocument;

    [Tooltip("Các object UI gameplay cần ẩn ở đầu map rồi hiện lại sau intro. Không kéo object chứa Box-mask / box_mask vào đây.")]
    public GameObject[] gameplayUIObjects;

    [Tooltip("Tên group UI máu boss trong UI Toolkit.")]
    public string bossUIGroupName = "boss-panel";

    [Tooltip("Tên group bảng địa danh dọc trong UI Toolkit. Code có fallback nên nhận được cả Box-mask và box_mask.")]
    public string locationBoxName = "Box-mask";

    [Tooltip("Tên Label hiển thị địa danh trong UI Toolkit.")]
    public string locationTextName = "box_text";

    [Tooltip("Tên ảnh nền/bảng bên trong UI địa điểm. Map3 hiện tại thường là box-image, Map2 là Box_image.")]
    public string locationImageName = "box-image";

    [Tooltip("Tên group avatar/ảnh phụ của UI địa điểm. Map3 hiện tại đang là box-avarta.")]
    public string locationAvatarName = "box-avarta";

    [Tooltip("Khi hiện địa điểm, ép bật cả element cha và con để tránh bị display none.")]
    public bool forceShowLocationChildren = true;

    [Tooltip("Opacity cao nhất khi hiện UI địa điểm. 1 = rõ hoàn toàn.")]
    [Range(0f, 1f)]
    public float locationVisibleOpacity = 1f;

    [TextArea(3, 6)]
    [Tooltip("Text địa danh đầu map. Dùng \\n để xuống dòng.")]
    public string locationTitleText = "ĐỘNG\nKỲ\nLÂN";

    [Tooltip("Thời gian fade in bảng địa danh.")]
    public float locationFadeInTime = 0.6f;

    [Tooltip("Thời gian giữ bảng địa danh.")]
    public float locationHoldTime = 1.6f;

    [Tooltip("Thời gian fade out bảng địa danh.")]
    public float locationFadeOutTime = 0.6f;

    [Tooltip("Chờ một chút sau khi vào scene để Wukong và đoàn spawn ổn định.")]
    public float waitBeforeLockTime = 0.25f;

    [Tooltip("Sau khi khóa di chuyển, chờ thêm một chút để Animator về Idle.")]
    public float waitIdleAfterLockTime = 0.25f;

    [Header("Phase 0 - Wait Idle Before Lock")]
    [Tooltip("Chờ Wukong và đoàn thỉnh kinh spawn xong, về Idle rồi mới khóa di chuyển.")]
    public bool waitCharactersIdleBeforeIntroLock = true;

    [Tooltip("Vận tốc nhỏ hơn ngưỡng này thì coi là đứng yên.")]
    public float introIdleVelocityThreshold = 0.05f;

    [Tooltip("Nhân vật phải đứng yên ổn định trong bao lâu thì mới khóa.")]
    public float introIdleStableTime = 0.25f;

    [Tooltip("Thời gian chờ tối đa để tránh kẹt nếu Animator/Rigidbody không về Idle.")]
    public float maxWaitIntroIdleTime = 5f;

    [Tooltip("Có bắt buộc Wukong phải đúng state Idle không.")]
    public bool requireWukongIdleStateBeforeIntro = true;

    [Tooltip("Có bắt buộc đoàn phải đúng state Idle không. Nếu chưa biết tên state Idle của đoàn thì để false.")]
    public bool requirePartyIdleStateBeforeIntro = false;

    [Header("Phase 0 - Freeze Characters")]
    [Tooltip("Các object nhân vật cần khóa ở đầu map. Kéo Wukong, Đường Tăng, Bát Giới, Sa Tăng vào đây.")]
    public GameObject[] charactersToFreeze;

    [Tooltip("Tên các script di chuyển cần tắt khi khóa.")]
    public string[] movementScriptNamesToDisable =
    {
        "PlayerController",
        "FollowerController"
    };

    [Tooltip("Khi khóa có dừng Rigidbody2D không.")]
    public bool freezeRigidbodyWhenLocked = true;

    [Tooltip("Khi khóa có set Animator Speed = 0 không.")]
    public bool setAnimatorSpeedToZeroWhenLocked = true;

    [Tooltip("Sau khi bảng địa danh biến mất có mở lại di chuyển không.")]
    public bool restoreMovementAfterIntro = true;

    [Header("Player / Party")]
    [Tooltip("Script điều khiển Ngộ Không.")]
    public PlayerController wukongController;

    [Tooltip("Rigidbody2D của Ngộ Không.")]
    public Rigidbody2D wukongRigidbody;

    [Tooltip("Animator của Ngộ Không.")]
    public Animator wukongAnimator;

    [Tooltip("Các script follow của Đường Tăng / Trư Bát Giới / Sa Tăng.")]
    public Behaviour[] partyFollowScripts;

    [Tooltip("Animator của Đường Tăng / Trư Bát Giới / Sa Tăng.")]
    public Animator[] partyAnimators;

    [Header("Wukong Animator")]
    [Tooltip("Tên state Idle thật của Wukong.")]
    public string wukongIdleStateName = "Wukong1Idle";

    [Tooltip("Tên parameter Speed trong Animator của Wukong.")]
    public string wukongSpeedParameterName = "Speed";

    [Tooltip("Các bool Animator của Wukong cần set false khi khóa thoại.")]
    public string[] wukongBoolParametersToFalse;

    [Tooltip("Các trigger Animator của Wukong cần reset khi khóa thoại.")]
    public string[] wukongTriggersToReset;

    [Header("Party Animator")]
    [Tooltip("Tên state Idle thật của đoàn. Nếu không chắc, để trống để tránh lỗi.")]
    public string partyIdleStateName = "";

    [Tooltip("Tên parameter Speed của đoàn.")]
    public string partySpeedParameterName = "Speed";

    [Tooltip("Các bool Animator của đoàn cần set false khi khóa thoại.")]
    public string[] partyBoolParametersToFalse;

    [Tooltip("Các trigger Animator của đoàn cần reset khi khóa thoại.")]
    public string[] partyTriggersToReset;

    [Header("Dialogue Controller")]
    public Map3DialogueController dialogueController;

    [Header("Phase 2 - Thoại trước enemy")]
    [Tooltip("Thoại khi Wukong chạm trigger đầu tiên.")]
    public Map3DialogueLine[] preEnemyDialogueLines;

    [Header("Enemy123 Wave")]
    [Tooltip("Spawner Enemy1 / Enemy2 / Enemy3.")]
    public Enemy123RandomSpawner enemy123Spawner;

    [Header("Phase 4 - Thoại trước boss")]
    [Tooltip("Sau khi diệt hết Enemy123, chờ Ngộ Không về Idle rồi mới hiện thoại trước boss.")]
    public bool waitWukongIdleBeforePreBossDialogue = true;

    [Tooltip("Tên state Idle thật của Wukong để chờ trước thoại boss.")]
    public string wukongIdleStateNameForDialogueWait = "Wukong1Idle";

    [Tooltip("Vận tốc nhỏ hơn số này thì coi là đứng yên.")]
    public float wukongIdleVelocityThreshold = 0.05f;

    [Tooltip("Ngộ Không phải Idle ổn định trong bao lâu.")]
    public float wukongIdleStableTime = 0.25f;

    [Tooltip("Thời gian chờ tối đa trước khi tự mở thoại để tránh kẹt.")]
    public float maxWaitWukongIdleTime = 5f;

    [Tooltip("Thoại sau khi enemy chết hết, trước khi boss đánh.")]
    public Map3DialogueLine[] preBossDialogueLines;

    [Header("Boss2")]
    public GameObject boss2Object;
    public MonoBehaviour boss2Controller;
    public Rigidbody2D boss2Rigidbody;
    public Animator boss2Animator;
    public Transform boss2Target;
    public string playerTag = "Player";

    [Tooltip("Khóa boss từ đầu, chỉ mở sau thoại trước boss.")]
    public bool lockBossAtStart = true;

    [Header("Phase 6 - Thoại sau boss chết")]
    [Tooltip("Thoại sau khi Boss2 chết. Không khóa Wukong khi hiện thoại này.")]
    public Map3DialogueLine[] postBossDialogueLines;

    [Tooltip("Fade out UI máu boss khi boss chết.")]
    public bool fadeOutBossUIOnBossDead = true;

    [Tooltip("Không ép tắt object boss. Nên bật để code boss tự xử lý chết.")]
    public bool doNotForceHideBossObject = true;

    [Header("Phase Cuối - Hồi Máu Sau Hội Thoại")]
    [Tooltip("Object hồi máu có gắn HealInteractable. Object này hiện sẵn từ đầu, StoryManager chỉ bật/tắt tương tác.")]
    public HealInteractable postDialogueHealObject;

    [Tooltip("Sau hội thoại cuối, yêu cầu Wukong hồi máu trước khi tiếp tục flow kết thúc.")]
    public bool requireHealBeforeLoadNextScene = true;

    [Tooltip("Tên hàm HealInteractable sẽ gọi ngược về StoryManager sau khi hồi máu xong.")]
    public string healCompletedNotifyMessageName = "NotifyPostDialogueHealCompleted";

    [Tooltip("Khi bật lại tương tác hồi máu, refresh Collider2D để nếu Wukong đang đứng sẵn trong vùng thì trigger có cơ hội nhận lại.")]
    public bool refreshHealTriggerWhenEnabled = true;

    [Header("Phase Cuối - Khóa Tương Tác Hồi Máu Từ Đầu")]
    [Tooltip("Khóa HealInteractable ngay từ Awake để object máu hiện nhưng chưa dùng được.")]
    public bool lockHealInteractableOnAwake = true;

    [Tooltip("Tắt luôn Collider2D trigger của object hồi máu cho tới khi hết thoại sau boss.")]
    public bool disableHealTriggerCollidersUntilUnlocked = true;

    [Tooltip("Chỉ tắt collider dạng Trigger, tránh ảnh hưởng collider vật lý nếu object có collider thường.")]
    public bool onlyDisableHealTriggerColliders = true;

    [Header("Phase Cuối - Chuyển Map Sau Khi Hồi Máu")]
    [Tooltip("Sau khi hồi máu xong thì chuyển sang scene tiếp theo.")]
    public bool loadNextSceneAfterHeal = true;

    [Tooltip("Tên scene tiếp theo. Phải đúng tên scene trong Build Settings.")]
    public string nextSceneName = "Map_4";

    [Tooltip("Delay trước khi bắt đầu fade chuyển scene.")]
    public float delayBeforeLoadNextScene = 1f;

    [Tooltip("Tắt UI tổng trước khi fade chuyển scene.")]
    public bool hideUIBeforeLoadNextScene = true;

    [Tooltip("Controller fade đen chuyển scene giống Map2.")]
    public Map3SceneFadeController sceneFadeController;

    [Header("Boss Death Check")]
    [Tooltip("Tên biến máu hiện tại trong Boss2Controller. Nếu không đúng, code sẽ thử thêm vài tên phổ biến.")]
    public string bossCurrentHealthFieldName = "currentHealth";

    public float bossDeathCheckInterval = 0.25f;

    [Header("Debug")]
    public bool enableDebugLog = true;

    private VisualElement root;
    private VisualElement bossUIGroup;
    private VisualElement locationBox;
    private VisualElement locationImage;
    private VisualElement locationAvatar;
    private Label locationText;

    private float locationBoxDefaultOpacity = 1f;
    private bool locationBoxDefaultOpacityCached;

    private bool introStarted;
    private bool introFinished;
    private bool pendingBoss2IntroAfterIntro;

    private bool boss2IntroStarted;
    private bool enemyWaveStarted;
    private bool waitingBeforePreBossDialogue;
    private bool preBossDialogueStarted;
    private bool bossFightStarted;
    private bool postBossDialogueStarted;

    private bool waitingPostDialogueHeal;
    private bool loadNextSceneFlowStarted;

    private Coroutine bossDeathWatchCoroutine;
    private Coroutine healRefreshCoroutine;
    private Collider2D[] postDialogueHealColliders;

    void Awake()
    {
        FindReferencesIfNeeded();
        FindUIElements();

        SetupDeathNotifyTargets();

        CachePostDialogueHealColliders();

        if (lockHealInteractableOnAwake)
        {
            SetupPostDialogueHealObject(false);
        }

        PrepareVeryEarlyUIState();
    }

    void Start()
    {
        StartCoroutine(BootMapIntroRoutine());
    }

    IEnumerator BootMapIntroRoutine()
    {
        currentState = Map3Boss2StoryState.StartIntro;

        // Chờ 1 frame để UIDocument, Wukong, đoàn thỉnh kinh, HUD controller spawn/khởi tạo xong.
        yield return null;

        FindReferencesIfNeeded();
        FindUIElements();
        SetupDeathNotifyTargets();

        PrepareMapStartState();

        if (!introStarted)
        {
            StartCoroutine(StartIntroRoutine());
        }

        Log("Map3 Boss2 Story bắt đầu Phase 0 sau khi scene ổn định.");
    }

    void Update()
    {
        if (currentState == Map3Boss2StoryState.NormalEnemyWave)
        {
            CheckNormalEnemyWaveFinished();
        }
    }

    void FindReferencesIfNeeded()
    {
        FindHUDDocumentIfNeeded();

#if UNITY_2023_1_OR_NEWER
        if (hudController == null)
            hudController = FindFirstObjectByType<Map3HUDController>();

        if (dialogueController == null)
            dialogueController = FindFirstObjectByType<Map3DialogueController>();
#else
        if (hudController == null)
            hudController = FindObjectOfType<Map3HUDController>();

        if (dialogueController == null)
            dialogueController = FindObjectOfType<Map3DialogueController>();
#endif

        if (boss2Controller == null && boss2Object != null)
        {
            Boss2Controller typedBoss2 = boss2Object.GetComponent<Boss2Controller>();

            if (typedBoss2 != null)
                boss2Controller = typedBoss2;
            else
                boss2Controller = boss2Object.GetComponent<MonoBehaviour>();
        }

        if (boss2Rigidbody == null && boss2Object != null)
            boss2Rigidbody = boss2Object.GetComponent<Rigidbody2D>();

        if (boss2Animator == null && boss2Object != null)
            boss2Animator = boss2Object.GetComponent<Animator>();

        if (boss2Target == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag(playerTag);

            if (playerObject != null)
                boss2Target = playerObject.transform;
        }
    }
    void SetupDeathNotifyTargets()
    {
        SetupWukongDeathNotifyTarget();
        SetupPartyDeathNotifyTargets();
    }

    void SetupWukongDeathNotifyTarget()
    {
        if (wukongController == null)
        {
#if UNITY_2023_1_OR_NEWER
        wukongController = FindFirstObjectByType<PlayerController>();
#else
            wukongController = FindObjectOfType<PlayerController>();
#endif
        }

        if (wukongController == null)
        {
            Debug.LogWarning("Map3Boss2StoryManager: Không tìm thấy PlayerController để setup Death Notify cho Wukong.");
            return;
        }

        wukongController.SetDeathNotifyTarget(gameObject);
        wukongController.SetDeathNotifyMessageName("NotifyWukongDeathFinished");

        if (enableDebugLog)
        {
            Debug.Log("Map3Boss2StoryManager: Đã setup Death Notify Target cho Wukong.");
        }
    }

    void SetupPartyDeathNotifyTargets()
    {
        bool hasSetupAnyPartyHealth = false;

        // Ưu tiên 1: dùng PartyHealth kéo trực tiếp trong Inspector.
        if (partyHealth != null)
        {
            SetupOnePartyHealthDeathNotify(partyHealth);
            hasSetupAnyPartyHealth = true;
        }

        // Ưu tiên 2: tìm trong Characters To Freeze nếu có.
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

        // Ưu tiên 3: nếu vẫn chưa thấy thì tự tìm trong scene.
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
            Debug.LogWarning("Map3Boss2StoryManager: Không tìm thấy PartyHealth nào để setup Death Notify cho đoàn. Hãy kéo PartyManager vào field Party Health.");
        }
    }

    void SetupOnePartyHealthDeathNotify(PartyHealth targetPartyHealth)
    {
        if (targetPartyHealth == null)
        {
            return;
        }

        targetPartyHealth.SetDeathNotifyTarget(gameObject);
        targetPartyHealth.SetDeathNotifyMessageName("NotifyPartyDead");

        if (enableDebugLog)
        {
            Debug.Log("Map3Boss2StoryManager: Đã setup Death Notify Target cho PartyHealth trên object: " + targetPartyHealth.gameObject.name);
        }
    }

    // ======================================================
    // PHASE 0: LOCATION TITLE INTRO
    // ======================================================

    void PrepareVeryEarlyUIState()
    {
        currentState = Map3Boss2StoryState.StartIntro;

        HideGameplayUI();
        HideBossUI();
        HideDialogueUI();
        HideLocationTitleImmediate();

        Log("Phase 0: Đã tắt UI tổng ngay từ Awake.");
    }

    void PrepareMapStartState()
    {
        currentState = Map3Boss2StoryState.StartIntro;

        HideGameplayUI();
        HideBossUI();
        HideDialogueUI();
        HideLocationTitleImmediate();

        if (enemy123Spawner != null)
        {
            enemy123Spawner.isSpawning = false;
            enemy123Spawner.spawnOnStart = false;
        }

        if (lockBossAtStart)
        {
            DeactivateBoss2Combat();
        }

        // Object hồi máu vẫn active trong scene, chỉ khóa script tương tác.
        SetupPostDialogueHealObject(false);

        Log("Phase 0: Đã chuẩn bị trạng thái đầu map.");
    }

    IEnumerator StartIntroRoutine()
    {
        introStarted = true;
        introFinished = false;
        currentState = Map3Boss2StoryState.StartIntro;

        // Tắt UI tổng và các box ngay đầu phase.
        FindReferencesIfNeeded();
        FindUIElements();

        HideGameplayUI();
        HideBossUI();
        HideDialogueUI();
        HideLocationTitleImmediate();
        DeactivateBoss2Combat();

        if (waitBeforeLockTime > 0f)
        {
            yield return new WaitForSeconds(waitBeforeLockTime);
        }

        // Chờ Wukong + đoàn spawn xong và về Idle ổn định rồi mới khóa di chuyển.
        yield return StartCoroutine(WaitCharactersIdleBeforeIntroLockRoutine());

        // Khóa di chuyển sau khi đã về Idle.
        LockWukongAndParty();
        LockCharacters(true);

        if (waitIdleAfterLockTime > 0f)
        {
            yield return new WaitForSeconds(waitIdleAfterLockTime);
        }

        // Trước khi hiện UI địa điểm, ép ẩn lại UI tổng, boss UI, dialogue.
        HideGameplayUI();
        HideBossUI();
        HideDialogueUI();
        HideLocationTitleImmediate();

        FindUIElements();

        // Hiện UI địa điểm: mờ -> rõ, giữ, rồi rõ -> mờ.
        yield return StartCoroutine(ShowLocationTitleRoutine());

        // UI địa điểm đã tắt thì bật lại UI tổng.
        ShowGameplayUI();

        // Boss UI và box thoại vẫn phải ẩn.
        HideBossUI();
        HideDialogueUI();
        HideLocationTitleImmediate();

        // Mở khóa di chuyển.
        if (restoreMovementAfterIntro)
        {
            LockCharacters(false);
            UnlockWukongAndParty();
        }

        currentState = Map3Boss2StoryState.ExploreBeforeBoss;
        introFinished = true;

        Log("Phase 0 kết thúc. UI tổng đã bật lại, nhân vật đã mở khóa, chuyển sang ExploreBeforeBoss.");

        if (pendingBoss2IntroAfterIntro)
        {
            pendingBoss2IntroAfterIntro = false;
            StartBoss2Intro();
        }
    }

    IEnumerator WaitCharactersIdleBeforeIntroLockRoutine()
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
                    Log("Phase 0: Wukong và đoàn đã về Idle ổn định. Bắt đầu khóa di chuyển.");
                    yield break;
                }
            }
            else
            {
                stableTimer = 0f;
            }

            if (waitTimer >= maxWaitIntroIdleTime)
            {
                Debug.LogWarning("Map3Boss2StoryManager: Chờ Wukong/đoàn về Idle quá lâu. Tiếp tục intro để tránh kẹt.");
                yield break;
            }

            yield return null;
        }
    }

    bool AreIntroCharactersIdleReady()
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

    bool IsCharacterIdleReadyForIntro(GameObject character)
    {
        if (character == null)
        {
            return true;
        }

        Rigidbody2D rb = character.GetComponent<Rigidbody2D>();

        if (rb != null)
        {
            if (Mathf.Abs(rb.linearVelocity.x) > introIdleVelocityThreshold)
            {
                return false;
            }

            if (Mathf.Abs(rb.linearVelocity.y) > introIdleVelocityThreshold)
            {
                return false;
            }
        }

        Rigidbody2D[] childBodies = character.GetComponentsInChildren<Rigidbody2D>(true);

        for (int i = 0; i < childBodies.Length; i++)
        {
            Rigidbody2D body = childBodies[i];

            if (body == null)
            {
                continue;
            }

            if (Mathf.Abs(body.linearVelocity.x) > introIdleVelocityThreshold)
            {
                return false;
            }

            if (Mathf.Abs(body.linearVelocity.y) > introIdleVelocityThreshold)
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

    bool IsWukongCharacter(GameObject character)
    {
        if (character == null)
        {
            return false;
        }

        if (wukongAnimator != null)
        {
            if (wukongAnimator.transform == character.transform)
            {
                return true;
            }

            if (wukongAnimator.transform.IsChildOf(character.transform))
            {
                return true;
            }
        }

        if (wukongController != null)
        {
            if (wukongController.transform == character.transform)
            {
                return true;
            }

            if (wukongController.transform.IsChildOf(character.transform))
            {
                return true;
            }
        }

        return false;
    }

    bool TryGetAnimatorFloat(Animator animator, string parameterName, out float value)
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
    IEnumerator ShowLocationTitleRoutine()
    {
        // Dùng trực tiếp Map3HUDController để hiện UI địa điểm.
        // Vì Map3HUDController quản lý cả Box-mask, box-avarta, box-image, box_text.
        if (hudController != null)
        {
            hudController.locationFadeInTime = locationFadeInTime;
            hudController.locationHoldTime = locationHoldTime;
            hudController.locationFadeOutTime = locationFadeOutTime;

            yield return StartCoroutine(hudController.PlayLocationTitle(locationTitleText));
            yield break;
        }

        // Fallback nếu quên gán Hud Controller.
        if (locationBox == null || locationText == null)
        {
            FindUIElements();
        }

        if (locationBox == null)
        {
            Debug.LogWarning(
                "Map3Boss2StoryManager: Không tìm thấy UI địa điểm và cũng chưa gán Hud Controller."
            );

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


    IEnumerator FadeVisualElement(VisualElement element, float from, float to, float duration)
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

    void ForceMapHUDDocumentActive()
    {
        if (mapHUDDocument == null)
        {
            return;
        }

        GameObject documentObject = mapHUDDocument.gameObject;

        if (documentObject != null && !documentObject.activeSelf)
        {
            documentObject.SetActive(true);

            if (enableDebugLog)
            {
                Debug.Log("Map3Boss2StoryManager: Đã bật lại UIDocument chứa UI địa điểm: " + documentObject.name);
            }
        }
    }

    void ForceShowVisualElement(VisualElement element)
    {
        if (element == null)
        {
            return;
        }

        element.style.display = DisplayStyle.Flex;
        element.style.visibility = Visibility.Visible;
    }

    VisualElement FindVisualElementByNames(VisualElement searchRoot, params string[] names)
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

    Label FindLabelByNames(VisualElement searchRoot, params string[] names)
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

    void FindHUDDocumentIfNeeded()
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
            if (documents[i] == null)
            {
                continue;
            }

            VisualElement documentRoot = documents[i].rootVisualElement;

            if (documentRoot == null)
            {
                continue;
            }

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

    void FindUIElements()
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
                Debug.LogWarning("Map3Boss2StoryManager: Chưa gán UIDocument chứa box địa danh.");
            }

            return;
        }

        root = mapHUDDocument.rootVisualElement;

        if (root == null)
        {
            if (enableDebugLog)
            {
                Debug.LogWarning("Map3Boss2StoryManager: UIDocument chưa có rootVisualElement.");
            }

            return;
        }

        bossUIGroup = FindVisualElementByNames(
            root,
            bossUIGroupName,
            "boss-panel",
            "boss-1-group",
            "boss_1_group"
        );

        locationBox = FindVisualElementByNames(
            root,
            locationBoxName,
            "Box-mask",
            "box_mask",
            "box-mask",
            "Box_mask"
        );

        locationText = FindLabelByNames(
            root,
            locationTextName,
            "box_text",
            "Box-text",
            "box-text",
            "Box_text"
        );

        locationImage = FindVisualElementByNames(
            root,
            locationImageName,
            "box-image",
            "Box_image",
            "box_image",
            "Box-image"
        );

        locationAvatar = FindVisualElementByNames(
            root,
            locationAvatarName,
            "box-avarta",
            "box_avatar",
            "box-avatar",
            "Box_avatar"
        );

        if (locationBox != null && !locationBoxDefaultOpacityCached)
        {
            locationBoxDefaultOpacity = GetCurrentOpacity(locationBox);
            locationBoxDefaultOpacityCached = true;
        }

        if (enableDebugLog)
        {
            Debug.Log(
                "Map3Boss2StoryManager FindUIElements | BossUIGroup: " + (bossUIGroup != null) +
                " | LocationBox: " + (locationBox != null) +
                " | LocationImage: " + (locationImage != null) +
                " | LocationAvatar: " + (locationAvatar != null) +
                " | LocationText: " + (locationText != null) +
                " | InputLocationBoxName: " + locationBoxName
            );
        }
    }

    float GetCurrentOpacity(VisualElement element)
    {
        if (element == null)
        {
            return 1f;
        }

        float resolvedOpacity = element.resolvedStyle.opacity;

        if (resolvedOpacity > 0f && resolvedOpacity <= 1f)
        {
            return resolvedOpacity;
        }

        return 1f;
    }

    void HideLocationTitleImmediate()
    {
        // Ưu tiên để Map3HUDController ẩn cả Box-mask và các con bên trong.
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

    void HideBossUI()
    {
        // Ẩn UI máu boss bằng UI Toolkit: boss-panel. Không đụng tới Box-mask / box_mask.
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

    void ShowBossUI()
    {
        // Chỉ hiện UI máu boss. Không hiện Box-mask / box_mask.
        if (bossUIGroup == null)
        {
            FindUIElements();
        }

        if (bossUIGroup != null)
        {
            bossUIGroup.style.display = DisplayStyle.Flex;
            bossUIGroup.style.opacity = 1f;
        }
    }

    void HideDialogueUI()
    {
        // Chỉ ẩn box thoại thật.
        // Không gọi hudController.HideBoxInstant() vì có thể ẩn nhầm Box-mask của UI địa điểm.
        if (dialogueController != null)
        {
            dialogueController.HideDialogue();
        }
    }

    void HideGameplayUI()
    {
        SetGameplayUIActive(false);
    }

    void ShowGameplayUI()
    {
        SetGameplayUIActive(true);
    }

    void SetGameplayUIActive(bool active)
    {
        if (globalHUD != null && !IsObjectContainingLocationUI(globalHUD))
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

            if (IsObjectContainingLocationUI(uiObject))
            {
                if (enableDebugLog)
                {
                    Debug.LogWarning("Map3Boss2StoryManager: Không tắt " + uiObject.name + " vì object này chứa box địa điểm.");
                }

                continue;
            }

            uiObject.SetActive(active);
        }
    }

    bool IsObjectContainingLocationUI(GameObject targetObject)
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

        UIDocument[] documents = targetObject.GetComponentsInChildren<UIDocument>(true);

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

            bool hasLocationBox =
                FindVisualElementByNames(documentRoot, locationBoxName, "Box-mask", "box_mask", "box-mask", "Box_mask") != null;

            if (hasLocationBox)
            {
                return true;
            }
        }

        return false;
    }

    void LockCharacters(bool locked)
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

    void FreezeRigidbody(GameObject character)
    {
        if (character == null)
        {
            return;
        }

        Rigidbody2D rb = character.GetComponent<Rigidbody2D>();

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        Rigidbody2D[] childBodies = character.GetComponentsInChildren<Rigidbody2D>(true);

        for (int i = 0; i < childBodies.Length; i++)
        {
            if (childBodies[i] != null)
            {
                childBodies[i].linearVelocity = Vector2.zero;
                childBodies[i].angularVelocity = 0f;
            }
        }
    }

    void SetAnimatorSpeedToZero(GameObject character)
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

    void SetMovementScriptsEnabled(GameObject character, bool enabled)
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
                            "Map3Boss2StoryManager: " +
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

    // ======================================================
    // PHASE 2: PRE ENEMY DIALOGUE
    // ======================================================

    public void StartBoss2Intro()
    {
        if (!introFinished && currentState == Map3Boss2StoryState.StartIntro)
        {
            pendingBoss2IntroAfterIntro = true;
            Log("Trigger boss được gọi khi Phase 0 chưa xong. Sẽ tự chạy sau intro.");
            return;
        }

        if (currentState != Map3Boss2StoryState.ExploreBeforeBoss &&
            currentState != Map3Boss2StoryState.Waiting)
        {
            return;
        }

        if (boss2IntroStarted)
            return;

        boss2IntroStarted = true;
        currentState = Map3Boss2StoryState.PreEnemyDialogue;

        FindReferencesIfNeeded();

        LockWukongAndParty();
        LockCharacters(true);
        DeactivateBoss2Combat();

        HideBossUI();
        HideDialogueUI();

        if (dialogueController != null && preEnemyDialogueLines != null && preEnemyDialogueLines.Length > 0)
        {
            dialogueController.StartDialogue(preEnemyDialogueLines, StartNormalEnemyWave);
        }
        else
        {
            StartNormalEnemyWave();
        }

        Log("Phase 2: Bắt đầu thoại trước Enemy123.");
    }

    public void StartPreEnemyDialogue()
    {
        StartBoss2Intro();
    }

    // ======================================================
    // PHASE 3: ENEMY WAVE
    // ======================================================

    void StartNormalEnemyWave()
    {
        if (enemyWaveStarted)
            return;

        enemyWaveStarted = true;
        currentState = Map3Boss2StoryState.NormalEnemyWave;

        UnlockWukongAndParty();
        LockCharacters(false);

        DeactivateBoss2Combat();

        HideBossUI();
        HideDialogueUI();
        HideLocationTitleImmediate();

        if (enemy123Spawner != null)
        {
            enemy123Spawner.StartSpawn();
        }
        else
        {
            Debug.LogWarning("Map3Boss2StoryManager chưa gán Enemy123RandomSpawner.");
        }

        Log("Phase 3: Bắt đầu wave Enemy123.");
    }

    void CheckNormalEnemyWaveFinished()
    {
        if (waitingBeforePreBossDialogue)
            return;

        if (preBossDialogueStarted)
            return;

        if (enemy123Spawner == null)
            return;

        if (enemy123Spawner.IsSpawnFinished())
        {
            StartCoroutine(WaitWukongIdleThenStartPreBossDialogue());
        }
    }

    IEnumerator WaitWukongIdleThenStartPreBossDialogue()
    {
        waitingBeforePreBossDialogue = true;

        if (enemy123Spawner != null)
            enemy123Spawner.StopSpawn();

        float waitTimer = 0f;
        float idleTimer = 0f;

        while (waitWukongIdleBeforePreBossDialogue)
        {
            waitTimer += Time.deltaTime;

            bool isIdleReady = IsWukongIdleReadyForDialogue();

            if (isIdleReady)
            {
                idleTimer += Time.deltaTime;

                if (idleTimer >= wukongIdleStableTime)
                    break;
            }
            else
            {
                idleTimer = 0f;
            }

            if (waitTimer >= maxWaitWukongIdleTime)
            {
                Debug.LogWarning("Map3Boss2StoryManager: chờ Wukong Idle quá lâu. Tự mở thoại trước boss để tránh kẹt.");
                break;
            }

            yield return null;
        }

        StartPreBossDialogue();
    }

    bool IsWukongIdleReadyForDialogue()
    {
        if (wukongAnimator == null)
            return true;

        if (wukongAnimator.IsInTransition(0))
            return false;

        AnimatorStateInfo stateInfo = wukongAnimator.GetCurrentAnimatorStateInfo(0);

        bool isIdleState = false;

        if (!string.IsNullOrEmpty(wukongIdleStateNameForDialogueWait))
            isIdleState = stateInfo.IsName(wukongIdleStateNameForDialogueWait);
        else
            isIdleState = true;

        if (!isIdleState)
            return false;

        if (wukongRigidbody != null)
        {
            float velocityX = Mathf.Abs(wukongRigidbody.linearVelocity.x);
            float velocityY = Mathf.Abs(wukongRigidbody.linearVelocity.y);

            if (velocityX > wukongIdleVelocityThreshold)
                return false;

            if (velocityY > wukongIdleVelocityThreshold)
                return false;
        }

        return true;
    }

    // ======================================================
    // PHASE 4: PRE BOSS DIALOGUE
    // ======================================================

    public void StartPreBossDialogue()
    {
        if (preBossDialogueStarted)
            return;

        preBossDialogueStarted = true;
        currentState = Map3Boss2StoryState.PreBossDialogue;

        LockWukongAndParty();
        LockCharacters(true);
        DeactivateBoss2Combat();

        HideBossUI();
        HideLocationTitleImmediate();

        if (dialogueController != null && preBossDialogueLines != null && preBossDialogueLines.Length > 0)
        {
            dialogueController.StartDialogue(preBossDialogueLines, StartBoss2Fight);
        }
        else
        {
            StartBoss2Fight();
        }

        Log("Phase 4: Bắt đầu thoại trước Boss2.");
    }

    // ======================================================
    // PHASE 5: BOSS FIGHT
    // ======================================================

    public void StartBoss2Fight()
    {
        if (bossFightStarted)
            return;

        bossFightStarted = true;
        currentState = Map3Boss2StoryState.BossFight;

        StartCoroutine(StartBoss2FightRoutine());
    }

    IEnumerator StartBoss2FightRoutine()
    {
        UnlockWukongAndParty();
        LockCharacters(false);

        if (wukongController != null)
            wukongController.enabled = true;

        if (wukongRigidbody != null)
            wukongRigidbody.linearVelocity = Vector2.zero;

        if (partyFollowScripts != null)
        {
            for (int i = 0; i < partyFollowScripts.Length; i++)
            {
                if (partyFollowScripts[i] != null)
                    partyFollowScripts[i].enabled = true;
            }
        }

        HideDialogueUI();
        HideLocationTitleImmediate();

        ShowBossUI();

        if (hudController != null)
        {
            yield return StartCoroutine(hudController.FadeInBossUI());
        }

        ActivateBoss2Combat();

        if (bossDeathWatchCoroutine != null)
            StopCoroutine(bossDeathWatchCoroutine);

        bossDeathWatchCoroutine = StartCoroutine(WatchBoss2Death());

        Log("Phase 5: Boss UI fade in xong. Boss2 bắt đầu tấn công.");
    }

    void DeactivateBoss2Combat()
    {
        if (boss2Controller != null)
        {
            boss2Controller.SendMessage("DeactivateCombat", SendMessageOptions.DontRequireReceiver);
            boss2Controller.enabled = false;
        }

        if (boss2Rigidbody != null)
        {
            boss2Rigidbody.linearVelocity = Vector2.zero;
            boss2Rigidbody.angularVelocity = 0f;
        }

        if (boss2Animator != null)
        {
            SetAnimatorFloatIfExists(boss2Animator, "Speed", 0f);
        }

        Log("Boss2 đang bị khóa.");
    }

    void ActivateBoss2Combat()
    {
        FindReferencesIfNeeded();

        if (boss2Controller != null)
        {
            boss2Controller.enabled = true;

            SetTransformFieldOrProperty(boss2Controller, "target", boss2Target);
            SetTransformFieldOrProperty(boss2Controller, "playerTarget", boss2Target);
            SetTransformFieldOrProperty(boss2Controller, "wukongTarget", boss2Target);

            SetBoolFieldOrProperty(boss2Controller, "canMove", true);
            SetBoolFieldOrProperty(boss2Controller, "canAttack", true);
            SetBoolFieldOrProperty(boss2Controller, "isActivated", true);
            SetBoolFieldOrProperty(boss2Controller, "combatActivated", true);
            SetBoolFieldOrProperty(boss2Controller, "canReceiveDamage", true);
            SetBoolFieldOrProperty(boss2Controller, "canShowBossUI", true);
            SetBoolFieldOrProperty(boss2Controller, "autoActivateByRange", false);

            boss2Controller.SendMessage("ActivateCombat", SendMessageOptions.DontRequireReceiver);
        }
        else
        {
            Debug.LogWarning("Map3Boss2StoryManager chưa gán Boss2Controller.");
        }

        if (boss2Rigidbody != null)
        {
            boss2Rigidbody.linearVelocity = Vector2.zero;
            boss2Rigidbody.angularVelocity = 0f;
        }

        Log("Boss2 đã được mở combat.");
    }

    // ======================================================
    // PHASE 6: POST BOSS DIALOGUE
    // ======================================================

    IEnumerator WatchBoss2Death()
    {
        while (currentState == Map3Boss2StoryState.BossFight)
        {
            if (IsBoss2Dead())
            {
                StartPostBossDialogue();
                yield break;
            }

            yield return new WaitForSeconds(bossDeathCheckInterval);
        }
    }

    public void StartPostBossDialogue()
    {
        if (postBossDialogueStarted)
            return;

        postBossDialogueStarted = true;
        currentState = Map3Boss2StoryState.PostBossDialogue;

        StartCoroutine(PostBossDialogueRoutine());
    }

    IEnumerator PostBossDialogueRoutine()
    {
        UnlockWukongAndParty();
        LockCharacters(false);

        if (fadeOutBossUIOnBossDead && hudController != null)
        {
            yield return StartCoroutine(hudController.FadeOutBossUI());
        }
        else
        {
            HideBossUI();
        }

        if (!doNotForceHideBossObject && boss2Object != null)
            boss2Object.SetActive(false);

        if (dialogueController != null && postBossDialogueLines != null && postBossDialogueLines.Length > 0)
        {
            dialogueController.StartDialogue(postBossDialogueLines, OnPostBossDialogueFinished);
        }
        else
        {
            OnPostBossDialogueFinished();
        }

        Log("Phase 6: Boss2 chết. Không khóa Wukong. Hiện thoại sau boss.");
    }

    void OnPostBossDialogueFinished()
    {
        currentState = Map3Boss2StoryState.Finished;

        UnlockWukongAndParty();
        LockCharacters(false);
        HideDialogueUI();
        HideBossUI();

        if (requireHealBeforeLoadNextScene && postDialogueHealObject != null)
        {
            waitingPostDialogueHeal = true;
            SetupPostDialogueHealObject(true);

            Log("Phase cuối: Đã bật tương tác hồi máu. Chờ Wukong hồi máu rồi mới chạy tiếp flow cũ.");
            return;
        }

        StartLoadNextSceneFlow();
    }

    public void NotifyPostDialogueHealCompleted()
    {
        if (!waitingPostDialogueHeal)
        {
            Log("NotifyPostDialogueHealCompleted được gọi nhưng StoryManager không ở trạng thái chờ hồi máu.");
            return;
        }

        waitingPostDialogueHeal = false;

        SetupPostDialogueHealObject(false);

        Log("Phase cuối: HealInteractable báo đã hồi máu xong. Tiếp tục flow cũ.");
        StartLoadNextSceneFlow();
    }

    public void NotifyHealUsed()
    {
        NotifyPostDialogueHealCompleted();
    }

    void StartLoadNextSceneFlow()
    {
        if (loadNextSceneFlowStarted)
        {
            return;
        }

        loadNextSceneFlowStarted = true;

        if (loadNextSceneAfterHeal)
        {
            StartCoroutine(LoadNextSceneAfterDelayRoutine());
            return;
        }

        FinishStory();
    }

    IEnumerator LoadNextSceneAfterDelayRoutine()
    {
        if (delayBeforeLoadNextScene > 0f)
        {
            yield return new WaitForSeconds(delayBeforeLoadNextScene);
        }

        if (string.IsNullOrEmpty(nextSceneName))
        {
            Debug.LogWarning("Map3Boss2StoryManager: Chưa nhập Next Scene Name.");
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
            Debug.LogWarning("Map3Boss2StoryManager: Chưa gán SceneFadeController, chuyển scene trực tiếp.");
            SceneManager.LoadScene(nextSceneName);
        }
    }

    void CachePostDialogueHealColliders()
    {
        if (postDialogueHealObject == null)
        {
            postDialogueHealColliders = null;
            return;
        }

        // Lấy cả collider ở object con để tránh trường hợp vùng trigger nằm trong child object.
        postDialogueHealColliders = postDialogueHealObject.GetComponentsInChildren<Collider2D>(true);
    }

    void SetPostDialogueHealCollidersActive(bool active)
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

    void SetupPostDialogueHealObject(bool interactionEnabled)
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
            // Hết thoại sau boss mới bật lại trigger + script tương tác.
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

            Log("Phase cuối: Đã bật tương tác HealInteractable sau thoại boss.");
        }
        else
        {
            // Tắt hint trước để đầu map không hiện hướng dẫn hồi máu.
            if (postDialogueHealObject.interactHintObject != null)
            {
                postDialogueHealObject.interactHintObject.SetActive(false);
            }

            // Khóa code hồi máu.
            postDialogueHealObject.SendMessage("DisableInteraction", SendMessageOptions.DontRequireReceiver);
            postDialogueHealObject.enabled = false;

            // Khóa luôn trigger để chắc chắn không tương tác được từ đầu map.
            SetPostDialogueHealCollidersActive(false);

            Log("Đã khóa HealInteractable và trigger hồi máu.");
        }
    }

    IEnumerator RefreshHealTriggerAfterEnable()
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

    void FinishStory()
    {
        currentState = Map3Boss2StoryState.Finished;

        UnlockWukongAndParty();
        LockCharacters(false);
        HideBossUI();

        Log("Map3 Boss2 Story Finished.");
    }

    bool IsBoss2Dead()
    {
        if (boss2Object == null && boss2Controller == null)
            return false;

        if (boss2Object != null && !boss2Object.activeInHierarchy)
            return true;

        if (boss2Controller == null)
            return false;

        float hp;

        if (TryGetNumberFieldOrProperty(boss2Controller, bossCurrentHealthFieldName, out hp))
            return hp <= 0f;

        if (TryGetNumberFieldOrProperty(boss2Controller, "currentHealth", out hp))
            return hp <= 0f;

        if (TryGetNumberFieldOrProperty(boss2Controller, "CurrentHealth", out hp))
            return hp <= 0f;

        if (TryGetNumberFieldOrProperty(boss2Controller, "health", out hp))
            return hp <= 0f;

        if (TryGetNumberFieldOrProperty(boss2Controller, "Health", out hp))
            return hp <= 0f;

        return false;
    }

    // ======================================================
    // LOCK / UNLOCK
    // ======================================================

    void LockWukongAndParty()
    {
        LockWukong();
        LockParty();
    }

    void UnlockWukongAndParty()
    {
        UnlockWukong();
        UnlockParty();
    }

    void LockWukong()
    {
        if (wukongRigidbody != null)
        {
            wukongRigidbody.linearVelocity = Vector2.zero;
            wukongRigidbody.angularVelocity = 0f;
        }

        if (wukongController != null)
            wukongController.enabled = false;

        ForceWukongIdle();
    }

    void UnlockWukong()
    {
        if (wukongRigidbody != null)
        {
            wukongRigidbody.linearVelocity = Vector2.zero;
            wukongRigidbody.angularVelocity = 0f;
        }

        if (wukongController != null)
            wukongController.enabled = true;
    }

    void LockParty()
    {
        if (partyFollowScripts != null)
        {
            for (int i = 0; i < partyFollowScripts.Length; i++)
            {
                if (partyFollowScripts[i] != null)
                    partyFollowScripts[i].enabled = false;
            }
        }

        ForcePartyIdle();
    }

    void UnlockParty()
    {
        if (partyFollowScripts != null)
        {
            for (int i = 0; i < partyFollowScripts.Length; i++)
            {
                if (partyFollowScripts[i] != null)
                    partyFollowScripts[i].enabled = true;
            }
        }
    }

    void ForceWukongIdle()
    {
        if (wukongAnimator == null)
            return;

        SetAnimatorFloatIfExists(wukongAnimator, wukongSpeedParameterName, 0f);

        if (wukongBoolParametersToFalse != null)
        {
            for (int i = 0; i < wukongBoolParametersToFalse.Length; i++)
                SetAnimatorBoolIfExists(wukongAnimator, wukongBoolParametersToFalse[i], false);
        }

        if (wukongTriggersToReset != null)
        {
            for (int i = 0; i < wukongTriggersToReset.Length; i++)
                ResetAnimatorTriggerIfExists(wukongAnimator, wukongTriggersToReset[i]);
        }

        if (string.IsNullOrEmpty(wukongIdleStateName))
            return;

        if (HasAnimatorState(wukongAnimator, wukongIdleStateName))
        {
            wukongAnimator.Play(wukongIdleStateName, 0, 0f);
            wukongAnimator.Update(0f);
        }
        else
        {
            Debug.LogWarning("Map3Boss2StoryManager: Wukong Animator không có state: " + wukongIdleStateName);
        }
    }

    void ForcePartyIdle()
    {
        if (partyAnimators == null)
            return;

        for (int i = 0; i < partyAnimators.Length; i++)
        {
            Animator targetAnimator = partyAnimators[i];

            if (targetAnimator == null)
                continue;

            SetAnimatorFloatIfExists(targetAnimator, partySpeedParameterName, 0f);

            if (partyBoolParametersToFalse != null)
            {
                for (int j = 0; j < partyBoolParametersToFalse.Length; j++)
                    SetAnimatorBoolIfExists(targetAnimator, partyBoolParametersToFalse[j], false);
            }

            if (partyTriggersToReset != null)
            {
                for (int j = 0; j < partyTriggersToReset.Length; j++)
                    ResetAnimatorTriggerIfExists(targetAnimator, partyTriggersToReset[j]);
            }

            if (string.IsNullOrEmpty(partyIdleStateName))
                continue;

            if (HasAnimatorState(targetAnimator, partyIdleStateName))
            {
                targetAnimator.Play(partyIdleStateName, 0, 0f);
                targetAnimator.Update(0f);
            }
            else
            {
                Debug.LogWarning("Map3Boss2StoryManager: Party Animator không có state: " + partyIdleStateName);
            }
        }
    }

    // ======================================================
    // ANIMATOR HELPERS
    // ======================================================

    void SetAnimatorFloatIfExists(Animator targetAnimator, string parameterName, float value)
    {
        if (targetAnimator == null || string.IsNullOrEmpty(parameterName))
            return;

        AnimatorControllerParameter[] parameters = targetAnimator.parameters;

        for (int i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].name == parameterName && parameters[i].type == AnimatorControllerParameterType.Float)
            {
                targetAnimator.SetFloat(parameterName, value);
                return;
            }
        }
    }

    void SetAnimatorBoolIfExists(Animator targetAnimator, string parameterName, bool value)
    {
        if (targetAnimator == null || string.IsNullOrEmpty(parameterName))
            return;

        AnimatorControllerParameter[] parameters = targetAnimator.parameters;

        for (int i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].name == parameterName && parameters[i].type == AnimatorControllerParameterType.Bool)
            {
                targetAnimator.SetBool(parameterName, value);
                return;
            }
        }
    }

    void ResetAnimatorTriggerIfExists(Animator targetAnimator, string parameterName)
    {
        if (targetAnimator == null || string.IsNullOrEmpty(parameterName))
            return;

        AnimatorControllerParameter[] parameters = targetAnimator.parameters;

        for (int i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].name == parameterName && parameters[i].type == AnimatorControllerParameterType.Trigger)
            {
                targetAnimator.ResetTrigger(parameterName);
                return;
            }
        }
    }

    bool HasAnimatorState(Animator anim, string stateName)
    {
        if (anim == null || string.IsNullOrEmpty(stateName))
            return false;

        return anim.HasState(0, Animator.StringToHash(stateName));
    }

    // ======================================================
    // REFLECTION HELPERS
    // ======================================================

    bool SetTransformFieldOrProperty(object targetObject, string memberName, Transform value)
    {
        if (targetObject == null || value == null)
            return false;

        BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        Type type = targetObject.GetType();

        FieldInfo field = type.GetField(memberName, flags);

        if (field != null && field.FieldType == typeof(Transform))
        {
            field.SetValue(targetObject, value);
            return true;
        }

        PropertyInfo property = type.GetProperty(memberName, flags);

        if (property != null && property.CanWrite && property.PropertyType == typeof(Transform))
        {
            property.SetValue(targetObject, value);
            return true;
        }

        return false;
    }

    bool SetBoolFieldOrProperty(object targetObject, string memberName, bool value)
    {
        if (targetObject == null)
            return false;

        BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        Type type = targetObject.GetType();

        FieldInfo field = type.GetField(memberName, flags);

        if (field != null && field.FieldType == typeof(bool))
        {
            field.SetValue(targetObject, value);
            return true;
        }

        PropertyInfo property = type.GetProperty(memberName, flags);

        if (property != null && property.CanWrite && property.PropertyType == typeof(bool))
        {
            property.SetValue(targetObject, value);
            return true;
        }

        return false;
    }

    bool TryGetNumberFieldOrProperty(object targetObject, string memberName, out float value)
    {
        value = 0f;

        if (targetObject == null || string.IsNullOrEmpty(memberName))
            return false;

        BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        Type type = targetObject.GetType();

        FieldInfo field = type.GetField(memberName, flags);

        if (field != null)
        {
            object raw = field.GetValue(targetObject);

            if (raw is int intValue)
            {
                value = intValue;
                return true;
            }

            if (raw is float floatValue)
            {
                value = floatValue;
                return true;
            }

            if (raw is double doubleValue)
            {
                value = (float)doubleValue;
                return true;
            }
        }

        PropertyInfo property = type.GetProperty(memberName, flags);

        if (property != null && property.CanRead)
        {
            object raw = property.GetValue(targetObject);

            if (raw is int intValue)
            {
                value = intValue;
                return true;
            }

            if (raw is float floatValue)
            {
                value = floatValue;
                return true;
            }

            if (raw is double doubleValue)
            {
                value = (float)doubleValue;
                return true;
            }
        }

        return false;
    }
    public void NotifyWukongDeathFinished()
    {
        if (!gameOverWhenWukongDead)
        {
            return;
        }

        if (enableDebugLog)
        {
            Debug.Log("Map3Boss2StoryManager: Đã nhận báo Wukong chết xong animation.");
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
            Debug.Log("Map3Boss2StoryManager: Đã nhận báo đoàn thỉnh kinh hết máu.");
        }

        ShowGameOver("Đoàn thỉnh kinh hết máu.");
    }

    void ShowGameOver(string reason)
    {
        if (gameOverStarted)
        {
            return;
        }

        gameOverStarted = true;

        if (enableDebugLog)
        {
            Debug.Log("Map3Boss2StoryManager: GAME OVER. Lý do: " + reason);
        }

        StopAllStoryCombatForGameOver();
        HideUIBeforeGameOver();

        if (gameOverMenuController != null)
        {
            gameOverMenuController.ShowGameOver();
        }
        else
        {
            Debug.LogWarning("Map3Boss2StoryManager: Chưa gán GameOverMenuController.");
        }
    }

    void StopAllStoryCombatForGameOver()
    {
        // Dừng enemy wave nếu đang spawn.
        if (enemy123Spawner != null)
        {
            enemy123Spawner.StopSpawn();
            enemy123Spawner.isSpawning = false;
        }

        // Khóa boss để không tiếp tục đánh khi GameOver.
        DeactivateBoss2Combat();

        // Khóa Wukong và đoàn.
        LockWukongAndParty();
        LockCharacters(true);

        // Dừng coroutine check boss chết nếu đang chạy.
        if (bossDeathWatchCoroutine != null)
        {
            StopCoroutine(bossDeathWatchCoroutine);
            bossDeathWatchCoroutine = null;
        }

        if (healRefreshCoroutine != null)
        {
            StopCoroutine(healRefreshCoroutine);
            healRefreshCoroutine = null;
        }
    }

    void HideUIBeforeGameOver()
    {
        HideBossUI();
        HideDialogueUI();
        HideLocationTitleImmediate();

        if (hudController != null)
        {
            hudController.HideBoxInstant();
        }

        if (globalHUD != null)
        {
            globalHUD.SetActive(false);
        }

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

        if (postDialogueHealObject != null)
        {
            if (postDialogueHealObject.interactHintObject != null)
            {
                postDialogueHealObject.interactHintObject.SetActive(false);
            }

            postDialogueHealObject.SendMessage("DisableInteraction", SendMessageOptions.DontRequireReceiver);
            postDialogueHealObject.enabled = false;
            SetPostDialogueHealCollidersActive(false);
        }
    }
    void Log(string message)
    {
        if (!enableDebugLog)
            return;

        Debug.Log("[Map3Boss2StoryManager] " + message + " Current State = " + currentState);
    }
}