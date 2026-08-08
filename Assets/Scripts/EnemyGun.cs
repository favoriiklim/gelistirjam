using UnityEngine;

/// <summary>
/// Düşman topu. EnemySpotter nişan almayı bitirince ateş eder ve
/// atışın isabet edip etmediğine karar verir.
///
/// Gerçek mermi, balistik ve çarpışma fiziği yok: atış anında tek bir
/// olasılık hesabı yapılır. 48 saatlik sürede balistik yazmak, oyuna
/// hiçbir şey katmadan günler yiyen bir iştir.
/// </summary>
[RequireComponent(typeof(EnemySpotter))]
public class EnemyGun : MonoBehaviour
{
    [Tooltip("Namlu ucu. Ses ve iz çizgisi buradan çıkar. " +
             "Boş bırakılırsa aracın kendisi kullanılır.")]
    [SerializeField] private Transform muzzle;

    [Header("İsabet")]
    [Tooltip("Dipten, oyuncu dururken isabet olasılığı.")]
    [Range(0f, 1f)]
    [SerializeField] private float baseAccuracy = 0.85f;

    [Tooltip("Azami menzilde isabetin düştüğü oran.")]
    [Range(0f, 1f)]
    [SerializeField] private float longRangeAccuracy = 0.35f;

    [Tooltip("Oyuncu tam hızdayken isabetin düştüğü oran. " +
             "Düşük tutmak 'görüldüysen kaç' davranışını ödüllendirir.")]
    [Range(0f, 1f)]
    [SerializeField] private float movingAccuracy = 0.45f;

    [Tooltip("İlk atış her zaman ışar. Oyuncuya bir uyarı hakkı verir; " +
             "gizlilik oyunlarında adalet hissini en ucuza sağlayan kural budur.")]
    [SerializeField] private bool firstShotAlwaysMisses = true;

    [Header("Zamanlama")]
    [Tooltip("İki atış arasındaki doldurma süresi.")]
    [SerializeField] private float reloadTime = 4f;

    [Tooltip("Işkalayınca nişan bu değere iner. Sıfırlanmaz; düşman " +
             "oyuncunun yerini zaten biliyor, sadece yeniden nişan alıyor.")]
    [Range(0f, 1f)]
    [SerializeField] private float suspicionAfterMiss = 0.55f;

    [Tooltip("Bekleyen atış, şüphe bu değerin altına düşerse iptal edilir. " +
             "Kulenin devreye girme eşiğinden biraz düşük tutulmalı.")]
    [Range(0f, 1f)]
    [SerializeField] private float cancelShotSuspicion = 0.3f;

    [Header("Geri bildirim")]
    [SerializeField] private AudioClip fireClip;

    [Tooltip("Yakına düşen merminin sesi. Oyuncunun kaçması gerektiğini " +
             "anladığı an bu sestir.")]
    [SerializeField] private AudioClip missImpactClip;

    [Range(0f, 1f)]
    [SerializeField] private float fireVolume = 0.7f;

    [Tooltip("Işkalayan atışın kamerada yarattığı sarsıntı.")]
    [SerializeField] private float missShake = 0.8f;

    /// <summary>Oyuncu vurulduğunda tetiklenir. GameManager dinler.</summary>
    public static event System.Action PlayerHit;

    /// <summary>
    /// Ateş edildiğinde tetiklenir: isabet edildi mi ve mermi nereye düştü.
    /// Görsel efektler bunu dinler; ateş kararı bu sınıfta, çizim başka yerde.
    /// </summary>
    public event System.Action<bool, Vector3> Fired;

    private EnemySpotter spotter;
    private EnemyTurret turret;
    private AudioSource fireSource;
    private VisorCamera visorCamera;

    private float reloadTimer;
    private int shotsFired;

    // Nişan tamamlandı ama kule henüz hizalanmadı: atış sıraya alınır.
    private bool shotPending;

    private Transform Muzzle => muzzle != null ? muzzle : transform;

