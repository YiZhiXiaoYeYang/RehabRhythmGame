using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[ExecuteAlways]
public class SongSelectController : MonoBehaviour
{
    [Header("Data")]
    public SongDatabase songDatabase;

    [Header("List")]
    public Transform contentRoot;
    public SongSelectItem itemPrefab;
    public ScrollRect scrollRect;

    [Header("Scrollbar Visual Control")]
    public Scrollbar verticalScrollbar;
    public RectTransform scrollbarSlidingArea;
    public RectTransform scrollbarHandle;
    public bool useFixedScrollbarHandleSize = true;
    public float fixedHandleHeight = 80f;
    public float scrollbarTopPadding = 0f;
    public float scrollbarBottomPadding = 0f;
    public bool forceScrollbarRaycastTargets = true;

    [Header("Custom Scrollbar Handle")]
    public bool useCustomScrollbarHandle = true;
    public RectTransform customScrollbarHandle;
    public float customHandleTopY = 0f;
    public float customHandleBottomY = -500f;
    public bool useFixedCustomHandleSize = true;
    public float customHandleWidth = 24f;
    public float customHandleHeight = 80f;
    public bool updateHandleInEditMode = true;

    [Header("Completion Icons")]
    public Sprite newIcon;
    public Sprite playedIcon;
    public Sprite completedIcon;

    [Header("Select Button")]
    public Button selectButton;
    public CanvasGroup selectButtonCanvasGroup;
    public string gameplaySceneName = "04_Gameplay";

    [Header("Select Button Visual")]
    [Range(0f, 1f)]
    public float selectButtonEnabledAlpha = 1f;
    [Range(0f, 1f)]
    public float selectButtonDisabledAlpha = 0.2f;
    public bool keepSelectButtonInteractableForVisual = true;

    [Header("Manual Item Layout")]
    public bool useManualLayout = true;
    public float itemScale = 1f;
    public float itemSpacing = 30f;
    public Vector2 firstItemAnchoredPosition = Vector2.zero;
    public bool preservePrefabSize = true;

    [Header("Editor Preview")]
    public bool livePreviewInEditor = true;
    public bool autoRebuildPreviewInEditor = false;

    [Header("State")]
    [SerializeField] private int selectedIndex = -1;

    private readonly List<SongSelectItem> items = new List<SongSelectItem>();

    private void Start()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        if (selectButton != null)
        {
            selectButton.onClick.AddListener(OnSelectButtonClicked);
        }

