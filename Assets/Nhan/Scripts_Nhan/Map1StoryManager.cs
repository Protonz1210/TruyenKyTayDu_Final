using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

/// <summary>
/// Quản lý cốt truyện Map1.
/// Flow chính:
/// Spawn
/// -> Intro bài thơ
/// -> Tutorial
/// -> Post Tutorial Dialogue
/// -> Mở giới hạn phải đầu map
/// -> Chờ trigger Enemy123
/// -> Spawn Enemy123 + hiện box nhiệm vụ
/// -> Enemy chết hết thì mở giới hạn phải EnemyWave
/// -> Các phase tiếp theo: Supply / Heal / ChangeMap sẽ làm sau.
/// </summary>
public class Map1StoryManager : MonoBehaviour
{
    [System.Serializable]
    public class DialogueLine
    {
        [TextArea(2, 5)]
        [Tooltip("Nội dung câu thoại / câu thơ sẽ hiện trong box.")]
        public string text;

        [Tooltip("File âm thanh đọc câu này. Mỗi câu nên dùng 1 audio riêng để chữ và tiếng khớp nhau.")]
        public AudioClip voiceClip;
    }

    [System.Serializable]
    public class PostTutorialDialogueLine
    {
        [TextArea(2, 5)]
        [Tooltip("Nội dung box hội thoại sau tutorial.")]
        public string text;
    }

    public enum TutorialCompleteMode
    {
        AnyKey,
        AllKeys
    }

    public enum Map1Phase
    {
        Spawn,
        IntroPoem,
        Tutorial,
        PostTutorialDialogue,
        FreeMoveBeforeEnemyWave,
        EnemyWaveMission,
        EnemyWaveFight,
        EnemyWaveCleared,
        SupplyDialogue,
        SupplyItemWait,
        HealFullParty,
        WaitWukongIdleBeforeChangeMap,
        ChangeMap
    }

    [System.Serializable]
    public class TutorialLine
    {
        [TextArea(2, 5)]
        [Tooltip("Nội dung hướng dẫn sẽ hiện trong box.")]
        public string text;

        [Tooltip("AnyKey = bấm 1 trong các phím. AllKeys = phải bấm đủ tất cả phím.")]
        public TutorialCompleteMode completeMode = TutorialCompleteMode.AnyKey;

        [Tooltip("Danh sách phím cần người chơi bấm. Có thể thêm bao nhiêu phím tùy ý trong Inspector.")]
        public Key[] requiredKeys;

        [Tooltip("Thời gian chờ nhẹ trước khi chuyển sang hướng dẫn tiếp theo.")]
        public float delayAfterComplete = 0.2f;

        [Tooltip("Bật lên để sau khi bấm đúng phím sẽ chờ Wukong thực hiện hành động xong rồi mới chuyển box.")]
        public bool waitWukongActionComplete = true;

        [Tooltip("Bật lên để bắt người chơi nhả hết phím yêu cầu rồi mới chuyển box tiếp theo.")]
        public bool requireKeyReleaseBeforeNextBox = true;

        [Tooltip("Sau khi bấm đúng phím, chờ tối thiểu một chút để Animator kịp chuyển khỏi Idle.")]
        public float minWaitBeforeIdleCheck = 0.15f;

        [Tooltip("Thời gian tối đa chờ Wukong rời khỏi Idle. Hết thời gian này vẫn tiếp tục bước chờ quay lại Idle.")]
        public float maxWaitForLeaveIdleTime = 0.5f;
    }

    [Header("Phase Debug")]
    [Tooltip("Phase hiện tại của Map1. Dùng để debug trong Inspector.")]
    public Map1Phase currentPhase = Map1Phase.Spawn;

    [Header("Auto Start")]
    [Tooltip("Bật lên để vừa vào Map1 là tự chạy đoạn thoại mở đầu.")]
    public bool autoStartIntroOnStart = true;

    [Tooltip("Thời gian chờ nhẹ sau khi Wukong đã sẵn sàng rồi mới hiện box thoại.")]
    public float startDelay = 0.3f;

    [Header("Intro Start Wait")]
    [Tooltip("Bật lên để chờ Wukong về Idle sau khi spawn rồi mới chạy intro.")]
    public bool waitWukongIdleBeforeIntro = true;

    [Tooltip("Tên state Idle trong Animator của Wukong.")]
    public string wukongIdleStateName = "Wukong Idle";

    [Tooltip("Thời gian chờ tối đa để Wukong tự về Idle. Hết thời gian này sẽ ép về Idle rồi chạy intro.")]
    public float maxWaitForIdleTime = 2f;

    [Tooltip("Chờ thêm một chút sau khi đã thấy Wukong Idle để tránh Animator chưa ổn định.")]
    public float extraDelayAfterIdle = 0.15f;

    [Header("Cooldown Control")]
    [Tooltip("Bật lên để tắt script hồi chiêu WukongSkillCooldown cho đến khi Tutorial kết thúc.")]
    public bool disableCooldownUntilTutorialEnd = true;

    [Tooltip("Bật lên để tự tìm WukongSkillCooldown trong Wukong Object.")]
    public bool autoFindWukongSkillCooldown = true;

    private Behaviour wukongSkillCooldown;

    [Header("Global HUD")]
    [Tooltip("Kéo object UI tổng GlobalHUD vào đây. Script sẽ tắt UI này khi intro bắt đầu và bật lại khi intro kết thúc.")]
    public GameObject globalHUDObject;

    [Tooltip("Bật lên nếu muốn tắt UI tổng trong lúc intro bài thơ.")]
    public bool hideGlobalHUDDuringIntro = true;

    [Tooltip("UIDocument của GlobalHUD. Dùng để tắt box hội thoại trong UI tổng khi vừa bật HUD.")]
    public UIDocument globalHUDUIDocument;

    [Tooltip("Bật lên để khi GlobalHUD hiện lại thì tự ẩn box hội thoại của UI tổng.")]
    public bool hideGlobalDialogueBoxWhenShowHUD = true;

    [Tooltip("Tên object cha của box hội thoại trong GlobalHUD.")]
    public string globalDialogueBoxElementName = "dialogue-box";

    [Header("Wukong Lock")]
    [Tooltip("Kéo object Wukong vào đây. Script sẽ tự tìm PlayerController, Rigidbody2D và Animator bên trong.")]
    public GameObject wukongObject;

    [Header("Party Lock")]
    [Tooltip("Kéo NPC1, NPC2, NPC3 vào đây để tắt di chuyển đoàn thỉnh kinh khi intro.")]
    public GameObject[] partyObjectsToStop;

    [Tooltip("Bật lên để đóng băng vật lý đoàn thỉnh kinh trong lúc intro.")]
    public bool freezePartyPhysicsDuringIntro = true;

