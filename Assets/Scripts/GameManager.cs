using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    public int totalLaps = 3;
    private int currentLap = 0;

    [Header("Ññûëêè íà UI")]
    public TextMeshProUGUI lapText;
    public GameObject winPanel;

    void Start()
    {
        UpdateLapUI();
        if (winPanel != null) winPanel.SetActive(false);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        currentLap++;
        UpdateLapUI();

        if (currentLap >= totalLaps)
        {
            if (winPanel != null) winPanel.SetActive(true);

            UIManager ui = FindObjectOfType<UIManager>();
            if (ui != null) ui.StopTimer();
        }
    }

    void UpdateLapUI()
    {
        if (lapText != null)
            lapText.text = $"Êğóã: {currentLap} / {totalLaps}";
    }

    // ÂÎÒ İÒÎÒ ÌÅÒÎÄ ÍÓÆÅÍ ÄËß ÊÍÎÏÊÈ
    public void RestartGame()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
    }
}