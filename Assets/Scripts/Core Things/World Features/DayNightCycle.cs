using UnityEngine;

public class DayNightCycle : MonoBehaviour
{
    [Header("Настройки времени")]
    [Tooltip("Длительность дня в секундах (дефолт 120 сек)")]
    public float dayDuration = 120f;

    [Header("Начальное время (0 = полночь, 0.25 = рассвет, 0.5 = полдень, 0.75 = закат)")]
    [Range(0f, 1f)]
    public float timeOfDay = 0.25f;

    [Header("Цвета света")]
    public Gradient lightColor;
    public AnimationCurve lightIntensity = AnimationCurve.Linear(0, 0, 1, 1);

    [Header("Интенсивность света как бы")]
    public float maxIntensity = 1.5f;

    private Light _sun;

    private void Awake()
    {
        _sun = GetComponent<Light>();

        if (lightColor == null || lightColor.colorKeys.Length == 0)
        {
            lightColor = CreateDefaultGradient();
        }
    }

    private void Update()
    {
        timeOfDay += Time.deltaTime / dayDuration;
        if (timeOfDay >= 1f) timeOfDay -= 1f;

        UpdateSun();
    }

    private void UpdateSun()
    {
        float sunAngle = (timeOfDay * 360f) - 90f;
        transform.localRotation = Quaternion.Euler(sunAngle, 170f, 0f);

        if (_sun != null)
        {
            _sun.color = lightColor.Evaluate(timeOfDay);
            _sun.intensity = lightIntensity.Evaluate(timeOfDay) * maxIntensity;
        }
    }
    private static Gradient CreateDefaultGradient()
    {
        var grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(new Color(0.05f, 0.05f, 0.15f), 0.00f),  // полночь
                new GradientColorKey(new Color(1.00f, 0.50f, 0.20f), 0.23f),  // рассвет
                new GradientColorKey(new Color(1.00f, 0.95f, 0.85f), 0.30f),  // утро
                new GradientColorKey(new Color(1.00f, 1.00f, 1.00f), 0.50f),  // полдень
                new GradientColorKey(new Color(1.00f, 0.60f, 0.20f), 0.75f),  // закат
                new GradientColorKey(new Color(0.05f, 0.05f, 0.15f), 1.00f),  // ночь
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 1f),
            }
        );
        return grad;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (_sun == null) _sun = GetComponent<Light>();
        UpdateSun();
    }
#endif
}