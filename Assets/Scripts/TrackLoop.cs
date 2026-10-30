using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Кольцевая трасса. Хранит вейпоинты по порядку проезда и умеет
/// определять, в каком месте круга находится машина.
/// </summary>
public static class TrackLoop
{
    private static Transform[] cached;

    /// <summary>Точки трассы по порядку. Ищутся в сцене один раз.</summary>
    public static Transform[] Points
    {
        get
        {
            // После перезагрузки сцены старые Transform'ы уничтожаются,
            // поэтому кэш обязательно проверяем на «живость».
            if (IsAlive(cached)) return cached;

            cached = FindWaypoints();
            return cached;
        }
    }

    private static Transform[] FindWaypoints()
    {
        List<Transform> found = new List<Transform>();

        for (int i = 1; i < 100; i++)
        {
            GameObject go = GameObject.Find("Waypoint_" + i);
            if (go == null) break;
            found.Add(go.transform);
        }

        return found.ToArray();
    }

    /// <summary>Все ссылки в массиве ещё существуют?</summary>
    private static bool IsAlive(Transform[] array)
    {
        if (array == null || array.Length == 0) return false;

        for (int i = 0; i < array.Length; i++)
        {
            // Для уничтоженных объектов Unity-оператор == возвращает true.
            if (array[i] == null) return false;
        }

        return true;
    }

    public static bool Ready => Points.Length >= 2;

    /// <summary>
    /// Положение точки на круге: 0 — старт, 0.99 — прямо перед финишем.
    /// Нужно для резиновой ленты и для подсчёта мест.
    /// </summary>
    public static float Fraction(Vector3 position)
    {
        Transform[] points = Points;
        int count = points.Length;
        if (count < 2) return 0f;

        float bestDistance = float.MaxValue;
        int bestSegment = 0;
        float bestT = 0f;

        for (int i = 0; i < count; i++)
        {
            Vector3 a = points[i].position;
            Vector3 b = points[(i + 1) % count].position;
            Vector3 ab = b - a;

            float lengthSqr = ab.sqrMagnitude;
            float t = lengthSqr < 0.0001f
                ? 0f
                : Mathf.Clamp01(Vector3.Dot(position - a, ab) / lengthSqr);

            Vector3 closest = a + ab * t;
            float distance = (closest - position).sqrMagnitude;

            if (distance >= bestDistance) continue;

            bestDistance = distance;
            bestSegment = i;
            bestT = t;
        }

        return (bestSegment + bestT) / count;
    }
}
