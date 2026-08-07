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

    [Tooltip("Hızın etkisi. 0 = hız hiç önemli değil, 1 = dururken neredeyse görünmezsin.")]
    [Range(0f, 1f)]
    [SerializeField] private float speedInfluence = 0.7f;

    /// <summary>0-1 arası şüphe seviyesi. 1 olduğunda oyuncu fark edilmiştir.</summary>
    public float Suspicion { get; private set; }

    /// <summary>Bu karede oyuncuyu görüyor mu.</summary>
    public bool HasLineOfSight { get; private set; }

    /// <summary>Herhangi bir düşman oyuncuyu tamamen fark ettiğinde bir kez tetiklenir.</summary>
    public static event System.Action<EnemySpotter> PlayerSpotted;

    private static readonly List<EnemySpotter> active = new List<EnemySpotter>();

    private bool hasReported;

    private Vector3 EyePosition => eye != null ? eye.position : transform.position;
    private Vector3 EyeForward => eye != null ? eye.forward : transform.forward;

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

        HasLineOfSight = CanSee(player.Position, out float distance);

        if (HasLineOfSight)
        {
            // Yakınlık çarpanı: menzilin ucunda 0, dibinde 1.
            float proximity = 1f - Mathf.Clamp01(distance / viewDistance);

            // Hız çarpanı: dururken speedInfluence kadar azalır, tam hızda 1 olur.
            float speedFactor = Mathf.Lerp(1f - speedInfluence, 1f, player.NormalizedSpeed);

            float rate = (1f / Mathf.Max(timeToSpot, 0.01f)) * proximity * speedFactor;
            Suspicion = Mathf.Clamp01(Suspicion + rate * Time.deltaTime);
        }
        else
        {
            Suspicion = Mathf.Clamp01(Suspicion - suspicionDecay * Time.deltaTime);
        }

        if (Suspicion >= 1f && !hasReported)
        {
            hasReported = true;
            PlayerSpotted?.Invoke(this);
        }
    }

    /// <summary>
    /// Ucuzdan pahalıya üç kontrol: mesafe, açı, engel.
    /// Raycast en pahalısı olduğu için en sona bırakılır.
    /// </summary>
    private bool CanSee(Vector3 targetPosition, out float distance)
    {
        Vector3 toTarget = targetPosition - EyePosition;
        distance = toTarget.magnitude;

        if (distance > viewDistance)
            return false;

        if (Vector3.Angle(EyeForward, toTarget) > viewAngle * 0.5f)
            return false;

        // Arada engel varsa görüş kesilir.
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
        UnityEditor.Handles.color = new Color(1f, 0.9f, 0.3f, 0.12f);
        UnityEditor.Handles.DrawSolidArc(origin, Vector3.up, leftEdge, viewAngle, viewDistance);
#endif
    }
}
