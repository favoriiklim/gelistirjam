using UnityEngine;

/// <summary>
/// Aracı her karede zemine oturtur. Düşman araçları fizikle değil doğrudan
/// transform ile hareket ettiği için arazi geldiğinde havada kalıyorlar.
///
/// Rigidbody eklemek yerine tek bir aşağı ışın kullanılıyor: bu araçların
/// sürüş hissi diye bir derdi yok, sadece zemini takip etmeleri gerekiyor.
/// Fizik eklemek burada sıfır fayda, çok fazla ayar demek.
/// </summary>
public class GroundSnap : MonoBehaviour
{
    [Tooltip("Zemin sayılacak katmanlar. Arazi ve kayaların bulunduğu " +
             "Obstacle katmanı seçilmeli. Aracın kendi katmanı BURADA OLMAMALI, " +
             "yoksa ışın kendi gövdesine çarpar ve araç yerinde titrer.")]
    [SerializeField] private LayerMask groundMask;

    [Tooltip("Işın aracın kaç metre üstünden başlar. Tepe çıkarken " +
             "aracın içinden başlamaması için gövde yüksekliğinden büyük olmalı.")]
    [SerializeField] private float rayStartHeight = 5f;

    [Tooltip("Işının toplam uzunluğu. Uçurumlarda yetsin diye geniş tut.")]
    [SerializeField] private float rayLength = 50f;

    [Tooltip("Pivot noktasının zeminden yüksekliği. Araç yere gömülüyorsa " +
             "artır, havada duruyorsa azalt.")]
    [SerializeField] private float heightOffset;

    [Tooltip("Dikey hareketin yumuşatılması. 0 = anında yapış. " +
             "Yüksek değer küçük tümsekleri yumuşatır.")]
    [SerializeField] private float smoothing = 12f;

    [Header("Eğim")]
    [Tooltip("Araç yamacın eğimine yatsın mı. KAPALI önerilir: kule dönüş " +
             "hesabı gövdenin dikey ekseninden çıkıyor, gövde yatınca " +
             "nişan açısı kayabilir. Oyuncu tankı da aynı sebeple düz kalıyor.")]
    [SerializeField] private bool alignToSlope;

    [Tooltip("Eğime yatma açısının üst sınırı.")]
    [Range(0f, 45f)]
    [SerializeField] private float maxSlopeAngle = 20f;

    private void LateUpdate()
    {
        Vector3 origin = transform.position + Vector3.up * rayStartHeight;

        if (!Physics.Raycast(origin, Vector3.down, out RaycastHit hit, rayLength, groundMask))
            return; // Zemin bulunamadı: aracı düşürmek yerine olduğu yerde bırak.

        float targetY = hit.point.y + heightOffset;

        Vector3 position = transform.position;
        position.y = smoothing <= 0f
            ? targetY
            : Mathf.Lerp(position.y, targetY, smoothing * Time.deltaTime);

        transform.position = position;

        if (alignToSlope)
            AlignTo(hit.normal);
    }

    private void AlignTo(Vector3 groundNormal)
    {
        float slope = Vector3.Angle(Vector3.up, groundNormal);
        if (slope > maxSlopeAngle)
            return;

        // Mevcut bakış yönü korunarak sadece yukarı vektör zemine uyduruluyor.
        Vector3 forward = Vector3.ProjectOnPlane(transform.forward, groundNormal);
        if (forward.sqrMagnitude < 0.001f)
            return;

        transform.rotation = Quaternion.LookRotation(forward.normalized, groundNormal);
    }
}