    [Header("Dialogue UI")]
    [Tooltip("Kéo UIDocument của Map1PoemDialogueUI vào đây. Không kéo GlobalHUD.")]
    public UIDocument dialogueUIDocument;

    [Tooltip("Tên VisualElement của box thoại trong UI Document.")]
    public string dialogueBoxElementName = "DialogueBox";

    [Tooltip("Tên Label hiển thị nội dung thoại trong UI Document.")]
    public string dialogueTextElementName = "DialogueText";

    [Tooltip("Tên Label hiển thị gợi ý skip, ví dụ nút E. Có thể để trống nếu không dùng.")]
    public string dialogueHintElementName = "DialogueHint";

    [Header("Input")]
    [Tooltip("Bật lên để cho phép nhấn phím bỏ qua intro / chuyển box thoại.")]
    public bool useSkipKeyToNext = true;

    [Tooltip("Phím dùng để bỏ qua intro / chuyển box thoại.")]
    public Key skipKey = Key.E;

    [Tooltip("Nội dung hiển thị trong DialogueHint. Ví dụ: E, SPACE, ENTER.")]
    public string nextHint = "E";

    [Header("Dialogue Lines")]
    [Tooltip("Danh sách câu thoại / câu thơ. Mỗi Element là 1 câu. Có thể gán audio riêng cho từng câu.")]
    public DialogueLine[] dialogueLines;

    [Header("Tutorial")]
    [Tooltip("Bật lên để sau intro tự chạy phần hướng dẫn thao tác.")]
    public bool startTutorialAfterIntro = true;

    [Tooltip("Danh sách các box hướng dẫn. Mỗi box có thể đặt nội dung và danh sách phím riêng.")]
    public TutorialLine[] tutorialLines;

    [Tooltip("Bật lên để sau mỗi thao tác tutorial phải chờ Wukong về Idle rồi mới chuyển box tiếp theo.")]
    public bool waitWukongIdleBeforeNextTutorialBox = true;

    [Tooltip("Thời gian chờ tối đa để Wukong tự về Idle sau mỗi thao tác tutorial.")]
    public float maxWaitForTutorialStepIdleTime = 5f;

    [Tooltip("Chờ thêm một chút sau khi Wukong đã về Idle rồi mới chuyển box tiếp theo.")]
    public float extraDelayAfterTutorialStepIdle = 0.15f;

    [Tooltip("Nếu chờ quá lâu mà Wukong chưa Idle thì có ép về Idle để tránh kẹt tutorial không.")]
    public bool forceIdleIfTutorialStepTimeout = false;

    [Tooltip("Bật lên để khi hết tutorial sẽ chờ Wukong về Idle rồi mới kết thúc tutorial.")]
    public bool waitWukongIdleBeforeEndTutorial = true;

    [Tooltip("Thời gian chờ tối đa để Wukong tự về Idle trước khi ép kết thúc tutorial.")]
    public float maxWaitForTutorialEndIdleTime = 3f;

    [Tooltip("Chờ thêm một chút sau khi Wukong đã về Idle rồi mới tắt box tutorial.")]
    public float extraDelayAfterTutorialIdle = 0.15f;

    [Header("Post Tutorial Dialogue")]
    [Tooltip("Bật lên để sau tutorial hiện thêm đoạn hội thoại trước khi bật GlobalHUD.")]
    public bool startPostTutorialDialogueAfterTutorial = true;

    [Tooltip("Danh sách box hội thoại sau tutorial. Không giới hạn số lượng box.")]
    public PostTutorialDialogueLine[] postTutorialDialogueLines;

    [Tooltip("Chờ nhẹ sau mỗi box hội thoại sau tutorial.")]
    public float postTutorialDelayBetweenLines = 0.15f;

    [Header("Map Limit Release")]
    [Tooltip("Object box chặn tạm thời bên phải đầu map. Hết Post Tutorial Dialogue sẽ tắt object này.")]
    public GameObject startTemporaryRightBlockerObject;

    [Tooltip("Bật lên để hết Post Tutorial Dialogue thì tắt box chặn phải đầu map.")]
    public bool releaseStartRightLimitAfterPostTutorialDialogue = true;

    [Tooltip("CameraFollowTarget có gắn Map1CameraFollowTargetLimiter.")]
    public Map1CameraFollowTargetLimiter map1CameraLimiter;

    [Header("Enemy Wave")]
    [Tooltip("Spawner Enemy123 của Map1.")]
    public Enemy123RandomSpawner enemyWaveSpawner;

    [Tooltip("Box chặn phải khi đang đánh Enemy123.")]
    public GameObject enemyWaveRightBlockerObject;

    [Tooltip("Dialogue hiện trên GlobalHUD khi Enemy123 xuất hiện.")]
    public Map1GlobalDialogueController enemyWaveDialogue;

    [Tooltip("Bao lâu kiểm tra một lần xem Enemy123 đã chết hết chưa.")]
    public float enemyWaveClearCheckInterval = 0.5f;

    [Header("After Enemy Wave Dialogue")]
    [Tooltip("Dialogue hiện sau khi Enemy123 chết hết. Không cho skip, chỉ hiện thông báo / thoại ngắn.")]
    public Map1GlobalDialogueController afterEnemyWaveDialogue;

    [Header("Supply / NPC Dialogue")]
    [Tooltip("Dialogue NPC tiếp tế. Dạng này cho phép nhấn E để chuyển câu.")]
    public Map1GlobalDialogueController supplyPointDialogue;

    [Tooltip("Khóa Wukong và đoàn khi nói chuyện với NPC.")]
    public bool lockPlayerAndPartyDuringSupplyDialogue = true;

    [Tooltip("Ẩn thoại sau EnemyWave khi bắt đầu nói chuyện với NPC.")]
    public bool hideAfterEnemyWaveDialogueWhenNpcTalk = true;

    private bool supplyPointStarted;

    [Header("Supply Item")]
    [Tooltip("Object hồi máu sẽ hiện ra sau khi nói chuyện xong với NPC.")]
    public GameObject supplyHealObject;

    [Tooltip("Ẩn object hồi máu khi bắt đầu scene, chỉ hiện sau hội thoại NPC.")]
    public bool hideSupplyHealObjectOnStart = true;

    [Header("Text And Audio Sync")]
    [Tooltip("Nếu câu không có audio, mỗi ký tự sẽ hiện sau khoảng thời gian này.")]
    public float fallbackCharDelay = 0.04f;

    [Tooltip("Thời gian nghỉ giữa 2 câu sau khi audio đọc xong.")]
    public float delayBetweenLines = 0.35f;

    [Tooltip("Bật để phát audio đọc thoại.")]
    public bool playVoiceAudio = true;

    [Header("Audio")]
    [Tooltip("AudioSource dùng để phát giọng đọc. Nếu bỏ trống, script sẽ tự thêm AudioSource vào object này.")]
    public AudioSource voiceAudioSource;

