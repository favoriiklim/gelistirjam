using UnityEngine;

/// <summary>
/// Topun görsel efektleri: namlu alevi ve mermi düşme patlaması.
/// Ateş kararına karışmaz, sadece EnemyGun'ın yaydığı olayı dinler.
///
/// Parçacık sistemleri koddan üretilmiyor, dışarıdan atanıyor. Sebep:
/// çalışma anında oluşturulan ParticleSystem'e URP'de doğru materyal
/// atanmaz ve efekt pembe çıkar. Editor'de oluşturulanlar doğru gelir.
/// </summary>
[RequireComponent(typeof(EnemyGun))]
public class EnemyGunEffects : MonoBehaviour
{
    [Header("Namlu")]
    [Tooltip("Namluya yerleştirilmiş parçacık sistemi. Play On Awake kapalı olmalı.")]
    [SerializeField] private ParticleSystem muzzleFlash;

    [Tooltip("Namlu alevinin ışığı. Kısa bir parlama için kullanılır.")]
    [SerializeField] private Light muzzleLight;

    [SerializeField] private float lightIntensity = 12f;

    [Tooltip("Işığın sönme süresi. Kısa tut; uzun parlama sahte durur.")]
    [SerializeField] private float lightDuration = 0.08f;

    [Header("Düşme noktası")]
    [Tooltip("Merminin düştüğü yerde oynatılacak patlama. Sahnede tek bir " +
             "tane bulunur, her atışta düşme noktasına taşınır. " +
             "Her seferinde Instantiate etmekten ucuzdur.")]
    [SerializeField] private ParticleSystem impactEffect;

    private EnemyGun gun;
    private float lightTimer;

    private void Awake()
    {
        gun = GetComponent<EnemyGun>();

        if (muzzleLight != null)
        {
            muzzleLight.intensity = 0f;
            muzzleLight.enabled = false;
        }
    }

    private void OnEnable() => gun.Fired += HandleFired;
    private void OnDisable() => gun.Fired -= HandleFired;

    private void HandleFired(bool hit, Vector3 impactPoint)
    {
        if (muzzleFlash != null)
            muzzleFlash.Play();

        if (muzzleLight != null)
        {
            muzzleLight.enabled = true;
            muzzleLight.intensity = lightIntensity;
            lightTimer = lightDuration;
        }

        if (impactEffect != null)
        {
            impactEffect.transform.position = impactPoint;
            impactEffect.Play();
        }
    }

    private void Update()
    {
        if (lightTimer <= 0f)
            return;

        lightTimer -= Time.deltaTime;

        if (muzzleLight == null)
            return;

        // Doğrusal sönme yeterli; parlama zaten göz kırpması kadar kısa.
        muzzleLight.intensity = lightIntensity * Mathf.Clamp01(lightTimer / lightDuration);

        if (lightTimer <= 0f)
            muzzleLight.enabled = false;
    }
}
