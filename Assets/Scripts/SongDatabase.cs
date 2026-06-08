using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SongDatabase", menuName = "Rehab Rhythm/Song Database")]
public class SongDatabase : ScriptableObject
{
    public List<SongData> songs = new List<SongData>();
}