    [Header("Change Map After Heal")]
    [Tooltip("Bật lên để sau khi dùng vật phẩm hồi máu sẽ chuyển sang màn tiếp theo.")]
    public bool changeMapAfterHeal = true;

    [Tooltip("Tên scene/map sẽ chuyển tới sau khi hồi máu.")]
    public string nextSceneName = "Map2";

    [Tooltip("Delay nhẹ sau khi hồi máu rồi mới chuyển màn.")]
    public float delayBeforeChangeMapAfterHeal = 1f;

    [Tooltip("Chờ Wukong về Idle trước khi chuyển màn.")]
    public bool waitWukongIdleBeforeChangeMapAfterHeal = true;

    [Tooltip("Thời gian tối đa chờ Wukong về Idle trước khi chuyển màn.")]
    public float maxWaitWukongIdleBeforeChangeMapAfterHeal = 3f;

    private bool healUsedStarted;

    private Behaviour wukongController;
    private Rigidbody2D wukongRigidbody;
    private Animator wukongAnimator;

    private RigidbodyConstraints2D originalWukongConstraints;
    private bool cachedWukongConstraints;

    private VisualElement dialogueBox;
    private Label dialogueText;

    private bool introRunning;
    private Coroutine introCoroutine;

    private bool tutorialRunning;
    private Coroutine tutorialCoroutine;

    private bool postTutorialDialogueRunning;
    private Coroutine postTutorialDialogueCoroutine;

    private Coroutine enemyWaveMonitorCoroutine;
    private bool enemyWaveStarted;
    private bool enemyWaveCleared;

    private bool skipIntroRequested;
    private Label dialogueHint;

    private Behaviour[] cachedPartyMoveScripts;
    private Rigidbody2D[] cachedPartyRigidbodies;
    private Animator[] cachedPartyAnimators;

    private RigidbodyType2D[] cachedPartyBodyTypes;
    private float[] cachedPartyGravityScales;
    private RigidbodyConstraints2D[] cachedPartyConstraints;

    private void Awake()
    {
        SetPhase(Map1Phase.Spawn);

        AutoFindMissingReferences();
        AutoFindWukongSkillCooldown();
        CachePartyComponents();
        BindUIElements();
        SetupSupplyHealObject();

        if (autoStartIntroOnStart && hideGlobalHUDDuringIntro)
        {
            HideGlobalHUD();
        }

        DisableCooldownUntilTutorialEnd();
        HideDialogueBox();
    }

    private void Start()
    {
        if (autoStartIntroOnStart)
        {
            StartCoroutine(StartIntroAfterWukongReadyRoutine());
        }
    }

    private void Update()
    {
        if (introRunning && useSkipKeyToNext)
        {
            if (WasKeyPressed(skipKey))
            {
                SkipIntro();
            }
        }
    }

    private void SetPhase(Map1Phase newPhase)
    {
        currentPhase = newPhase;
        Debug.Log("Map1StoryManager: Chuyển phase sang " + currentPhase);
    }

    private bool WasKeyPressed(Key key)
    {
        if (Keyboard.current == null)
        {
            return false;
        }

        KeyControl keyControl = Keyboard.current[key];

        return keyControl != null && keyControl.wasPressedThisFrame;
    }

    private bool IsKeyPressed(Key key)
    {
        if (Keyboard.current == null)
        {
            return false;
        }

        KeyControl keyControl = Keyboard.current[key];

        return keyControl != null && keyControl.isPressed;
    }

    private IEnumerator StartIntroAfterWukongReadyRoutine()
    {
        yield return null;

        FindWukongComponents();

        if (waitWukongIdleBeforeIntro)
        {
            float timer = 0f;

            while (timer < maxWaitForIdleTime)
            {
                if (IsWukongIdleAndStable())
                {
                    break;
                }

                timer += Time.deltaTime;
                yield return null;
            }

            if (!IsWukongIdleAndStable())
            {
                ForceWukongIdle();
            }

            if (extraDelayAfterIdle > 0f)
            {
                yield return new WaitForSeconds(extraDelayAfterIdle);
            }
        }

        StartMap1Intro();
    }
    private void SetupSupplyHealObject()
    {
        if (!hideSupplyHealObjectOnStart)
        {
            return;
        }

        if (supplyHealObject != null)
        {
            supplyHealObject.SetActive(false);
        }
    }
    public void StartMap1Intro()
    {
        if (introRunning)
        {
            return;
        }

        SetPhase(Map1Phase.IntroPoem);
        skipIntroRequested = false;

        if (introCoroutine != null)
        {
            StopCoroutine(introCoroutine);
        }

        introCoroutine = StartCoroutine(PlayIntroRoutine());
    }
    private void ShowSupplyHealObject()
    {
        if (supplyHealObject != null)
        {
            supplyHealObject.SetActive(true);
            Debug.Log("Map1StoryManager: Đã hiện object hồi máu sau hội thoại NPC.");
        }
        else
        {
            Debug.LogWarning("Map1StoryManager: Chưa gán Supply Heal Object.");
        }
    }
    private IEnumerator PlayIntroRoutine()
    {
        introRunning = true;
        skipIntroRequested = false;

        HideGlobalHUD();
        LockPlayerAndParty();
        HideDialogueBox();

        if (startDelay > 0f)
        {
            yield return StartCoroutine(WaitWithSkip(startDelay));
        }

        if (skipIntroRequested)
        {
            yield break;
        }

        ShowDialogueBox();

        if (dialogueLines == null || dialogueLines.Length == 0)
        {
            Debug.LogWarning("Map1StoryManager: Chưa có câu nào trong Dialogue Lines.");

            HideDialogueBox();
            UnlockPlayerAndParty();

            introRunning = false;
            introCoroutine = null;

            if (startTutorialAfterIntro)
            {
                StartTutorial();
            }
            else
            {
                ShowGlobalHUD();
                EnableCooldownAfterTutorial();
            }

            yield break;
        }

        for (int i = 0; i < dialogueLines.Length; i++)
        {
            if (skipIntroRequested)
            {
                yield break;
            }

            DialogueLine line = dialogueLines[i];

            if (line == null)
            {
                continue;
            }

            yield return StartCoroutine(PlayOneLineRoutine(line));
        }

        if (skipIntroRequested)
        {
            yield break;
        }

        HideDialogueBox();
        UnlockPlayerAndParty();

        introRunning = false;
        introCoroutine = null;

        Debug.Log("Map1StoryManager: Đã chạy xong đoạn thoại mở đầu Map1.");

        if (startTutorialAfterIntro)
        {
            StartTutorial();
        }
        else
        {
            ShowGlobalHUD();
            EnableCooldownAfterTutorial();
        }
    }

