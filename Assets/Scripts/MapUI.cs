using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// M tuşuyla açılan harita katmanı. Sahneyi kamerayla render etmez;
/// hazır bir üstten görünüm dokusu kullanır ve oyuncunun dünya konumunu
/// harita dikdörtgenine oranlayarak işaretler.
/// </summary>
public class MapUI : MonoBehaviour
{
    [Header("Dünya sınırları")]
    [Tooltip("Haritanın sol-alt köşesinin dünya koordinatı (X, Z). " +
             "Terrain (-300, 0, -300) konumundaysa buraya (-300, -300) yazılır.")]
    [SerializeField] private Vector2 worldOrigin = new Vector2(-300f, -300f);

    [Tooltip("Haritanın kapsadığı alan (genişlik, uzunluk). Terrain boyutuyla aynı olmalı.")]
    [SerializeField] private Vector2 worldSize = new Vector2(600f, 600f);

    [Header("Görsel")]
    [Tooltip("Sanatçının üstten görünüm çizimi. Boş bırakılırsa düz bir dikdörtgen çizilir.")]
    [SerializeField] private Sprite mapSprite;

    [SerializeField] private Color mapFallbackColor = new Color(0.78f, 0.71f, 0.55f);
    [SerializeField] private Color backdropColor = new Color(0f, 0f, 0f, 0.75f);

    [Tooltip("Haritanın ekranda kapladığı oran.")]
    [Range(0.4f, 1f)]
    [SerializeField] private float screenFill = 0.8f;

    [Header("İşaretler")]
    [SerializeField] private Color playerMarkerColor = new Color(0.15f, 0.15f, 0.15f);
    [SerializeField] private Color goalMarkerColor = new Color(0.15f, 0.55f, 0.25f);
    [SerializeField] private float markerSize = 26f;

    [Tooltip("Hedef bölge. Atanırsa haritada ayrı bir işaretle gösterilir.")]
    [SerializeField] private Transform goal;

    [Header("Düşman işaretleri")]
    [Tooltip("Keşfedilmiş düşmanlar haritada gösterilsin mi. " +
             "Kapatmak oyunu tamamen körlemesine oynanır hâle getirir.")]
    [SerializeField] private bool showDiscoveredEnemies = true;

    [SerializeField] private Color enemyMarkerColor = new Color(0.55f, 0.13f, 0.1f);

    [Tooltip("Şüphe sıfırken görüş konisinin rengi. Kum rengi harita üzerinde " +
             "seçilebilmesi için alfa yeterince yüksek olmalı.")]
    [SerializeField] private Color coneCalmColor = new Color(0.45f, 0.10f, 0.08f, 0.5f);

    [Tooltip("Şüphe doluyken görüş konisinin rengi.")]
    [SerializeField] private Color coneAlertColor = new Color(0.95f, 0.15f, 0.1f, 0.8f);

    [Header("Davranış")]
    [Tooltip("Harita açıkken oyun dursun mu? Kapalıyken haritaya bakmak risk taşır; " +
             "bu oyunun gerginliğine kapalı hâli daha çok uyuyor.")]
    [SerializeField] private bool pauseWhileOpen;

    private GameObject root;
    private RectTransform mapRect;
    private RectTransform playerMarker;
    private RectTransform goalMarker;

    // Her düşman için bir koni ve bir nokta; gözcü sırasıyla eşleşir.
    private readonly List<RectTransform> enemyMarkers = new List<RectTransform>();
    private readonly List<ViewConeGraphic> enemyCones = new List<ViewConeGraphic>();

    private bool isOpen;

    private void Awake()
    {
        BuildUI();
        root.SetActive(false);
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        // Oyun bittiyse harita açılmasın; kazanma ekranının üstüne binerdi.
        bool gameOver = GameManager.Instance != null && GameManager.Instance.State != GameState.Playing;
        if (gameOver)
        {
            if (isOpen)
                SetOpen(false);
            return;
        }

        if (keyboard.mKey.wasPressedThisFrame)
            SetOpen(!isOpen);

        if (isOpen)
            UpdateMarkers();
    }

    private void SetOpen(bool open)
    {
        isOpen = open;
        root.SetActive(open);

        if (pauseWhileOpen)
            Time.timeScale = open ? 0f : 1f;

        if (open)
            UpdateMarkers();
    }

    private void UpdateMarkers()
    {
        PlayerTarget player = PlayerTarget.Instance;
        if (player != null)
        {
            PlaceMarker(playerMarker, player.Position);

            // İşaretçi tankın baktığı yönü göstersin. Dünya Y ekseni etrafındaki
            // dönüş saat yönünde artar, UI dönüşü ise saat yönünün tersinde;
            // bu yüzden işaret ters çevriliyor.
            float yaw = player.transform.eulerAngles.y;
            playerMarker.localEulerAngles = new Vector3(0f, 0f, -yaw);
        }

        if (goal != null)
            PlaceMarker(goalMarker, goal.position);

        UpdateEnemyMarkers();
    }

