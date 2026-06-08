using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SongSelectController : MonoBehaviour
{
    [Header("Data")]
    public SongDatabase songDatabase;

    [Header("List")]
    public Transform contentRoot;
    public SongSelectItem itemPrefab;
    public ScrollRect scrollRect;

    [Header("Completion Icons")]
    public Sprite newIcon;
    public Sprite playedIcon;
    public Sprite completedIcon;

    [Header("Select Button")]
    public Button selectButton;
    public CanvasGroup selectButtonCanvasGroup;
    public string gameplaySceneName = "04_Gameplay";

    [Header("State")]
    [SerializeField] private int selectedIndex = -1;

    private readonly List<SongSelectItem> items = new List<SongSelectItem>();

    private void Start()
    {
        if (selectButton != null)
        {
            selectButton.onClick.AddListener(OnSelectButtonClicked);
        }

        BuildList();
    }

    private void OnDestroy()
    {
        if (selectButton != null)
        {
            selectButton.onClick.RemoveListener(OnSelectButtonClicked);
        }
    }

    public void BuildList()
    {
        items.Clear();
        selectedIndex = -1;

        if (contentRoot == null || itemPrefab == null)
        {
            Debug.LogWarning("[SongSelectController] contentRoot or itemPrefab is missing.", this);
            RefreshSelectButtonState();
            return;
        }

        for (int i = contentRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(contentRoot.GetChild(i).gameObject);
        }

        if (songDatabase == null || songDatabase.songs == null)
        {
            Debug.LogWarning("[SongSelectController] songDatabase is missing.", this);
            RefreshSelectButtonState();
            return;
        }

        for (int i = 0; i < songDatabase.songs.Count; i++)
        {
            SongData song = songDatabase.songs[i];
            SongSelectItem item = Instantiate(itemPrefab, contentRoot);
            item.gameObject.SetActive(true);
            item.Setup(song, i, this);
            item.SetCompletionIcon(GetCompletionIcon(song));
            item.SetSelected(false);
            items.Add(item);
        }

        RefreshSelectButtonState();
    }

    public void SelectSong(int index)
    {
        if (songDatabase == null || songDatabase.songs == null)
        {
            return;
        }

        if (index < 0 || index >= songDatabase.songs.Count)
        {
            return;
        }

        selectedIndex = index;
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] != null)
            {
                items[i].SetSelected(i == selectedIndex);
            }
        }

        RefreshSelectButtonState();
    }

    public void OnSelectButtonClicked()
    {
        if (selectedIndex < 0)
        {
            return;
        }

        SongData song = songDatabase != null && songDatabase.songs != null && selectedIndex < songDatabase.songs.Count
            ? songDatabase.songs[selectedIndex]
            : null;

        if (GameSessionManager.Instance != null)
        {
            GameSessionManager.Instance.selectedSongIndex = selectedIndex;
            GameSessionManager.Instance.selectedSongId = song != null ? song.songId : "";
            GameSessionManager.Instance.selectedSongTitle = song != null ? song.title : "";
        }

        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.LoadSceneWithFade(gameplaySceneName);
        }
        else
        {
            SceneManager.LoadScene(gameplaySceneName);
        }
    }

    private void RefreshSelectButtonState()
    {
        bool hasSelection = selectedIndex >= 0;

        if (selectButton != null)
        {
            selectButton.interactable = hasSelection;
        }

        if (selectButtonCanvasGroup != null)
        {
            selectButtonCanvasGroup.alpha = hasSelection ? 1f : 0.45f;
        }
    }

    private Sprite GetCompletionIcon(SongData song)
    {
        if (song == null)
        {
            return null;
        }

        switch (song.completionState)
        {
            case SongCompletionState.Completed:
                return completedIcon;
            case SongCompletionState.Played:
                return playedIcon;
            default:
                return newIcon;
        }
    }
}
