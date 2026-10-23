using UnityEngine;

public class BotCar : MonoBehaviour
{
    public Transform[] waypoints;
    public Transform player;

    [Header("Настройки скорости")]
    public float baseSpeed = 18f;
    public float maxSpeed = 28f;
    public float minSpeed = 12f;

    [Header("Настройки полосы (чтобы не слипались)")]
    public float laneOffset = 0f;      // -2 для левой, 0 для центра, 2 для правой

    private int currentWaypoint = 0;
    private float currentSpeed;

    void Start()
    {
        currentSpeed = baseSpeed;
        // Если игрок не привязан вручную, ищем его по тегу
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }
    }

    void Update()
    {
        if (waypoints.Length == 0 || player == null) return;

        // --- УМНАЯ РЕЗИНОВАЯ ЛЕНТА ---
        float distToPlayer = Vector3.Distance(transform.position, player.position);

        // Если бот отстал больше чем на 25 метров - жмем газ
        if (distToPlayer > 25f)
            currentSpeed = Mathf.Lerp(currentSpeed, maxSpeed, Time.deltaTime * 2f);
        // Если бот слишком близко (перегнал) - сбрасываем скорость
        else if (distToPlayer < 10f)
            currentSpeed = Mathf.Lerp(currentSpeed, minSpeed, Time.deltaTime * 2f);
        // Если едем рядом - держим средний темп
        else
            currentSpeed = Mathf.Lerp(currentSpeed, baseSpeed, Time.deltaTime * 2f);

        // --- ДВИЖЕНИЕ С УЧЕТОМ ПОЛОСЫ ---
        Vector3 targetPos = waypoints[currentWaypoint].position;

        // Смещаем цель вбок, чтобы каждый бот ехал по своей траектории
        Vector3 dirToTarget = (targetPos - transform.position).normalized;
        Vector3 rightDir = Vector3.Cross(Vector3.up, dirToTarget);
        targetPos += rightDir * laneOffset;

        Vector3 dir = targetPos - transform.position;
        dir.y = 0;

        if (dir.magnitude < 5f)
        {
            currentWaypoint = (currentWaypoint + 1) % waypoints.Length;
        }
        else
        {
            Quaternion targetRot = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 4f);

            transform.position += transform.forward * currentSpeed * Time.deltaTime;
        }
    }
}