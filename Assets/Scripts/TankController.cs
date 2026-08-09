using UnityEngine;

/// <summary>
/// Tankın hareketini uygular. Klavyeyi ASLA okumaz; sadece iki palet
/// değeri alır. Girdi şeması değişse bile bu sınıf hiç değişmez.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class TankController : MonoBehaviour
{
    [Header("Hareket")]
    [Tooltip("Metre/saniye cinsinden azami ileri hız.")]
    [SerializeField] private float maxSpeed = 6f;

    [Tooltip("Geri vites, ileri hızın bu oranı kadar olur.")]
    [SerializeField] private float reverseSpeedFactor = 0.5f;

    [Tooltip("Saniyede kaç m/s hız kazanılır. Düşük değer = ağır tank hissi.")]
    [SerializeField] private float acceleration = 2f;

    [Tooltip("Gaz kesince ya da fren yapınca saniyede kaç m/s kaybedilir. " +
             "Hızlanmadan yüksek olmalı: ağır araç isteksiz kalkar ama görece çabuk durur.")]
    [SerializeField] private float braking = 8f;

    [Tooltip("Saniyede kaç derece dönülür.")]
    [SerializeField] private float turnSpeed = 35f;

    [Header("Eğim")]
    [Tooltip("Açık: tank yokuşta öne arkaya eğilir, ama devrilebilir. " +
             "Kapalı: gövde her zaman düz kalır, devrilmek imkânsızdır.")]
    [SerializeField] private bool allowTilt = true;

    [Tooltip("Ağırlık merkezinin gövde merkezinden ne kadar aşağıda olduğu. " +
             "Devrilmeyi engelleyen asıl ayar budur; gerçek tanklarda da öyle. " +
             "Tank hâlâ takla atıyorsa bu değeri daha da düşür.")]
    [SerializeField] private float centerOfMassHeight = -0.6f;

    private Rigidbody rb;

    // -1 ile 1 arası palet girdileri. Dışarıdan SetTrackInput ile yazılır.
    private float leftTrackInput;
    private float rightTrackInput;

    // Yumuşatılmış anlık ileri hız; ani hız değişimini engeller.
    private float currentSpeed;

    /// <summary>Fark edilme hesabı için 0-1 arası normalize hız.</summary>
    public float NormalizedSpeed => Mathf.Abs(currentSpeed) / Mathf.Max(maxSpeed, 0.01f);

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        rb.constraints = allowTilt
            ? RigidbodyConstraints.None
            : RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        // Ağırlık merkezini aşağı almak devrilmeyi ciddi ölçüde zorlaştırır.
        rb.centerOfMass = new Vector3(0f, centerOfMassHeight, 0f);

        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    /// <summary>
    /// Palet girdilerini ayarlar. Her ikisi de -1 ile 1 arasındadır.
    /// İkisi de ileri = düz gider, ters yönde = yerinde döner.
    /// </summary>
    public void SetTrackInput(float left, float right)
    {
        leftTrackInput = Mathf.Clamp(left, -1f, 1f);
        rightTrackInput = Mathf.Clamp(right, -1f, 1f);
    }

    private void FixedUpdate()
    {
        // İki paletin ortalaması ileri hareketi, farkı ise dönüşü verir.
        float driveInput = (leftTrackInput + rightTrackInput) * 0.5f;
        float turnInput = (leftTrackInput - rightTrackInput) * 0.5f;

        // Geri vites daha yavaş olsun.
        float speedLimit = driveInput >= 0f ? maxSpeed : maxSpeed * reverseSpeedFactor;
        float targetSpeed = driveInput * speedLimit;

        // Yavaşlıyor muyuz? Ya hedef hız daha düşük, ya da ters yöne geçiliyor.
        // Ters yöne geçerken sıfırı aşana kadar fren, sonrası hızlanma sayılır.
        bool slowingDown = Mathf.Abs(targetSpeed) < Mathf.Abs(currentSpeed)
                           || targetSpeed * currentSpeed < 0f;

        float rate = slowingDown ? braking : acceleration;
        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, rate * Time.fixedDeltaTime);

        // Yatay hızı biz belirliyoruz ama dikey bileşene dokunmuyoruz,
        // yoksa tank yerçekimini yok sayıp havada asılı kalır.
        Vector3 velocity = transform.forward * currentSpeed;
        velocity.y = rb.linearVelocity.y;
        rb.linearVelocity = velocity;

        if (Mathf.Abs(turnInput) > 0.001f)
        {
            float yaw = turnInput * turnSpeed * Time.fixedDeltaTime;

            // Dönüş dünyanın dikey ekseninde olmalı. Soldan çarpmak bunu
            // sağlar; sağdan çarpsaydık tank yokuşta kendi eğik ekseninde
            // dönerdi ve yamaçta direksiyon garipleşirdi.
            rb.MoveRotation(Quaternion.Euler(0f, yaw, 0f) * rb.rotation);
        }
    }
}
