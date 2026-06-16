using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void Play()
    {
        SceneManager.LoadScene(2);
    }

    // Update is called once per frame
    public void Setting()
    {
        
    }
    public void Out()
    {
        Application.Quit();
    }
}
