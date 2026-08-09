using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Düşman aracının görüş sistemi. Mesafe, görüş konisi ve engel kontrolüne
/// göre bir şüphe sayacı doldurur. Sayaç dolarsa oyuncu fark edilmiştir.
/// Fark edilme kararı burada verilir; kamera ya da UI kodunda değil.
/// </summary>
public class EnemySpotter : MonoBehaviour
{
    [Header("Görüş")]
    [Tooltip("Bu mesafeden uzaktaki oyuncu hiç görülmez. " +
             "KURAL: bu değer sis mesafesinden küçük olmalı, yoksa oyuncu " +
             "kendisini gören düşmanı göremez ve oyun adaletsiz hissettirir.")]
    [SerializeField] private float viewDistance = 55f;

    [Tooltip("Görüş konisinin toplam açısı (derece).")]
    [Range(10f, 360f)]
    [SerializeField] private float viewAngle = 80f;

    [Tooltip("Yön farketmeksizin fark edilinen yarıçap. Mürettebatın " +
             "yakındaki bir aracı duyması / göz ucuyla görmesi. " +
             "Bu olmadan düşmanın tam arkasına park edip görünmez kalınabilir.")]
    [SerializeField] private float awarenessRadius = 18f;

    [Tooltip("Görüşü kesen katmanlar: arazi, kayalar, tepeler. " +
             "Oyuncunun katmanı BURADA OLMAMALI, yoksa oyuncu kendini gizler.")]
    [SerializeField] private LayerMask obstacleMask;

    [Tooltip("Gözün konumu. Boş bırakılırsa objenin kendisi kullanılır.")]
    [SerializeField] private Transform eye;

    [Header("Şüphe")]
    [Tooltip("En kötü durumda (çok yakın ve tam hız) fark edilmek kaç saniye sürer.")]
    [SerializeField] private float timeToSpot = 1.8f;

    [Tooltip("Görüş kesilince saniyede ne kadar şüphe düşer. " +
             "Dolmadan yavaş olmalı: kaçmak, görünmekten daha uzun sürsün.")]
    [SerializeField] private float suspicionDecay = 0.25f;

    [Tooltip("Hızın dolma oranına etkisi. 0 = hız hiç önemli değil.")]
    [Range(0f, 1f)]
    [SerializeField] private float speedInfluence = 0.7f;

    [Tooltip("Tank tamamen dururken görüş menzili bu oranla çarpılır. " +
             "Asıl hissedilen mekanik budur: durunca düşman seni menzil dışında bırakıp kaybeder. " +
             "Sadece dolma oranını yavaşlatmak oyuncu tarafından fark edilmez.")]
    [Range(0.1f, 1f)]
    [SerializeField] private float stillRangeFactor = 0.45f;

    [Header("Keşif")]
    [Tooltip("Oyuncunun bu aracı görüp haritaya kaydedebileceği azami mesafe. " +
             "Görüş hattı da açık olmalı; tepenin ardındaki düşman keşfedilmez.\n\n" +
             "Bu değer ile View Distance arasındaki fark, oyuncunun bilgi avantajıdır: " +
             "düşmanı ne kadar önce görürse rotasını o kadar rahat kurar. " +
             "Fark daralırsa harita planlama aracı olmaktan çıkar.\n\n" +
             "Üst sınır sis mesafesidir: göremeyeceğin bir düşmanı haritaya " +
             "işaretlemek oyuncuya sebepsiz bilgi vermek olur.")]
    [SerializeField] private float discoveryRange = 100f;

    /// <summary>0-1 arası şüphe seviyesi. 1 olduğunda oyuncu fark edilmiştir.</summary>
    public float Suspicion { get; private set; }

    /// <summary>Bu karede oyuncuyu görüyor mu.</summary>
    public bool HasLineOfSight { get; private set; }

    /// <summary>
    /// Oyuncu bu aracı en az bir kez gördü mü. Bir kez keşfedilen düşman
    /// haritada kalıcı kalır; sürücünün hafızasını temsil eder.
    /// </summary>
    public bool IsDiscovered { get; private set; }

    /// <summary>
    /// Atıştan sonra nişanı geri alır. Sayaç sıfırlanmaz; düşman zaten
    /// oyuncuyu biliyor, sadece yeniden nişan alması gerekiyor.
    /// </summary>
    public void ResetAfterShot(float newSuspicion)
    {
        Suspicion = Mathf.Clamp01(newSuspicion);
        hasReported = false;
    }

