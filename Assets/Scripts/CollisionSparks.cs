using UnityEngine;

/// <summary>
/// Искры при столкновении. Система частиц и материал создаются в рантайме,
/// поэтому в сцене ничего настраивать не нужно — компонент ставится сам.
/// </summary>
public class CollisionSparks : MonoBehaviour
{
    [Header("Когда сыплются искры")]
    [Tooltip("Удар слабее этого (м/с) искр не даёт")]
    public float minImpactSpeed = 2.5f;

    [Tooltip("Минимальная пауза между вспышками, сек")]
    public float cooldown = 0.1f;

    [Header("Внешний вид")]
    [Tooltip("Сколько искр даёт самый сильный удар")]
    public int maxSparks = 26;

    [Tooltip("Цвет в момент удара — почти белый, «раскалённый»")]
    public Color hotColor = new Color(1f, 0.95f, 0.70f);

    [Tooltip("Цвет, в который искра остывает")]
    public Color coolColor = new Color(1f, 0.30f, 0.03f);

    private static Material sparkMaterial;

    private ParticleSystem sparks;
    private float lastBurstTime = -10f;

    private void Awake()
    {
        sparks = CreateSystem();
    }

    private ParticleSystem CreateSystem()
    {
        GameObject child = new GameObject("Sparks");
        child.transform.SetParent(transform, false);

        ParticleSystem ps = child.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = ps.main;
        main.duration = 5f;

        // Система должна работать постоянно, иначе Emit() не даст частиц.
        // Всё, что нужно, включается вручную в момент удара.
        main.loop = true;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.6f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 8f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.10f);
        main.startColor = Color.white;
        main.gravityModifier = 1.4f;
        main.maxParticles = 400;

        // Автоматической эмиссии нет — только через Emit().
        ParticleSystem.EmissionModule emission = ps.emission;
        emission.enabled = false;

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.enabled = false;

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;

        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(hotColor, 0f),
                new GradientColorKey(coolColor, 0.35f),
                new GradientColorKey(new Color(coolColor.r * 0.5f, coolColor.g * 0.25f, 0f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 0.55f),
                new GradientAlphaKey(0f, 1f)
            });
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

        ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0.15f));

        ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();

        // Полоски по направлению полёта — так искры читаются как «искры»,
        // а не как круглые точки.
        renderer.renderMode = ParticleSystemRenderMode.Stretch;
        renderer.alignment = ParticleSystemRenderSpace.World;
        renderer.lengthScale = 2.5f;
        renderer.velocityScale = 0.12f;
        renderer.sharedMaterial = SparkMaterial();

        ps.Play();
        return ps;
    }

    /// <summary>
    /// Общий материал на все машины. Аддитивное смешивание: искры
    /// складываются с фоном и выглядят светящимися.
    /// </summary>
    private static Material SparkMaterial()
    {
        if (sparkMaterial != null) return sparkMaterial;

        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
        if (shader == null) return null;

        Material material = new Material(shader);
        material.name = "Sparks (runtime)";

        if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", Texture2D.whiteTexture);
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Color.white);

        if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 1f);
        if (material.HasProperty("_Blend")) material.SetFloat("_Blend", 2f);
        if (material.HasProperty("_SrcBlend")) material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.One);
        if (material.HasProperty("_DstBlend")) material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
        if (material.HasProperty("_ZWrite")) material.SetFloat("_ZWrite", 0f);
        if (material.HasProperty("_Cull")) material.SetFloat("_Cull", (float)UnityEngine.Rendering.CullMode.Off);

        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

        sparkMaterial = material;
        return sparkMaterial;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (sparks == null) return;
        if (Time.time - lastBurstTime < cooldown) return;

        float impact = collision.relativeVelocity.magnitude;
        if (impact < minImpactSpeed) return;
        if (collision.contactCount == 0) return;

        lastBurstTime = Time.time;

        float strength = Mathf.Clamp01((impact - minImpactSpeed) / 12f);
        ContactPoint contact = collision.GetContact(0);

        Burst(contact.point, contact.normal, strength);
    }

    /// <summary>Выброс искр в точке контакта.</summary>
    public void Burst(Vector3 worldPoint, Vector3 worldNormal, float strength)
    {
        if (sparks == null) return;
        if (!sparks.isPlaying) sparks.Play();

        strength = Mathf.Clamp01(strength);
        int count = Mathf.Max(4, Mathf.RoundToInt(maxSparks * Mathf.Lerp(0.3f, 1f, strength)));

        for (int i = 0; i < count; i++)
        {
            // Искры летят в стороны от поверхности удара, а не строго по нормали.
            Vector3 direction = worldNormal + Random.onUnitSphere * 0.9f;
            if (direction.sqrMagnitude < 0.0001f) direction = worldNormal;
            direction.Normalize();

            ParticleSystem.EmitParams emit = new ParticleSystem.EmitParams
            {
                position = worldPoint + Random.insideUnitSphere * 0.10f,
                velocity = direction * Random.Range(3f, 10f) * Mathf.Lerp(0.6f, 1.4f, strength),
                startLifetime = Random.Range(0.22f, 0.55f),
                startSize = Random.Range(0.03f, 0.09f),
                startColor = Color.white
            };

            sparks.Emit(emit, 1);
        }
    }
}
