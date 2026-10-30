using UnityEngine;

/// <summary>
/// Машина игрока. Работает через физическое тело, поэтому машины
/// сталкиваются друг с другом и с объектами на карте.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class CarController : MonoBehaviour
{
    [Header("Скорости")]
    [Tooltip("Максимальная скорость вперёд, м/с")]
    public float speed = 20f;

    [Tooltip("Максимальная скорость назад, м/с")]
    public float reverseSpeed = 10f;

    [Tooltip("Скорость поворота, градусов в секунду")]
    public float turnSpeed = 100f;

    [Header("Разгон")]
    [Tooltip("Разгон, м/с²")]
    public float acceleration = 25f;

    [Tooltip("Торможение и сброс газа, м/с²")]
    public float braking = 40f;

    [Header("Камера")]
    [Tooltip("Камера сама едет за машиной. Если выключить — останется как в сцене (ребёнком машины).")]
    public bool followCamera = true;

    [Tooltip("Смещение камеры относительно машины: назад и вверх")]
    public Vector3 cameraOffset = new Vector3(0f, 6f, -12f);

    [Tooltip("Плавность движения камеры. Больше — жёстче.")]
    public float cameraPositionSmooth = 8f;

    [Tooltip("Плавность поворота камеры")]
    public float cameraRotationSmooth = 6f;

    [Tooltip("Высота точки, на которую смотрит камера")]
    public float cameraLookHeight = 1.5f;

    private Rigidbody rb;
    private float currentSpeed;
    private float yaw;
    private Transform cameraTransform;

    /// <summary>Скорость для спидометра, км/ч.</summary>
    public float SpeedKmh => Mathf.Abs(currentSpeed) * 3.6f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        // Без этого машины не сталкиваются: kinematic-тела проходят друг сквозь друга.
        rb.isKinematic = false;
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        // Не даём машине заваливаться набок и проваливаться под трассу.
        rb.constraints = RigidbodyConstraints.FreezeRotationX
                       | RigidbodyConstraints.FreezeRotationZ
                       | RigidbodyConstraints.FreezePositionY;

        yaw = transform.eulerAngles.y;

        // Звук мотора и ударов. Компонент создаётся сам, сцену править не нужно.
        CarAudio carAudio = GetComponent<CarAudio>();
        if (carAudio == null) carAudio = gameObject.AddComponent<CarAudio>();
        carAudio.isPlayer = true;
        carAudio.maxSpeed = speed;

        // Искры при столкновениях.
        if (GetComponent<CollisionSparks>() == null) gameObject.AddComponent<CollisionSparks>();

        if (followCamera) DetachCamera();
    }

    /// <summary>
    /// Камера в сцене — ребёнок машины, поэтому она повторяет каждый
    /// микро-рывок физики и картинка трясётся. Отцепляем и водим сглаженно.
    /// </summary>
    private void DetachCamera()
    {
        Camera cam = GetComponentInChildren<Camera>();
        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        cameraTransform = cam.transform;
        cameraTransform.SetParent(null, true);
    }

    private void Update()
    {
        float turn = 0f;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) turn -= 1f;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) turn += 1f;

        bool gas = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow);
        bool back = Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow);

        // Плавный разгон и торможение вместо мгновенного включения скорости.
        float target = gas ? speed : (back ? -reverseSpeed : 0f);
        float rate = Mathf.Abs(target) > Mathf.Abs(currentSpeed) ? acceleration : braking;
        currentSpeed = Mathf.MoveTowards(currentSpeed, target, rate * Time.deltaTime);

        // Руль работает только на ходу и тем слабее, чем меньше скорость.
        if (turn != 0f && Mathf.Abs(currentSpeed) > 0.2f)
        {
            float grip = Mathf.Clamp01(Mathf.Abs(currentSpeed) / speed);
            yaw += turn * turnSpeed * Mathf.Sign(currentSpeed) * grip * Time.deltaTime;
        }
    }

    private void FixedUpdate()
    {
        Quaternion rotation = Quaternion.Euler(0f, yaw, 0f);

        // Гасим угловую скорость, иначе столкновения начинают крутить машину
        // и камеру вместе с ней.
        rb.angularVelocity = Vector3.zero;
        rb.MoveRotation(rotation);

        rb.linearVelocity = rotation * Vector3.forward * currentSpeed;
    }

    private void LateUpdate()
    {
        if (cameraTransform == null) return;

        Vector3 desiredPosition = transform.TransformPoint(cameraOffset);

        float positionK = 1f - Mathf.Exp(-cameraPositionSmooth * Time.deltaTime);
        cameraTransform.position = Vector3.Lerp(cameraTransform.position, desiredPosition, positionK);

        Vector3 lookPoint = transform.position + Vector3.up * cameraLookHeight;
        Vector3 direction = lookPoint - cameraTransform.position;

        if (direction.sqrMagnitude < 0.0001f) return;

        Quaternion desiredRotation = Quaternion.LookRotation(direction);
        float rotationK = 1f - Mathf.Exp(-cameraRotationSmooth * Time.deltaTime);
        cameraTransform.rotation = Quaternion.Slerp(cameraTransform.rotation, desiredRotation, rotationK);
    }

    /// <summary>Мгновенная остановка (например, после финиша).</summary>
    public void Stop()
    {
        currentSpeed = 0f;
    }
}