    public float ViewAngle => viewAngle;
    public float ViewDistance => viewDistance;
    public float AwarenessRadius => awarenessRadius;
    public Vector3 EyeWorldPosition => EyePosition;

    /// <summary>
    /// Nişan alma tamamlandığında tetiklenir. Sayaç "fark edilme" değil
    /// "nişan alma süresi" anlamına gelir; kaybetme kararını EnemyGun verir.
    /// </summary>
    public event System.Action<EnemySpotter> AimComplete;

    private static readonly List<EnemySpotter> active = new List<EnemySpotter>();

    /// <summary>Sahnedeki tüm aktif gözcüler. Hata ayıklama göstergesi okur.</summary>
    public static IReadOnlyList<EnemySpotter> Active => active;

    private bool hasReported;

    private Vector3 EyePosition => eye != null ? eye.position : transform.position;

    private bool warnedAboutEyeAxis;

    /// <summary>
    /// Görüş yönü. Kara aracı olduğu için daima yataya düzleştirilir:
    /// namlu hafif eğik durursa koni yere ya da göğe kaçmasın.
    ///
    /// Z-up çizilmiş modeller Unity'ye -90 X rotasyonuyla gelir; o durumda
    /// ileri yön tam dikey olur ve yatay bileşen kalmaz. Bu sessizce
    /// geçilirse görüş sistemi hiç çalışmaz.
    /// </summary>
    private Vector3 EyeForward
    {
        get
        {
            Vector3 forward = eye != null ? eye.forward : transform.forward;
            Vector3 flat = new Vector3(forward.x, 0f, forward.z);

            if (flat.sqrMagnitude > 0.0001f)
                return flat.normalized;

            if (!warnedAboutEyeAxis)
            {
                warnedAboutEyeAxis = true;
                Debug.LogWarning(
                    $"{name}: Eye objesinin ileri ekseni tam dikey. Muzzle'ın " +
                    "Rotation X değerini kulenin rotasyonunu sıfırlayacak şekilde " +
                    "ayarla (kule -90 ise Muzzle 90). Şimdilik gövde yönü kullanılıyor.", this);
            }

            // Kurulum düzeltilene kadar gövdenin yönüne düş.
            Vector3 fallback = new Vector3(transform.forward.x, 0f, transform.forward.z);
            return fallback.sqrMagnitude > 0.0001f ? fallback.normalized : Vector3.forward;
        }
    }

    private void OnEnable() => active.Add(this);
    private void OnDisable() => active.Remove(this);

    /// <summary>Sahnedeki en yüksek şüphe seviyesi. Ekran geri bildirimi bunu okur.</summary>
    public static float HighestSuspicion()
    {
        float highest = 0f;
        for (int i = 0; i < active.Count; i++)
        {
            if (active[i].Suspicion > highest)
                highest = active[i].Suspicion;
        }
        return highest;
    }

    private void Update()
    {
        PlayerTarget player = PlayerTarget.Instance;
        if (player == null)
            return;

        // Hareket hâlindeki tank daha uzaktan seçilir. Durunca menzil kısalır
        // ve düşman oyuncuyu fiilen kaybeder; oyuncunun hissettiği mekanik budur.
        float effectiveDistance = viewDistance * Mathf.Lerp(stillRangeFactor, 1f, player.NormalizedSpeed);

        HasLineOfSight = CanSee(player.Position, effectiveDistance, out float distance);

        if (HasLineOfSight)
        {
            // Yakınlık çarpanı: menzilin ucunda 0, dibinde 1.
            // Referans iki menzilin büyüğü olmalı; yoksa yakın algıyla
            // fark edilen ama koni menzilinin dışındaki oyuncuda oran
            // sıfır çıkar ve sayaç hiç dolmaz.
            float referenceDistance = Mathf.Max(effectiveDistance, awarenessRadius);
            float proximity = 1f - Mathf.Clamp01(distance / referenceDistance);

            // Hız çarpanı: dururken speedInfluence kadar azalır, tam hızda 1 olur.
            float speedFactor = Mathf.Lerp(1f - speedInfluence, 1f, player.NormalizedSpeed);

            float rate = (1f / Mathf.Max(timeToSpot, 0.01f)) * proximity * speedFactor;
            Suspicion = Mathf.Clamp01(Suspicion + rate * Time.deltaTime);
        }
        else
        {
            Suspicion = Mathf.Clamp01(Suspicion - suspicionDecay * Time.deltaTime);
        }

        UpdateDiscovery(player.Position);

        if (Suspicion >= 1f && !hasReported)
        {
            hasReported = true;

            if (AimComplete == null)
            {
                // Topu olmayan araç nişan alır ama ateş edemez. Sessizce
                // yutulursa oyun kaybedilemez hâle gelir ve sebebi bulunmaz.
                Debug.LogWarning($"{name}: nişan tamamlandı ama EnemyGun yok, ateş edilemiyor.", this);
                Suspicion = 0.5f;
                hasReported = false;
            }
            else
            {
                AimComplete.Invoke(this);
            }
        }
    }

