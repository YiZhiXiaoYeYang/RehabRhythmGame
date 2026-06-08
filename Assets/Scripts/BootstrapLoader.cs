using UnityEngine;
using UnityEngine.SceneManagement;

public class BootstrapLoader : MonoBehaviour
{
    public string firstSceneName = "01_Start";

    private void Start()
    {
        SceneManager.LoadScene(firstSceneName);
    }
}
