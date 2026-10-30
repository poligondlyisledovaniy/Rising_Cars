/// <summary>Одна строка таблицы результатов.</summary>
public class RacerInfo
{
    public string name;
    public bool isPlayer;

    /// <summary>Сколько кругов проехал.</summary>
    public int lapsDone;

    /// <summary>Полный прогресс в кругах (круги + положение на круге).</summary>
    public float progress;

    public bool finished;
    public float finishTime;
}