        BuildList();
        SetupCustomScrollbarHandle();
        SyncCustomHandleToScrollRect();
    }

    private void Update()
    {
        if (Application.isPlaying && useCustomScrollbarHandle)
        {
            SyncCustomHandleToScrollRect();
        }
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
        if (useCustomScrollbarHandle)
        {
            SetupCustomScrollbarHandle();
        }
        else
        {
            PrepareScrollbar();
        }

        if (contentRoot == null || itemPrefab == null)
        {
            Debug.LogWarning("[SongSelectController] contentRoot or itemPrefab is missing.", this);
            RefreshSelectButtonState();
            RefreshScrollbarVisuals();
            return;
        }

        if (useManualLayout)
        {
            DisableAutomaticContentLayout();
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

        if (useManualLayout)
        {
            ApplyManualLayoutToItems(items);
        }

        RefreshScrollbarVisuals();
        RefreshSelectButtonState();
    }

    public void RefreshScrollbarVisuals()
    {
        if (useCustomScrollbarHandle)
        {
            SetupCustomScrollbarHandle();
            SyncCustomHandleToScrollRect();
            return;
        }

        PrepareScrollbar();
        ApplyScrollbarTravelRange();
        ApplyFixedScrollbarHandle();
    }

    public void SetupCustomScrollbarHandle()
    {
        if (customScrollbarHandle == null)
        {
            customScrollbarHandle = scrollbarHandle;
        }

        if (customScrollbarHandle == null && verticalScrollbar != null)
        {
            customScrollbarHandle = verticalScrollbar.handleRect;
        }

        if (customScrollbarHandle == null && verticalScrollbar != null)
        {
            Transform slidingAreaTransform = verticalScrollbar.transform.Find("Sliding Area");
            RectTransform slidingArea = slidingAreaTransform as RectTransform;
            Transform handleTransform = slidingArea != null ? slidingArea.Find("Handle") : null;
            customScrollbarHandle = handleTransform as RectTransform;
        }

        if (customScrollbarHandle != null)
        {
            Image handleImage = customScrollbarHandle.GetComponent<Image>();
            if (handleImage != null)
            {
                handleImage.raycastTarget = true;
            }

            SongSelectScrollbarHandle handle = customScrollbarHandle.GetComponent<SongSelectScrollbarHandle>();
            if (handle == null)
            {
                handle = customScrollbarHandle.gameObject.AddComponent<SongSelectScrollbarHandle>();
            }

            handle.controller = this;
        }

        if (scrollRect != null)
        {
            scrollRect.vertical = true;
            scrollRect.horizontal = false;
            if (useCustomScrollbarHandle)
            {
                scrollRect.verticalScrollbar = null;
            }
        }

        if (useCustomScrollbarHandle && verticalScrollbar != null)
        {
            verticalScrollbar.interactable = false;
            verticalScrollbar.handleRect = null;
            verticalScrollbar.targetGraphic = null;
        }

        ApplyCustomHandleSize();
        SyncCustomHandleToScrollRect();
    }

    private void ApplyCustomHandleSize()
    {
        if (customScrollbarHandle == null)
        {
            return;
        }

        customScrollbarHandle.anchorMin = new Vector2(0.5f, 0.5f);
        customScrollbarHandle.anchorMax = new Vector2(0.5f, 0.5f);
        customScrollbarHandle.pivot = new Vector2(0.5f, 0.5f);
        customScrollbarHandle.localScale = Vector3.one;
        if (useFixedCustomHandleSize)
        {
            customScrollbarHandle.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mathf.Max(1f, customHandleWidth));
            customScrollbarHandle.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Max(1f, customHandleHeight));
        }
    }

    public void SyncCustomHandleToScrollRect()
    {
        if (!useCustomScrollbarHandle || scrollRect == null || customScrollbarHandle == null)
        {
            return;
        }

        float normalized = scrollRect.verticalNormalizedPosition;
        float y = Mathf.Lerp(customHandleBottomY, customHandleTopY, normalized);
        Vector2 position = customScrollbarHandle.anchoredPosition;
        position.y = y;
        customScrollbarHandle.anchoredPosition = position;
    }

    public void SetScrollFromCustomHandleY(float handleY)
    {
        if (scrollRect == null)
        {
            return;
        }

        float clampedY = ClampCustomHandleY(handleY);
        float normalized = Mathf.InverseLerp(customHandleBottomY, customHandleTopY, clampedY);
        scrollRect.verticalNormalizedPosition = Mathf.Clamp01(normalized);
        SyncCustomHandleToScrollRect();
    }

    public float ClampCustomHandleY(float y)
    {
        float minY = Mathf.Min(customHandleBottomY, customHandleTopY);
        float maxY = Mathf.Max(customHandleBottomY, customHandleTopY);
        return Mathf.Clamp(y, minY, maxY);
    }

    public void PrepareScrollbar()
    {
        if (useCustomScrollbarHandle)
        {
            SetupCustomScrollbarHandle();
            return;
        }

        if (verticalScrollbar == null && scrollRect != null && scrollRect.verticalScrollbar != null)
        {
            verticalScrollbar = scrollRect.verticalScrollbar;
        }

        if (scrollRect != null)
        {
            scrollRect.vertical = true;
            scrollRect.horizontal = false;
            if (verticalScrollbar != null)
            {
                scrollRect.verticalScrollbar = verticalScrollbar;
                scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
            }
        }

        if (verticalScrollbar == null)
        {
            return;
        }

        verticalScrollbar.interactable = true;
        if (verticalScrollbar.direction != Scrollbar.Direction.BottomToTop &&
            verticalScrollbar.direction != Scrollbar.Direction.TopToBottom)
        {
            verticalScrollbar.direction = Scrollbar.Direction.BottomToTop;
        }

        if (scrollbarSlidingArea == null)
        {
            Transform slidingAreaTransform = verticalScrollbar.transform.Find("Sliding Area");
            scrollbarSlidingArea = slidingAreaTransform as RectTransform;
        }

        if (scrollbarHandle == null)
        {
            scrollbarHandle = verticalScrollbar.handleRect;
        }

        if (scrollbarHandle == null && scrollbarSlidingArea != null)
        {
            Transform handleTransform = scrollbarSlidingArea.Find("Handle");
            scrollbarHandle = handleTransform as RectTransform;
        }

        if (scrollbarHandle != null)
        {
            verticalScrollbar.handleRect = scrollbarHandle;
            Image handleImage = scrollbarHandle.GetComponent<Image>();
            if (handleImage != null)
            {
                verticalScrollbar.targetGraphic = handleImage;
            }
        }

        if (forceScrollbarRaycastTargets)
        {
            SetRaycastTarget(verticalScrollbar.GetComponent<Image>(), true);
            if (scrollbarSlidingArea != null)
            {
                SetRaycastTarget(scrollbarSlidingArea.GetComponent<Image>(), true);
            }

            if (scrollbarHandle != null)
            {
                SetRaycastTarget(scrollbarHandle.GetComponent<Image>(), true);
            }
        }
    }

    public void ApplyScrollbarTravelRange()
    {
        if (useCustomScrollbarHandle || scrollbarSlidingArea == null)
        {
            return;
        }

        Vector2 offsetMin = scrollbarSlidingArea.offsetMin;
        Vector2 offsetMax = scrollbarSlidingArea.offsetMax;
        scrollbarSlidingArea.anchorMin = new Vector2(0f, 0f);
        scrollbarSlidingArea.anchorMax = new Vector2(1f, 1f);
        scrollbarSlidingArea.pivot = new Vector2(0.5f, 0.5f);
        scrollbarSlidingArea.offsetMin = new Vector2(offsetMin.x, scrollbarBottomPadding);
        scrollbarSlidingArea.offsetMax = new Vector2(offsetMax.x, -scrollbarTopPadding);
        ApplyFixedScrollbarHandle();
    }

    public void ApplyFixedScrollbarHandle()
    {
        if (useCustomScrollbarHandle || !useFixedScrollbarHandleSize || verticalScrollbar == null || scrollbarHandle == null)
        {
            return;
        }

        verticalScrollbar.size = 0.0001f;
        scrollbarHandle.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Max(1f, fixedHandleHeight));
    }

    public void DisableAutomaticContentLayout()
    {
        if (contentRoot == null)
        {
            return;
        }

        VerticalLayoutGroup layoutGroup = contentRoot.GetComponent<VerticalLayoutGroup>();
        if (layoutGroup != null)
        {
            layoutGroup.enabled = false;
        }

        ContentSizeFitter sizeFitter = contentRoot.GetComponent<ContentSizeFitter>();
        if (sizeFitter != null)
        {
            sizeFitter.enabled = false;
        }
    }

    public void ApplyManualLayoutToItems(IList<SongSelectItem> layoutItems)
    {
        if (contentRoot == null || layoutItems == null)
        {
            return;
        }

        if (useManualLayout)
        {
            DisableAutomaticContentLayout();
        }

        float safeScale = Mathf.Max(0.0001f, itemScale);
        float totalHeight = 0f;
        int laidOutCount = 0;
        for (int i = 0; i < layoutItems.Count; i++)
        {
            SongSelectItem item = layoutItems[i];
            if (item == null)
            {
                continue;
            }

            RectTransform itemRect = item.GetComponent<RectTransform>();
            if (itemRect == null)
            {
                continue;
            }

            DisableItemLayoutElement(item);

            float itemOriginalHeight = GetItemInstanceHeight(itemRect);
            itemRect.anchorMin = new Vector2(0f, 1f);
            itemRect.anchorMax = new Vector2(0f, 1f);
            itemRect.pivot = new Vector2(0f, 1f);
            itemRect.localScale = Vector3.one * safeScale;
            float step = itemOriginalHeight * safeScale + itemSpacing;
            itemRect.anchoredPosition = firstItemAnchoredPosition + new Vector2(0f, -i * step);
            totalHeight += itemOriginalHeight * safeScale;
            laidOutCount++;
        }

        float contentHeight = totalHeight + Mathf.Max(0, laidOutCount - 1) * itemSpacing;
        float finalHeight = Mathf.Max(contentHeight, GetViewportHeight());
        RectTransform contentRect = contentRoot as RectTransform;
        if (contentRect != null)
        {
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(0f, 1f);
            contentRect.pivot = new Vector2(0f, 1f);
            contentRect.sizeDelta = new Vector2(contentRect.sizeDelta.x, finalHeight);
        }
    }

    public void RelayoutExistingPreviewItems(bool refreshScrollbar = true)
    {
        if (contentRoot == null)
        {
            return;
        }

        List<SongSelectItem> existingItems = new List<SongSelectItem>();
        for (int i = 0; i < contentRoot.childCount; i++)
        {
            SongSelectItem item = contentRoot.GetChild(i).GetComponent<SongSelectItem>();
            if (item != null)
            {
                existingItems.Add(item);
            }
        }

        ApplyManualLayoutToItems(existingItems);
        if (refreshScrollbar)
        {
            RefreshScrollbarVisuals();
        }

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.EditorUtility.SetDirty(contentRoot);
            if (gameObject.scene.IsValid())
            {
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
            }
        }