    private void SkipIntro()
    {
        if (!introRunning)
        {
            return;
        }

        skipIntroRequested = true;

        if (voiceAudioSource != null && voiceAudioSource.isPlaying)
        {
            voiceAudioSource.Stop();
        }

        if (introCoroutine != null)
        {
            StopCoroutine(introCoroutine);
            introCoroutine = null;
        }

        HideDialogueBox();
        UnlockPlayerAndParty();

        introRunning = false;

        Debug.Log("Map1StoryManager: Người chơi đã skip intro Map1.");

        if (startTutorialAfterIntro)
        {
            StartTutorial();
        }
        else
        {
            ShowGlobalHUD();
            EnableCooldownAfterTutorial();
        }
    }

    private void StartTutorial()
    {
        if (tutorialRunning)
        {
            return;
        }

        SetPhase(Map1Phase.Tutorial);

        if (tutorialCoroutine != null)
        {
            StopCoroutine(tutorialCoroutine);
        }

        tutorialCoroutine = StartCoroutine(PlayTutorialRoutine());
    }

    private IEnumerator PlayTutorialRoutine()
    {
        tutorialRunning = true;

        if (tutorialLines == null || tutorialLines.Length == 0)
        {
            ShowGlobalHUD();
            EnableCooldownAfterTutorial();

            tutorialRunning = false;
            tutorialCoroutine = null;

            Debug.LogWarning("Map1StoryManager: Chưa có Tutorial Lines. Bật GlobalHUD luôn.");
            yield break;
        }

        ShowTutorialBox();

        for (int i = 0; i < tutorialLines.Length; i++)
        {
            TutorialLine line = tutorialLines[i];

            if (line == null)
            {
                continue;
            }

            if (dialogueText != null)
            {
                dialogueText.text = line.text;
            }

            yield return StartCoroutine(WaitForTutorialInput(line));
            yield return StartCoroutine(WaitTutorialActionCompleteRoutine(line));
        }

        HideDialogueBox();

        tutorialRunning = false;
        tutorialCoroutine = null;

        Debug.Log("Map1StoryManager: Đã hoàn thành tutorial Map1.");

        if (startPostTutorialDialogueAfterTutorial)
        {
            StartPostTutorialDialogue();
        }
        else
        {
            ShowGlobalHUD();
            EnableCooldownAfterTutorial();

            Debug.Log("Map1StoryManager: Không có post tutorial dialogue. GlobalHUD và cooldown đã được bật.");
        }
    }

    private IEnumerator WaitTutorialActionCompleteRoutine(TutorialLine line)
    {
        if (line == null)
        {
            yield break;
        }

        if (!line.waitWukongActionComplete)
        {
            yield break;
        }

        FindWukongComponents();

        if (line.minWaitBeforeIdleCheck > 0f)
        {
            yield return new WaitForSeconds(line.minWaitBeforeIdleCheck);
        }

        float leaveIdleTimer = 0f;

        while (leaveIdleTimer < line.maxWaitForLeaveIdleTime)
        {
            if (!IsWukongIdleAndStable())
            {
                break;
            }

            leaveIdleTimer += Time.deltaTime;
            yield return null;
        }

        if (line.requireKeyReleaseBeforeNextBox)
        {
            while (IsAnyTutorialKeyPressed(line))
            {
                yield return null;
            }
        }

        while (!IsWukongIdleAndStable())
        {
            yield return null;
        }

        if (line.delayAfterComplete > 0f)
        {
            yield return new WaitForSeconds(line.delayAfterComplete);
        }
    }

    private bool IsAnyTutorialKeyPressed(TutorialLine line)
    {
        if (Keyboard.current == null)
        {
            return false;
        }

        if (line == null || line.requiredKeys == null)
        {
            return false;
        }

        for (int i = 0; i < line.requiredKeys.Length; i++)
        {
            Key key = line.requiredKeys[i];
            KeyControl keyControl = Keyboard.current[key];

            if (keyControl != null && keyControl.isPressed)
            {
                return true;
            }
        }

        return false;
    }

    private IEnumerator WaitForTutorialInput(TutorialLine line)
    {
        if (line.requiredKeys == null || line.requiredKeys.Length == 0)
        {
            Debug.LogWarning("Map1StoryManager: TutorialLine chưa có Required Keys. Bước này sẽ tự bỏ qua.");
            yield break;
        }

        bool[] pressedKeys = new bool[line.requiredKeys.Length];

        while (true)
        {
            for (int i = 0; i < line.requiredKeys.Length; i++)
            {
                Key key = line.requiredKeys[i];

                if (WasKeyPressed(key))
                {
                    pressedKeys[i] = true;

                    if (line.completeMode == TutorialCompleteMode.AnyKey)
                    {
                        yield break;
                    }
                }
            }

            if (line.completeMode == TutorialCompleteMode.AllKeys)
            {
                bool allPressed = true;

                for (int i = 0; i < pressedKeys.Length; i++)
                {
                    if (!pressedKeys[i])
                    {
                        allPressed = false;
                        break;
                    }
                }

                if (allPressed)
                {
                    yield break;
                }
            }

            yield return null;
        }
    }

    private void StartPostTutorialDialogue()
    {
        if (postTutorialDialogueRunning)
        {
            return;
        }

        SetPhase(Map1Phase.PostTutorialDialogue);

        if (postTutorialDialogueCoroutine != null)
        {
            StopCoroutine(postTutorialDialogueCoroutine);
        }

        postTutorialDialogueCoroutine = StartCoroutine(PlayPostTutorialDialogueRoutine());
    }

    private IEnumerator PlayPostTutorialDialogueRoutine()
    {
        postTutorialDialogueRunning = true;

        if (postTutorialDialogueLines == null || postTutorialDialogueLines.Length == 0)
        {
            HideDialogueBox();

            ReleaseStartRightLimitAfterPostTutorialDialogue();
            SetPhase(Map1Phase.FreeMoveBeforeEnemyWave);

            ShowGlobalHUD();
            EnableCooldownAfterTutorial();

            postTutorialDialogueRunning = false;
            postTutorialDialogueCoroutine = null;

            Debug.LogWarning("Map1StoryManager: Chưa có Post Tutorial Dialogue Lines. Đã mở đường sang phase Enemy Wave.");
            yield break;
        }

        ShowPostTutorialDialogueBox();

        for (int i = 0; i < postTutorialDialogueLines.Length; i++)
        {
            PostTutorialDialogueLine line = postTutorialDialogueLines[i];

            if (line == null)
            {
                continue;
            }

            if (dialogueText != null)
            {
                dialogueText.text = line.text;
            }

            yield return StartCoroutine(WaitForPostTutorialNextKey());

            if (postTutorialDelayBetweenLines > 0f)
            {
                yield return new WaitForSeconds(postTutorialDelayBetweenLines);
            }
        }

        HideDialogueBox();

        ReleaseStartRightLimitAfterPostTutorialDialogue();
        SetPhase(Map1Phase.FreeMoveBeforeEnemyWave);

        ShowGlobalHUD();
        EnableCooldownAfterTutorial();

        postTutorialDialogueRunning = false;
        postTutorialDialogueCoroutine = null;

        Debug.Log("Map1StoryManager: Đã hoàn thành post tutorial dialogue. Đã mở đường sang phase Enemy Wave.");
    }

