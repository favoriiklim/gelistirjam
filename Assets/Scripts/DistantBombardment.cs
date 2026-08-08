using UnityEngine;

/// <summary>
/// Uzaktan gelen bombardıman sesleri. Rastgele aralıklarla, rastgele
/// yönden ve rastgele seviyede çalar.
///
/// Amaç atmosfer: cephenin devam ettiğini, oyuncunun düşman hattının
/// gerisinde yalnız olduğunu hissettirir. Oynanışa etkisi yoktur,
/// bilerek öyle: oyuncuyu yanlış yönlendiren bir ses kafa karıştırır.
/// </summary>
public class DistantBombardment : MonoBehaviour
{
    [Tooltip("Bombardıman klipleri. Her seferinde biri rastgele seçilir.")]
    [SerializeField] private AudioClip[] clips;

    [Header("Zamanlama")]
    [Tooltip("İki patlama arasındaki en kısa süre (saniye).")]
    [SerializeField] private float minInterval = 14f;

    [Tooltip("En uzun süre. Aralığı geniş tut; düzenli aralık yapaylaşır.")]
    [SerializeField] private float maxInterval = 35f;

    [Tooltip("Oyunun başında ilk patlamaya kadar beklenecek süre.")]
    [SerializeField] private float initialDelay = 8f;

    [Header("Seviye")]
    [Range(0f, 1f)]
    [SerializeField] private float minVolume = 0.15f;

    [Range(0f, 1f)]
    [SerializeField] private float maxVolume = 0.35f;

    [Tooltip("Perde sapması. Aynı klip her seferinde biraz farklı duyulsun " +
             "diye; tekrar eden ses kulakta hemen sırıtır.")]
    [Range(0f, 0.3f)]
    [SerializeField] private float pitchVariation = 0.12f;

    private AudioSource source;
    private float timer;

    private void Awake()
    {
        source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
        // Konumdan bağımsız çalıyoruz, yön hissi pan ile veriliyor.
        source.spatialBlend = 0f;

        timer = initialDelay;
    }

    private void Update()
    {
        if (clips == null || clips.Length == 0)
            return;

        // Oyun bitince yeni patlama başlamasın.
        if (GameManager.Instance != null && GameManager.Instance.State != GameState.Playing)
            return;

        timer -= Time.deltaTime;
        if (timer > 0f)
            return;

        PlayOne();
        timer = Random.Range(minInterval, maxInterval);
    }

    private void PlayOne()
    {
        AudioClip clip = clips[Random.Range(0, clips.Length)];
        if (clip == null)
            return;

        // Rastgele yön: uzaktaki topçu her seferinde aynı yerden gelmesin.
        source.panStereo = Random.Range(-0.8f, 0.8f);
        source.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);

        source.PlayOneShot(clip, Random.Range(minVolume, maxVolume));
    }
}
