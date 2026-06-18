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
        ProjectDebug.Log("[PauseManager] Game paused.", DebugChannel.UI, this);
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
        ProjectDebug.Log("[PauseManager] Game resumed.", DebugChannel.UI, this);
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
            ProjectDebug.LogWarning($"[PauseManager] Missing {(IsPaused ? "playSprite" : "pauseSprite")} for pause button.", DebugChannel.UI, this);
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
            ProjectDebug.Log($"[PauseManager] Bound bgmSource from RhythmManager: {bgmSource.name}", DebugChannel.UI, this);
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
