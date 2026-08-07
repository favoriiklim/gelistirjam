using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Kazanma ve kaybetme ekranı. Karar vermez; GameManager'ın yayınladığı
/// durumu dinleyip gösterir. Sanatçının ekranı geldiğinde sadece bu sınıf
/// değişir, oyun mantığına dokunulmaz.
/// </summary>
public class GameOverUI : MonoBehaviour
{
    [Header("Metinler")]
    [SerializeField] private string wonMessage = "LİMANA ULAŞTIN";
    [SerializeField] private string lostMessage = "FARK EDİLDİN";
    [SerializeField] private string hintMessage = "Yeniden başlatmak için R";

    [Header("Renkler")]
    [SerializeField] private Color wonColor = new Color(0.75f, 0.7f, 0.45f);
    [SerializeField] private Color lostColor = new Color(0.7f, 0.2f, 0.12f);
    [SerializeField] private Color backdropColor = new Color(0f, 0f, 0f, 0.82f);

    private GameObject root;
    private Image backdrop;
    private Text titleText;
    private Text hintText;

    private void Awake()
    {
        BuildUI();
        root.SetActive(false);
    }

    private void Start()
    {
        // GameManager'ın Awake'inden sonra bağlanmak için Start kullanıyoruz.
        if (GameManager.Instance != null)
            GameManager.Instance.StateChanged += HandleStateChanged;
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.StateChanged -= HandleStateChanged;
    }

    private void HandleStateChanged(GameState state)
    {
        if (state == GameState.Playing)
        {
            root.SetActive(false);
            return;
        }

        bool won = state == GameState.Won;

        backdrop.color = backdropColor;
        titleText.text = won ? wonMessage : lostMessage;
        titleText.color = won ? wonColor : lostColor;
        hintText.text = hintMessage;

        root.SetActive(true);
    }

    private void BuildUI()
    {
        var canvasObject = new GameObject("GameOverCanvas", typeof(Canvas), typeof(CanvasScaler));
        canvasObject.transform.SetParent(transform, false);

        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        // Vizör maskesinin (100) üstünde kalmalı, yoksa yarığın dışında görünmez.
        canvas.sortingOrder = 200;

        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        root = new GameObject("Root", typeof(RectTransform), typeof(Image));
        root.transform.SetParent(canvasObject.transform, false);
        Stretch(root.GetComponent<RectTransform>());

        backdrop = root.GetComponent<Image>();
        backdrop.raycastTarget = false;

        titleText = CreateText("Title", 84, new Vector2(0f, 40f));
        hintText = CreateText("Hint", 32, new Vector2(0f, -70f));
        hintText.color = new Color(0.75f, 0.75f, 0.72f);
    }

    private Text CreateText(string objectName, int fontSize, Vector2 offset)
    {
        var textObject = new GameObject(objectName, typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(root.transform, false);

        var rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(1400f, 200f);
        rect.anchoredPosition = offset;

        var text = textObject.GetComponent<Text>();
        // Unity 6'da yerleşik font bu isimle gelir; asset eklemeye gerek yok.
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.alignment = TextAnchor.MiddleCenter;
        text.raycastTarget = false;

        if (text.font == null)
            Debug.LogWarning("GameOverUI: yerleşik font bulunamadı, metin görünmeyecek.");

        return text;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
