using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class HandSettingController : MonoBehaviour
{
    [Header("Scene Names")]
    public string songSelectSceneName = "02_SongSelect";
    public string gameplaySceneName = "04_Gameplay";

    [Header("Buttons")]
    public Button backButton;
    public Button startButton;
    public Button leftHandButton;
    public Button rightHandButton;

    [Header("Selection Rings")]
    public GameObject leftSelectedRing;
    public GameObject rightSelectedRing;

    [Header("State")]
    [SerializeField] private bool isLeftSelected = true;

    private void Start()
    {
        if (backButton != null)
        {
            backButton.onClick.AddListener(OnBackClicked);
        }

        if (startButton != null)
        {
            startButton.onClick.AddListener(OnStartClicked);
        }

        if (leftHandButton != null)
        {
            leftHandButton.onClick.AddListener(SelectLeft);
        }

        if (rightHandButton != null)
        {
            rightHandButton.onClick.AddListener(SelectRight);
        }

        RefreshHandSelectionVisual();
    }

    private void OnDestroy()
    {
        if (backButton != null)
        {
            backButton.onClick.RemoveListener(OnBackClicked);
        }

        if (startButton != null)
        {
            startButton.onClick.RemoveListener(OnStartClicked);
        }

        if (leftHandButton != null)
        {
            leftHandButton.onClick.RemoveListener(SelectLeft);
        }

        if (rightHandButton != null)
        {
            rightHandButton.onClick.RemoveListener(SelectRight);
        }
    }

    public void SelectLeft()
    {
        isLeftSelected = true;
        RefreshHandSelectionVisual();
    }

    public void SelectRight()
    {
        isLeftSelected = false;
        RefreshHandSelectionVisual();
    }

    public void RefreshHandSelectionVisual()
    {
        if (leftSelectedRing != null)
        {
            leftSelectedRing.SetActive(isLeftSelected);
        }

        if (rightSelectedRing != null)
        {
            rightSelectedRing.SetActive(!isLeftSelected);
        }
    }

    private void OnBackClicked()
    {
        LoadScene(songSelectSceneName);
    }

    private void OnStartClicked()
    {
        LoadScene(gameplaySceneName);
    }

    private void LoadScene(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            ProjectDebug.LogWarning("[HandSettingController] Scene name is empty.", DebugChannel.UI, this);
            return;
        }

        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.LoadSceneWithFade(sceneName);
        }
        else
        {
            SceneManager.LoadScene(sceneName);
        }
    }
}
