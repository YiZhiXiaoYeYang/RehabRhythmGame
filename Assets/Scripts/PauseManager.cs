using UnityEngine;
using UnityEngine.UI;

public class PauseManager : MonoBehaviour
{
    public static bool IsPaused { get; private set; }

    public AudioSource bgmSource;
    public Button pauseButton;
    public Image pauseButtonImage;
    public Sprite pauseSprite;
    public Sprite playSprite;
    public KeyCode toggleKey = KeyCode.Escape;
    public bool pauseOnStart = false;

    private void Start()
    {
        if (pauseButton != null)
        {
            pauseButton.onClick.AddListener(TogglePause);
        }

        TryBindBgmSource();

        if (pauseOnStart)
        {
            PauseGame();
        }
        else
        {
            IsPaused = false;
            Time.timeScale = 1f;
            UpdateButtonVisual();
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            TogglePause();
        }
    }

    public void TogglePause()
    {
        if (IsPaused)
        {
            ResumeGame();
        }
        else
        {
            PauseGame();
        }
    }

    public void PauseGame()
    {
        IsPaused = true;
        Time.timeScale = 0f;

        if (bgmSource != null && bgmSource.isPlaying)
        {
            bgmSource.Pause();
        }

        UpdateButtonVisual();
        Debug.Log("[PauseManager] Game paused.", this);
    }

    public void ResumeGame()
    {
        IsPaused = false;
        Time.timeScale = 1f;

        if (bgmSource != null)
        {
            bgmSource.UnPause();
        }

        UpdateButtonVisual();
        Debug.Log("[PauseManager] Game resumed.", this);
    }

    private void UpdateButtonVisual()
    {
        if (pauseButtonImage == null)
        {
            return;
        }

        Sprite targetSprite = IsPaused ? playSprite : pauseSprite;
        if (targetSprite == null)
        {
            Debug.LogWarning($"[PauseManager] Missing {(IsPaused ? "playSprite" : "pauseSprite")} for pause button.", this);
            return;
        }

        pauseButtonImage.sprite = targetSprite;
    }

    private void TryBindBgmSource()
    {
        if (bgmSource != null)
        {
            return;
        }

        RhythmManager rhythmManager = FindObjectOfType<RhythmManager>();
        if (rhythmManager != null && rhythmManager.bgmSource != null)
        {
            bgmSource = rhythmManager.bgmSource;
            Debug.Log($"[PauseManager] Bound bgmSource from RhythmManager: {bgmSource.name}", this);
        }
    }

    private void OnDestroy()
    {
        if (pauseButton != null)
        {
            pauseButton.onClick.RemoveListener(TogglePause);
        }

        if (IsPaused)
        {
            Time.timeScale = 1f;
        }

        IsPaused = false;
    }
}
