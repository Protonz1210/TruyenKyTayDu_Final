using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.Serialization;
using UnityEngine.UIElements;

[Serializable]
public class Map2GlobalDialogueLine
{
    [Tooltip("Ảnh nhân vật đang nói.")]
    public Sprite avatar;

    [Tooltip("Tên nhân vật đang nói.")]
    public string speakerName;

    [TextArea(2, 5)]
    [Tooltip("Nội dung lời thoại.")]
    public string dialogueText;
}

public class Map2GlobalDialogueController : MonoBehaviour
{
    public enum DialogueMode
    {
        MissionSingleLine,
        ConversationNextKey
    }

    [Header("UI Document")]
    [Tooltip("UIDocument của GlobalHUD hoặc Map2HUD đang chứa box thoại.")]
    public UIDocument uiDocument;

    [Header("Element Names")]
    [Tooltip("Tên VisualElement cha của toàn bộ box hội thoại.")]
    public string dialogueBoxName = "dialogue-box";

    [Tooltip("Tên VisualElement hiển thị avatar.")]
    public string avatarImageName = "dialogue-avatar";

    [Tooltip("Tên Label hiển thị tên nhân vật.")]
    public string speakerNameTextName = "dialogue-name";

    [Tooltip("Tên Label hiển thị lời thoại.")]
    public string dialogueTextName = "dialogue-text";

    [Tooltip("Tên Label hiển thị gợi ý bấm phím.")]
    public string nextHintTextName = "dialogue-hint";

    [Header("Mode")]
    [Tooltip("MissionSingleLine = hiện 1 câu nhiệm vụ. ConversationNextKey = hội thoại nhiều câu, nhấn phím để chuyển.")]
    public DialogueMode dialogueMode = DialogueMode.ConversationNextKey;

    [Header("Input")]
    [Tooltip("Phím chuyển câu khi dùng ConversationNextKey.")]
    public Key nextKey = Key.E;

    [Tooltip("Dùng phím tương tác để chuyển câu thoại.")]
    public bool useKeyToNext = true;

    [Tooltip("Text gợi ý chuyển thoại.")]
    public string nextHint = "E";

    [Tooltip("Hiện hint khi đang ở chế độ ConversationNextKey.")]
    public bool showHintInConversation = true;

    [Tooltip("Chờ người chơi nhả phím trước rồi mới cho nhận phím tiếp, tránh vừa mở thoại đã skip luôn.")]
    public bool waitKeyReleaseBeforeConversationInput = true;

    [Header("Pause Control")]
    [Tooltip("Bật lên để khi Pause Game thì không cho bấm phím chuyển thoại.")]
    public bool blockInputWhenGamePaused = true;

    [Header("Dialogue Lines")]
    [Tooltip("Danh sách thoại test chỉnh trực tiếp trong Inspector.")]
    public Map2GlobalDialogueLine[] dialogueLines;

    [Header("UI Ready Wait")]
    [Tooltip("Thời gian tối đa chờ UIDocument sẵn sàng khi StartDialogue được gọi.")]
    public float maxWaitForUIDocumentReady = 2f;

    [Header("State")]
    public bool isDialoguePlaying;

    [Header("Debug")]
    public bool enableDebugLog = false;

    private VisualElement root;
    private VisualElement dialogueBox;
    private VisualElement avatarImage;
    private Label speakerNameText;
    private Label dialogueText;
    private Label nextHintText;

    private Map2GlobalDialogueLine[] currentLines;
    private int currentIndex;
    private Action onDialogueFinished;

    private Coroutine startDialogueCoroutine;
    private bool waitingKeyRelease;

    private void Awake()
    {
        TrySetupReferences(false);
        HideDialogueIfReady();
    }

    private void OnEnable()
    {
        TrySetupReferences(false);
    }

    private void Start()
    {
        TrySetupReferences(false);
        HideDialogueIfReady();
    }

    private void Update()
    {
        // Khi đang Pause, không cho dialogue nhận input E.
        if (IsGamePausedAndBlocked())
        {
            return;
        }

        if (!isDialoguePlaying)
        {
            return;
        }

        if (dialogueMode != DialogueMode.ConversationNextKey)
        {
            return;
        }

        if (!useKeyToNext)
        {
            return;
        }

        if (waitingKeyRelease)
        {
            if (!IsKeyPressed(nextKey))
            {
                waitingKeyRelease = false;
            }

            return;
        }

        if (WasKeyPressed(nextKey))
        {
            ShowNextLine();
        }
    }

    public void PlayDialogue()
    {
        StartDialogue(dialogueLines, null);
    }

    public void StartDialogue(Map2GlobalDialogueLine[] lines, Action onFinished = null)
    {
        if (startDialogueCoroutine != null)
        {
            StopCoroutine(startDialogueCoroutine);
            startDialogueCoroutine = null;
        }

        startDialogueCoroutine = StartCoroutine(StartDialogueWhenUIReadyRoutine(lines, onFinished));
    }

