using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

[Serializable]
public class DialogueLine
{
    [Tooltip("Ảnh nhân vật đang nói.")]
    public Sprite avatar;

    [Tooltip("Tên nhân vật đang nói.")]
    public string speakerName;

    [TextArea(2, 5)]
    [Tooltip("Nội dung lời thoại.")]
    public string dialogueText;
}

public class DialogueController : MonoBehaviour
{
    [Header("UI Document")]
    [Tooltip("UIDocument chứa box hội thoại.")]
    public UIDocument uiDocument;

    [Header("Element Names")]
    [Tooltip("Tên VisualElement cha của box hội thoại trong UI Builder.")]
    public string dialogueBoxName = "DialogueBox";

    [Tooltip("Tên VisualElement hiển thị avatar.")]
    public string avatarImageName = "AvatarImage";

    [Tooltip("Tên Label hiển thị tên nhân vật.")]
    public string speakerNameTextName = "SpeakerNameText";

    [Tooltip("Tên Label hiển thị lời thoại.")]
    public string dialogueTextName = "DialogueText";

    [Tooltip("Tên Label hiển thị gợi ý bấm phím.")]
    public string nextHintTextName = "NextHintText";

    [Header("Input")]
    [Tooltip("Dùng phím E để chuyển câu thoại.")]
    public bool useEKeyToNext = true;

    [Tooltip("Nội dung gợi ý chuyển thoại.")]
    public string nextHint = "Nhấn E để tiếp tục";

    [Header("State")]
    public bool isDialoguePlaying;

    private VisualElement root;
    private VisualElement dialogueBox;
    private VisualElement avatarImage;
    private Label speakerNameText;
    private Label dialogueText;
    private Label nextHintText;

    private DialogueLine[] currentLines;
    private int currentIndex;
    private Action onDialogueFinished;

    void Awake()
    {
        SetupReferences();
        HideDialogue();
    }

    void OnEnable()
    {
        SetupReferences();
        HideDialogue();
    }

    void Update()
    {
        if (!isDialoguePlaying) return;

        if (useEKeyToNext && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            ShowNextLine();
        }
    }

    void SetupReferences()
    {
        if (uiDocument == null)
        {
            uiDocument = GetComponent<UIDocument>();
        }

        if (uiDocument == null)
        {
            Debug.LogWarning("DialogueController chưa có UIDocument.");
            return;
        }

        root = uiDocument.rootVisualElement;

        if (root == null)
        {
            Debug.LogWarning("DialogueController không tìm thấy rootVisualElement.");
            return;
        }

        dialogueBox = root.Q<VisualElement>(dialogueBoxName);
        avatarImage = root.Q<VisualElement>(avatarImageName);
        speakerNameText = root.Q<Label>(speakerNameTextName);
        dialogueText = root.Q<Label>(dialogueTextName);
        nextHintText = root.Q<Label>(nextHintTextName);

        if (dialogueBox == null)
        {
            Debug.LogWarning("Không tìm thấy DialogueBox trong UI Document. Kiểm tra Name trong UI Builder: " + dialogueBoxName);
        }

        if (avatarImage == null)
        {
            Debug.LogWarning("Không tìm thấy AvatarImage trong UI Document. Kiểm tra Name trong UI Builder: " + avatarImageName);
        }

        if (speakerNameText == null)
        {
            Debug.LogWarning("Không tìm thấy SpeakerNameText trong UI Document. Kiểm tra Name trong UI Builder: " + speakerNameTextName);
        }

        if (dialogueText == null)
        {
            Debug.LogWarning("Không tìm thấy DialogueText trong UI Document. Kiểm tra Name trong UI Builder: " + dialogueTextName);
        }

        if (nextHintText == null)
        {
            Debug.LogWarning("Không tìm thấy NextHintText trong UI Document. Kiểm tra Name trong UI Builder: " + nextHintTextName);
        }
    }

    public void StartDialogue(DialogueLine[] lines, Action onFinished = null)
    {
        SetupReferences();

        if (lines == null || lines.Length == 0)
        {
            onFinished?.Invoke();
            return;
        }

        currentLines = lines;
        currentIndex = 0;
        onDialogueFinished = onFinished;
        isDialoguePlaying = true;

        ShowDialogue();
        ShowCurrentLine();
    }

    public void ShowNextLine()
    {
        if (!isDialoguePlaying) return;

        currentIndex++;

        if (currentIndex >= currentLines.Length)
        {
            FinishDialogue();
            return;
        }

        ShowCurrentLine();
    }

    void ShowCurrentLine()
    {
        if (currentLines == null) return;
        if (currentIndex < 0 || currentIndex >= currentLines.Length) return;

        DialogueLine line = currentLines[currentIndex];

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
            nextHintText.text = nextHint;
        }
    }

    void FinishDialogue()
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
        if (dialogueBox != null)
        {
            dialogueBox.style.display = DisplayStyle.Flex;
        }
    }

    public void HideDialogue()
    {
        if (dialogueBox != null)
        {
            dialogueBox.style.display = DisplayStyle.None;
        }
    }
}