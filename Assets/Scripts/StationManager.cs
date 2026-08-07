using UnityEngine;

/// <summary>
/// Tankın içindeki istasyonlar. MVP'de sadece Driver kullanılıyor,
/// diğerleri baştan tanımlı ki sonradan koltuk eklemek mimariyi bozmasın.
/// </summary>
public enum Station
{
    Driver,
    Commander,
    Observer,
    RadioOperator
}

/// <summary>
/// O an hangi istasyonda olunduğunu tutan basit durum makinesi.
/// Kamera konumunu ve hangi girdilerin dinleneceğini bu sınıf belirler.
/// </summary>
public class StationManager : MonoBehaviour
{
    [SerializeField] private Station startingStation = Station.Driver;

    /// <summary>Aktif istasyon. Sadece bu sınıf üzerinden değiştirilir.</summary>
    public Station CurrentStation { get; private set; }

    /// <summary>İstasyon değiştiğinde tetiklenir; kamera ve UI buna abone olur.</summary>
    public event System.Action<Station> StationChanged;

    private void Awake()
    {
        CurrentStation = startingStation;
    }

    private void Start()
    {
        // Abone olanların ilk durumu kaçırmaması için bir kez yayınla.
        StationChanged?.Invoke(CurrentStation);
    }

    public void SetStation(Station station)
    {
        if (CurrentStation == station)
            return;

        CurrentStation = station;
        StationChanged?.Invoke(CurrentStation);
    }

    public bool IsAt(Station station) => CurrentStation == station;
}