    private IEnumerator WaitForPostTutorialNextKey()
    {
        while (useSkipKeyToNext && IsKeyPressed(skipKey))
        {
            yield return null;
        }

        while (true)
        {
            if (useSkipKeyToNext && WasKeyPressed(skipKey))
            {
                yield break;
            }

            yield return null;
        }
    }

    public void OnMap1StoryTriggerEntered(Map1StoryTrigger.Map1TriggerType triggerType)
    {
        if (triggerType == Map1StoryTrigger.Map1TriggerType.EnemyWave)
        {
            StartEnemyWaveByTrigger();
            return;
        }

        if (triggerType == Map1StoryTrigger.Map1TriggerType.SupplyPoint)
        {
            StartSupplyPointByTrigger();
            return;
        }

        if (triggerType == Map1StoryTrigger.Map1TriggerType.EndMap)
        {
            StartEndMapByTrigger();
            return;
        }
    }

    public void StartEnemyWaveByTrigger()
    {
        if (enemyWaveStarted)
        {
            Debug.Log("Map1StoryManager: EnemyWave đã bắt đầu rồi, không gọi lại StartSpawn.");
            return;
        }

        if (currentPhase != Map1Phase.FreeMoveBeforeEnemyWave)
        {
            Debug.Log("Map1StoryManager: Không thể bắt đầu EnemyWave vì phase hiện tại là " + currentPhase);
            return;
        }

        StartEnemyWave();
    }

    public void StartSupplyPointByTrigger()
    {
        if (supplyPointStarted)
        {
            Debug.Log("Map1StoryManager: SupplyPoint đã chạy rồi, không kích hoạt lại.");
            return;
        }

        if (currentPhase != Map1Phase.EnemyWaveCleared)
        {
            Debug.Log("Map1StoryManager: Chưa thể kích hoạt SupplyPoint vì phase hiện tại là " + currentPhase);
            return;
        }

        supplyPointStarted = true;

        SetPhase(Map1Phase.SupplyDialogue);

        // Khi bắt đầu nói chuyện với NPC thì tắt box thoại sau EnemyWave nếu đang hiện.
        if (hideAfterEnemyWaveDialogueWhenNpcTalk && afterEnemyWaveDialogue != null)
        {
            afterEnemyWaveDialogue.HideDialogue();
        }

        if (lockPlayerAndPartyDuringSupplyDialogue)
        {
            LockPlayerAndParty();
        }

        if (supplyPointDialogue != null)
        {
            supplyPointDialogue.StartDialogue(
                supplyPointDialogue.dialogueLines,
                OnSupplyPointDialogueFinished
            );
        }
        else
        {
            Debug.LogWarning("Map1StoryManager: Chưa gán Supply Point Dialogue.");
            OnSupplyPointDialogueFinished();
        }

        Debug.Log("Map1StoryManager: Bắt đầu hội thoại NPC tiếp tế.");
    }

    private void OnSupplyPointDialogueFinished()
    {
        if (lockPlayerAndPartyDuringSupplyDialogue)
        {
            UnlockPlayerAndParty();
        }

        ShowSupplyHealObject();

        SetPhase(Map1Phase.SupplyItemWait);

        Debug.Log("Map1StoryManager: Đã nói chuyện xong với NPC. Đã hiện object hồi máu và chuyển sang phase SupplyItemWait.");
    }

    public void StartEndMapByTrigger()
    {
        Debug.Log("Map1StoryManager: Trigger EndMap đã được kích hoạt. Phase chuyển map sẽ làm sau.");

        // Sau này:
        // SetPhase(Map1Phase.WaitWukongIdleBeforeChangeMap);
        // Chờ Wukong về Idle.
        // Chuyển sang map tiếp theo.
    }

    private void StartEnemyWave()
    {
        if (enemyWaveStarted)
        {
            Debug.Log("Map1StoryManager: StartEnemyWave bị gọi lại, đã chặn.");
            return;
        }

        enemyWaveStarted = true;
        enemyWaveCleared = false;

        SetPhase(Map1Phase.EnemyWaveMission);

        if (enemyWaveRightBlockerObject != null)
        {
            enemyWaveRightBlockerObject.SetActive(true);
        }

        if (map1CameraLimiter != null)
        {
            map1CameraLimiter.ActivateEnemyWaveRightLimit();
        }

        if (enemyWaveDialogue != null)
        {
            enemyWaveDialogue.PlayDialogue();
        }
        else
        {
            Debug.LogWarning("Map1StoryManager: Chưa gán Enemy Wave Dialogue.");
        }

        if (enemyWaveSpawner != null)
        {
            enemyWaveSpawner.StartSpawn();
            Debug.Log("Map1StoryManager: Đã gọi Enemy123RandomSpawner.StartSpawn đúng 1 lần.");
        }
        else
        {
            Debug.LogWarning("Map1StoryManager: Chưa gán Enemy Wave Spawner.");
        }

        SetPhase(Map1Phase.EnemyWaveFight);

        if (enemyWaveMonitorCoroutine != null)
        {
            StopCoroutine(enemyWaveMonitorCoroutine);
            enemyWaveMonitorCoroutine = null;
        }

        enemyWaveMonitorCoroutine = StartCoroutine(MonitorEnemyWaveRoutine());

        Debug.Log("Map1StoryManager: Đã bắt đầu Enemy123 Wave.");
    }

    private IEnumerator MonitorEnemyWaveRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(enemyWaveClearCheckInterval);

            if (enemyWaveSpawner == null)
            {
                yield break;
            }

