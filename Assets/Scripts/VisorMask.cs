using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Ekranın üzerine dar yatay bir yarık bırakan maske katmanı.
/// Dört siyah panelden oluşur; sanatçının PNG'si hazır olana kadar geçicidir.
/// Yarığın ölçüleri oyunun zorluk dengesini doğrudan belirler, bu yüzden
/// Play modunda canlı ayarlanabilir olmaları önemli.
/// </summary>
public class VisorMask : MonoBehaviour
{
    [Header("Yarık ölçüleri (ekran oranı, 0-1)")]
    [Range(0.05f, 1f)]
    [SerializeField] private float slitWidth = 0.55f;

    [Range(0.02f, 1f)]
    [SerializeField] private float slitHeight = 0.18f;

    [Tooltip("Yarığın dikey merkezi. 0.5 tam orta, altına inmek daha bunaltıcı hissettirir.")]
    [Range(0f, 1f)]
    [SerializeField] private float slitCenterY = 0.52f;

    [Header("Görünüm")]
    [SerializeField] private Color maskColor = Color.black;

    private RectTransform top;
    private RectTransform bottom;
    private RectTransform left;
    private RectTransform right;

    private void Awake()
    {
        BuildOverlay();
        ApplyLayout();
    }

    /// <summary>Canvas ve dört paneli çalışma anında oluşturur.</summary>
    private void BuildOverlay()
    {
        var canvasObject = new GameObject("VisorMaskCanvas", typeof(Canvas), typeof(CanvasScaler));
        canvasObject.transform.SetParent(transform, false);

        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        // Harita ve kazanma ekranı bunun üstünde kalsın diye orta bir değer.
        canvas.sortingOrder = 100;

        top = CreatePanel(canvasObject.transform, "Top");
        bottom = CreatePanel(canvasObject.transform, "Bottom");
        left = CreatePanel(canvasObject.transform, "Left");
        right = CreatePanel(canvasObject.transform, "Right");
    }

    private RectTransform CreatePanel(Transform parent, string panelName)
    {
        var panelObject = new GameObject(panelName, typeof(RectTransform), typeof(Image));
        panelObject.transform.SetParent(parent, false);

        var image = panelObject.GetComponent<Image>();
        image.color = maskColor;
        // Maske tıklamaları yutmasın; ileride harita butonları eklenirse sorun çıkarır.
        image.raycastTarget = false;

        var rect = panelObject.GetComponent<RectTransform>();
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return rect;
    }

    /// <summary>Dört paneli yarık ölçülerine göre yerleştirir.</summary>
    private void ApplyLayout()
    {
        if (top == null)
            return;

        float halfHeight = slitHeight * 0.5f;
        float halfWidth = slitWidth * 0.5f;

        float slitBottom = Mathf.Clamp01(slitCenterY - halfHeight);
        float slitTop = Mathf.Clamp01(slitCenterY + halfHeight);
        float slitLeft = Mathf.Clamp01(0.5f - halfWidth);
        float slitRight = Mathf.Clamp01(0.5f + halfWidth);

        SetAnchors(top, 0f, slitTop, 1f, 1f);
        SetAnchors(bottom, 0f, 0f, 1f, slitBottom);
        SetAnchors(left, 0f, slitBottom, slitLeft, slitTop);
        SetAnchors(right, slitRight, slitBottom, 1f, slitTop);
    }

    private static void SetAnchors(RectTransform rect, float minX, float minY, float maxX, float maxY)
    {
        rect.anchorMin = new Vector2(minX, minY);
        rect.anchorMax = new Vector2(maxX, maxY);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private void OnValidate()
    {
        // Play modunda Inspector'dan canlı ayar yapabilmek için.
        if (Application.isPlaying)
            ApplyLayout();
    }
}
