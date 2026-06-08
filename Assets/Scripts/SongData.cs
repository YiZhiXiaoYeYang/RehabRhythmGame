using UnityEngine;

[CreateAssetMenu(fileName = "SongData", menuName = "Rehab Rhythm/Song Data")]
public class SongData : ScriptableObject
{
    public string songId;
    public string title;
    public string displayNumber;
    public SongCompletionState completionState = SongCompletionState.New;

    [Header("Optional Demo Fields")]
    public AudioClip audioClip;
    public string beatmapFileName;
}