    /// <summary>
    /// Sadece keşfedilmiş düşmanları çizer. Bilgi keşifle kazanılsın diye
    /// hiç görülmemiş araçlar haritada yer almaz.
    /// </summary>
    private void UpdateEnemyMarkers()
    {
        if (!showDiscoveredEnemies)
            return;

        var spotters = EnemySpotter.Active;

        // Sahnedeki gözcü sayısı kadar işaret hazırla; düşman sonradan
        // eklenirse liste kendiliğinden büyür.
        while (enemyMarkers.Count < spotters.Count)
            CreateEnemyMarker();

        // Dünya metresi başına düşen harita pikseli.
        float pixelsPerMeter = mapRect.sizeDelta.x / Mathf.Max(worldSize.x, 0.01f);

        for (int i = 0; i < enemyMarkers.Count; i++)
        {
            bool visible = i < spotters.Count && spotters[i].IsDiscovered;

            enemyMarkers[i].gameObject.SetActive(visible);
            enemyCones[i].gameObject.SetActive(visible);

            if (!visible)
                continue;

            EnemySpotter spotter = spotters[i];

            PlaceMarker(enemyMarkers[i], spotter.EyeWorldPosition);
            PlaceMarker((RectTransform)enemyCones[i].transform, spotter.EyeWorldPosition);

            float yaw = spotter.transform.eulerAngles.y;
            enemyCones[i].transform.localEulerAngles = new Vector3(0f, 0f, -yaw);

            enemyCones[i].ConeAngle = spotter.ViewAngle;
            enemyCones[i].ConeRadius = spotter.ViewDistance * pixelsPerMeter;

            // Şüphe yükseldikçe koni kızarır: hangi düşmanın seni aldığı belli olur.
            enemyCones[i].color = Color.Lerp(coneCalmColor, coneAlertColor, spotter.Suspicion);
        }
    }

    private void CreateEnemyMarker()
    {
        var coneObject = new GameObject("EnemyCone", typeof(RectTransform), typeof(ViewConeGraphic));
        coneObject.transform.SetParent(mapRect, false);

        // Script'le eklenen RectTransform sıfır boyutla gelir; mesh boyuttan
        // bağımsız üretiliyor ama dejenere rect bazı yeniden çizim yollarını atlatıyor.
        var coneRect = coneObject.GetComponent<RectTransform>();
        coneRect.pivot = new Vector2(0.5f, 0.5f);
        coneRect.sizeDelta = new Vector2(1f, 1f);

        var cone = coneObject.GetComponent<ViewConeGraphic>();
        cone.raycastTarget = false;
        cone.color = coneCalmColor;
        enemyCones.Add(cone);

        // Nokta koniden sonra oluşturuluyor ki üstünde çizilsin.
        enemyMarkers.Add(CreateMarker("EnemyMarker", enemyMarkerColor));

        // Oyuncu ve hedef her zaman en üstte kalsın, koninin altında kaybolmasın.
        goalMarker.SetAsLastSibling();
        playerMarker.SetAsLastSibling();
    }

    /// <summary>Dünya konumunu harita dikdörtgeni içindeki orana çevirir.</summary>
    private void PlaceMarker(RectTransform marker, Vector3 worldPosition)
    {
        float x = Mathf.InverseLerp(worldOrigin.x, worldOrigin.x + worldSize.x, worldPosition.x);
        float y = Mathf.InverseLerp(worldOrigin.y, worldOrigin.y + worldSize.y, worldPosition.z);

        // Anchor kullanmak çözünürlükten bağımsız çalışır; piksel hesabı yapılırsa
        // farklı ekran boyutlarında işaret kayar.
        marker.anchorMin = new Vector2(x, y);
        marker.anchorMax = new Vector2(x, y);
        marker.anchoredPosition = Vector2.zero;
    }

    private void BuildUI()
    {
        var canvasObject = new GameObject("MapCanvas", typeof(Canvas), typeof(CanvasScaler));
        canvasObject.transform.SetParent(transform, false);

        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        // Vizör maskesinin (100) üstünde, bitiş ekranının (200) altında.
        canvas.sortingOrder = 150;

        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        root = new GameObject("Root", typeof(RectTransform), typeof(Image));
        root.transform.SetParent(canvasObject.transform, false);
        Stretch(root.GetComponent<RectTransform>());

        var backdrop = root.GetComponent<Image>();
        backdrop.color = backdropColor;
        backdrop.raycastTarget = false;

        // Harita her zaman kare kalsın; dikdörtgen olursa işaret konumu bozulur.
        var mapObject = new GameObject("Map", typeof(RectTransform), typeof(Image));
        mapObject.transform.SetParent(root.transform, false);

        mapRect = mapObject.GetComponent<RectTransform>();
        mapRect.anchorMin = new Vector2(0.5f, 0.5f);
        mapRect.anchorMax = new Vector2(0.5f, 0.5f);
        mapRect.anchoredPosition = Vector2.zero;

        float side = 1080f * screenFill;
        mapRect.sizeDelta = new Vector2(side, side);

        var mapImage = mapObject.GetComponent<Image>();
        mapImage.sprite = mapSprite;
        mapImage.color = mapSprite != null ? Color.white : mapFallbackColor;
        mapImage.raycastTarget = false;

        playerMarker = CreateMarker("PlayerMarker", playerMarkerColor);
        goalMarker = CreateMarker("GoalMarker", goalMarkerColor);
        goalMarker.gameObject.SetActive(goal != null);
    }

    private RectTransform CreateMarker(string markerName, Color color)
    {
        var markerObject = new GameObject(markerName, typeof(RectTransform), typeof(Image));
        markerObject.transform.SetParent(mapRect, false);

        var rect = markerObject.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(markerSize, markerSize);
        rect.pivot = new Vector2(0.5f, 0.5f);

        var image = markerObject.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;

        return rect;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    /// <summary>Harita sınırlarının araziyle örtüştüğünü Scene view'da doğrulamak için.</summary>
    private void OnDrawGizmosSelected()
    {
        Vector3 center = new Vector3(worldOrigin.x + worldSize.x * 0.5f, 0f, worldOrigin.y + worldSize.y * 0.5f);
        Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.9f);
        Gizmos.DrawWireCube(center, new Vector3(worldSize.x, 1f, worldSize.y));
    }
}