    /// <summary>
    /// Oyuncunun bu aracı görüp görmediğini kontrol eder. Bilgi keşifle
    /// kazanılsın diye harita sadece keşfedilmiş düşmanları gösterir.
    /// </summary>
    private void UpdateDiscovery(Vector3 playerPosition)
    {
        // Bir kez keşfedildiyse tekrar sorgulamaya gerek yok; raycast'ten tasarruf.
        if (IsDiscovered)
            return;

        Vector3 eye = EyePosition;
        if (Vector3.Distance(playerPosition, eye) > discoveryRange)
            return;

        // Tepenin ardındaki düşman keşfedilmemeli.
        if (Physics.Linecast(playerPosition, eye, obstacleMask))
            return;

        IsDiscovered = true;
    }

    /// <summary>
    /// Ucuzdan pahalıya üç kontrol: mesafe, açı, engel.
    /// Raycast en pahalısı olduğu için en sona bırakılır.
    /// </summary>
    private bool CanSee(Vector3 targetPosition, float coneDistance, out float distance)
    {
        Vector3 toTarget = targetPosition - EyePosition;
        distance = toTarget.magnitude;

        // İleri bakan uzun koni: kulenin baktığı yön.
        bool inCone = distance <= coneDistance
                      && Vector3.Angle(EyeForward, toTarget) <= viewAngle * 0.5f;

        // Çevredeki kısa 360 derece alan. Bilerek hızdan bağımsız:
        // on metredeki bir tank, dursa da belli olur.
        bool inAwareness = distance <= awarenessRadius;

        if (!inCone && !inAwareness)
            return false;

        // Her iki durumda da arada engel varsa görüş kesilir.
        return !Physics.Linecast(EyePosition, targetPosition, obstacleMask);
    }

    /// <summary>Düşman yerleşimi yaparken koniyi sahnede görebilmek için.</summary>
    private void OnDrawGizmosSelected()
    {
        Vector3 origin = EyePosition;
        Vector3 forward = EyeForward;

        Gizmos.color = HasLineOfSight ? Color.red : new Color(1f, 0.9f, 0.3f, 0.8f);

        float half = viewAngle * 0.5f;
        Vector3 leftEdge = Quaternion.Euler(0f, -half, 0f) * forward;
        Vector3 rightEdge = Quaternion.Euler(0f, half, 0f) * forward;

        Gizmos.DrawLine(origin, origin + leftEdge * viewDistance);
        Gizmos.DrawLine(origin, origin + rightEdge * viewDistance);
        Gizmos.DrawLine(origin, origin + forward * viewDistance);

#if UNITY_EDITOR
        // Dış koni: tam hızdaki menzil. İç koni: dururkenki menzil.
        UnityEditor.Handles.color = new Color(1f, 0.9f, 0.3f, 0.10f);
        UnityEditor.Handles.DrawSolidArc(origin, Vector3.up, leftEdge, viewAngle, viewDistance);

        UnityEditor.Handles.color = new Color(0.3f, 0.8f, 1f, 0.14f);
        UnityEditor.Handles.DrawSolidArc(origin, Vector3.up, leftEdge, viewAngle, viewDistance * stillRangeFactor);

        // Yön farketmeksizin fark edilinen 360 derece alan.
        UnityEditor.Handles.color = new Color(1f, 0.35f, 0.25f, 0.18f);
        UnityEditor.Handles.DrawSolidDisc(origin, Vector3.up, awarenessRadius);
#endif
    }
}
