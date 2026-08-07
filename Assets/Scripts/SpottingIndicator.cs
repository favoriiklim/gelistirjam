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

    private Image overlay;

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
}
