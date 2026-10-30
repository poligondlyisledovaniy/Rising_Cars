using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Правила гонки: подсчёт кругов, защита от обмана с линией финиша
/// и таблица результатов.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Гонка")]
    public int totalLaps = 3;

    [Header("Ссылки на UI")]
    public TextMeshProUGUI lapText;
    public GameObject winPanel;

    [Header("Проверка круга")]
    [Tooltip("Радиус, в котором засчитывается промежуточная точка трассы")]
    public float checkpointRadius = 16f;

    /// <summary>Сколько кругов игрок уже закончил.</summary>
    public int CurrentLap { get; private set; }

    /// <summary>Время гонки в секундах.</summary>
    public float RaceTime { get; private set; }

    /// <summary>Гонка закончена, таймер остановлен.</summary>
    public bool IsFinished { get; private set; }

    /// <summary>Прогресс игрока в кругах — для резиновой ленты и мест.</summary>
    public float PlayerProgress
    {
        get
        {
            // Transform может быть уже уничтожен при выгрузке сцены.
            if (playerTransform == null) return CurrentLap;
            return CurrentLap + TrackLoop.Fraction(playerTransform.position);
        }
    }

    private Transform playerTransform;
    private int nextCheckpoint;

    /// <summary>
    /// true, когда игрок проехал все промежуточные точки круга.
    /// Линия финиша засчитывается только в этом состоянии,
    /// поэтому кататься через неё туда-сюда бесполезно.
    /// </summary>
    private bool finishArmed;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        playerTransform = FindPlayer();
        UpdateLapUI();
        if (winPanel != null) winPanel.SetActive(false);
    }

    private void Update()
    {
        // R работает всегда — и до финиша, и после.
        if (Input.GetKeyDown(KeyCode.R))
        {
            RestartGame();
            return;
        }

        if (IsFinished) return;

        RaceTime += Time.deltaTime;
        CheckCheckpoints();
    }

    /// <summary>Проезд промежуточных точек по порядку.</summary>
    private void CheckCheckpoints()
    {
        if (playerTransform == null || !TrackLoop.Ready) return;

        Transform[] points = TrackLoop.Points;
        if (nextCheckpoint >= points.Length) nextCheckpoint = 0;

        Vector3 target = points[nextCheckpoint].position;
        target.y = playerTransform.position.y;

        if ((playerTransform.position - target).sqrMagnitude > checkpointRadius * checkpointRadius)
            return;

        nextCheckpoint++;

        if (nextCheckpoint < points.Length) return;

        // Круг по точкам замкнут — теперь линия финиша снова что-то значит.
        nextCheckpoint = 0;
        finishArmed = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (IsFinished) return;
        if (!other.CompareTag("Player")) return;

        // Без полного круга по точкам линия финиша игнорируется.
        if (!finishArmed) return;

        finishArmed = false;
        CurrentLap++;
        UpdateLapUI();

        if (CurrentLap >= totalLaps)
            FinishRace();
    }

    private void FinishRace()
    {
        IsFinished = true;

        List<RacerInfo> results = new List<RacerInfo>
        {
            new RacerInfo
            {
                name = "Игрок",
                isPlayer = true,
                lapsDone = CurrentLap,
                progress = totalLaps,
                finished = true,
                finishTime = RaceTime
            }
        };

        foreach (BotCar bot in FindObjectsByType<BotCar>(FindObjectsSortMode.None))
        {
            results.Add(new RacerInfo
            {
                name = bot.RacerName,
                isPlayer = false,
                lapsDone = bot.LapsDone,
                progress = bot.Progress,
                finished = bot.Finished,
                finishTime = bot.FinishTime
            });
        }

        results.Sort(CompareRacers);
        ResultsPanel.Show(winPanel, results, totalLaps);
    }

    /// <summary>Кто финишировал — выше; иначе по пройденному расстоянию.</summary>
    private static int CompareRacers(RacerInfo a, RacerInfo b)
    {
        if (a.finished != b.finished) return a.finished ? -1 : 1;
        if (a.finished) return a.finishTime.CompareTo(b.finishTime);
        return b.progress.CompareTo(a.progress);
    }

    private void UpdateLapUI()
    {
        if (lapText == null) return;

        int shown = Mathf.Clamp(CurrentLap + 1, 1, totalLaps);
        lapText.text = $"Круг: {shown} / {totalLaps}";
    }

    private static Transform FindPlayer()
    {
        GameObject byTag = GameObject.FindGameObjectWithTag("Player");
        if (byTag != null) return byTag.transform;

        CarController car = FindFirstObjectByType<CarController>();
        return car != null ? car.transform : null;
    }

    /// <summary>Перезапуск — висит на кнопке в панели победы.</summary>
    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
