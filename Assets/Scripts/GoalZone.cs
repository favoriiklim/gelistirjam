using UnityEngine;

/// <summary>
/// Haritanın ucundaki güvenli liman. Oyuncu içine girdiğinde oyun kazanılır.
/// Karar vermez, sadece GameManager'a haber verir.
/// </summary>
[RequireComponent(typeof(Collider))]
public class GoalZone : MonoBehaviour
{
    [SerializeField] private Color gizmoColor = new Color(0.2f, 1f, 0.4f, 0.25f);

    private void Reset()
    {
        // Trigger olmayan bir kutu oyuncuyu duvar gibi durdurur; en sık hata bu.
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        // Tank'ın Rigidbody'si kökte, collider da kökte; yine de child bir
        // collider girerse diye parent zincirine bakıyoruz.
        if (other.GetComponentInParent<PlayerTarget>() == null)
            return;

        if (GameManager.Instance != null)
            GameManager.Instance.ReportGoalReached();
    }

    private void OnDrawGizmos()
    {
        var box = GetComponent<BoxCollider>();
        if (box == null)
            return;

        Gizmos.color = gizmoColor;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawCube(box.center, box.size);
    }
}
