using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Собирает таблицу результатов прямо в существующей панели победы,
/// поэтому в сцене ничего донастраивать не нужно.
/// </summary>
public static class ResultsPanel
{
    private const int UiLayer = 5;

    /// <summary>Показать итоговую таблицу.</summary>
    public static void Show(GameObject winPanel, List<RacerInfo> results, int totalLaps)
    {
        if (winPanel == null || results == null) return;

        winPanel.SetActive(true);

        int playerPlace = FindPlayerPlace(results);

        TextMeshProUGUI title = FindTitle(winPanel);
        if (title != null)
        {
            RectTransform rect = title.rectTransform;
            rect.anchoredPosition = new Vector2(0f, 235f);
            rect.sizeDelta = new Vector2(760f, 90f);

            title.fontSize = 52f;
            title.alignment = TextAlignmentOptions.Center;
            title.color = new Color(1f, 0.85f, 0.2f);
            title.text = playerPlace == 1
                ? "Ты победил!"
                : $"Ты занял {playerPlace}-е место";
        }

        // Кнопку «Заново» уводим вниз, чтобы она не налезала на таблицу.
        Button restart = winPanel.GetComponentInChildren<Button>(true);
        if (restart != null)
            ((RectTransform)restart.transform).anchoredPosition = new Vector2(0f, -255f);

        RectTransform table = CreateContainer(winPanel.transform, "ResultsTable",
            new Vector2(0f, -10f), new Vector2(780f, 320f));

        float y = 105f;
        for (int i = 0; i < results.Count; i++)
        {
            RacerInfo racer = results[i];
            Color color = racer.isPlayer ? new Color(1f, 0.85f, 0.2f) : Color.white;

            MakeLabel(table, $"Place{i}", $"{i + 1}",
                new Vector2(-315f, y), new Vector2(80f, 44f),
                TextAlignmentOptions.Center, color, 36f);

            MakeLabel(table, $"Name{i}", racer.name,
                new Vector2(-40f, y), new Vector2(430f, 44f),
                TextAlignmentOptions.Left, color, 36f);

            MakeLabel(table, $"Result{i}", ResultText(racer, totalLaps),
                new Vector2(265f, y), new Vector2(280f, 44f),
                TextAlignmentOptions.Right, color, 36f);

            y -= 54f;
        }
    }

    private static int FindPlayerPlace(List<RacerInfo> results)
    {
        for (int i = 0; i < results.Count; i++)
            if (results[i].isPlayer) return i + 1;

        return results.Count;
    }

    private static string ResultText(RacerInfo racer, int totalLaps)
    {
        if (racer.finished) return FormatTime(racer.finishTime);

        int lap = Mathf.Clamp(racer.lapsDone + 1, 1, totalLaps);
        return $"круг {lap} из {totalLaps}";
    }

    private static string FormatTime(float seconds)
    {
        if (seconds < 0f) seconds = 0f;

        int minutes = Mathf.FloorToInt(seconds / 60f);
        return $"{minutes}:{seconds % 60f:00.0}";
    }

    /// <summary>Заголовок — единственный TMP прямо в панели, а не внутри кнопки.</summary>
    private static TextMeshProUGUI FindTitle(GameObject winPanel)
    {
        foreach (Transform child in winPanel.transform)
        {
            TextMeshProUGUI tmp = child.GetComponent<TextMeshProUGUI>();
            if (tmp != null) return tmp;
        }

        return winPanel.GetComponentInChildren<TextMeshProUGUI>(true);
    }

    private static RectTransform CreateContainer(Transform parent, string name, Vector2 position, Vector2 size)
    {
        Transform existing = parent.Find(name);
        if (existing != null) Object.Destroy(existing.gameObject);

        GameObject go = new GameObject(name, typeof(RectTransform));
        go.layer = UiLayer;

        RectTransform rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        return rect;
    }

    private static void MakeLabel(RectTransform parent, string name, string text,
        Vector2 position, Vector2 size, TextAlignmentOptions alignment, Color color, float fontSize)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.layer = UiLayer;

        RectTransform rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        TextMeshProUGUI label = go.AddComponent<TextMeshProUGUI>();

        if (TMP_Settings.defaultFontAsset != null)
            label.font = TMP_Settings.defaultFontAsset;

        label.text = text;
        label.fontSize = fontSize;
        label.alignment = alignment;
        label.color = color;
        label.raycastTarget = false;
    }
}
