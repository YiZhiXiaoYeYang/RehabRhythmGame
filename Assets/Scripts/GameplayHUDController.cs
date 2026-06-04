using TMPro;
using UnityEngine;

public class GameplayHUDController : MonoBehaviour
{
    public RhythmManager rhythmManager;
    public TMP_Text beatText;
    public TMP_Text comboText;
    public TMP_Text hitText;
    public TMP_Text missText;
    public string numberFormat = "000";

    private void Start()
    {
        if (rhythmManager == null)
        {
            rhythmManager = FindObjectOfType<RhythmManager>();
        }

        Refresh();
    }

    private void Update()
    {
        Refresh();
    }

    public void Refresh()
    {
        if (rhythmManager == null)
        {
            return;
        }

        SetText(beatText, rhythmManager.GetBeatCount());
        SetText(comboText, rhythmManager.GetCombo());
        SetText(hitText, rhythmManager.GetHitCount());
        SetText(missText, rhythmManager.GetMissCount());
    }

    private void SetText(TMP_Text targetText, int value)
    {
        if (targetText != null)
        {
            targetText.text = value.ToString(numberFormat);
        }
    }
}