            if (enemyWaveSpawner.IsSpawnFinished())
            {
                EnemyWaveCleared();
                yield break;
            }
        }
    }
    public void NotifyHealUsed()
    {
        if (healUsedStarted)
        {
            return;
        }

        healUsedStarted = true;

        SetPhase(Map1Phase.HealFullParty);

        Debug.Log("Map1StoryManager: Người chơi đã dùng vật phẩm hồi máu. Chuyển sang phase HealFullParty.");

        if (changeMapAfterHeal)
        {
            StartCoroutine(ChangeMapAfterHealRoutine());
        }
    }
    private IEnumerator ChangeMapAfterHealRoutine()
    {
        if (delayBeforeChangeMapAfterHeal > 0f)
        {
            yield return new WaitForSeconds(delayBeforeChangeMapAfterHeal);
        }

        SetPhase(Map1Phase.WaitWukongIdleBeforeChangeMap);

        if (waitWukongIdleBeforeChangeMapAfterHeal)
        {
            FindWukongComponents();

            float timer = 0f;

            while (timer < maxWaitWukongIdleBeforeChangeMapAfterHeal)
            {
                if (IsWukongIdleAndStable())
                {
                    break;
                }

                timer += Time.deltaTime;
                yield return null;
            }
        }

        SetPhase(Map1Phase.ChangeMap);

        if (string.IsNullOrEmpty(nextSceneName))
        {
            Debug.LogWarning("Map1StoryManager: Chưa nhập Next Scene Name nên không thể chuyển màn.");
            yield break;
        }

        Debug.Log("Map1StoryManager: Chuyển sang scene " + nextSceneName);

        SceneManager.LoadScene(nextSceneName);
    }
    private void EnemyWaveCleared()
    {
        if (enemyWaveCleared)
        {
            return;
        }

        enemyWaveCleared = true;

        if (enemyWaveSpawner != null)
        {
            enemyWaveSpawner.StopSpawn();
        }

        // Ẩn box nhiệm vụ khi đang đánh EnemyWave.
        if (enemyWaveDialogue != null)
        {
            enemyWaveDialogue.HideDialogue();
        }

        if (enemyWaveRightBlockerObject != null)
        {
            enemyWaveRightBlockerObject.SetActive(false);
        }

        if (map1CameraLimiter != null)
        {
            map1CameraLimiter.ReleaseEnemyWaveRightLimit();
        }

        SetPhase(Map1Phase.EnemyWaveCleared);

        enemyWaveMonitorCoroutine = null;

        // Hiện thoại sau khi enemy chết hết.
        // Dialogue này nên để MissionSingleLine, không có E, không skip.
        if (afterEnemyWaveDialogue != null)
        {
            afterEnemyWaveDialogue.PlayDialogue();
        }
        else
        {
            Debug.LogWarning("Map1StoryManager: Chưa gán After Enemy Wave Dialogue.");
        }

        Debug.Log("Map1StoryManager: Enemy123 Wave đã clear. Đã hiện thoại sau trận.");
    }
    private void ShowTutorialBox()
    {
        if (dialogueBox != null)
        {
            dialogueBox.style.display = DisplayStyle.Flex;
        }

        if (dialogueHint != null)
        {
            dialogueHint.style.display = DisplayStyle.None;
        }
    }

    private void ShowPostTutorialDialogueBox()
    {
        if (dialogueBox != null)
        {
            dialogueBox.style.display = DisplayStyle.Flex;
        }

        if (dialogueHint != null)
        {
            dialogueHint.text = nextHint;
            dialogueHint.style.display = DisplayStyle.Flex;
        }
    }

    private IEnumerator PlayOneLineRoutine(DialogueLine line)
    {
        if (dialogueText == null)
        {
            yield break;
        }

        string fullText = line.text;

        if (string.IsNullOrEmpty(fullText))
        {
            fullText = "";
        }

        dialogueText.text = "";

        float audioLength = 0f;

        if (line.voiceClip != null)
        {
            audioLength = line.voiceClip.length;
        }

        if (playVoiceAudio && voiceAudioSource != null && line.voiceClip != null)
        {
            voiceAudioSource.Stop();
            voiceAudioSource.clip = line.voiceClip;
            voiceAudioSource.Play();
        }

        float charDelay = fallbackCharDelay;

        if (audioLength > 0f && fullText.Length > 0)
        {
            charDelay = audioLength / fullText.Length;
        }

        for (int i = 0; i < fullText.Length; i++)
        {
            if (skipIntroRequested)
            {
                yield break;
            }

            dialogueText.text += fullText[i];

            yield return StartCoroutine(WaitWithSkip(charDelay));
        }

        if (skipIntroRequested)
        {
            yield break;
        }

        dialogueText.text = fullText;

        if (playVoiceAudio && voiceAudioSource != null)
        {
            while (voiceAudioSource.isPlaying)
            {
                if (skipIntroRequested)
                {
                    yield break;
                }

                yield return null;
            }
        }
        else if (audioLength > 0f)
        {
            yield return StartCoroutine(WaitWithSkip(audioLength));
        }

        if (delayBetweenLines > 0f)
        {
            yield return StartCoroutine(WaitWithSkip(delayBetweenLines));
        }
    }

    private IEnumerator WaitWithSkip(float duration)
    {
        float timer = 0f;

        while (timer < duration)
        {
            if (skipIntroRequested)
            {
                yield break;
            }

            timer += Time.deltaTime;
            yield return null;
        }
    }

    private void AutoFindMissingReferences()
    {
        FindWukongComponents();

        if (dialogueUIDocument == null)
        {
            dialogueUIDocument = Object.FindFirstObjectByType<UIDocument>();
        }

        if (voiceAudioSource == null)
        {
            voiceAudioSource = GetComponent<AudioSource>();

            if (voiceAudioSource == null)
            {
                voiceAudioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        if (voiceAudioSource != null)
        {
            voiceAudioSource.playOnAwake = false;
            voiceAudioSource.loop = false;
            voiceAudioSource.spatialBlend = 0f;
        }
    }

    private void FindWukongComponents()
    {
        if (wukongObject == null)
        {
            Debug.LogWarning("Map1StoryManager: Chưa gán Wukong Object.");
            return;
        }

        Behaviour[] behaviours = wukongObject.GetComponents<Behaviour>();

        foreach (Behaviour behaviour in behaviours)
        {
            if (behaviour != null && behaviour.GetType().Name == "PlayerController")
            {
                wukongController = behaviour;
                break;
            }
        }

        if (wukongController == null)
        {
            Behaviour[] childBehaviours = wukongObject.GetComponentsInChildren<Behaviour>(true);

            foreach (Behaviour behaviour in childBehaviours)
            {
                if (behaviour != null && behaviour.GetType().Name == "PlayerController")
                {
                    wukongController = behaviour;
                    break;
                }
            }
        }

        if (wukongController == null)
        {
            Debug.LogWarning("Map1StoryManager: Không tìm thấy PlayerController trên Wukong hoặc object con.");
        }

        wukongRigidbody = wukongObject.GetComponent<Rigidbody2D>();

        if (wukongRigidbody == null)
        {
            wukongRigidbody = wukongObject.GetComponentInChildren<Rigidbody2D>(true);
        }

        if (wukongRigidbody == null)
        {
            Debug.LogWarning("Map1StoryManager: Không tìm thấy Rigidbody2D trên Wukong hoặc object con.");
        }

        wukongAnimator = wukongObject.GetComponent<Animator>();

        if (wukongAnimator == null)
        {
            wukongAnimator = wukongObject.GetComponentInChildren<Animator>(true);
        }

        if (wukongAnimator == null)
        {
            Debug.LogWarning("Map1StoryManager: Không tìm thấy Animator trên Wukong hoặc object con.");
        }
    }

    private void AutoFindWukongSkillCooldown()
    {
        if (!autoFindWukongSkillCooldown)
        {
            return;
        }

        if (wukongSkillCooldown != null)
        {
            return;
        }

        if (wukongObject == null)
        {
            Debug.LogWarning("Map1StoryManager: Chưa gán Wukong Object nên không thể tự tìm WukongSkillCooldown.");
            return;
        }

        Behaviour[] behaviours = wukongObject.GetComponentsInChildren<Behaviour>(true);

        foreach (Behaviour behaviour in behaviours)
        {
            if (behaviour == null)
            {
                continue;
            }

            if (behaviour.GetType().Name == "WukongSkillCooldown")
            {
                wukongSkillCooldown = behaviour;
                Debug.Log("Map1StoryManager: Đã tìm thấy WukongSkillCooldown trong Wukong Object.");
                return;
            }
        }

        Debug.LogWarning("Map1StoryManager: Không tìm thấy WukongSkillCooldown trong Wukong Object.");
    }

    private bool IsWukongIdleAndStable()
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
        bool isIdleState = stateInfo.IsName(wukongIdleStateName);

        bool isRigidbodyStable = true;

        if (wukongRigidbody != null)
        {
            isRigidbodyStable =
                Mathf.Abs(wukongRigidbody.linearVelocity.x) < 0.05f &&
                Mathf.Abs(wukongRigidbody.linearVelocity.y) < 0.05f;
        }

        return isIdleState && isRigidbodyStable;
    }

    private IEnumerator WaitWukongIdleBeforeEndTutorialRoutine()
    {
        if (!waitWukongIdleBeforeEndTutorial)
        {
            yield break;
        }

        FindWukongComponents();

        float timer = 0f;

        while (timer < maxWaitForTutorialEndIdleTime)
        {
            if (IsWukongIdleAndStable())
            {
                break;
            }

            timer += Time.deltaTime;
            yield return null;
        }

        if (!IsWukongIdleAndStable())
        {
            ForceWukongIdle();
        }

        if (extraDelayAfterTutorialIdle > 0f)
        {
            yield return new WaitForSeconds(extraDelayAfterTutorialIdle);
        }
    }

    private void ForceWukongIdle()
    {
        if (wukongAnimator == null)
        {
            return;
        }

        if (HasAnimatorParameter(wukongAnimator, "Speed"))
        {
            wukongAnimator.SetFloat("Speed", 0f);
        }

        if (HasAnimatorParameter(wukongAnimator, "VerticalVelocity"))
        {
            wukongAnimator.SetFloat("VerticalVelocity", 0f);
        }

        wukongAnimator.Play(wukongIdleStateName, 0, 0f);
    }

    private bool HasAnimatorParameter(Animator animator, string parameterName)
    {
        if (animator == null)
        {
            return false;
        }

        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.name == parameterName)
            {
                return true;
            }
        }

        return false;
    }

    private void BindUIElements()
    {
        if (dialogueUIDocument == null)
        {
            Debug.LogWarning("Map1StoryManager: Chưa gán Dialogue UIDocument.");
            return;
        }

        VisualElement root = dialogueUIDocument.rootVisualElement;

        if (root == null)
        {
            Debug.LogWarning("Map1StoryManager: UIDocument chưa có rootVisualElement.");
            return;
        }

        dialogueBox = root.Q<VisualElement>(dialogueBoxElementName);
        dialogueText = root.Q<Label>(dialogueTextElementName);

        if (!string.IsNullOrEmpty(dialogueHintElementName))
        {
            dialogueHint = root.Q<Label>(dialogueHintElementName);
        }

        if (dialogueBox == null)
        {
            Debug.LogWarning("Map1StoryManager: Không tìm thấy UI element tên " + dialogueBoxElementName + " trong UXML.");
        }

        if (dialogueText == null)
        {
            Debug.LogWarning("Map1StoryManager: Không tìm thấy UI element tên " + dialogueTextElementName + " trong UXML.");
        }

        if (!string.IsNullOrEmpty(dialogueHintElementName) && dialogueHint == null)
        {
            Debug.LogWarning("Map1StoryManager: Không tìm thấy UI element tên " + dialogueHintElementName + " trong UXML.");
        }
    }

    private void ShowDialogueBox()
    {
        if (dialogueBox != null)
        {
            dialogueBox.style.display = DisplayStyle.Flex;
        }

        if (dialogueHint != null)
        {
            dialogueHint.text = nextHint;
            dialogueHint.style.display = DisplayStyle.Flex;
        }
    }

    private void HideDialogueBox()
    {
        if (dialogueBox != null)
        {
            dialogueBox.style.display = DisplayStyle.None;
        }

        if (dialogueText != null)
        {
            dialogueText.text = "";
        }

        if (dialogueHint != null)
        {
            dialogueHint.style.display = DisplayStyle.None;
        }
    }

    private void HideGlobalHUD()
    {
        if (!hideGlobalHUDDuringIntro)
        {
            return;
        }

        if (globalHUDObject != null)
        {
            globalHUDObject.SetActive(false);
        }
    }

    private void ShowGlobalHUD()
    {
        if (!hideGlobalHUDDuringIntro)
        {
            return;
        }

        if (globalHUDObject != null)
        {
            globalHUDObject.SetActive(true);
        }

        HideGlobalHUDDialogueBox();
    }

    private void HideGlobalHUDDialogueBox()
    {
        if (!hideGlobalDialogueBoxWhenShowHUD)
        {
            return;
        }

        if (globalHUDUIDocument == null && globalHUDObject != null)
        {
            globalHUDUIDocument = globalHUDObject.GetComponent<UIDocument>();
        }

        if (globalHUDUIDocument == null)
        {
            Debug.LogWarning("Map1StoryManager: Chưa gán Global HUD UIDocument nên không thể ẩn dialogue-box.");
            return;
        }

        VisualElement root = globalHUDUIDocument.rootVisualElement;

        if (root == null)
        {
            return;
        }

        VisualElement globalDialogueBox = root.Q<VisualElement>(globalDialogueBoxElementName);

        if (globalDialogueBox != null)
        {
            globalDialogueBox.style.display = DisplayStyle.None;
        }

        Debug.Log("Map1StoryManager: Đã ẩn dialogue-box của GlobalHUD.");
    }

    private void ReleaseStartRightLimitAfterPostTutorialDialogue()
    {
        if (!releaseStartRightLimitAfterPostTutorialDialogue)
        {
            return;
        }

        if (map1CameraLimiter != null)
        {
            map1CameraLimiter.ReleaseStartTemporaryRightLimit();
        }

        if (startTemporaryRightBlockerObject != null)
        {
            startTemporaryRightBlockerObject.SetActive(false);
        }

        Debug.Log("Map1StoryManager: Đã tắt giới hạn phải tạm thời đầu map và mở camera.");
    }

    private void DisableCooldownUntilTutorialEnd()
    {
        if (!disableCooldownUntilTutorialEnd)
        {
            return;
        }

        if (wukongSkillCooldown == null)
        {
            AutoFindWukongSkillCooldown();
        }

        if (wukongSkillCooldown != null)
        {
            wukongSkillCooldown.enabled = true;

            wukongSkillCooldown.SendMessage(
                "SetCooldownEnabled",
                false,
                SendMessageOptions.DontRequireReceiver
            );

            Debug.Log("Map1StoryManager: Đã tắt tạm cơ chế hồi chiêu Wukong trong tutorial.");
        }
        else
        {
            Debug.LogWarning("Map1StoryManager: Không tắt được hồi chiêu vì chưa tìm thấy WukongSkillCooldown.");
        }
    }

    private void EnableCooldownAfterTutorial()
    {
        if (!disableCooldownUntilTutorialEnd)
        {
            return;
        }

        if (wukongSkillCooldown == null)
        {
            AutoFindWukongSkillCooldown();
        }

        if (wukongSkillCooldown != null)
        {
            wukongSkillCooldown.enabled = true;

            wukongSkillCooldown.SendMessage(
                "SetCooldownEnabled",
                true,
                SendMessageOptions.DontRequireReceiver
            );

            wukongSkillCooldown.SendMessage(
                "RefreshHUDReference",
                SendMessageOptions.DontRequireReceiver
            );

            wukongSkillCooldown.SendMessage(
                "UpdateHUD",
                SendMessageOptions.DontRequireReceiver
            );

            Debug.Log("Map1StoryManager: Đã bật lại hồi chiêu và ép cập nhật UI cooldown.");
        }
        else
        {
            Debug.LogWarning("Map1StoryManager: Không bật được hồi chiêu vì chưa tìm thấy WukongSkillCooldown.");
        }
    }

    private void LockPlayerAndParty()
    {
        if (wukongController != null)
        {
            wukongController.enabled = false;
        }

        if (wukongRigidbody != null)
        {
            wukongRigidbody.linearVelocity = Vector2.zero;
            wukongRigidbody.angularVelocity = 0f;

            if (!cachedWukongConstraints)
            {
                originalWukongConstraints = wukongRigidbody.constraints;
                cachedWukongConstraints = true;
            }

            wukongRigidbody.constraints =
                originalWukongConstraints |
                RigidbodyConstraints2D.FreezePositionX |
                RigidbodyConstraints2D.FreezeRotation;
        }

        ForceWukongIdle();
        StopPartyMovement();
    }

    private void UnlockPlayerAndParty()
    {
        if (wukongRigidbody != null && cachedWukongConstraints)
        {
            wukongRigidbody.constraints = originalWukongConstraints;
            wukongRigidbody.linearVelocity = Vector2.zero;
            wukongRigidbody.angularVelocity = 0f;
        }

        if (wukongController != null)
        {
            wukongController.enabled = true;
        }

        RestorePartyMovement();
    }

    private void CachePartyComponents()
    {
        if (partyObjectsToStop == null)
        {
            return;
        }

        cachedPartyMoveScripts = new Behaviour[partyObjectsToStop.Length];
        cachedPartyRigidbodies = new Rigidbody2D[partyObjectsToStop.Length];
        cachedPartyAnimators = new Animator[partyObjectsToStop.Length];

        cachedPartyBodyTypes = new RigidbodyType2D[partyObjectsToStop.Length];
        cachedPartyGravityScales = new float[partyObjectsToStop.Length];
        cachedPartyConstraints = new RigidbodyConstraints2D[partyObjectsToStop.Length];

        for (int i = 0; i < partyObjectsToStop.Length; i++)
        {
            GameObject partyObject = partyObjectsToStop[i];

            if (partyObject == null)
            {
                continue;
            }

            Behaviour[] behaviours = partyObject.GetComponents<Behaviour>();

            foreach (Behaviour behaviour in behaviours)
            {
                if (behaviour != null && behaviour.GetType().Name == "FollowerController")
                {
                    cachedPartyMoveScripts[i] = behaviour;
                    break;
                }
            }

            cachedPartyRigidbodies[i] = partyObject.GetComponent<Rigidbody2D>();
            cachedPartyAnimators[i] = partyObject.GetComponent<Animator>();

            if (cachedPartyRigidbodies[i] != null)
            {
                cachedPartyBodyTypes[i] = cachedPartyRigidbodies[i].bodyType;
                cachedPartyGravityScales[i] = cachedPartyRigidbodies[i].gravityScale;
                cachedPartyConstraints[i] = cachedPartyRigidbodies[i].constraints;
            }
        }
    }

    private void StopPartyMovement()
    {
        if (partyObjectsToStop == null)
        {
            return;
        }

        for (int i = 0; i < partyObjectsToStop.Length; i++)
        {
            if (cachedPartyMoveScripts != null && i < cachedPartyMoveScripts.Length && cachedPartyMoveScripts[i] != null)
            {
                cachedPartyMoveScripts[i].enabled = false;
            }

            if (cachedPartyRigidbodies != null && i < cachedPartyRigidbodies.Length && cachedPartyRigidbodies[i] != null)
            {
                Rigidbody2D rb = cachedPartyRigidbodies[i];

                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;

                if (freezePartyPhysicsDuringIntro)
                {
                    rb.gravityScale = 0f;
                    rb.bodyType = RigidbodyType2D.Kinematic;
                    rb.constraints = RigidbodyConstraints2D.FreezeAll;
                }
            }

            if (cachedPartyAnimators != null && i < cachedPartyAnimators.Length && cachedPartyAnimators[i] != null)
            {
                cachedPartyAnimators[i].SetFloat("Speed", 0f);
            }
        }
    }

    private void RestorePartyMovement()
    {
        if (partyObjectsToStop == null)
        {
            return;
        }

        for (int i = 0; i < partyObjectsToStop.Length; i++)
        {
            if (cachedPartyRigidbodies != null && i < cachedPartyRigidbodies.Length && cachedPartyRigidbodies[i] != null)
            {
                Rigidbody2D rb = cachedPartyRigidbodies[i];

                if (freezePartyPhysicsDuringIntro)
                {
                    rb.bodyType = cachedPartyBodyTypes[i];
                    rb.gravityScale = cachedPartyGravityScales[i];
                    rb.constraints = cachedPartyConstraints[i];
                }

                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
            }

            if (cachedPartyMoveScripts != null && i < cachedPartyMoveScripts.Length && cachedPartyMoveScripts[i] != null)
            {
                cachedPartyMoveScripts[i].enabled = true;
            }

            if (cachedPartyAnimators != null && i < cachedPartyAnimators.Length && cachedPartyAnimators[i] != null)
            {
                cachedPartyAnimators[i].SetFloat("Speed", 0f);
            }
        }
    }
}