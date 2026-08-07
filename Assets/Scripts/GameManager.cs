using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public enum GameState
{
    Playing,
    Won,
    Lost
}

/// <summary>
/// Kazanma, kaybetme ve yeniden başlatma kararlarını verir.
/// Hiçbir şey çizmez; durumu olay olarak yayınlar, UI dinler.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public GameState State { get; private set; } = GameState.Playing;

    /// <summary>Oyun durumu değiştiğinde tetiklenir.</summary>
    public event System.Action<GameState> StateChanged;

    private void Awake()
    {
        Instance = this;

        // Önceki turdan donmuş kalmış olabilir; sahne her açıldığında sıfırla.
        Time.timeScale = 1f;
    }

    private void OnEnable()
    {
        EnemySpotter.PlayerSpotted += HandlePlayerSpotted;
    }

    private void OnDisable()
    {
        // Statik olaydan çıkmak şart: sahne yeniden yüklendiğinde ölü
        // GameManager'lar dinlemeye devam eder ve oyun anında biter.
        EnemySpotter.PlayerSpotted -= HandlePlayerSpotted;

        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        if (keyboard.rKey.wasPressedThisFrame)
            Restart();
    }

    private void HandlePlayerSpotted(EnemySpotter spotter)
    {
        EndGame(GameState.Lost);
    }

    /// <summary>Hedef bölgeye ulaşıldığında GoalZone tarafından çağrılır.</summary>
    public void ReportGoalReached()
    {
        EndGame(GameState.Won);
    }

    private void EndGame(GameState newState)
    {
        // Oyun zaten bittiyse ikinci kez tetiklenmesin; birden fazla düşman
        // aynı karede fark ederse kaybetme ekranı üst üste açılırdı.
        if (State != GameState.Playing)
            return;

        State = newState;
        Time.timeScale = 0f;
        StateChanged?.Invoke(State);
    }

    public void Restart()
    {
        // Yeni sahne donmuş açılmasın diye önce zamanı geri ver.
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
