using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Şüphe seviyesini oyuncuya gösteren ekran katmanı. Fark edilme kararını
/// vermez; sadece EnemySpotter'ların ürettiği değeri okur ve görselleştirir.
/// Bunsuz oyuncu neden kaybettiğini anlamaz ve denge ayarı yapılamaz.
/// </summary>
public class SpottingIndicator : MonoBehaviour
{
    [Tooltip("Şüphe dolduğunda ekranın alacağı renk.")]
    [SerializeField] private Color alertColor = new Color(0.6f, 0.05f, 0.02f);

    [Tooltip("Tam şüphede azami saydamlık. Yükseltmek görüşü kapatır.")]
    [Range(0f, 1f)]
    [SerializeField] private float maxAlpha = 0.4f;

    [Tooltip("Şüphe bu değeri aşınca nabız gibi atmaya başlar.")]
    [Range(0f, 1f)]
    [SerializeField] private float pulseThreshold = 0.5f;

    [SerializeField] private float pulseSpeed = 6f;

    [Header("Hata ayıklama")]
    [Tooltip("Sayısal gösterge. Sadece Editor'de ve development build'de çalışır; " +
             "yayın build'inde tik açık kalsa bile ekrana hiçbir şey çizilmez.")]
    [SerializeField] private bool showDebugReadout;

    private Image overlay;
    private GUIStyle debugStyle;

    private void Awake()
    {
        var canvasObject = new GameObject("SpottingCanvas", typeof(Canvas));
        canvasObject.transform.SetParent(transform, false);

        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        // Vizör maskesinin (100) altında kalsın ki kızarma sadece yarıkta görünsün.
        canvas.sortingOrder = 99;

        var overlayObject = new GameObject("Overlay", typeof(RectTransform), typeof(Image));
        overlayObject.transform.SetParent(canvasObject.transform, false);

        overlay = overlayObject.GetComponent<Image>();
        overlay.raycastTarget = false;

        var rect = overlayObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private void Update()
    {
        float suspicion = EnemySpotter.HighestSuspicion();
        float alpha = suspicion * maxAlpha;

        // Eşiği geçince nabız: oyuncu "hâlâ görülüyorum" sinyalini net alsın.
        if (suspicion > pulseThreshold)
        {
            float pulse = 0.75f + 0.25f * Mathf.Sin(Time.time * pulseSpeed);
            alpha *= pulse;
        }

        overlay.color = new Color(alertColor.r, alertColor.g, alertColor.b, alpha);
    }

    /// <summary>
    /// Geçici sayısal gösterge. OnGUI kasıtlı tercih: font asset'i ve Canvas
    /// kurulumu gerektirmez, iş bitince tek satırla silinir.
    /// </summary>
    private void OnGUI()
    {
        // Yayın build'ine sızmasın diye derleme seviyesinde kapatılıyor:
        // Inspector'daki tiki kapatmayı unutmak jam'de sık yapılan bir hata.
#if !UNITY_EDITOR && !DEVELOPMENT_BUILD
        return;
#else
        if (!showDebugReadout)
            return;

        if (debugStyle == null)
        {
            debugStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                normal = { textColor = Color.yellow }
            };
        }

        float speed = PlayerTarget.Instance != null ? PlayerTarget.Instance.NormalizedSpeed : 0f;

        var rect = new Rect(12f, 12f, 700f, 22f);
        GUI.Label(rect, $"Hız: {speed:F2}   Şüphe: {EnemySpotter.HighestSuspicion():F2}", debugStyle);

        Vector3 playerPosition = PlayerTarget.Instance != null
            ? PlayerTarget.Instance.Position
            : Vector3.zero;

        var spotters = EnemySpotter.Active;
        for (int i = 0; i < spotters.Count; i++)
        {
            EnemySpotter spotter = spotters[i];

            rect.y += 20f;
            float distance = Vector3.Distance(playerPosition, spotter.EyeWorldPosition);
            string sight = spotter.HasLineOfSight ? "GÖRÜYOR" : "kayıp";

            GUI.Label(rect,
                $"  {spotter.name}: şüphe {spotter.Suspicion:F2}  mesafe {distance:F0}m  [{sight}]",
                debugStyle);

            // Zincirin nerede koptuğu görünsün: görüş → şüphe → kule → nişan.
            var turret = spotter.GetComponent<EnemyTurret>();
            if (turret == null)
                continue;

            rect.y += 20f;
            string state = turret.IsAimedAtPlayer ? "NİŞAN ALDI"
                         : turret.IsEngaged ? "sana dönüyor"
                         : "tarıyor";

            GUI.Label(rect,
                $"      kule: {state}  açı {turret.CurrentYaw:F0}° → {turret.TargetYaw:F0}°  " +
                $"(okuduğu şüphe {turret.SpotterSuspicion:F2} / eşik {turret.EngageSuspicion:F2})",
                debugStyle);
        }
#endif
    }
}
