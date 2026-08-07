using UnityEngine;

/// <summary>
/// Düşmanların takip edeceği hedef. Oyuncu tankının üzerinde durur.
/// Her EnemySpotter'ın sahnede oyuncuyu tek tek araması yerine
/// tek bir statik referans üzerinden ulaşılır.
/// </summary>
public class PlayerTarget : MonoBehaviour
{
    public static PlayerTarget Instance { get; private set; }

    [Tooltip("Düşmanların nişan aldığı nokta. Boş bırakılırsa objenin kendisi kullanılır. " +
             "Gövde merkezi yerine üst kısmı seçmek, tepelerin arkasında saklanmayı doğru hesaplatır.")]
    [SerializeField] private Transform aimPoint;

    [SerializeField] private TankController tankController;

    /// <summary>Görüş hesabında kullanılacak dünya konumu.</summary>
    public Vector3 Position => aimPoint != null ? aimPoint.position : transform.position;

    /// <summary>0-1 arası normalize hız; fark edilme çarpanı olarak kullanılır.</summary>
    public float NormalizedSpeed => tankController != null ? tankController.NormalizedSpeed : 0f;

    private void Awake()
    {
        Instance = this;

        if (tankController == null)
            tankController = GetComponent<TankController>();
    }

    private void OnDestroy()
    {
        // Yeniden başlatmada eski referansın kalmaması için.
        if (Instance == this)
            Instance = null;
    }
}