    private IEnumerator StartDialogueWhenUIReadyRoutine(Map2GlobalDialogueLine[] lines, Action onFinished)
    {
        if (lines == null || lines.Length == 0)
        {
            Debug.LogWarning("Map2GlobalDialogueController: Chưa có Dialogue Lines.");
            onFinished?.Invoke();
            yield break;
        }

        float timer = 0f;

        while (!TrySetupReferences(false))
        {
            timer += Time.unscaledDeltaTime;

            if (timer >= maxWaitForUIDocumentReady)
            {
                Debug.LogWarning("Map2GlobalDialogueController: UIDocument chưa sẵn sàng. Kiểm tra UI Document có active và tên element có đúng không.");
                onFinished?.Invoke();
                yield break;
            }

            yield return null;
        }

        currentLines = lines;
        currentIndex = 0;
        onDialogueFinished = onFinished;

        ShowDialogue();

        if (dialogueMode == DialogueMode.MissionSingleLine)
        {
            isDialoguePlaying = false;
            ShowCurrentLine();
            HideHint();

            startDialogueCoroutine = null;
            yield break;
        }

        isDialoguePlaying = true;

        if (waitKeyReleaseBeforeConversationInput)
        {
            waitingKeyRelease = IsKeyPressed(nextKey);
        }
        else
        {
            waitingKeyRelease = false;
        }

        ShowCurrentLine();

        startDialogueCoroutine = null;
    }

    public void ShowNextLine()
    {
        // Chặn cả trường hợp hàm ShowNextLine bị gọi từ nơi khác đúng lúc Pause.
        if (IsGamePausedAndBlocked())
        {
            return;
        }

        if (!isDialoguePlaying)
        {
            return;
        }

        currentIndex++;

        if (currentLines == null || currentIndex >= currentLines.Length)
        {
            FinishDialogue();
            return;
        }

        ShowCurrentLine();
    }

    private void ShowCurrentLine()
    {
        if (currentLines == null)
        {
            return;
        }

        if (currentIndex < 0 || currentIndex >= currentLines.Length)
        {
            return;
        }

        Map2GlobalDialogueLine line = currentLines[currentIndex];

        if (avatarImage != null)
        {
            if (line.avatar != null)
            {
                avatarImage.style.display = DisplayStyle.Flex;
                avatarImage.style.backgroundImage = new StyleBackground(line.avatar);
            }
            else
            {
                avatarImage.style.backgroundImage = null;
                avatarImage.style.display = DisplayStyle.None;
            }
        }

        if (speakerNameText != null)
        {
            speakerNameText.text = line.speakerName;
        }

        if (dialogueText != null)
        {
            dialogueText.text = line.dialogueText;
        }

        if (nextHintText != null)
        {
            if (dialogueMode == DialogueMode.ConversationNextKey && showHintInConversation)
            {
                nextHintText.text = nextHint;
                nextHintText.style.display = DisplayStyle.Flex;
            }
            else
            {
                nextHintText.style.display = DisplayStyle.None;
            }
        }

        if (enableDebugLog)
        {
            Debug.Log("Map2GlobalDialogueController: " + line.speakerName + ": " + line.dialogueText);
        }
    }

    private void FinishDialogue()
    {
        isDialoguePlaying = false;
        waitingKeyRelease = false;

        Action callback = onDialogueFinished;

        onDialogueFinished = null;
        currentLines = null;
        currentIndex = 0;

        HideDialogue();

        callback?.Invoke();
    }

    public void ShowDialogue()
    {
        if (!TrySetupReferences(true))
        {
            return;
        }

        if (dialogueBox != null)
        {
            dialogueBox.style.display = DisplayStyle.Flex;
            dialogueBox.style.visibility = Visibility.Visible;
            dialogueBox.style.opacity = 1f;
        }
    }

    public void HideDialogue()
    {
        if (startDialogueCoroutine != null)
        {
            StopCoroutine(startDialogueCoroutine);
            startDialogueCoroutine = null;
        }

        isDialoguePlaying = false;
        waitingKeyRelease = false;

        if (!TrySetupReferences(false))
        {
            return;
        }

        if (dialogueBox != null)
        {
            dialogueBox.style.display = DisplayStyle.None;
            dialogueBox.style.opacity = 0f;
        }

        if (dialogueText != null)
        {
            dialogueText.text = "";
        }

        if (speakerNameText != null)
        {
            speakerNameText.text = "";
        }

        if (avatarImage != null)
        {
            avatarImage.style.backgroundImage = null;
            avatarImage.style.display = DisplayStyle.None;
        }

        HideHint();
    }

    public void ForceHideDialogueBox()
    {
        HideDialogue();
    }

