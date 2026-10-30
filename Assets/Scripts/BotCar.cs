using UnityEngine;

/// <summary>
/// Машина соперника. Едет по вейпоинтам и подтягивается к игроку
/// («резиновая лента»): отстал — ускоряется, уехал вперёд — сбрасывает.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class BotCar : MonoBehaviour
{
    [Header("Маршрут")]
    public Transform[] waypoints;
    public Transform player;

    [Header("Скорость")]
    [Tooltip("Базовая скорость бота, м/с")]
    public float baseSpeed = 18f;

    [Header("Резиновая лента")]
    [Tooltip("Максимальное ускорение, если бот отстал (1.35 = +35%)")]
    [Range(1f, 2f)]
    public float maxBoost = 1.35f;

    [Tooltip("Максимальный сброс скорости, если бот уехал вперёд (0.75 = -25%)")]
    [Range(0.3f, 1f)]
    public float maxSlowdown = 0.75f;

    [Tooltip("Отставание в кругах, при котором лента выходит на максимум")]
    public float rubberRange = 0.4f;

    [Header("Вождение")]
    [Tooltip("Смещение от осевой линии: -2 слева, 0 по центру, 2 справа")]
    public float laneOffset = 0f;

    [Tooltip("Скорость поворота к следующей точке")]
    public float turnRate = 4f;

    [Tooltip("С какого расстояния точка считается пройденной")]
    public float pointReachDistance = 6f;

    /// <summary>Сколько кругов бот уже проехал.</summary>
    public int LapsDone { get; private set; }

    /// <summary>Бот финишировал.</summary>
    public bool Finished { get; private set; }

    /// <summary>Время финиша (в секундах от старта гонки).</summary>
    public float FinishTime { get; private set; }

    /// <summary>Имя для таблицы результатов.</summary>
    public string RacerName { get; private set; }

    /// <summary>Полный прогресс в кругах — по нему считается место.</summary>
    public float Progress => LapsDone + TrackLoop.Fraction(transform.position);

    private Rigidbody rb;
    private float currentSpeed;
    private float yaw;
    private int currentWaypoint;

    private float stuckTimer;
    private Vector3 lastPosition;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        rb.isKinematic = false;
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.constraints = RigidbodyConstraints.FreezeRotationX
                       | RigidbodyConstraints.FreezeRotationZ
                       | RigidbodyConstraints.FreezePositionY;

        RacerName = gameObject.name.Replace("BotCar", "Бот").Trim();
        if (string.IsNullOrEmpty(RacerName)) RacerName = "Бот";

        yaw = transform.eulerAngles.y;
        lastPosition = transform.position;

        // Звук мотора и ударов. Компонент создаётся сам, сцену править не нужно.
        CarAudio carAudio = GetComponent<CarAudio>();
        if (carAudio == null) carAudio = gameObject.AddComponent<CarAudio>();
        carAudio.isPlayer = false;
        carAudio.maxSpeed = baseSpeed;

        // Искры при столкновениях.
        if (GetComponent<CollisionSparks>() == null) gameObject.AddComponent<CollisionSparks>();
    }

    private void Start()
    {
        currentSpeed = baseSpeed;

        if (player == null)
        {
            GameObject found = GameObject.FindGameObjectWithTag("Player");
            if (found != null) player = found.transform;
        }
    }

    private void Update()
    {
        if (waypoints == null || waypoints.Length == 0) return;

        float targetSpeed = baseSpeed * RubberBandFactor();
        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, 12f * Time.deltaTime);

        Vector3 target = TargetPosition(currentWaypoint);
        Vector3 direction = target - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude <= pointReachDistance * pointReachDistance)
        {
            AdvanceWaypoint();
            return;
        }

        Quaternion desired = Quaternion.LookRotation(direction);
        yaw = Mathf.LerpAngle(yaw, desired.eulerAngles.y, turnRate * Time.deltaTime);
    }

    private void FixedUpdate()
    {
        Quaternion rotation = Quaternion.Euler(0f, yaw, 0f);

        rb.angularVelocity = Vector3.zero;
        rb.MoveRotation(rotation);
        rb.linearVelocity = rotation * Vector3.forward * currentSpeed;

        DetectStuck();
    }

    /// <summary>
    /// Множитель скорости. Если бот позади игрока — больше единицы,
    /// если впереди — меньше. Чем больше разрыв, тем сильнее эффект.
    /// </summary>
    private float RubberBandFactor()
    {
        // Финишировавший бот просто катится и никому не мешает.
        if (Finished) return 0.6f;

        GameManager gm = GameManager.Instance;
        if (gm == null || !TrackLoop.Ready) return 1f;

        // Положительная разница — бот позади игрока.
        float gap = gm.PlayerProgress - Progress;
        if (Mathf.Abs(gap) < 0.005f) return 1f;

        float strength = Mathf.Clamp01(Mathf.Abs(gap) / Mathf.Max(0.01f, rubberRange));

        return gap > 0f
            ? Mathf.Lerp(1f, maxBoost, strength)
            : Mathf.Lerp(1f, maxSlowdown, strength);
    }

    /// <summary>Точка маршрута с учётом смещения в полосу.</summary>
    private Vector3 TargetPosition(int index)
    {
        Vector3 point = waypoints[index].position;
        if (Mathf.Approximately(laneOffset, 0f)) return point;

        Vector3 next = waypoints[(index + 1) % waypoints.Length].position;
        Vector3 forward = next - point;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.01f) return point;

        Vector3 right = Vector3.Cross(Vector3.up, forward.normalized);
        return point + right * laneOffset;
    }

    private void AdvanceWaypoint()
    {
        currentWaypoint = (currentWaypoint + 1) % waypoints.Length;

        // Пересечение стартовой точки = законченный круг.
        if (currentWaypoint != 0 || Finished) return;

        LapsDone++;

        GameManager gm = GameManager.Instance;
        if (gm != null && LapsDone >= gm.totalLaps)
        {
            Finished = true;
            FinishTime = gm.RaceTime;
        }
    }

    /// <summary>
    /// Страховка: если бот застрял в дереве или упёрся в другую машину,
    /// аккуратно выталкиваем его к следующей точке.
    /// </summary>
    private void DetectStuck()
    {
        if ((transform.position - lastPosition).sqrMagnitude > 0.01f)
            stuckTimer = 0f;
        else
            stuckTimer += Time.fixedDeltaTime;

        lastPosition = transform.position;

        if (stuckTimer < 1.5f) return;

        Vector3 direction = TargetPosition(currentWaypoint) - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude > 0.01f)
            rb.position += direction.normalized * 0.5f;

        stuckTimer = 0f;
    }
}
