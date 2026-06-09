using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SongSelectItem : MonoBehaviour
{
    [Header("UI")]
    public Button button;
    public Image backgroundImage;
    public Image selectedFrameImage;
    public TMP_Text titleText;
    public TMP_Text numberText;
    public Image completionIconImage;

    [Header("Visual")]
    public bool tintBackgroundBySelection = false;
    public Color defaultColor = new Color32(0x7F, 0xB7, 0xAA, 0xFF);
    public Color selectedColor = new Color32(0x9A, 0xB5, 0x76, 0xFF);
    public Color disabledColor = new Color(1f, 1f, 1f, 0.45f);

    private Color originalBackgroundColor = Color.white;
    private bool hasOriginalBackgroundColor = false;
    private SongData data;
    private int index = -1;
    private SongSelectController controller;

    private void Awake()
    {
        CacheVisualDefaults();
        ConfigureImageSettings();
    }

    public void Setup(SongData data, int index, SongSelectController controller)
    {
        this.data = data;
        this.index = index;
        this.controller = controller;

        if (button == null)
        {
            button = GetComponent<Button>();
        }

        CacheVisualDefaults();
        ConfigureImageSettings();

        if (button != null)
        {
            button.onClick.RemoveListener(OnClicked);
            button.onClick.AddListener(OnClicked);
        }

        if (titleText != null)
        {
            titleText.text = data != null && !string.IsNullOrEmpty(data.displayNumber)
                ? $"TITLE {data.displayNumber}"
                : data != null ? data.title : "TITLE";
        }

        if (numberText != null)
        {
            numberText.text = data != null ? data.displayNumber : "";
        }

        SetSelected(false);
    }

    public void SetSelected(bool selected)
    {
        if (backgroundImage != null && tintBackgroundBySelection)
        {
            backgroundImage.color = selected ? selectedColor : defaultColor;
        }

        if (selectedFrameImage != null)
        {
            selectedFrameImage.gameObject.SetActive(selected);
        }
    }

    public void SetCompletionIcon(Sprite sprite)
    {
        if (completionIconImage == null)
        {
            return;
        }

        completionIconImage.sprite = sprite;
        completionIconImage.enabled = sprite != null;
        completionIconImage.preserveAspect = true;
        completionIconImage.raycastTarget = false;
    }

    private void CacheVisualDefaults()
    {
        if (backgroundImage != null && !hasOriginalBackgroundColor)
        {
            originalBackgroundColor = backgroundImage.color;
            hasOriginalBackgroundColor = true;
        }
    }

    private void ConfigureImageSettings()
    {
        if (backgroundImage != null)
        {
            backgroundImage.preserveAspect = false;
        }

        if (selectedFrameImage != null)
        {
            selectedFrameImage.preserveAspect = false;
        }

        if (completionIconImage != null)
        {
            completionIconImage.preserveAspect = true;
            completionIconImage.raycastTarget = false;
        }
    }

    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(OnClicked);
        }
    }

    private void OnClicked()
    {
        if (controller != null)
        {
            controller.SelectSong(index);
        }
    }
}
