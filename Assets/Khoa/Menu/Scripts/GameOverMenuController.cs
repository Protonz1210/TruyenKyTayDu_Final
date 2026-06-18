using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Điều khiển UI GameOver bằng chuột.
/// Gắn script này vào object cha OVER.
/// 
/// Cơ chế chặt:
/// - Có thể để OVER tắt sẵn ngoài Hierarchy.
/// - Khi MapStory gọi ShowGameOver(), script vẫn tự bật lại OVER.
/// - Không dùng phím tắt, chỉ dùng click chuột.
/// </summary>
public class GameOverMenuController : MonoBehaviour
{
    [Header("Root")]
    [Tooltip("Object cha chứa toàn bộ UI GameOver. Nếu để trống sẽ dùng chính object đang gắn script.")]
    public GameObject gameOverRoot;

    [Tooltip("Ẩn GameOver khi bắt đầu scene.")]
    public bool hideOnStart = true;

    [Tooltip("Pause game khi hiện GameOver.")]
    public bool pauseGameWhenShow = true;

    [Header("Buttons")]
    [Tooltip("Nút Chơi lại.")]
    public Button playAgainButton;

    [Tooltip("Nút Thoát ra menu.")]
    public Button exitToMenuButton;

    [Header("Scene Names")]
    [Tooltip("Tên scene sẽ load khi bấm Chơi lại. Nếu để trống thì tự load lại scene hiện tại.")]
    public string playAgainSceneName = "";

    [Tooltip("Tên scene menu sẽ load khi bấm Thoát ra menu.")]
    public string menuSceneName = "MainMenu";

    [Header("Cursor")]
    [Tooltip("Hiện chuột khi GameOver.")]
    public bool showCursorWhenGameOver = true;

    [Header("Safety")]
    [Tooltip("Nếu OVER đang nằm trong parent bị tắt, bật cả parent lên để UI chắc chắn hiện.")]
    public bool activateParentsWhenShow = false;

    [Header("Debug")]
    public bool enableDebugLog = true;

    private bool isGameOverShown;

    private void Awake()
    {
        EnsureRoot();

        BindButtons();

        if (hideOnStart)
        {
            HideGameOver();
        }
    }

    private void OnEnable()
    {
        EnsureRoot();
        BindButtons();
    }

    private void EnsureRoot()
    {
        if (gameOverRoot == null)
        {
            gameOverRoot = gameObject;
        }
    }

    private void BindButtons()
    {
        if (playAgainButton != null)
        {
            playAgainButton.onClick.RemoveListener(PlayAgain);
            playAgainButton.onClick.AddListener(PlayAgain);
        }

        if (exitToMenuButton != null)
        {
            exitToMenuButton.onClick.RemoveListener(ExitToMenu);
            exitToMenuButton.onClick.AddListener(ExitToMenu);
        }
    }

    public void ShowGameOver()
    {
        EnsureRoot();

        isGameOverShown = true;

        if (activateParentsWhenShow)
        {
            ActivateParents(gameOverRoot.transform);
        }

        if (gameOverRoot != null)
        {
            gameOverRoot.SetActive(true);
        }

        // Sau khi object vừa bật lại, bind lại button để chắc chắn click ăn.
        BindButtons();

        if (pauseGameWhenShow)
        {
            Time.timeScale = 0f;
        }

        if (showCursorWhenGameOver)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        if (enableDebugLog)
        {
            Debug.Log(
                "GameOverMenuController: Đã hiện GameOver. "
                + "Root = " + (gameOverRoot != null ? gameOverRoot.name : "NULL")
                + " | ActiveSelf = " + (gameOverRoot != null && gameOverRoot.activeSelf)
                + " | ActiveInHierarchy = " + (gameOverRoot != null && gameOverRoot.activeInHierarchy)
            );
        }
    }

    public void HideGameOver()
    {
        EnsureRoot();

        isGameOverShown = false;

        if (gameOverRoot != null)
        {
            gameOverRoot.SetActive(false);
        }

        if (pauseGameWhenShow)
        {
            Time.timeScale = 1f;
        }
    }

    public void PlayAgain()
    {
        Time.timeScale = 1f;

        string sceneToLoad = playAgainSceneName;

        if (string.IsNullOrEmpty(sceneToLoad))
        {
            sceneToLoad = SceneManager.GetActiveScene().name;
        }

        if (enableDebugLog)
        {
            Debug.Log("GameOverMenuController: Chơi lại scene " + sceneToLoad);
        }

        SceneManager.LoadScene(sceneToLoad);
    }

    public void ExitToMenu()
    {
        Time.timeScale = 1f;

        if (string.IsNullOrEmpty(menuSceneName))
        {
            Debug.LogWarning("GameOverMenuController: Chưa nhập Menu Scene Name.");
            return;
        }

        if (enableDebugLog)
        {
            Debug.Log("GameOverMenuController: Thoát ra menu scene " + menuSceneName);
        }

        SceneManager.LoadScene(menuSceneName);
    }

    private void ActivateParents(Transform child)
    {
        if (child == null)
        {
            return;
        }

        Transform current = child.parent;

        while (current != null)
        {
            if (!current.gameObject.activeSelf)
            {
                current.gameObject.SetActive(true);
            }

            current = current.parent;
        }
    }

    public bool IsGameOverShown()
    {
        return isGameOverShown;
    }
}