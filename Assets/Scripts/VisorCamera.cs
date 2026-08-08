using UnityEngine;

/// <summary>
/// Sürücü vizörü kamerası. Hıza bağlı hafif bir sarsıntı uygular.
/// Dar yarıktan bakarken hareket algısı düşer; bu sarsıntı olmadan
/// oyun "kayan bir fotoğraf" gibi hissedilir.
/// </summary>
public class VisorCamera : MonoBehaviour
{
    [SerializeField] private TankController tankController;

    [Header("Sarsıntı")]
    [Tooltip("Azami hızdaki konum sapması (metre). Küçük tut; 0.05 üstü mide bulandırır.")]
    [SerializeField] private float shakeAmount = 0.03f;

    [Tooltip("Azami hızdaki yalpalama açısı (derece).")]
    [SerializeField] private float shakeRoll = 0.6f;

    [SerializeField] private float shakeFrequency = 8f;

    [Tooltip("Tank dururken bile kalan taban sarsıntı oranı (rölanti titreşimi).")]
    [Range(0f, 1f)]
    [SerializeField] private float idleShake = 0.15f;

    [Header("Darbe")]
    [Tooltip("Dışarıdan gelen sarsıntının saniyede ne kadar söndüğü.")]
    [SerializeField] private float impulseDecay = 2.5f;

    private Vector3 basePosition;
    private Quaternion baseRotation;
    private float impulse;

    /// <summary>
    /// Anlık sarsıntı ekler. Yakına düşen mermi gibi olaylar için;
    /// hız sarsıntısının üstüne biner ve zamanla söner.
    /// </summary>
    public void AddShake(float amount)
    {
        impulse = Mathf.Max(impulse, amount);
    }

    private void Awake()
    {
        // Vizörün asıl konumu; sarsıntı bu değerin etrafında salınır.
        basePosition = transform.localPosition;
        baseRotation = transform.localRotation;

        if (tankController == null)
            tankController = GetComponentInParent<TankController>();
    }

    private void LateUpdate()
    {
        float speed = tankController != null ? tankController.NormalizedSpeed : 0f;

        impulse = Mathf.MoveTowards(impulse, 0f, impulseDecay * Time.deltaTime);

        // Darbe hız sarsıntısının üstüne biniyor; yakına düşen mermi
        // dururken de hissedilmeli.
        float intensity = Mathf.Lerp(idleShake, 1f, speed) + impulse;

        // Perlin gürültüsü rastgeleye göre daha yumuşak salınır; Random.value
        // kullanmak titreşimi sinirli ve dijital gösterir.
        float t = Time.time * shakeFrequency;
        float noiseX = Mathf.PerlinNoise(t, 0f) - 0.5f;
        float noiseY = Mathf.PerlinNoise(0f, t) - 0.5f;
        float noiseRoll = Mathf.PerlinNoise(t, t) - 0.5f;

        transform.localPosition = basePosition
                                  + new Vector3(noiseX, noiseY, 0f) * (shakeAmount * intensity * 2f);

        transform.localRotation = baseRotation
                                  * Quaternion.Euler(0f, 0f, noiseRoll * shakeRoll * intensity * 2f);
    }
}
