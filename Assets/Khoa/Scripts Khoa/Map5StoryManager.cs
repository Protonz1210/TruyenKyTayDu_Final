using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class Map5StoryManager : MonoBehaviour
{
    public enum Map5StoryMode
    {
        Ground_5_0,
        Heaven_5_1
    }

    [Header("Map Mode")]
    [Tooltip("Ground_5_0: map dưới mặt đất, không có Phật Tổ, chạy xong thì fade đen, hiện chữ cinematic và chuyển sang MAP 5.1. Heaven_5_1: map thiên đình, có Phật Tổ và BuddhaBowl.")]
    public Map5StoryMode storyMode = Map5StoryMode.Ground_5_0;

    [Header("References")]
    [Tooltip("Kéo object có script Map5DialogueController vào đây.")]
    public Map5DialogueController dialogueController;

    [Tooltip("Kéo object FinalWukongZoneDuelTest vào đây.")]
    public FinalWukongZoneDuelTest duelTest;

    [Tooltip("Kéo object có script Map5SceneFadeController vào đây.")]
    public Map5SceneFadeController sceneFadeController;

    [Tooltip("Chỉ dùng ở Heaven_5_1. Kéo object có script Map5BuddhaInterventionController vào đây.")]
    public Map5BuddhaInterventionController buddhaInterventionController;

    [Header("Location Title UI")]
    [Tooltip("Bật: trước Intro Dialogue sẽ hiện UI địa điểm rồi mới bắt đầu thoại.")]
    public bool showLocationTitleBeforeIntroDialogue = true;

    [Tooltip("UIDocument chứa Box_mask / Box_text của UI địa điểm. Nếu bỏ trống, code sẽ tự tìm UIDocument có Location Box Name.")]
    public UIDocument locationUIDocument;

    [Tooltip("Tên VisualElement cha của bảng địa điểm trong UI Builder. Theo ảnh của bạn là Box_mask.")]
    public string locationBoxName = "Box_mask";

    [Tooltip("Tên Label hiển thị chữ địa điểm. Theo ảnh của bạn là Box_text.")]
    public string locationTextName = "Box_text";

    [TextArea(3, 6)]
    [Tooltip("Text địa điểm cho MAP 5.0 dưới mặt đất.")]
    public string groundLocationTitleText = "HOA\nQUẢ\nSƠN";

    [TextArea(3, 6)]
    [Tooltip("Text địa điểm cho MAP 5.1 thiên đình / trước Phật Tổ.")]
    public string heavenLocationTitleText = "LINH\nSƠN";

    [Tooltip("Thời gian fade in UI địa điểm.")]
    public float locationFadeInTime = 1f;

    [Tooltip("Thời gian giữ UI địa điểm sau khi hiện rõ.")]
    public float locationHoldTime = 2f;

    [Tooltip("Thời gian fade out UI địa điểm.")]
    public float locationFadeOutTime = 1f;

    [Tooltip("Ép ẩn UI địa điểm ngay khi vào scene, chỉ hiện khi chạy Location Title Beat.")]
    public bool hideLocationTitleOnAwake = true;

    [Header("Dialogue Box Pre-Hide")]
    [Tooltip("UIDocument chứa box thoại. Nếu bỏ trống, code sẽ tự tìm theo Dialogue Box Name.")]
    public UIDocument dialogueUIDocument;

    [Tooltip("Tên VisualElement cha của box thoại trong UI Builder. Theo ảnh của bạn là dialogue-box.")]
    public string dialogueBoxName = "dialogue-box";

    [Tooltip("Ép ẩn box thoại trước khi hiện UI địa điểm để tránh vừa vào Map5.1 đã hiện box thoại.")]
    public bool hideDialogueBoxBeforeLocationTitle = true;

    [Header("Dialogue Beats")]
    [Tooltip("Hội thoại mở đầu của map hiện tại.")]
    public Map5DialogueLine[] introDialogueLines;

    [Tooltip("Hội thoại sau khi 2 Wukong đánh xong lần 1.")]
    public Map5DialogueLine[] afterBeat1DialogueLines;

    [Tooltip("Hội thoại sau lần đánh thứ 2 ở MAP 5.1, dùng làm đoạn chuẩn bị Phật Tổ can thiệp.")]
    public Map5DialogueLine[] afterBeat3DialogueLines;

    [Tooltip("Chỉ dùng ở Heaven_5_1, sau khi Phật Tổ dùng bát và FakeWukong Die.")]
    public Map5DialogueLine[] endDialogueLines;

    [Header("Ground 5.0 Settings")]
    [Tooltip("Tên scene thiên đình sẽ chuyển sang sau khi MAP 5.0 kết thúc.")]
    public string nextSceneName = "MAP 5.1";

    [Tooltip("Thời gian chờ sau hội thoại cuối MAP 5.0 rồi mới bắt đầu fade đen.")]
    public float delayBeforeLoadNextScene = 1f;

    [Header("Heaven 5.1 Settings")]
    [Tooltip("Heaven_5_1 có dùng Phật Tổ can thiệp không.")]
    public bool useBuddhaIntervention = true;

    [Tooltip("Bật: ở MAP 5.1 sẽ chờ fade sáng xong rồi mới bắt đầu hội thoại.")]
    public bool waitFadeInBeforeStartStory = true;

    [Tooltip("Thời gian chờ nhẹ trước khi bắt đầu story, tránh việc StoryManager chạy trước FadeController.")]
    public float startStoryDelay = 0.1f;

    [Header("Heaven 5.1 End Scene Load")]
    [Tooltip("Bật: sau khi MAP 5.1 chạy xong End Dialogue sẽ fade đen rồi chuyển sang scene tiếp theo.")]
    public bool loadSceneAfterHeavenFinished = true;

    [Tooltip("Tên scene sẽ chuyển tới sau MAP 5.1. Phải đúng tên trong Build Settings.")]
    public string heavenNextSceneName = "MainMenu";

    [Tooltip("Thời gian chờ sau End Dialogue trước khi bắt đầu fade chuyển scene.")]
    public float delayBeforeHeavenLoadScene = 1f;

    [Tooltip("Bật: trước khi load scene sẽ fade đen map.")]
    public bool fadeOutBeforeHeavenLoadScene = true;

    [Header("Start Settings")]
    [Tooltip("Bật: vào Play là tự chạy flow cinematic.")]
    public bool autoStartOnPlay = true;

    [Tooltip("Bật: bấm phím K để test lại flow từ đầu.")]
    public bool enableTestKey = true;

    [Header("State")]
    [Tooltip("Đang chạy flow cinematic hay không. Khi bật thì không cho kích hoạt lại.")]
    public bool isStoryRunning;

    [Tooltip("Đang ở đoạn đánh nhau hay không.")]
    public bool isDuelBeatRunning;

    [Tooltip("Đang ở đoạn Phật Tổ can thiệp hay không.")]
    public bool isBuddhaInterventionRunning;

    [Tooltip("Đang ở đoạn ending cuối map hay không.")]
    public bool isEndingRunning;

    [Tooltip("Beat hiện tại đang chạy. 0 = dialogue, 1/2 = duel beat, 99 = Buddha intervention, 100 = end dialogue, 999 = ending.")]
    public int currentBeatIndex;

    private Coroutine storyRoutine;

    private VisualElement locationRoot;
    private VisualElement locationBox;
    private Label locationText;

    private VisualElement dialogueRoot;
    private VisualElement dialogueBox;

    private void Awake()
    {
        FindLocationUIElements();
        FindDialogueBoxElement();

        if (hideLocationTitleOnAwake)
        {
            HideLocationTitleImmediate();
        }

        if (hideDialogueBoxBeforeLocationTitle)
        {
            HideDialogueBoxImmediate();
        }
    }

    private void Start()
    {
        if (autoStartOnPlay)
        {
            StartCoroutine(AutoStartRoutine());
        }
    }

    private IEnumerator AutoStartRoutine()
    {
        yield return new WaitForSeconds(startStoryDelay);

        if (storyMode == Map5StoryMode.Heaven_5_1 && waitFadeInBeforeStartStory)
        {
            if (sceneFadeController != null)
            {
                yield return new WaitUntil(() => sceneFadeController.isFading == false);
            }
        }

        if (hideDialogueBoxBeforeLocationTitle)
        {
            HideDialogueBoxImmediate();
        }

        if (hideLocationTitleOnAwake)
        {
            HideLocationTitleImmediate();
        }

        StartStoryFlow();
    }

    private void Update()
    {
        if (!enableTestKey)
        {
            return;
        }

        if (Keyboard.current == null)
        {
            return;
        }

        if (Keyboard.current.kKey.wasPressedThisFrame)
        {
            StartStoryFlow();
        }
    }

    public void StartStoryFlow()
    {
        if (isStoryRunning)
        {
            Debug.LogWarning("[Map5StoryManager] Story đang chạy, không chạy lại.");
            return;
        }

        if (storyMode == Map5StoryMode.Ground_5_0)
        {
            storyRoutine = StartCoroutine(GroundMapFlowRoutine());
        }
        else
        {
            storyRoutine = StartCoroutine(HeavenMapFlowRoutine());
        }
    }

    private IEnumerator GroundMapFlowRoutine()
    {
        isStoryRunning = true;
        isDuelBeatRunning = false;
        isBuddhaInterventionRunning = false;
        isEndingRunning = false;
        currentBeatIndex = 0;

        Debug.Log("[Map5StoryManager] Bắt đầu flow MAP 5.0 dưới mặt đất.");

        yield return StartCoroutine(PlayLocationTitleBeatRoutine());

        yield return StartCoroutine(PlayDialogueBeatRoutine(
            introDialogueLines,
            "Ground Intro Dialogue",
            0
        ));

        yield return StartCoroutine(PlayDuelBeatRoutine(
            1,
            "Ground Duel Beat 1"
        ));

        yield return StartCoroutine(PlayDialogueBeatRoutine(
            afterBeat1DialogueLines,
            "Ground After Beat 1 Dialogue",
            0
        ));

        Debug.Log("[Map5StoryManager] MAP 5.0 kết thúc. Chuẩn bị fade đen, hiện chữ và chuyển sang scene: " + nextSceneName);

        yield return new WaitForSeconds(delayBeforeLoadNextScene);

        if (sceneFadeController != null)
        {
            yield return StartCoroutine(sceneFadeController.FadeOutRoutine());

            SceneManager.LoadScene(nextSceneName);
        }
        else
        {
            Debug.LogWarning("[Map5StoryManager] Chưa gán Scene Fade Controller, sẽ chuyển scene không có fade.");
            SceneManager.LoadScene(nextSceneName);
        }
    }

    private IEnumerator HeavenMapFlowRoutine()
    {
        isStoryRunning = true;
        isDuelBeatRunning = false;
        isBuddhaInterventionRunning = false;
        isEndingRunning = false;
        currentBeatIndex = 0;

        Debug.Log("[Map5StoryManager] Bắt đầu flow MAP 5.1 thiên đình.");

        yield return StartCoroutine(PlayLocationTitleBeatRoutine());

        yield return StartCoroutine(PlayDialogueBeatRoutine(
            introDialogueLines,
            "Heaven Intro Dialogue",
            0
        ));

        yield return StartCoroutine(PlayDuelBeatRoutine(
            1,
            "Heaven Duel Beat 1"
        ));

        yield return StartCoroutine(PlayDialogueBeatRoutine(
            afterBeat1DialogueLines,
            "Heaven After Beat 1 Dialogue",
            0
        ));

        yield return StartCoroutine(PlayDuelBeatRoutine(
            2,
            "Heaven Duel Beat 2"
        ));

        yield return StartCoroutine(PlayDialogueBeatRoutine(
            afterBeat3DialogueLines,
            "Heaven After Beat 3 Dialogue",
            0
        ));

        if (useBuddhaIntervention)
        {
            yield return StartCoroutine(PlayBuddhaInterventionBeatRoutine());
        }

        yield return StartCoroutine(PlayDialogueBeatRoutine(
            endDialogueLines,
            "Heaven End Dialogue",
            100
        ));

        if (loadSceneAfterHeavenFinished)
        {
            yield return StartCoroutine(LoadNextSceneAfterHeavenFinishedRoutine());
            yield break;
        }

        Debug.Log("[Map5StoryManager] Flow MAP 5.1 đã chạy xong.");

        isStoryRunning = false;
        isDuelBeatRunning = false;
        isBuddhaInterventionRunning = false;
        isEndingRunning = false;
        currentBeatIndex = 0;
        storyRoutine = null;
    }

    private IEnumerator PlayLocationTitleBeatRoutine()
    {
        if (!showLocationTitleBeforeIntroDialogue)
        {
            yield break;
        }

        if (hideDialogueBoxBeforeLocationTitle)
        {
            HideDialogueBoxImmediate();
        }

        FindLocationUIElements();

        if (locationBox == null)
        {
            Debug.LogWarning("[Map5StoryManager] Không tìm thấy Location Box. Kiểm tra Location UIDocument và Location Box Name.");
            yield break;
        }

        if (locationText != null)
        {
            locationText.text = GetLocationTitleTextByCurrentMapMode();
        }

        Debug.Log("[Map5StoryManager] Bắt đầu Location Title UI: " + GetLocationTitleTextByCurrentMapMode());

        locationBox.style.display = DisplayStyle.Flex;
        locationBox.style.visibility = Visibility.Visible;
        locationBox.style.opacity = 0f;
        locationBox.pickingMode = PickingMode.Ignore;

        yield return StartCoroutine(FadeLocationElementRoutine(locationBox, 0f, 1f, locationFadeInTime));

        if (locationHoldTime > 0f)
        {
            yield return new WaitForSeconds(locationHoldTime);
        }

        yield return StartCoroutine(FadeLocationElementRoutine(locationBox, 1f, 0f, locationFadeOutTime));

        HideLocationTitleImmediate();

        if (hideDialogueBoxBeforeLocationTitle)
        {
            HideDialogueBoxImmediate();
        }

        Debug.Log("[Map5StoryManager] Kết thúc Location Title UI.");
    }

    private string GetLocationTitleTextByCurrentMapMode()
    {
        if (storyMode == Map5StoryMode.Ground_5_0)
        {
            return groundLocationTitleText;
        }

        return heavenLocationTitleText;
    }

    private void FindLocationUIElements()
    {
        if (locationUIDocument == null)
        {
            locationUIDocument = FindUIDocumentContainingElement(locationBoxName);

            if (locationUIDocument == null)
            {
                locationUIDocument = FindUIDocumentContainingElement("Box_mask");
            }

            if (locationUIDocument == null)
            {
                locationUIDocument = FindUIDocumentContainingElement("box_mask");
            }

            if (locationUIDocument == null)
            {
                locationUIDocument = FindUIDocumentContainingElement("Box-mask");
            }
        }

        if (locationUIDocument == null)
        {
            return;
        }

        locationRoot = locationUIDocument.rootVisualElement;

        if (locationRoot == null)
        {
            return;
        }

        locationBox = FindVisualElementByNames(
            locationRoot,
            locationBoxName,
            "Box_mask",
            "box_mask",
            "Box-mask",
            "box-mask",
            "Box_mask"
        );

        locationText = FindLabelByNames(
            locationRoot,
            locationTextName,
            "Box_text",
            "box_text",
            "Box-text",
            "box-text",
            "Box_text"
        );
    }

    private void FindDialogueBoxElement()
    {
        if (dialogueUIDocument == null)
        {
            dialogueUIDocument = FindUIDocumentContainingElement(dialogueBoxName);

            if (dialogueUIDocument == null)
            {
                dialogueUIDocument = FindUIDocumentContainingElement("dialogue-box");
            }

            if (dialogueUIDocument == null)
            {
                dialogueUIDocument = FindUIDocumentContainingElement("DialogueBox");
            }

            if (dialogueUIDocument == null)
            {
                dialogueUIDocument = FindUIDocumentContainingElement("dialogue_box");
            }
        }

        if (dialogueUIDocument == null)
        {
            return;
        }

        dialogueRoot = dialogueUIDocument.rootVisualElement;

        if (dialogueRoot == null)
        {
            return;
        }

        dialogueBox = FindVisualElementByNames(
            dialogueRoot,
            dialogueBoxName,
            "dialogue-box",
            "DialogueBox",
            "dialogue_box",
            "Dialogue_Box",
            "dialogueBox"
        );
    }

    private UIDocument FindUIDocumentContainingElement(string elementName)
    {
        if (string.IsNullOrEmpty(elementName))
        {
            return null;
        }

#if UNITY_2023_1_OR_NEWER
        UIDocument[] documents = FindObjectsByType<UIDocument>(FindObjectsSortMode.None);
#else
        UIDocument[] documents = FindObjectsOfType<UIDocument>();
#endif

        for (int i = 0; i < documents.Length; i++)
        {
            UIDocument document = documents[i];

            if (document == null || document.rootVisualElement == null)
            {
                continue;
            }

            if (document.rootVisualElement.Q<VisualElement>(elementName) != null)
            {
                return document;
            }
        }

        return null;
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

    private IEnumerator FadeLocationElementRoutine(VisualElement element, float from, float to, float duration)
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

    private void HideLocationTitleImmediate()
    {
        if (locationBox == null)
        {
            FindLocationUIElements();
        }

        if (locationBox == null)
        {
            return;
        }

        locationBox.style.display = DisplayStyle.None;
        locationBox.style.visibility = Visibility.Hidden;
        locationBox.style.opacity = 0f;
        locationBox.pickingMode = PickingMode.Ignore;
    }

    private void HideDialogueBoxImmediate()
    {
        if (dialogueBox == null)
        {
            FindDialogueBoxElement();
        }

        if (dialogueBox == null)
        {
            return;
        }

        // Chỉ dùng display None để không phá ShowDialogue của Map5DialogueController.
        // Khi StartDialogue chạy, controller chỉ cần set display Flex là box thoại hiện lại bình thường.
        dialogueBox.style.display = DisplayStyle.None;
    }

    private IEnumerator PlayDialogueBeatRoutine(Map5DialogueLine[] lines, string beatName, int beatIndex)
    {
        currentBeatIndex = beatIndex;

        Debug.Log("[Map5StoryManager] Bắt đầu " + beatName);

        if (dialogueController == null)
        {
            Debug.LogError("[Map5StoryManager] Chưa gán Dialogue Controller.");
            yield break;
        }

        bool dialogueFinished = false;

        dialogueController.StartDialogue(lines, () =>
        {
            dialogueFinished = true;
        });

        yield return new WaitUntil(() => dialogueFinished);

        Debug.Log("[Map5StoryManager] Kết thúc " + beatName);
    }

    private IEnumerator PlayDuelBeatRoutine(int beatIndex, string beatName)
    {
        Debug.Log("[Map5StoryManager] Bắt đầu " + beatName);

        if (duelTest == null)
        {
            Debug.LogError("[Map5StoryManager] Chưa gán Duel Test.");
            yield break;
        }

        currentBeatIndex = beatIndex;
        isDuelBeatRunning = true;

        bool duelFinished = false;

        duelTest.PlayDuelOnce(() =>
        {
            duelFinished = true;
        });

        yield return new WaitUntil(() => duelFinished);

        isDuelBeatRunning = false;

        Debug.Log("[Map5StoryManager] Kết thúc " + beatName);
    }

    private IEnumerator PlayBuddhaInterventionBeatRoutine()
    {
        Debug.Log("[Map5StoryManager] Bắt đầu Buddha Intervention.");

        if (buddhaInterventionController == null)
        {
            Debug.LogError("[Map5StoryManager] Chưa gán Buddha Intervention Controller.");
            yield break;
        }

        currentBeatIndex = 99;
        isBuddhaInterventionRunning = true;

        bool interventionFinished = false;

        buddhaInterventionController.PlayInterventionOnce(() =>
        {
            interventionFinished = true;
        });

        yield return new WaitUntil(() => interventionFinished);

        isBuddhaInterventionRunning = false;

        Debug.Log("[Map5StoryManager] Kết thúc Buddha Intervention.");
    }

    private IEnumerator LoadNextSceneAfterHeavenFinishedRoutine()
    {
        Debug.Log("[Map5StoryManager] Bắt đầu flow chuyển scene cuối MAP 5.1.");

        currentBeatIndex = 999;
        isEndingRunning = true;

        if (delayBeforeHeavenLoadScene > 0f)
        {
            yield return new WaitForSeconds(delayBeforeHeavenLoadScene);
        }

        if (string.IsNullOrEmpty(heavenNextSceneName))
        {
            Debug.LogWarning("[Map5StoryManager] Chưa nhập Heaven Next Scene Name, không thể chuyển scene cuối MAP 5.1.");

            isStoryRunning = false;
            isDuelBeatRunning = false;
            isBuddhaInterventionRunning = false;
            isEndingRunning = false;
            currentBeatIndex = 0;
            storyRoutine = null;

            yield break;
        }

        if (sceneFadeController != null && fadeOutBeforeHeavenLoadScene)
        {
            yield return StartCoroutine(sceneFadeController.FadeOutRoutine());

        }
        else if (sceneFadeController == null && fadeOutBeforeHeavenLoadScene)
        {
            Debug.LogWarning("[Map5StoryManager] Chưa gán Scene Fade Controller, chuyển scene không có fade.");
        }

        SceneManager.LoadScene(heavenNextSceneName);
    }



}