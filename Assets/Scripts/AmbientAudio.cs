using UnityEngine;

/// <summary>
/// Çöl ambiyansı. Ham kayıt döngüye uygun olmasa da SeamlessLoop
/// sayesinde kopukluk duyulmaz.
///
/// Seviyeyi kısık tut: brief'in istediği "sessizlik anları" ancak
/// arka plan boşken hissedilir. Ambiyans yükseltilirse motor
/// sustuğunda ortaya çıkan boşluk dolar ve gerginlik kaybolur.
/// </summary>
public class AmbientAudio : MonoBehaviour
{
    [SerializeField] private AudioClip ambienceClip;

    [Range(0f, 1f)]
    [SerializeField] private float volume = 0.15f;

    private SeamlessLoop loop;

    private void Awake()
    {
        // Ambiyans her yerden aynı duyulmalı, konuma bağlanmamalı.
        loop = new SeamlessLoop(transform, "Ambience", ambienceClip, 0f, 500f);
    }

    private void Update()
    {
        loop.SetVolume(volume);
        loop.Update();
    }
}
