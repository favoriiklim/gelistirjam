using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Düşman aracını noktalar arasında gezdirir. Kovalama ya da yol bulma yok;
/// rota elle kurulur. Nokta atanmazsa araç sabit kalır ki aynı bileşen
/// hem devriye hem nöbetçi araçlarda kullanılabilsin.
/// </summary>
public class EnemyPatrol : MonoBehaviour
{
    [Tooltip("Devriye noktaları. Boş bırakılırsa araç hiç hareket etmez.")]
    [SerializeField] private List<Transform> waypoints = new List<Transform>();

    [Header("Hareket")]
    [SerializeField] private float moveSpeed = 4f;

    [Tooltip("Saniyede kaç derece dönülür. Düşük değer, dönüşlerde görüş konisini " +
             "yavaş süpürdüğü için oyuncuya kaçma fırsatı verir.")]
    [SerializeField] private float turnSpeed = 60f;

    [Tooltip("Bir noktaya varınca kaç saniye beklenir.")]
    [SerializeField] private float waitAtWaypoint = 2f;

    [Tooltip("Açık: rota sonunda geri döner. Kapalı: ilk noktaya sarar.")]
    [SerializeField] private bool pingPong = true;

    [Tooltip("Noktaya bu mesafede varılmış sayılır.")]
    [SerializeField] private float arriveThreshold = 1.5f;

    [Tooltip("Hedefle arasındaki açı bu değerin üstündeyken araç ilerlemez, " +
             "yerinde döner. Paletli araç davranışı budur; yüksek tutmak " +
             "aracı araba gibi yay çizdirir.")]
    [Range(1f, 90f)]
    [SerializeField] private float alignAngle = 5f;

    private int targetIndex;
    private int direction = 1;
    private float waitTimer;

    private void Update()
    {
        if (waypoints == null || waypoints.Count == 0)
            return;

        Transform target = waypoints[targetIndex];
        if (target == null)
            return;

        if (waitTimer > 0f)
        {
            waitTimer -= Time.deltaTime;
            return;
        }

        // Yükseklik farkını yok sayıyoruz; araç zaten zeminde duruyor ve
        // dikey bileşen hesaba katılırsa araç tepelere doğru burnunu kaldırır.
        Vector3 toTarget = target.position - transform.position;
        toTarget.y = 0f;

        if (toTarget.magnitude <= arriveThreshold)
        {
            AdvanceToNextWaypoint();
            return;
        }

        Quaternion desired = Quaternion.LookRotation(toTarget.normalized, Vector3.up);

        // Hizalanmadan ilerlemiyoruz: paletli araç önce yerinde döner,
        // sonra düz gider. Aynı anda yapılırsa araba gibi yay çizer.
        float angleToTarget = Quaternion.Angle(transform.rotation, desired);

        transform.rotation = Quaternion.RotateTowards(
            transform.rotation, desired, turnSpeed * Time.deltaTime);

        if (angleToTarget <= alignAngle)
            transform.position += transform.forward * (moveSpeed * Time.deltaTime);
    }

    private void AdvanceToNextWaypoint()
    {
        waitTimer = waitAtWaypoint;

        if (waypoints.Count == 1)
            return;

        if (pingPong)
        {
            // Uçtaki noktaya gelince yönü çevir.
            if (targetIndex + direction < 0 || targetIndex + direction >= waypoints.Count)
                direction = -direction;

            targetIndex += direction;
        }
        else
        {
            targetIndex = (targetIndex + 1) % waypoints.Count;
        }
    }

    /// <summary>Rotayı Scene view'da göstermek; seviye tasarımı buna bakarak yapılır.</summary>
    private void OnDrawGizmos()
    {
        if (waypoints == null || waypoints.Count == 0)
            return;

        Gizmos.color = new Color(1f, 0.6f, 0.1f, 0.9f);

        for (int i = 0; i < waypoints.Count; i++)
        {
            if (waypoints[i] == null)
                continue;

            Gizmos.DrawSphere(waypoints[i].position, 1f);

            Transform next = null;
            if (i + 1 < waypoints.Count)
                next = waypoints[i + 1];
            else if (!pingPong)
                next = waypoints[0];

            if (next != null)
                Gizmos.DrawLine(waypoints[i].position, next.position);
        }
    }
}
