using UnityEngine;

/// <summary>
/// Aracın arkasında hıza bağlı toz bulutu. Hem düşman araçlarında hem
/// oyuncu tankında kullanılabilir.
///
/// Düşman araçlarında oynanışa doğrudan katkısı var: hareket eden bir
/// tankı, tankın kendisini görmeden önce tozundan fark edersin. Dar
/// vizörlü bir oyunda bu, sesle birlikte üçüncü bilgi kanalıdır.
/// </summary>
public class DustTrail : MonoBehaviour
{
    [Tooltip("Aracın arkasına yerleştirilmiş parçacık sistemi. " +
             "Looping açık, Play On Awake açık olmalı; emisyonu bu script sürer.")]
    [SerializeField] private ParticleSystem dust;

    [Tooltip("Tam hızdaki saniyelik parçacık sayısı.")]
    [SerializeField] private float maxEmission = 35f;

    [Tooltip("Bu normalize hızın altında toz hiç çıkmaz. " +
             "Sıfır bırakılırsa duran araç bile tütmeye devam eder.")]
    [Range(0f, 1f)]
    [SerializeField] private float speedThreshold = 0.08f;

    [Tooltip("Emisyon değişiminin yumuşatılması.")]
    [SerializeField] private float smoothing = 4f;

    private ParticleSystem.EmissionModule emission;
    private TankController tankController;

    private Vector3 lastPosition;
    private float smoothedSpeed;

    /// <summary>Konumdan ölçüm yapılırken azami sayılacak hız (m/s).</summary>
    private const float MeasuredSpeedReference = 6f;

    private void Awake()
    {
        tankController = GetComponent<TankController>();
        lastPosition = transform.position;

        if (dust != null)
            emission = dust.emission;
    }

    private void Update()
    {
        if (dust == null)
            return;

        float targetSpeed = ReadNormalizedSpeed();
        smoothedSpeed = Mathf.Lerp(smoothedSpeed, targetSpeed, smoothing * Time.deltaTime);

        // Eşiğin altındaki hız tamamen kesilir, üstü 0-1 aralığına yeniden yayılır.
        float amount = smoothedSpeed <= speedThreshold
            ? 0f
            : Mathf.InverseLerp(speedThreshold, 1f, smoothedSpeed);

        emission.rateOverTime = amount * maxEmission;
    }

    /// <summary>
    /// Oyuncu tankında hazır normalize hız var. Düşman araçlarında
    /// TankController yok, o yüzden hız konum değişiminden ölçülür.
    /// </summary>
    private float ReadNormalizedSpeed()
    {
        if (tankController != null)
            return tankController.NormalizedSpeed;

        if (Time.deltaTime <= 0f)
            return smoothedSpeed;

        float measured = (transform.position - lastPosition).magnitude / Time.deltaTime;
        lastPosition = transform.position;

        return Mathf.Clamp01(measured / MeasuredSpeedReference);
    }
}