    private void Awake()
    {
        spotter = GetComponent<EnemySpotter>();
        turret = GetComponent<EnemyTurret>();

        fireSource = Muzzle.gameObject.AddComponent<AudioSource>();
        fireSource.playOnAwake = false;
        fireSource.loop = false;
        // Atış sesi konuma bağlı olmalı: hangi yönden geldiği bilgi taşır.
        fireSource.spatialBlend = 1f;
        fireSource.rolloffMode = AudioRolloffMode.Linear;
        fireSource.maxDistance = 200f;
    }

    private void OnEnable()
    {
        spotter.AimComplete += HandleAimComplete;
    }

    private void OnDisable()
    {
        spotter.AimComplete -= HandleAimComplete;
    }

    private void Update()
    {
        if (reloadTimer > 0f)
            reloadTimer -= Time.deltaTime;

        if (!shotPending)
            return;

        // Oyuncu bu arada kaçtıysa bekleyen atışı iptal et. Aksi hâlde
        // atış askıda kalır ve düşman çok sonra, sebepsiz yere ateş eder.
        if (spotter.Suspicion < cancelShotSuspicion)
        {
            shotPending = false;

            // Sayacı yeniden kullanılabilir hâle getir; bu yapılmazsa
            // nişan bir daha hiç tamamlanmaz ve düşman kalıcı olarak susar.
            spotter.ResetAfterShot(spotter.Suspicion);
            return;
        }

        if (reloadTimer > 0f)
            return;

        // Kule varsa hizalanmasını bekle. Bu bekleme oyuncunun kaçma
        // penceresidir; kulenin dönüşü atışın gözle görülür fitilidir.
        if (turret != null && !turret.IsAimedAtPlayer)
            return;

        shotPending = false;
        Fire();
    }

    private void HandleAimComplete(EnemySpotter source)
    {
        if (reloadTimer > 0f)
        {
            // Top dolu değil: nişan yeniden alınsın, atış kaybolmasın.
            spotter.ResetAfterShot(suspicionAfterMiss);
            return;
        }

        // Ateş kararı Update'te veriliyor; kule hizalanana kadar bekleyecek.
        shotPending = true;
    }

    private void Fire()
    {
        PlayerTarget player = PlayerTarget.Instance;
        if (player == null)
            return;

        reloadTimer = reloadTime;
        shotsFired++;

        if (fireClip != null)
            fireSource.PlayOneShot(fireClip, fireVolume);

        bool hit = ResolveHit(player);

        // Işkalayan mermi oyuncunun yakınına düşer; isabet edense üstüne.
        Vector3 impactPoint = hit
            ? player.Position
            : player.Position + new Vector3(Random.Range(-6f, 6f), 0f, Random.Range(-6f, 6f));

        Fired?.Invoke(hit, impactPoint);

        if (hit)
        {
            PlayerHit?.Invoke();
            return;
        }

        HandleMiss(impactPoint);
    }

    /// <summary>
    /// İsabet olasılığı saf rastgele değil: mesafe ve oyuncunun hızıyla
    /// belirlenir. Böylece ışkalamak zar atışı değil, oyuncunun kendi
    /// davranışının sonucu olur.
    /// </summary>
    private bool ResolveHit(PlayerTarget player)
    {
        if (firstShotAlwaysMisses && shotsFired == 1)
            return false;

        float distance = Vector3.Distance(Muzzle.position, player.Position);
        float normalizedDistance = Mathf.Clamp01(distance / Mathf.Max(spotter.ViewDistance, 0.01f));

        float distanceFactor = Mathf.Lerp(1f, longRangeAccuracy, normalizedDistance);
        float speedFactor = Mathf.Lerp(1f, movingAccuracy, player.NormalizedSpeed);

        float hitChance = baseAccuracy * distanceFactor * speedFactor;
        return Random.value < hitChance;
    }

    private void HandleMiss(Vector3 impactPoint)
    {
        spotter.ResetAfterShot(suspicionAfterMiss);

        if (missImpactClip != null)
            AudioSource.PlayClipAtPoint(missImpactClip, impactPoint, fireVolume);

        PlayerTarget player = PlayerTarget.Instance;
        if (visorCamera == null && player != null)
            visorCamera = player.GetComponentInChildren<VisorCamera>();

        if (visorCamera != null)
            visorCamera.AddShake(missShake);
    }
}
