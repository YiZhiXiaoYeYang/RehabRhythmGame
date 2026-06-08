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
    public Color defaultColor = new Color32(0x7F, 0xB7, 0xAA, 0xFF);
    public Color selectedColor = new Color32(0x9A, 0xB5, 0x76, 0xFF);
    public Color disabledColor = new Color(1f, 1f, 1f, 0.45f);

    private SongData data;
    private int index = -1;
    private SongSelectController controller;

    public void Setup(SongData data, int index, SongSelectController controller)
    {
        this.data = data;
        this.index = index;
        this.controller = controller;

        if (button == null)
        {
            button = GetComponent<Button>();
        }

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
        if (backgroundImage != null)
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
