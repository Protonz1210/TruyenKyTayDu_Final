using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

/// <summary>
/// Quản lý cốt truyện Map1.
/// Cơ chế hiện tại:
/// - Vừa vào map sẽ chờ Wukong spawn xong và về Idle.
/// - Sau đó tắt UI tổng.
/// - Khóa điều khiển Wukong.
/// - Khóa đoàn thỉnh kinh đứng yên.
/// - Hiện một UI Document riêng chỉ có box chữ.
/// - Mỗi câu thoại / câu thơ có thể gán audio riêng.
/// - Chữ hiện dần theo thời lượng audio.
/// - Audio đọc xong câu hiện tại thì tự chuyển sang câu tiếp theo.
/// - Hết toàn bộ lời thoại thì ẩn box, bật lại UI tổng và mở lại điều khiển Wukong.
/// 
/// UI Document riêng chỉ cần có:
/// - VisualElement tên: DialogueBox
/// - Label tên: DialogueText
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

        [Tooltip(" AnyKey = bấm 1 trong các phím. AllKeys = phải bấm đủ tất cả phím.")]
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
    [Tooltip("Bật lên để cho phép nhấn phím bỏ qua intro.")]
    public bool useSkipKeyToNext = true;
    [Tooltip("Phím dùng để bỏ qua intro. Mặc định là E.")]
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
    private void SetPhase(Map1Phase newPhase)
    {
        currentPhase = newPhase;
        Debug.Log("Map1StoryManager: Chuyển phase sang " + currentPhase);
    }
    private void Update()
    {
        // Skip intro bằng phím đã chọn trong Inspector.
        if (introRunning && useSkipKeyToNext)
        {
            if (WasKeyPressed(skipKey))
            {
                SkipIntro();
            }
        }
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
    /// <summary>
    /// Chờ Wukong spawn ổn định và về Idle rồi mới chạy intro.
    /// </summary>
    private IEnumerator StartIntroAfterWukongReadyRoutine()
    {
        // Chờ 1 frame để Wukong, Animator, Rigidbody2D spawn ổn định trước.
        yield return null;

        // Tìm lại component nếu lúc Awake chưa kịp tìm đủ.
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

            // Nếu chờ quá lâu mà Wukong vẫn chưa Idle thì ép về Idle để tránh kẹt.
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

    /// <summary>
    /// Gọi hàm này nếu muốn bắt đầu intro thủ công từ script khác.
    /// </summary>
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

        // Sau intro thì chạy tutorial.
        // GlobalHUD chưa bật ở đây. Chỉ bật sau khi tutorial kết thúc.
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

        // Dừng âm thanh đang đọc.
        if (voiceAudioSource != null && voiceAudioSource.isPlaying)
        {
            voiceAudioSource.Stop();
        }

        // Nếu đang chạy coroutine intro thì dừng luôn.
        if (introCoroutine != null)
        {
            StopCoroutine(introCoroutine);
            introCoroutine = null;
        }

        HideDialogueBox();
        UnlockPlayerAndParty();

        introRunning = false;

        Debug.Log("Map1StoryManager: Người chơi đã skip intro Map1.");

        // Skip intro xong vẫn vào tutorial.
        // GlobalHUD chưa bật ở đây. Chỉ bật sau khi tutorial kết thúc.
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

        // 1. Chờ tối thiểu một chút để Animator / Rigidbody kịp nhận hành động.
        if (line.minWaitBeforeIdleCheck > 0f)
        {
            yield return new WaitForSeconds(line.minWaitBeforeIdleCheck);
        }

        // 2. Nếu Wukong vẫn đang Idle, chờ một khoảng ngắn xem có rời Idle không.
        // Mục đích: tránh vừa bấm phím là code thấy Idle rồi chuyển box ngay.
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

        // 3. Nếu yêu cầu nhả phím, bắt buộc người chơi nhả hết các phím của box hiện tại.
        if (line.requireKeyReleaseBeforeNextBox)
        {
            while (IsAnyTutorialKeyPressed(line))
            {
                yield return null;
            }
        }

        // 4. Chờ Wukong thật sự về Idle ổn định.
        while (!IsWukongIdleAndStable())
        {
            yield return null;
        }

        // 5. Chờ thêm delay riêng của box sau khi đã Idle.
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

            // Sau khi bấm đúng phím, chờ hành động của Wukong kết thúc thật,
            // nhả phím xong và về Idle rồi mới chuyển box.
            yield return StartCoroutine(WaitTutorialActionCompleteRoutine(line));

        }

        HideDialogueBox();

        tutorialRunning = false;
        tutorialCoroutine = null;

        Debug.Log("Map1StoryManager: Đã hoàn thành tutorial Map1.");

        // Sau tutorial, nếu có hội thoại sau tutorial thì chạy trước.
        // GlobalHUD và cooldown vẫn chưa bật.
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

            // Chờ người chơi nhấn phím Next để qua box tiếp theo.
            yield return StartCoroutine(WaitForPostTutorialNextKey());

            if (postTutorialDelayBetweenLines > 0f)
            {
                yield return new WaitForSeconds(postTutorialDelayBetweenLines);
            }
        }

        HideDialogueBox();

        // Hết Post Tutorial Dialogue thì mở chặn phải đầu map.
        ReleaseStartRightLimitAfterPostTutorialDialogue();

        // Từ đây người chơi được đi tiếp đến trigger Enemy Wave.
        SetPhase(Map1Phase.FreeMoveBeforeEnemyWave);

        // Hết hội thoại sau tutorial mới bật UI và hồi chiêu.
        ShowGlobalHUD();
        EnableCooldownAfterTutorial();

        postTutorialDialogueRunning = false;
        postTutorialDialogueCoroutine = null;

        Debug.Log("Map1StoryManager: Đã hoàn thành post tutorial dialogue. Đã mở đường sang phase Enemy Wave.");
    }

    private IEnumerator WaitForPostTutorialNextKey()
    {
        // Chờ nhả phím trước để tránh ăn phím từ bước tutorial trước đó.
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

    private void ShowTutorialBox()
    {
        if (dialogueBox != null)
        {
            dialogueBox.style.display = DisplayStyle.Flex;
        }

        // Tutorial bắt người chơi thao tác nên không hiện nút skip.
        if (dialogueHint != null)
        {
            dialogueHint.style.display = DisplayStyle.None;
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
            // Chia thời lượng audio cho số ký tự để chữ hiện khớp tương đối với giọng đọc.
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

        // Đảm bảo hiện đủ câu sau khi chạy hiệu ứng chữ.
        dialogueText.text = fullText;

        // Nếu audio còn đang đọc thì chờ đọc xong mới qua câu sau.
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
            // Chỉ là dự phòng.
            // Tốt nhất vẫn nên kéo tay UIDocument riêng của Map1PoemDialogueUI vào Inspector.
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

        // Tìm PlayerController trên chính object Wukong.
        Behaviour[] behaviours = wukongObject.GetComponents<Behaviour>();

        foreach (Behaviour behaviour in behaviours)
        {
            if (behaviour != null && behaviour.GetType().Name == "PlayerController")
            {
                wukongController = behaviour;
                break;
            }
        }

        // Nếu root không có PlayerController thì tìm trong object con.
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
            Debug.LogWarning(
                "Map1StoryManager: Không tìm thấy PlayerController trên Wukong hoặc object con. " +
                "Intro vẫn chạy, nhưng có thể không khóa được điều khiển Wukong."
            );
        }

        // Tìm Rigidbody2D trên root Wukong.
        wukongRigidbody = wukongObject.GetComponent<Rigidbody2D>();

        // Nếu root không có Rigidbody2D thì tìm trong object con.
        if (wukongRigidbody == null)
        {
            wukongRigidbody = wukongObject.GetComponentInChildren<Rigidbody2D>(true);
        }

        if (wukongRigidbody == null)
        {
            Debug.LogWarning(
                "Map1StoryManager: Không tìm thấy Rigidbody2D trên Wukong hoặc object con. " +
                "Intro vẫn chạy, nhưng không thể dừng vận tốc Wukong."
            );
        }

        // Tìm Animator trên root Wukong.
        wukongAnimator = wukongObject.GetComponent<Animator>();

        // Nếu root không có Animator thì tìm trong object con.
        if (wukongAnimator == null)
        {
            wukongAnimator = wukongObject.GetComponentInChildren<Animator>(true);
        }

        if (wukongAnimator == null)
        {
            Debug.LogWarning(
                "Map1StoryManager: Không tìm thấy Animator trên Wukong hoặc object con. " +
                "Intro vẫn chạy, nhưng không thể ép Wukong về Idle."
            );
        }
    }
    private void AutoFindWukongSkillCooldown()
    {
        if (!autoFindWukongSkillCooldown)
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
                Debug.Log("Map1StoryManager: Đã tự tìm thấy WukongSkillCooldown trong Wukong Object.");
                return;
            }
        }

        Debug.LogWarning("Map1StoryManager: Không tìm thấy WukongSkillCooldown trong Wukong Object.");
    }
    private bool IsWukongIdleAndStable()
    {
        if (wukongAnimator == null)
        {
            // Không có Animator thì coi như ổn để tránh kẹt intro.
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

        // Nếu chờ quá lâu mà Wukong vẫn chưa Idle thì ép về Idle để không kẹt tutorial.
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
            Debug.LogWarning("Map1StoryManager: Chưa gán Dialogue UIDocument. Hãy kéo UIDocument của Map1PoemDialogueUI vào Inspector.");
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
            Debug.LogWarning("Map1StoryManager: Không tìm thấy UI element tên " + dialogueHintElementName + " trong UXML. Nếu không dùng hint thì có thể để trống field này.");
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
            // Chữ hint có thể đổi trong Inspector.
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
    }
    private void ReleaseStartRightLimitAfterPostTutorialDialogue()
    {
        if (!releaseStartRightLimitAfterPostTutorialDialogue)
        {
            return;
        }

        if (startTemporaryRightBlockerObject != null)
        {
            startTemporaryRightBlockerObject.SetActive(false);
        }

        Debug.Log("Map1StoryManager: Đã tắt giới hạn phải tạm thời đầu map.");
    }
    private void DisableCooldownUntilTutorialEnd()
    {
        if (!disableCooldownUntilTutorialEnd)
        {
            return;
        }

        if (wukongSkillCooldown != null)
        {
            wukongSkillCooldown.SendMessage("SetCooldownEnabled", false, SendMessageOptions.DontRequireReceiver);
        }
    }

    private void EnableCooldownAfterTutorial()
    {
        if (!disableCooldownUntilTutorialEnd)
        {
            return;
        }

        if (wukongSkillCooldown != null)
        {
            wukongSkillCooldown.SendMessage("SetCooldownEnabled", true, SendMessageOptions.DontRequireReceiver);
        }
    }
    private void LockPlayerAndParty()
    {
        // Tắt điều khiển Wukong.
        if (wukongController != null)
        {
            wukongController.enabled = false;
        }

        // Dừng vật lý Wukong để không bị trôi lúc intro.
        if (wukongRigidbody != null)
        {
            wukongRigidbody.linearVelocity = Vector2.zero;
            wukongRigidbody.angularVelocity = 0f;

            if (!cachedWukongConstraints)
            {
                originalWukongConstraints = wukongRigidbody.constraints;
                cachedWukongConstraints = true;
            }

            // Khóa X và Rotation để Wukong không trôi ngang.
            // Không khóa Y để tránh kẹt nếu nhân vật vừa spawn hơi lệch mặt đất.
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
        // Trả lại constraint gốc cho Wukong.
        if (wukongRigidbody != null && cachedWukongConstraints)
        {
            wukongRigidbody.constraints = originalWukongConstraints;
            wukongRigidbody.linearVelocity = Vector2.zero;
            wukongRigidbody.angularVelocity = 0f;
        }

        // Mở lại điều khiển Wukong.
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