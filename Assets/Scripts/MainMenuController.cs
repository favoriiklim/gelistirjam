using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Ana menü davranışı. Kamerayı yavaşça döndürür ve butonları bağlar.
///
/// Menü sahnesi bilerek küçük tutuluyor: WebGL'de asıl haritayı da menüyle
/// birlikte yüklemek açılışı geciktirir. Harita ancak Başla'ya basınca yüklenir.
/// </summary>
public class MainMenuController : MonoBehaviour
{
    [Header("Sahne")]
    [Tooltip("Başla'ya basınca yüklenecek sahne. Build Settings'te ekli olmalı.")]
    [SerializeField] private string gameSceneName = "Main";

    [Header("Butonlar")]
    [SerializeField] private Button startButton;
    [SerializeField] private Button quitButton;

    [Header("Kamera")]
    [Tooltip("Kameranın etrafında döneceği merkez. Kamera bunun child'ı olmalı.")]
    [SerializeField] private Transform cameraPivot;

    [Tooltip("Saniyede kaç derece dönülür. Yavaş tut; hızlı dönüş menüyü huzursuz eder.")]
    [SerializeField] private float driftSpeed = 1.5f;

    private void Awake()
    {
        // Menüden gelirken oyun donmuş kalmasın.
        Time.timeScale = 1f;

        if (startButton != null)
            startButton.onClick.AddListener(StartGame);

        if (quitButton != null)
            quitButton.onClick.AddListener(QuitGame);

#if UNITY_WEBGL && !UNITY_EDITOR
        // Tarayıcıda Application.Quit hiçbir şey yapmaz; butonu göstermek
        // oyuncuya çalışmayan bir seçenek sunmak olur.
        if (quitButton != null)
            quitButton.gameObject.SetActive(false);
#endif
    }

    private void Update()
    {
        if (cameraPivot != null)
            cameraPivot.Rotate(Vector3.up, driftSpeed * Time.deltaTime);
    }

    public void StartGame()
    {
        SceneManager.LoadScene(gameSceneName);
    }

    public void QuitGame()
    {
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
