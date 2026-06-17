using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

[Serializable]
public class Map3DialogueLine
{
    public Sprite avatar;
    public string speakerName;

    [TextArea(2, 5)]
    public string dialogueText;
}

public class Map3DialogueController : MonoBehaviour
{
    [Header("UI Document - GlobalHUD")]
    public UIDocument uiDocument;

    [Header("Element Names")]
    public string dialogueBoxName = "dialogue-box";
    public string dialogueFrameName = "dialogue-frame";
    public string avatarImageName = "dialogue-avatar";
    public string speakerNameTextName = "dialogue-name";
    public string dialogueTextName = "dialogue-text";
    public string nextHintTextName = "dialogue-hint";

    [Header("Input")]
    public bool useEKeyToNext = true;
    public string nextHint = "E";

    [Header("Fade")]
    public bool useFade = true;
    public float fadeInTime = 0.25f;
    public float fadeOutTime = 0.2f;

    [Header("State")]
    public bool isDialoguePlaying;

    private VisualElement root;
    private VisualElement dialogueBox;
    private VisualElement dialogueFrame;
    private VisualElement avatarImage;
    private Label speakerNameText;
    private Label dialogueText;
    private Label nextHintText;

    private Map3DialogueLine[] currentLines;
    private int currentIndex;
    private Action onDialogueFinished;
    private bool isFinishing;

    private void Awake()
    {
        SetupReferences();
        HideDialogue();
    }

    private void OnEnable()
    {
        SetupReferences();
    }

    private void Update()
    {
        if (!isDialoguePlaying || isFinishing)
            return;

        if (useEKeyToNext && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            ShowNextLine();
    }

    private void SetupReferences()
    {
        if (uiDocument == null)
            uiDocument = GetComponent<UIDocument>();

        if (uiDocument == null)
        {
            Debug.LogWarning("Map3DialogueController: Chưa gán UIDocument GlobalHUD.");
            return;
        }

        root = uiDocument.rootVisualElement;

        if (root == null)
            return;

        dialogueBox = root.Q<VisualElement>(dialogueBoxName);
        dialogueFrame = root.Q<VisualElement>(dialogueFrameName);
        avatarImage = root.Q<VisualElement>(avatarImageName);
        speakerNameText = root.Q<Label>(speakerNameTextName);
        dialogueText = root.Q<Label>(dialogueTextName);
        nextHintText = root.Q<Label>(nextHintTextName);
    }

    public void StartDialogue(Map3DialogueLine[] lines, Action onFinished = null)
    {
        SetupReferences();

        if (lines == null || lines.Length == 0)
        {
            Debug.LogWarning("Map3DialogueController: Không có câu thoại.");
            onFinished?.Invoke();
            return;
        }

        currentLines = lines;
        currentIndex = 0;
        onDialogueFinished = onFinished;
        isDialoguePlaying = true;
        isFinishing = false;

        ShowCurrentLine();
        StartCoroutine(StartDialogueRoutine());
    }

    private IEnumerator StartDialogueRoutine()
    {
        if (useFade)
            yield return StartCoroutine(FadeInDialogue());
        else
            ShowDialogueInstant();

        ShowCurrentLine();
    }

    public void ShowNextLine()
    {
        if (!isDialoguePlaying)
            return;

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
            return;

        if (currentIndex < 0 || currentIndex >= currentLines.Length)
            return;

        SetupReferences();

        Map3DialogueLine line = currentLines[currentIndex];

        if (avatarImage != null)
        {
            avatarImage.style.display = DisplayStyle.Flex;
            avatarImage.style.opacity = 1f;

            if (line.avatar != null)
                avatarImage.style.backgroundImage = new StyleBackground(line.avatar);
            else
                avatarImage.style.backgroundImage = null;
        }

        if (speakerNameText != null)
        {
            speakerNameText.style.display = DisplayStyle.Flex;
            speakerNameText.style.opacity = 1f;
            speakerNameText.text = line.speakerName;
        }

        if (dialogueText != null)
        {
            dialogueText.style.display = DisplayStyle.Flex;
            dialogueText.style.opacity = 1f;
            dialogueText.text = line.dialogueText;
        }

        if (nextHintText != null)
        {
            nextHintText.style.display = DisplayStyle.Flex;
            nextHintText.style.opacity = 1f;
            nextHintText.text = nextHint;
        }
    }

    private void FinishDialogue()
    {
        if (isFinishing)
            return;

        isFinishing = true;
        StartCoroutine(FinishDialogueRoutine());
    }

    private IEnumerator FinishDialogueRoutine()
    {
        isDialoguePlaying = false;

        if (useFade)
            yield return StartCoroutine(FadeOutDialogue());
        else
            HideDialogue();

        Action callback = onDialogueFinished;

        onDialogueFinished = null;
        currentLines = null;
        currentIndex = 0;
        isFinishing = false;

        callback?.Invoke();
    }

    public void ShowDialogueInstant()
    {
        SetupReferences();

        if (dialogueBox != null)
        {
            dialogueBox.style.display = DisplayStyle.Flex;
            dialogueBox.style.opacity = 1f;
        }
    }

    public void HideDialogue()
    {
        SetupReferences();

        if (dialogueBox != null)
        {
            dialogueBox.style.display = DisplayStyle.None;
            dialogueBox.style.opacity = 0f;
        }
    }

    private IEnumerator FadeInDialogue()
    {
        SetupReferences();

        if (dialogueBox == null)
            yield break;

        dialogueBox.style.display = DisplayStyle.Flex;
        dialogueBox.style.opacity = 0f;

        float timer = 0f;

        while (timer < fadeInTime)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / fadeInTime);
            dialogueBox.style.opacity = Mathf.Lerp(0f, 1f, t);
            yield return null;
        }

        dialogueBox.style.opacity = 1f;
    }

    private IEnumerator FadeOutDialogue()
    {
        SetupReferences();

        if (dialogueBox == null)
            yield break;

        dialogueBox.style.display = DisplayStyle.Flex;
        dialogueBox.style.opacity = 1f;

        float timer = 0f;

        while (timer < fadeOutTime)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / fadeOutTime);
            dialogueBox.style.opacity = Mathf.Lerp(1f, 0f, t);
            yield return null;
        }

        dialogueBox.style.opacity = 0f;
        dialogueBox.style.display = DisplayStyle.None;
    }
}