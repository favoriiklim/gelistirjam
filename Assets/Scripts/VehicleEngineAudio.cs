using UnityEngine;

/// <summary>
/// Araç motor sesi. Hem oyuncu tankında hem düşman araçlarında kullanılır:
/// TankController varsa hızı ondan alır, yoksa konum değişiminden ölçer.
///
/// Klipler dikişsiz döngüye SeamlessLoop ile çevrilir; ham kayıtların
/// başı ve sonu uyuşmasa bile kopukluk duyulmaz.
/// </summary>
public class VehicleEngineAudio : MonoBehaviour
{
    [Header("Klipler")]
    [Tooltip("Düşük devir sesi. Oyuncu tankında iç rölanti kaydı, " +
             "düşman araçlarında dıştan alınmış kayıt.")]
    [SerializeField] private AudioClip lowClip;

    [Tooltip("Yüksek devir sesi. Boş bırakılırsa sadece düşük devir kullanılır " +
             "ve hız farkı yalnızca perdeyle verilir.")]
    [SerializeField] private AudioClip highClip;

    [Tooltip("Palet gıcırtısı. Sadece araç hareket ederken duyulur. İsteğe bağlı.")]
    [SerializeField] private AudioClip trackClip;

    [Header("Seviye")]
    [Range(0f, 1f)]
    [SerializeField] private float idleVolume = 0.35f;

    [Range(0f, 1f)]
    [SerializeField] private float maxVolume = 0.85f;

    [Range(0f, 1f)]
    [SerializeField] private float trackVolume = 0.5f;

    [Header("Perde")]
    [Tooltip("İki katman varken perde değişimi hafif tutulmalı; " +
             "asıl hız hissini katman geçişi verir.")]
    [SerializeField] private float idlePitch = 0.9f;

    [SerializeField] private float maxPitch = 1.1f;

    [Header("Uzamsal ayar")]
    [Tooltip("0 = her yerden aynı duyulur (oyuncu tankı). " +
             "1 = konuma bağlı duyulur (düşman araçları).")]
    [Range(0f, 1f)]
    [SerializeField] private float spatialBlend;

    [Tooltip("Sesin kesildiği mesafe. Düşman araçlarında görüş mesafesinden " +
             "BÜYÜK olmalı: düşmanı görmeden önce duymalısın.")]
    [SerializeField] private float maxDistance = 120f;

    [Header("Yumuşatma")]
    [Tooltip("Düşük değer sesi sinirli titretir.")]
    [SerializeField] private float smoothing = 6f;

    private SeamlessLoop lowLoop;
    private SeamlessLoop highLoop;
    private SeamlessLoop trackLoop;

    private TankController tankController;
    private Vector3 lastPosition;
    private float smoothedSpeed;

    /// <summary>Konumdan ölçüm yapılırken azami sayılacak hız (m/s).</summary>
    private const float MeasuredSpeedReference = 6f;

    private void Awake()
    {
        tankController = GetComponent<TankController>();
        lastPosition = transform.position;

        lowLoop = new SeamlessLoop(transform, "EngineLow", lowClip, spatialBlend, maxDistance);
        highLoop = new SeamlessLoop(transform, "EngineHigh", highClip, spatialBlend, maxDistance);
        trackLoop = new SeamlessLoop(transform, "Tracks", trackClip, spatialBlend, maxDistance);
    }

    private void Update()
    {
        float targetSpeed = ReadNormalizedSpeed();
        smoothedSpeed = Mathf.Lerp(smoothedSpeed, targetSpeed, smoothing * Time.deltaTime);

        // Oyun bitince motor sussun; donmuş ekranda ses devam etmesi kötü hissettirir.
        bool gameOver = GameManager.Instance != null && GameManager.Instance.State != GameState.Playing;
        float gate = gameOver ? 0f : 1f;

        float level = Mathf.Lerp(idleVolume, maxVolume, smoothedSpeed) * gate;
        float pitch = Mathf.Lerp(idlePitch, maxPitch, smoothedSpeed);

        if (highLoop.IsValid)
        {
            // İki katman arasında eşit güçlü geçiş: toplam seviye sabit kalır.
            float blend = smoothedSpeed;
            lowLoop.SetVolume(level * Mathf.Cos(blend * Mathf.PI * 0.5f));
            highLoop.SetVolume(level * Mathf.Sin(blend * Mathf.PI * 0.5f));
        }
        else
        {
            lowLoop.SetVolume(level);
        }

        lowLoop.SetPitch(pitch);
        highLoop.SetPitch(pitch);

        trackLoop.SetVolume(smoothedSpeed * trackVolume * gate);

        lowLoop.Update();
        highLoop.Update();
        trackLoop.Update();
    }

    /// <summary>
    /// Oyuncu tankında hazır normalize hız var. Düşman araçlarında
    /// TankController yok, o yüzden hız konum değişiminden ölçülür.
    /// </summary>
    private float ReadNormalizedSpeed()
    {
        if (tankController != null)
            return tankController.NormalizedSpeed;

        // Time.timeScale sıfırken bölme patlamasın.
        if (Time.deltaTime <= 0f)
            return smoothedSpeed;

        float measured = (transform.position - lastPosition).magnitude / Time.deltaTime;
        lastPosition = transform.position;

        return Mathf.Clamp01(measured / MeasuredSpeedReference);
    }
}
