using UnityEngine;

/// <summary>Kulenin boştayken izlediği tarama düzeni.</summary>
public enum TurretScanMode
{
    /// <summary>Gövde ekseninin iki yanında gidip gelir.</summary>
    Sweep,

    /// <summary>Kesintisiz tam tur atar.</summary>
    FullRotation
}

/// <summary>
/// Düşman aracının kulesi. Boştayken yavaşça tarar, şüphe eşiği geçilince
/// oyuncuya döner.
///
/// Asıl işlevi görsel geri bildirim: kulenin sana dönmesi, hiçbir arayüz
/// elemanı olmadan "fark edildin" bilgisini veren gözle görülür bir
/// geri sayımdır. Top da ancak kule hizalandığında ateş eder, böylece
/// dönüş süresi oyuncunun kaçma penceresi olur.
/// </summary>
[RequireComponent(typeof(EnemySpotter))]
public class EnemyTurret : MonoBehaviour
{
    [Tooltip("Dönecek kule objesi. Gövdenin child'ı olmalı.")]
    [SerializeField] private Transform turret;

    [Tooltip("Modelin namlusu +Z yönüne bakmıyorsa buradan düzelt. " +
             "Namlu sağa bakıyorsa -90, sola bakıyorsa 90, geriye bakıyorsa 180. " +
             "Boş obje ile sarmalamaktan güvenlidir: model ölçeği bozulmaz.")]
    [Range(-180f, 180f)]
    [SerializeField] private float yawOffset;

    [Header("Dönüş")]
    [Tooltip("Saniyede kaç derece döner. Düşük tutmak oyuncuya kaçma " +
             "fırsatı verir; asıl zorluk ayarı bu sayıdır.")]
    [SerializeField] private float traverseSpeed = 25f;

    [Tooltip("Bu açının altında hizalanmış sayılır ve top ateş edebilir.")]
    [SerializeField] private float aimTolerance = 4f;

    [Tooltip("Şüphe bu değeri geçince kule oyuncuya dönmeye başlar. " +
             "Sıfıra yakın tutmak kuleyi sürekli kilitli tutar ve " +
             "koniden kaçmayı imkânsızlaştırır.")]
    [Range(0f, 1f)]
    [SerializeField] private float engageSuspicion = 0.35f;

    [Header("Tarama")]
    [Tooltip("Sweep: gövde ekseninin iki yanında gidip gelir. " +
             "FullRotation: sürekli tam tur atar. Kilitlenme her iki modda da " +
             "360 derece serbesttir; bu ayar yalnızca boştaki davranışı belirler.")]
    [SerializeField] private TurretScanMode scanMode = TurretScanMode.Sweep;

    [Tooltip("Sweep modunda gövde ekseninden kaç derece sağa ve sola taranır.")]
    [Range(0f, 180f)]
    [SerializeField] private float scanAngle = 55f;

    [Tooltip("Taramanın saniyedeki derece hızı.")]
    [SerializeField] private float scanSpeed = 10f;

    /// <summary>Kule oyuncuya hizalandı mı. Top bunu bekler.</summary>
    public bool IsAimedAtPlayer { get; private set; }

    /// <summary>
    /// Kule oyuncuya dönmeye başladı mı. Devriye hareketi bunu okuyup
    /// durur; tek eşik üzerinden yürüsün diye ayrı bir ayar tutulmuyor.
    /// </summary>
    public bool IsEngaged { get; private set; }

    /// <summary>Kulenin şu anki ve hedeflediği yerel açısı. Hata ayıklama için.</summary>
    public float CurrentYaw => currentYaw;
    public float TargetYaw { get; private set; }

    /// <summary>
    /// Kulenin gerçekte okuduğu şüphe ve eşiği. Bu değer ekrandaki düşman
    /// satırıyla tutmuyorsa objede ikinci bir EnemySpotter vardır.
    /// </summary>
    public float SpotterSuspicion => spotter != null ? spotter.Suspicion : -1f;
    public float EngageSuspicion => engageSuspicion;

    private EnemySpotter spotter;
    private float currentYaw;

    // Modelin çizim anındaki yerel rotasyonu. Sıfırlanırsa kule gövdenin
    // içine gömülür ya da yan yatar; FBX'lerin çoğu X/Z ekseninde
    // döndürülmüş olarak gelir.
    private Quaternion baseLocalRotation = Quaternion.identity;

