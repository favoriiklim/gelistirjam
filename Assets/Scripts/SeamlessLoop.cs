using UnityEngine;

/// <summary>
/// Başı ve sonu uyuşmayan bir klibi dikişsiz döngüye çevirir.
///
/// Aynı klip iki kaynakta, yarım klip arayla çalınır. Her kaynağın kazancı
/// kendi konumundan sinüs eğrisiyle hesaplanır: bir kaynak kendi kopuk
/// noktasına gelirken tamamen kısılır, o sırada diğeri klibin ortasındadır.
/// sin² + cos² = 1 olduğu için toplam ses gücü sabit kalır.
///
/// Ses dosyalarını düzenlemeye gerek bırakmaz.
/// </summary>
public class SeamlessLoop
{
    private readonly AudioSource first;
    private readonly AudioSource second;
    private readonly int clipSamples;

    private float volume = 1f;
    private bool offsetVerified;

    public bool IsValid => first != null;

    public SeamlessLoop(Transform parent, string name, AudioClip clip,
                        float spatialBlend, float maxDistance)
    {
        if (clip == null || clip.samples <= 0)
            return;

        clipSamples = clip.samples;

        first = CreateSource(parent, name + "_A", clip, spatialBlend, maxDistance);
        second = CreateSource(parent, name + "_B", clip, spatialBlend, maxDistance);

        // Konum Play'den ÖNCE verilmeli. Oynatma başladıktan sonra yazmak
        // güvenilir değildir; kaynak 0'dan başlar, iki dikiş üst üste biner
        // ve tam da kopuk noktada ses kesilir.
        first.timeSamples = 0;
        second.timeSamples = clipSamples / 2;

        first.Play();
        second.Play();
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

        // İki kaynak aynı perdede kalmalı, yoksa aradaki yarım klip farkı kayar.
        first.pitch = value;
        second.pitch = value;
    }

    /// <summary>Her karede çağrılmalı; çapraz geçiş kazançlarını uygular.</summary>
    public void Update()
    {
        if (!IsValid)
            return;

        VerifyOffsetOnce();

        // Kazançlar her kaynağın KENDİ konumundan hesaplanıyor. Böylece
        // kaynaklar bir sebeple kaysa bile her biri kendi dikişinde susar.
        first.volume = GainFor(first) * volume;
        second.volume = GainFor(second) * volume;
    }

    private float GainFor(AudioSource source)
    {
        float phase = source.timeSamples / (float)clipSamples;
        return Mathf.Sin(Mathf.Clamp01(phase) * Mathf.PI);
    }

    /// <summary>
    /// İlk karede aradaki yarım klip farkının gerçekten oluştuğunu doğrular.
    /// Oluşmadıysa düzeltir; iki kaynak aynı fazdayken dikiş yerinde
    /// ses tamamen kesilir ve sorun kulakta net duyulur.
    /// </summary>
    private void VerifyOffsetOnce()
    {
        if (offsetVerified)
            return;

        // Kaynaklar gerçekten çalmaya başlayana kadar ölçüm anlamsız.
        if (!first.isPlaying || !second.isPlaying)
            return;

        offsetVerified = true;

        int offset = Mathf.Abs(second.timeSamples - first.timeSamples);
        int expected = clipSamples / 2;

        // Beşte birlik sapmaya kadar tolerans; ötesinde düzelt.
        if (Mathf.Abs(offset - expected) > clipSamples / 5)
            second.timeSamples = (first.timeSamples + expected) % clipSamples;
    }
}
