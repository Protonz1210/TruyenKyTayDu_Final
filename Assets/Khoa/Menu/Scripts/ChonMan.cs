using UnityEngine;
using UnityEngine.SceneManagement;

public class ChonMan : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void Map1()
    {
        SceneManager.LoadScene(2);
    }

    // Update is called once per frame
    public void Map2()
    {
        SceneManager.LoadScene(3);
    }
    public void Map3()
    {
        SceneManager.LoadScene(4);
    }
    public void Map4()
    {
        SceneManager.LoadScene(5);
    }
    public void Map5()
    {
        SceneManager.LoadScene(6);
    }
}