    private void Awake()
    {
        spotter = GetComponent<EnemySpotter>();

        if (turret != null)
            baseLocalRotation = turret.localRotation;
        else
            Debug.LogWarning($"{name}: EnemyTurret'in Turret alanı boş, kule dönmeyecek.", this);
    }

    /// <summary>
    /// Hesap Update'te yapılır ki IsEngaged aynı karede EnemyPatrol
    /// tarafından taze okunabilsin; bir kare gecikirse araç durması
    /// gerekirken bir adım daha atar.
    /// </summary>
    private void Update()
    {
        if (turret == null)
            return;

        PlayerTarget player = PlayerTarget.Instance;
        IsEngaged = player != null && spotter.Suspicion >= engageSuspicion;

        float desired = IsEngaged ? YawTowardPlayer(player) : ScanYaw();

        // Ofset hedeften düşülmeli. Sadece uygulama anında eklenirse kule
        // her zaman ofset kadar yana nişan alır ama kod hizalandığını sanır.
        float targetYaw = desired - yawOffset;
        TargetYaw = targetYaw;

        currentYaw = Mathf.MoveTowardsAngle(currentYaw, targetYaw, traverseSpeed * Time.deltaTime);

        float error = Mathf.Abs(Mathf.DeltaAngle(currentYaw, targetYaw));
        IsAimedAtPlayer = IsEngaged && error <= aimTolerance;
    }

    /// <summary>
    /// Transform yazımı LateUpdate'te: modelle gelen Animator transform
    /// değerlerini LateUpdate'te ezer, Update'te yazılan dönüş silinir.
    /// </summary>
    private void LateUpdate()
    {
        if (turret == null)
            return;

        // yawOffset burada EKLENMEZ: hedef açı hesaplanırken zaten düşülüyor.
        // İki yerde birden uygulanırsa etkisi sıfırlanır ve kule, modelin
        // namlu ofseti kadar sürekli yana nişan alır.
        //
        // Dönüş modelin kendi rotasyonunun ÜSTÜNE biniyor. Soldan çarpmak
        // dönüşü parent'ın dikey ekseninde yapar; localEulerAngles'a doğrudan
        // yazmak modelin rotasyonunu siler ve kule yan yatar.
        turret.localRotation = Quaternion.Euler(0f, currentYaw, 0f) * baseLocalRotation;
    }

    /// <summary>Oyuncuya bakan açıyı gövdeye göre yerel açıya çevirir.</summary>
    private float YawTowardPlayer(PlayerTarget player)
    {
        Vector3 toPlayer = player.Position - turret.position;
        toPlayer.y = 0f;

        if (toPlayer.sqrMagnitude < 0.001f)
            return currentYaw;

        float worldYaw = Quaternion.LookRotation(toPlayer, Vector3.up).eulerAngles.y;

        // Yerel açı, kulenin GERÇEK parent'ına göre olmalı. Kök obje ile
        // kule arasında dönmüş bir model düğümü varsa köke göre hesaplamak
        // o düğümün açısı kadar kaydırır.
        return worldYaw - ParentYaw;
    }

    private float ParentYaw => turret != null && turret.parent != null
        ? turret.parent.eulerAngles.y
        : 0f;

    /// <summary>Boştaki tarama davranışı.</summary>
    private float ScanYaw()
    {
        if (scanMode == TurretScanMode.FullRotation)
        {
            // Hedef sürekli ilerler; MoveTowardsAngle en kısa yoldan takip
            // ettiği için kule kesintisiz tur atar.
            return Mathf.Repeat(Time.time * scanSpeed, 360f);
        }

        return Mathf.PingPong(Time.time * scanSpeed, scanAngle * 2f) - scanAngle;
    }

    private void OnDrawGizmosSelected()
    {
        if (turret == null)
            return;

        // Tarama sınırlarını göster; düşman yerleştirirken buna bakılır.
        Gizmos.color = new Color(0.4f, 0.8f, 1f, 0.9f);

        Vector3 origin = turret.position;
        Quaternion left = Quaternion.Euler(0f, ParentYaw - scanAngle, 0f);
        Quaternion right = Quaternion.Euler(0f, ParentYaw + scanAngle, 0f);

        Gizmos.DrawLine(origin, origin + left * Vector3.forward * 20f);
        Gizmos.DrawLine(origin, origin + right * Vector3.forward * 20f);
    }
}
