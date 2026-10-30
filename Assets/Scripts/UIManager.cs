using UnityEngine;
using TMPro;

/// <summary>Таймер и спидометр на экране.</summary>
public class UIManager : MonoBehaviour
{
    public Transform player;
    public TextMeshProUGUI speedText;
    public TextMeshProUGUI timeText;

    private CarController car;

    private void Start()
    {
        if (player == null)
        {
            GameObject found = GameObject.FindGameObjectWithTag("Player");
            if (found != null) player = found.transform;
        }

        if (player != null) car = player.GetComponent<CarController>();
    }

    private void Update()
    {
        if (timeText != null)
        {
            GameManager gm = GameManager.Instance;
            float time = gm != null ? gm.RaceTime : 0f;
            timeText.text = $"Время: {time:F1} с";
        }

        if (speedText != null)
        {
            float kmh = car != null ? car.SpeedKmh : 0f;
            speedText.text = $"Скорость: {kmh:F0} км/ч";
        }
    }
}