    private void HideDialogueIfReady()
    {
        if (!TrySetupReferences(false))
        {
            return;
        }

        if (dialogueBox != null)
        {
            dialogueBox.style.display = DisplayStyle.None;
            dialogueBox.style.opacity = 0f;
        }

        HideHint();
    }

    private void HideHint()
    {
        if (nextHintText != null)
        {
            nextHintText.style.display = DisplayStyle.None;
        }
    }

    private bool TrySetupReferences(bool logWarning)
    {
        if (uiDocument == null)
        {
            uiDocument = GetComponent<UIDocument>();
        }

        if (uiDocument == null)
        {
            uiDocument = GetComponentInParent<UIDocument>();
        }

#if UNITY_2023_1_OR_NEWER
        if (uiDocument == null)
        {
            UIDocument[] documents = FindObjectsByType<UIDocument>(FindObjectsSortMode.None);

            for (int i = 0; i < documents.Length; i++)
            {
                if (documents[i] == null)
                {
                    continue;
                }

                if (!documents[i].gameObject.activeInHierarchy)
                {
                    continue;
                }

                VisualElement documentRoot = documents[i].rootVisualElement;

                if (documentRoot == null)
                {
                    continue;
                }

                if (documentRoot.Q<VisualElement>(dialogueBoxName) != null)
                {
                    uiDocument = documents[i];
                    break;
                }
            }
        }
#else
        if (uiDocument == null)
        {
            UIDocument[] documents = FindObjectsOfType<UIDocument>();

            for (int i = 0; i < documents.Length; i++)
            {
                if (documents[i] == null)
                {
                    continue;
                }

                if (!documents[i].gameObject.activeInHierarchy)
                {
                    continue;
                }

                VisualElement documentRoot = documents[i].rootVisualElement;

                if (documentRoot == null)
                {
                    continue;
                }

                if (documentRoot.Q<VisualElement>(dialogueBoxName) != null)
                {
                    uiDocument = documents[i];
                    break;
                }
            }
        }
#endif

        if (uiDocument == null)
        {
            if (logWarning)
            {
                Debug.LogWarning("Map2GlobalDialogueController: Chưa gán UIDocument.");
            }

            return false;
        }

        if (!uiDocument.gameObject.activeInHierarchy)
        {
            return false;
        }

        root = uiDocument.rootVisualElement;

        if (root == null)
        {
            if (logWarning)
            {
                Debug.LogWarning("Map2GlobalDialogueController: UIDocument chưa có rootVisualElement.");
            }

            return false;
        }

        dialogueBox = root.Q<VisualElement>(dialogueBoxName);
        avatarImage = root.Q<VisualElement>(avatarImageName);
        speakerNameText = root.Q<Label>(speakerNameTextName);
        dialogueText = root.Q<Label>(dialogueTextName);
        nextHintText = root.Q<Label>(nextHintTextName);

        bool hasRequiredUI = true;

        if (dialogueBox == null)
        {
            hasRequiredUI = false;

            if (logWarning)
            {
                Debug.LogWarning("Map2GlobalDialogueController: Không tìm thấy dialogue box: " + dialogueBoxName);
            }
        }

        if (dialogueText == null)
        {
            hasRequiredUI = false;

            if (logWarning)
            {
                Debug.LogWarning("Map2GlobalDialogueController: Không tìm thấy dialogue text: " + dialogueTextName);
            }
        }

        if (speakerNameText == null && logWarning)
        {
            Debug.LogWarning("Map2GlobalDialogueController: Không tìm thấy speaker name: " + speakerNameTextName);
        }

        if (avatarImage == null && logWarning)
        {
            Debug.LogWarning("Map2GlobalDialogueController: Không tìm thấy avatar image: " + avatarImageName);
        }

        if (nextHintText == null && logWarning)
        {
            Debug.LogWarning("Map2GlobalDialogueController: Không tìm thấy next hint: " + nextHintTextName);
        }

        return hasRequiredUI;
    }

    private bool WasKeyPressed(Key key)
    {
        if (IsGamePausedAndBlocked())
        {
            return false;
        }

        if (Keyboard.current == null)
        {
            return false;
        }

        KeyControl keyControl = Keyboard.current[key];

        return keyControl != null && keyControl.wasPressedThisFrame;
    }

    private bool IsKeyPressed(Key key)
    {
        if (IsGamePausedAndBlocked())
        {
            return false;
        }

        if (Keyboard.current == null)
        {
            return false;
        }

        KeyControl keyControl = Keyboard.current[key];

        return keyControl != null && keyControl.isPressed;
    }

    private bool IsGamePausedAndBlocked()
    {
        if (!blockInputWhenGamePaused)
        {
            return false;
        }

        return PauseMenuController.IsPausedGlobal;
    }
}