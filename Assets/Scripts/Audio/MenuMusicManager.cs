using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuMusicManager : MonoBehaviour
{
    public static MenuMusicManager Instance { get; private set; }

    [Header("Audio")]
    public AudioClip menuMusicClip;
    [Range(0f, 1f)]
    public float menuVolume = 0.35f;
    public float fadeDuration = 0.8f;

    [Header("Scene Rules")]
    public string[] menuSceneNames = { "01_Start", "02_SongSelect", "03_HandSetting" };
    public string gameplaySceneName = "04_Gameplay";

    private AudioSource audioSource;
    private Coroutine fadeCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        GameObject persistentRoot = transform.root.gameObject;
        DontDestroyOnLoad(persistentRoot);

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.loop = true;
        audioSource.playOnAwake = false;
        audioSource.volume = 0f;

        EnsureAudioListenerForCurrentScene();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        HandleScene(SceneManager.GetActiveScene().name);
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureAudioListenerForCurrentScene();
        HandleScene(scene.name);
    }

    private void HandleScene(string sceneName)
    {
        if (IsMenuScene(sceneName))
        {
            Debug.Log($"[MenuMusicManager] Menu scene detected: {sceneName}. Fading in menu music.", this);
            FadeInToMenuMusic();
            return;
        }

        if (sceneName == gameplaySceneName)
        {
            Debug.Log("[MenuMusicManager] Gameplay scene detected. Fading out menu music.", this);
            FadeOutAndStop();
            return;
        }

        if (!string.IsNullOrEmpty(sceneName))
        {
            FadeOutAndStop();
        }
    }

    public void FadeInToMenuMusic()
    {
        if (menuMusicClip == null)
        {
            Debug.LogWarning("[MenuMusicManager] menuMusicClip is missing.", this);
            return;
        }

        EnsureAudioSource();

        if (audioSource.clip != menuMusicClip)
        {
            audioSource.clip = menuMusicClip;
        }

        if (!audioSource.isPlaying)
        {
            audioSource.Play();
        }

        StartFade(menuVolume, false);
    }

    public void FadeOutAndStop()
    {
        EnsureAudioSource();

        if (!audioSource.isPlaying && audioSource.volume <= 0f)
        {
            return;
        }

        StartFade(0f, true);
    }

    private void StartFade(float targetVolume, bool stopWhenDone)
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }

        fadeCoroutine = StartCoroutine(FadeRoutine(Mathf.Clamp01(targetVolume), stopWhenDone));
    }

    private IEnumerator FadeRoutine(float targetVolume, bool stopWhenDone)
    {
        float startVolume = audioSource.volume;
        float elapsed = 0f;
        float safeDuration = Mathf.Max(0.0001f, fadeDuration);

        while (elapsed < safeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / safeDuration);
            audioSource.volume = Mathf.Lerp(startVolume, targetVolume, t);
            yield return null;
        }

        audioSource.volume = targetVolume;
        if (stopWhenDone)
        {
            audioSource.Stop();
        }

        fadeCoroutine = null;
    }

    private void EnsureAudioSource()
    {
        if (audioSource != null)
        {
            return;
        }

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.loop = true;
        audioSource.playOnAwake = false;
    }

    private void EnsureAudioListenerForCurrentScene()
    {
        if (FindObjectOfType<AudioListener>() != null)
        {
            return;
        }

        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            mainCamera.gameObject.AddComponent<AudioListener>();
            Debug.Log($"[MenuMusicManager] Added AudioListener to {mainCamera.gameObject.name}.", mainCamera);
            return;
        }

        Camera camera = FindObjectOfType<Camera>();
        if (camera != null)
        {
            camera.gameObject.AddComponent<AudioListener>();
            Debug.Log($"[MenuMusicManager] Added AudioListener to {camera.gameObject.name}.", camera);
            return;
        }

        GameObject listenerObject = new GameObject("RuntimeAudioListener");
        listenerObject.AddComponent<AudioListener>();
        Debug.Log("[MenuMusicManager] Added AudioListener to RuntimeAudioListener.", listenerObject);
    }

    private bool IsMenuScene(string sceneName)
    {
        if (menuSceneNames == null)
        {
            return false;
        }

        for (int i = 0; i < menuSceneNames.Length; i++)
        {
            if (sceneName == menuSceneNames[i])
            {
                return true;
            }
        }

        return false;
    }
}
