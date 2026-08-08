using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Haritada düşman görüş konisini çizen UI elemanı. Doku kullanmaz;
/// üçgen yelpazeyi doğrudan mesh olarak üretir. Böylece açı ve yarıçap
/// serbestçe değişebilir ve sanatçıdan hazır görsel beklemeye gerek kalmaz.
/// </summary>
public class ViewConeGraphic : MaskableGraphic
{
    [SerializeField] private float coneAngle = 80f;
    [SerializeField] private float coneRadius = 60f;
    [SerializeField] private int segments = 24;

    public float ConeAngle
    {
        get => coneAngle;
        set
        {
            if (Mathf.Approximately(coneAngle, value))
                return;

            coneAngle = value;
            SetVerticesDirty();
        }
    }

    public float ConeRadius
    {
        get => coneRadius;
        set
        {
            if (Mathf.Approximately(coneRadius, value))
                return;

            coneRadius = value;
            SetVerticesDirty();
        }
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        int steps = Mathf.Max(3, segments);
        float half = coneAngle * 0.5f;
        Color32 vertexColor = color;

        // Tepe noktası: koninin çıktığı yer.
        vh.AddVert(Vector3.zero, vertexColor, Vector2.zero);

        for (int i = 0; i <= steps; i++)
        {
            float degrees = Mathf.Lerp(-half, half, i / (float)steps);

            // 90 derece eklemek koniyi yukarı baktırır; harita kuzey yukarı.
            float radians = (degrees + 90f) * Mathf.Deg2Rad;

            var point = new Vector3(Mathf.Cos(radians), Mathf.Sin(radians), 0f) * coneRadius;
            vh.AddVert(point, vertexColor, Vector2.zero);
        }

        for (int i = 1; i <= steps; i++)
            vh.AddTriangle(0, i, i + 1);
    }
}
