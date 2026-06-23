using UnityEngine;
using UnityEngine.SceneManagement;

public class GlobalResetManager : MonoBehaviour
{
    public static GlobalResetManager Instance { get; private set; }

    [Header("Reset")]
    public KeyCode resetKey = KeyCode.F5;
    public string resetSceneName = "01_Start";
    public bool useSceneTransition = true;
    public bool logReset = true;
    public bool requireHold = false;
    public float holdSeconds = 1.0f;

    private bool isResetting;
    private float holdTimer;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(transform.root.gameObject);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Update()
    {
        if (isResetting)
        {
            return;
        }

        if (!requireHold)
        {
            if (Input.GetKeyDown(resetKey))
            {
                ResetToStart();
            }

            return;
        }

        if (Input.GetKey(resetKey))
        {
            holdTimer += Time.unscaledDeltaTime;
            if (holdTimer >= Mathf.Max(0.01f, holdSeconds))
            {
                ResetToStart();
            }
        }
        else
        {
            holdTimer = 0f;
        }
    }

    public void ResetToStart()
    {
        if (isResetting)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(resetSceneName))
        {
            ProjectDebug.LogWarning("[GlobalResetManager] resetSceneName is empty.", DebugChannel.Scene, this);
            return;
        }

        isResetting = true;
        holdTimer = 0f;

        Time.timeScale = 1f;
        AudioListener.pause = false;

        ResumePauseManagerIfPresent();
        NotifySessionResetIfPresent();

        if (logReset)
        {
            ProjectDebug.Log($"[GlobalResetManager] Resetting to scene: {resetSceneName}", DebugChannel.Scene, this);
        }

        if (useSceneTransition && SceneTransitionManager.Instance != null && !SceneTransitionManager.Instance.isTransitioning)
        {
            SceneTransitionManager.Instance.LoadSceneWithFade(resetSceneName);
        }
        else
        {
            SceneManager.LoadScene(resetSceneName);
        }
    }

    private void ResumePauseManagerIfPresent()
    {
        PauseManager pauseManager = FindObjectOfType<PauseManager>();
        if (pauseManager != null && PauseManager.IsPaused)
        {
            pauseManager.ResumeGame();
        }
    }

    private void NotifySessionResetIfPresent()
    {
        GameSessionManager sessionManager = GameSessionManager.Instance;
        if (sessionManager == null)
        {
            sessionManager = FindObjectOfType<GameSessionManager>();
        }

        if (sessionManager == null)
        {
            return;
        }

        sessionManager.SendMessage("ResetSession", SendMessageOptions.DontRequireReceiver);
        sessionManager.SendMessage("ClearSession", SendMessageOptions.DontRequireReceiver);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!isResetting)
        {
            return;
        }

        Time.timeScale = 1f;
        AudioListener.pause = false;

        if (scene.name == resetSceneName)
        {
            isResetting = false;
            holdTimer = 0f;

            if (logReset)
            {
                ProjectDebug.Log($"[GlobalResetManager] Reset complete: {scene.name}", DebugChannel.Scene, this);
            }
        }
    }
}
