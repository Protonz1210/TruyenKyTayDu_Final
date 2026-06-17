using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOver : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void ChoiLai()
    {
        SceneManager.LoadScene(1);
    }

    // Update is called once per frame
    public void ThoatRaMenu()
    {
        SceneManager.LoadScene(0);
    }
}
