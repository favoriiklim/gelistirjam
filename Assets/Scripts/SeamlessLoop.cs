using UnityEngine;

/// <summary>
/// Başı ve sonu uyuşmayan bir klibi dikişsiz döngüye çevirir.
///
/// Aynı klip iki kaynakta, yarım klip arayla çalınır. Kazançlar sinüs
/// eğrisiyle sürülür: bir kaynağın kopuk noktası geldiğinde o kaynak
/// tamamen kısıktır, diğeri tam açıktır. sin² + cos² = 1 olduğu için
/// toplam ses gücü sabit kalır, dalgalanma duyulmaz.
///
/// Ses dosyalarını Audacity'de düzenlemeye gerek bırakmaz.
/// </summary>
public class SeamlessLoop
{
    private readonly AudioSource first;
    private readonly AudioSource second;
    private readonly float clipLength;

    private float volume = 1f;

    public bool IsValid => first != null;

    public SeamlessLoop(Transform parent, string name, AudioClip clip,
                        float spatialBlend, float maxDistance)
    {
        if (clip == null)
            return;

        clipLength = clip.length;

        first = CreateSource(parent, name + "_A", clip, spatialBlend, maxDistance);
        second = CreateSource(parent, name + "_B", clip, spatialBlend, maxDistance);

        first.Play();
        second.Play();

        // İkinci kaynak yarım klip ileriden başlar; kopuk noktalar böylece
        // hiçbir zaman aynı anda denk gelmez.
        second.time = clipLength * 0.5f;
    }

    private static AudioSource CreateSource(Transform parent, string name, AudioClip clip,
                                            float spatialBlend, float maxDistance)
    {
        var sourceObject = new GameObject(name);
        sourceObject.transform.SetParent(parent, false);

        var source = sourceObject.AddComponent<AudioSource>();
        source.clip = clip;
        source.loop = true;
        source.playOnAwake = false;
        source.volume = 0f;
        source.spatialBlend = spatialBlend;
        source.rolloffMode = AudioRolloffMode.Linear;
        source.maxDistance = maxDistance;

        return source;
    }

    public void SetVolume(float value) => volume = Mathf.Max(0f, value);

    public void SetPitch(float value)
    {
        if (!IsValid)
            return;

        // İki kaynak aynı perdede kalmalı, yoksa aradaki yarım klip
        // farkı kayar ve dikiş yeri açığa çıkar.
        first.pitch = value;
        second.pitch = value;
    }

    /// <summary>Her karede çağrılmalı; çapraz geçiş kazançlarını uygular.</summary>
    public void Update()
    {
        if (!IsValid || clipLength <= 0f)
            return;

        float phase = first.time / clipLength;
        float otherPhase = Mathf.Repeat(phase + 0.5f, 1f);

        first.volume = Mathf.Sin(phase * Mathf.PI) * volume;
        second.volume = Mathf.Sin(otherPhase * Mathf.PI) * volume;
    }
}
