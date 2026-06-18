using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class MainMenu : MonoBehaviour
{
    [Header("Scene")]
    [Tooltip("Tên scene map đầu tiên. Nên dùng tên scene thay vì số index để tránh sai Build Settings.")]
    public string firstMapSceneName = "Map1";

    public void Play()
    {
        if (!string.IsNullOrEmpty(firstMapSceneName))
        {
            SceneManager.LoadScene(firstMapSceneName);
        }
        else
        {
            SceneManager.LoadScene(1);
        }
    }

    public void Setting()
    {
        Debug.Log("MainMenu: Bấm nút Tùy Chọn.");
    }

    public void Out()
    {
        Debug.Log("MainMenu: Bấm nút Thoát Game.");

#if UNITY_EDITOR
        // Khi đang test trong Unity Editor thì dừng Play Mode.
        EditorApplication.isPlaying = false;
#else
        // Khi build game thật thì thoát game.
        Application.Quit();
#endif
    }
}