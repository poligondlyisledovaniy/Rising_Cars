using UnityEngine;

/// <summary>
/// Звук машины: мотор, удары, визг шин.
/// Все клипы синтезируются в рантайме математикой, поэтому проекту
/// не нужны никакие .wav / .mp3 файлы.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class CarAudio : MonoBehaviour
{
    [Header("Общее")]
    [Tooltip("Максимальная скорость машины, м/с — от неё считается тон мотора")]
    public float maxSpeed = 20f;

    [Tooltip("Включается автоматически у машины игрока")]
    public bool isPlayer = false;

    [Header("Мотор")]
    [Range(0f, 1f)]
    public float engineVolume = 0.35f;

    [Tooltip("Тон на холостых (1 = базовые 55 Гц)")]
    public float idlePitch = 0.75f;

    [Tooltip("Тон на максимальной скорости")]
    public float maxPitch = 2.4f;

    [Header("Столкновения")]
    [Range(0f, 1f)]
    [Tooltip("Громкость удара. 0.85 было слишком громко — снижено до 0.35")]
    public float impactVolume = 0.15f;

    [Tooltip("Удар слабее этого (м/с) считается касанием и молчит")]
    public float minImpactSpeed = 1.5f;

    [Tooltip("Минимальная пауза между звуками ударов, сек")]
    public float impactCooldown = 0.12f;

    [Header("Визг шин")]
    public bool tyreSqueal = true;

    [Range(0f, 1f)]
    public float squealVolume = 0.15f;

    [Header("Свои звуки (необязательно)")]
    [Tooltip("Если пусто — берётся Assets/Resources/Audio/engine, иначе синтез")]
    public AudioClip engineOverride;

    [Tooltip("Если пусто — берётся Assets/Resources/Audio/impact, иначе синтез")]
    public AudioClip impactOverride;

    [Tooltip("Если пусто — берётся Assets/Resources/Audio/squeal, иначе синтез")]
    public AudioClip squealOverride;

    private const int SampleRate = 44100;

    // Синтезированные клипы общие на всю сцену: 4 машины не должны генерировать их по очереди.
    private static AudioClip sharedEngine;
    private static AudioClip sharedImpact;
    private static AudioClip sharedSqueal;

    private AudioClip engineClip;
    private AudioClip impactClip;
    private AudioClip squealClip;

    private Rigidbody rb;
    private AudioSource engineSource;
    private AudioSource impactSource;
    private AudioSource squealSource;

    private float previousSpeed;
    private float lastYaw;
    private float lastImpactTime = -10f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        engineClip = ResolveClip(engineOverride, "engine", ref sharedEngine, BuildEngineClip);
        impactClip = ResolveClip(impactOverride, "impact", ref sharedImpact, BuildImpactClip);
        squealClip = ResolveClip(squealOverride, "squeal", ref sharedSqueal, BuildSquealClip);
    }

    /// <summary>
    /// Порядок выбора клипа: назначенный в инспекторе → файл из
    /// Assets/Resources/Audio → синтез. То есть можно ничего не назначать,
    /// достаточно положить файлы с нужными именами.
    /// </summary>
    private static AudioClip ResolveClip(AudioClip overrideClip, string resourceName,
                                         ref AudioClip shared, System.Func<AudioClip> builder)
    {
        if (overrideClip != null) return overrideClip;

        AudioClip loaded = Resources.Load<AudioClip>("Audio/" + resourceName);
        if (loaded != null) return loaded;

        if (shared == null) shared = builder();
        return shared;
    }

    private void Start()
    {
        // Источники создаём в Start, а не в Awake: CarController ставит
        // isPlayer уже после AddComponent, то есть после нашего Awake.
        engineSource = CreateSource("Engine", engineClip, true);
        impactSource = CreateSource("Impact", impactClip, false);
        squealSource = CreateSource("Tyre", squealClip, true);

        engineSource.volume = 0f;
        squealSource.volume = 0f;

        engineSource.Play();
        squealSource.Play();

        lastYaw = transform.eulerAngles.y;
        previousSpeed = rb != null ? rb.linearVelocity.magnitude : 0f;
    }

    private AudioSource CreateSource(string sourceName, AudioClip clip, bool loop)
    {
        GameObject child = new GameObject(sourceName);
        child.transform.SetParent(transform, false);

        AudioSource source = child.AddComponent<AudioSource>();
        source.clip = clip;
        source.loop = loop;
        source.playOnAwake = false;

        // Мотор игрока — «в голове», мотор ботов — в пространстве.
        source.spatialBlend = isPlayer ? 0f : 1f;
        source.rolloffMode = AudioRolloffMode.Linear;
        source.minDistance = 6f;
        source.maxDistance = 70f;

        // Без этого мотор воет от любого изменения дистанции.
        source.dopplerLevel = 0f;

        return source;
    }

    private void Update()
    {
        if (engineSource == null) return;

        UpdateEngine();
        UpdateSqueal();

        if (isPlayer && Input.GetKeyDown(KeyCode.M))
            AudioListener.volume = AudioListener.volume > 0f ? 0f : 1f;
    }

    private void UpdateEngine()
    {
        float speed = rb != null ? rb.linearVelocity.magnitude : 0f;
        float speed01 = Mathf.Clamp01(speed / Mathf.Max(0.01f, maxSpeed));

        // Прирост скорости за кадр — по нему делаем «подхват» при разгоне.
        float acceleration = (speed - previousSpeed) / Mathf.Max(0.0001f, Time.deltaTime);
        previousSpeed = speed;

        float targetPitch = Mathf.Lerp(idlePitch, maxPitch, speed01);
        targetPitch += Mathf.Clamp01(acceleration / 20f) * 0.28f;

        engineSource.pitch = Mathf.Lerp(engineSource.pitch, targetPitch, 6f * Time.deltaTime);

        float targetVolume = engineVolume * Mathf.Lerp(0.5f, 1f, speed01);
        engineSource.volume = Mathf.Lerp(engineSource.volume, targetVolume, 5f * Time.deltaTime);
    }

    private void UpdateSqueal()
    {
        if (squealSource == null) return;

        float yawNow = transform.eulerAngles.y;
        float turnRate = Mathf.Abs(Mathf.DeltaAngle(lastYaw, yawNow)) / Mathf.Max(0.0001f, Time.deltaTime);
        lastYaw = yawNow;

        float speed01 = rb != null ? Mathf.Clamp01(rb.linearVelocity.magnitude / Mathf.Max(0.01f, maxSpeed)) : 0f;

        float target = 0f;
        if (tyreSqueal)
        {
            // Поворот должен быть резким И машина должна ехать.
            float sharp = Mathf.Clamp01((turnRate - 25f) / 80f);
            float fast = Mathf.Clamp01((speed01 - 0.35f) / 0.45f);
            target = squealVolume * sharp * fast;
        }

        squealSource.volume = Mathf.Lerp(squealSource.volume, target, 8f * Time.deltaTime);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (impactSource == null) return;

        float impact = collision.relativeVelocity.magnitude;
        if (impact < minImpactSpeed) return;
        if (Time.time - lastImpactTime < impactCooldown) return;

        lastImpactTime = Time.time;

        // Слабый тычок — тихий, сильный удар — на всю.
        float strength = Mathf.Clamp01((impact - minImpactSpeed) / 12f);
        float volume = impactVolume * Mathf.Lerp(0.35f, 1f, strength);

        impactSource.pitch = Random.Range(0.85f, 1.15f);
        impactSource.PlayOneShot(impactClip, volume);
    }

    // ───────────────────────── синтез клипов ─────────────────────────

    /// <summary>
    /// Рокот мотора: сумма обертонов на 55 Гц. Ровно 1 секунда и целое
    /// число периодов — поэтому петля стыкуется без щелчка.
    /// </summary>
    private static AudioClip BuildEngineClip()
    {
        const float baseHz = 55f;
        int samples = SampleRate;
        float[] data = new float[samples];

        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)SampleRate;
            float phase = 2f * Mathf.PI * baseHz * t;

            float tone = Mathf.Sin(phase) * 0.55f
                       + Mathf.Sin(phase * 2f) * 0.26f
                       + Mathf.Sin(phase * 3f) * 0.13f
                       + Mathf.Sin(phase * 5f) * 0.07f;

            // Пульсация «тарахтения» — тоже целое число периодов.
            float pulse = 0.78f + 0.22f * Mathf.Sin(phase);

            data[i] = tone * pulse * 0.7f;
        }

        AudioClip clip = AudioClip.Create("EngineLoop", samples, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    /// <summary>Удар: сглаженный шум (хруст металла) плюс низкий «бум».</summary>
    private static AudioClip BuildImpactClip()
    {
        const float duration = 0.45f;
        int samples = Mathf.RoundToInt(SampleRate * duration);
        float[] data = new float[samples];

        System.Random random = new System.Random(20240607);
        float filtered = 0f;

        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)SampleRate;
            float k = t / duration;

            float white = (float)(random.NextDouble() * 2.0 - 1.0);
            filtered += (white - filtered) * 0.18f;

            float noiseEnvelope = Mathf.Exp(-k * 7f);
            float thump = Mathf.Sin(2f * Mathf.PI * 65f * t) * Mathf.Exp(-k * 11f);

            data[i] = Mathf.Clamp((filtered * 1.6f + thump * 0.8f) * noiseEnvelope, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create("Impact", samples, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    /// <summary>
    /// Визг шин: полосовой шум. Концы клипа перекрёстно смешаны,
    /// чтобы петля не щёлкала.
    /// </summary>
    private static AudioClip BuildSquealClip()
    {
        int loopSamples = SampleRate / 2;
        int fade = SampleRate / 50; // 20 мс
        float[] raw = new float[loopSamples + fade];

        System.Random random = new System.Random(777);
        float low = 0f;

        for (int i = 0; i < raw.Length; i++)
        {
            float white = (float)(random.NextDouble() * 2.0 - 1.0);
            low += (white - low) * 0.35f;

            // Убираем низа — остаётся именно «скрип» резины.
            raw[i] = (white - low) * 0.6f;
        }

        float[] data = new float[loopSamples];
        for (int i = 0; i < loopSamples; i++) data[i] = raw[i];

        for (int i = 0; i < fade; i++)
        {
            float k = i / (float)fade;
            data[loopSamples - fade + i] = raw[loopSamples - fade + i] * (1f - k) + raw[i] * k;
        }

        AudioClip clip = AudioClip.Create("TyreSqueal", loopSamples, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
