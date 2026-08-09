using UnityEngine;

/// <summary>
/// Tankın devrilip devrilmediğini izler. Devrilme kalıcıysa oyunu bitirir;
/// oyuncu vizörden yalnızca gökyüzünü ya da kumu görür ve neden ilerleyemediğini
/// anlamaz, o yüzden durum açıkça bildirilmeli.
/// </summary>
public class FlipDetector : MonoBehaviour
{
    [Tooltip("Tankın yukarı ekseninin dünya yukarısıyla çarpımı bu değerin " +
             "altındaysa devrilmiş sayılır. 0 = tam yan yatmış, 1 = dümdüz.")]
    [Range(-1f, 1f)]
    [SerializeField] private float uprightThreshold = 0.25f;

    [Tooltip("Bu kadar saniye devrik kalmadan oyun bitmez. Tümsekten " +
             "inerken bir anlık yatma yüzünden tur bitmesin diye.")]
    [SerializeField] private float graceTime = 2f;

    private float flippedTimer;

    private void Update()
    {
        if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing)
            return;

        bool flipped = Vector3.Dot(transform.up, Vector3.up) < uprightThreshold;

        // Kesintisiz devrik kalma süresi ölçülüyor; düzelirse sayaç sıfırlanır.
        flippedTimer = flipped ? flippedTimer + Time.deltaTime : 0f;

        if (flippedTimer >= graceTime)
            GameManager.Instance.ReportTankFlipped();
    }
}