#endif
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
            selectButton.interactable = keepSelectButtonInteractableForVisual || hasSelection;
        }

        if (selectButtonCanvasGroup != null)
        {
            selectButtonCanvasGroup.alpha = hasSelection
                ? Mathf.Clamp01(selectButtonEnabledAlpha)
                : Mathf.Clamp01(selectButtonDisabledAlpha);
            selectButtonCanvasGroup.interactable = hasSelection;
            selectButtonCanvasGroup.blocksRaycasts = hasSelection;
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

    private float GetViewportHeight()
    {
        if (scrollRect != null && scrollRect.viewport != null)
        {
            return Mathf.Max(0f, scrollRect.viewport.rect.height);
        }

        RectTransform contentRect = contentRoot as RectTransform;
        RectTransform parentRect = contentRect != null ? contentRect.parent as RectTransform : null;
        return parentRect != null ? Mathf.Max(0f, parentRect.rect.height) : 0f;
    }

    private float GetItemInstanceHeight(RectTransform itemRect)
    {
        float itemHeight = Mathf.Abs(itemRect.rect.height);
        if (itemHeight <= 0.001f)
        {
            itemHeight = Mathf.Abs(itemRect.sizeDelta.y);
        }

        return Mathf.Max(1f, itemHeight);
    }

    private void DisableItemLayoutElement(SongSelectItem item)
    {
        LayoutElement layoutElement = item != null ? item.GetComponent<LayoutElement>() : null;
        if (layoutElement != null)
        {
            layoutElement.enabled = false;
        }
    }

    private void SetRaycastTarget(Graphic graphic, bool value)
    {
        if (graphic != null)
        {
            graphic.raycastTarget = value;
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (Application.isPlaying || !livePreviewInEditor)
        {
            return;
        }

        UnityEditor.EditorApplication.delayCall -= DelayedRelayoutExistingPreviewItems;
        UnityEditor.EditorApplication.delayCall += DelayedRelayoutExistingPreviewItems;
    }

    private void DelayedRelayoutExistingPreviewItems()
    {
        if (this == null || Application.isPlaying || !livePreviewInEditor)
        {
            return;
        }

        RelayoutExistingPreviewItems(updateHandleInEditMode);
    }
#endif
}
