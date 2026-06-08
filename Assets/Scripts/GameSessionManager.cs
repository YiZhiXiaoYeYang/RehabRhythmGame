using UnityEngine;

public class GameSessionManager : MonoBehaviour
{
    public static GameSessionManager Instance { get; private set; }

    public int selectedSongIndex = 0;
    public string selectedSongTitle = "";
    public string selectedHand = "Left";
    public string selectedFinger = "Index";
    public bool hardwareInputEnabled = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
}
