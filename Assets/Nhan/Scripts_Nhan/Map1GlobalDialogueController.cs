using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

[Serializable]
public class Map1GlobalDialogueLine
{
    [Tooltip("Ảnh nhân vật đang nói.")]
    public Sprite avatar;

    [Tooltip("Tên nhân vật đang nói.")]
    public string speakerName;

    [TextArea(2, 5)]
    [Tooltip("Nội dung lời thoại.")]
    public string dialogueText;
}

public class Map1GlobalDialogueController : MonoBehaviour
{
    public enum DialogueMode
    {
        MissionSingleLine,
        ConversationNextKey
    }

    [Header("UI Document")]
    [Tooltip("UIDocument của GlobalHUD.")]
    public UIDocument uiDocument;

    [Header("Element Names")]
    [Tooltip("Tên VisualElement cha của toàn bộ box hội thoại. Với UI hiện tại là dialogue-box.")]
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
    [Tooltip("MissionSingleLine = hiện 1 câu nhiệm vụ, không cần bấm E. ConversationNextKey = hội thoại nhiều câu, nhấn E để chuyển.")]
    public DialogueMode dialogueMode = DialogueMode.MissionSingleLine;

    [Header("Input")]
    [Tooltip("Dùng phím E để chuyển câu thoại khi ở ConversationNextKey.")]
    public bool useEKeyToNext = true;

    [Tooltip("Nội dung gợi ý chuyển thoại.")]
    public string nextHint = "E";

    [Tooltip("Hiện hint E khi nói chuyện NPC.")]
    public bool showHintInConversation = true;

    [Header("Dialogue Lines")]
    [Tooltip("Danh sách thoại chỉnh trực tiếp trong Inspector.")]
    public Map1GlobalDialogueLine[] dialogueLines;

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

    private Map1GlobalDialogueLine[] currentLines;
    private int currentIndex;
    private Action onDialogueFinished;

    private void Awake()
    {
        SetupReferences();
        HideDialogue();
    }

    private void OnEnable()
    {
        SetupReferences();
        HideDialogue();
    }

    private void Update()
    {
        if (!isDialoguePlaying)
        {
            return;
        }

        if (dialogueMode != DialogueMode.ConversationNextKey)
        {
            return;
        }

        if (!useEKeyToNext)
        {
            return;
        }

        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            ShowNextLine();
        }
    }

    private void SetupReferences()
    {
        if (uiDocument == null)
        {
            uiDocument = GetComponent<UIDocument>();
        }

        if (uiDocument == null)
        {
            Debug.LogWarning("Map1GlobalDialogueController chưa có UIDocument.");
            return;
        }

        root = uiDocument.rootVisualElement;

        if (root == null)
        {
            Debug.LogWarning("Map1GlobalDialogueController không tìm thấy rootVisualElement.");
            return;
        }

        dialogueBox = root.Q<VisualElement>(dialogueBoxName);
        avatarImage = root.Q<VisualElement>(avatarImageName);
        speakerNameText = root.Q<Label>(speakerNameTextName);
        dialogueText = root.Q<Label>(dialogueTextName);
        nextHintText = root.Q<Label>(nextHintTextName);

        if (dialogueBox == null)
        {
            Debug.LogWarning("Không tìm thấy dialogue box trong UI Document. Kiểm tra Name trong UI Builder: " + dialogueBoxName);
        }

        if (avatarImage == null)
        {
            Debug.LogWarning("Không tìm thấy avatar image trong UI Document. Kiểm tra Name trong UI Builder: " + avatarImageName);
        }

        if (speakerNameText == null)
        {
            Debug.LogWarning("Không tìm thấy speaker name trong UI Document. Kiểm tra Name trong UI Builder: " + speakerNameTextName);
        }

        if (dialogueText == null)
        {
            Debug.LogWarning("Không tìm thấy dialogue text trong UI Document. Kiểm tra Name trong UI Builder: " + dialogueTextName);
        }

        if (nextHintText == null)
        {
            Debug.LogWarning("Không tìm thấy next hint trong UI Document. Kiểm tra Name trong UI Builder: " + nextHintTextName);
        }
    }

    public void PlayDialogue()
    {
        StartDialogue(dialogueLines, null);
    }

    public void StartDialogue(Map1GlobalDialogueLine[] lines, Action onFinished = null)
    {
        SetupReferences();

        if (lines == null || lines.Length == 0)
        {
            Debug.LogWarning("Map1GlobalDialogueController: Chưa có Dialogue Lines.");
            onFinished?.Invoke();
            return;
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
            return;
        }

        isDialoguePlaying = true;
        ShowCurrentLine();
    }

    public void ShowNextLine()
    {
        if (!isDialoguePlaying)
        {
            return;
        }

        currentIndex++;

        if (currentIndex >= currentLines.Length)
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

        Map1GlobalDialogueLine line = currentLines[currentIndex];

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
            Debug.Log("Map1GlobalDialogueController: " + line.speakerName + ": " + line.dialogueText);
        }
    }

    private void FinishDialogue()
    {
        isDialoguePlaying = false;
        HideDialogue();

        Action callback = onDialogueFinished;

        onDialogueFinished = null;
        currentLines = null;
        currentIndex = 0;

        callback?.Invoke();
    }

    public void ShowDialogue()
    {
        SetupReferences();

        if (dialogueBox != null)
        {
            dialogueBox.style.display = DisplayStyle.Flex;
            dialogueBox.style.visibility = Visibility.Visible;
        }
    }

    public void HideDialogue()
    {
        if (dialogueBox != null)
        {
            dialogueBox.style.display = DisplayStyle.None;
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
        }

        HideHint();

        isDialoguePlaying = false;
    }

    private void HideHint()
    {
        if (nextHintText != null)
        {
            nextHintText.style.display = DisplayStyle.None;
        }
    }
}