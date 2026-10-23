using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    public Transform player;
    public TextMeshProUGUI speedText;
    public TextMeshProUGUI timeText;

    private float timer = 0f;
    private Vector3 lastPos;
    private bool finished = false;

    void Start()
    {
        if (player != null) lastPos = player.position;
    }

    void Update()
    {
        if (finished) return;

        timer += Time.deltaTime;
        if (timeText != null) timeText.text = $"Время: {timer:F1} с";

        if (speedText != null && player != null)
        {
            float speedMs = Vector3.Distance(player.position, lastPos) / Time.deltaTime;
            speedText.text = $"Скорость: {speedMs * 3.6f:F0} км/ч";
            lastPos = player.position;
        }
    }

    public void StopTimer()
    {
        finished = true;
    }
}