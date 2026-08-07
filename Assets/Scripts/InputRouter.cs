using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Aktif girdi şeması. Ayarlardan seçilecek; MVP'de sadece Simple kullanılıyor.
/// </summary>
public enum ControlScheme
{
    /// <summary>W/S ileri-geri, A/D dönüş.</summary>
    Simple,

    /// <summary>Q/A sol palet, E/D sağ palet. (Sonradan eklenecek.)</summary>
    TrackLevers
}

/// <summary>
/// Klavyeyi okur, aktif şemaya göre iki palet değerine çevirir ve
/// aktif istasyona yönlendirir. Oyun mantığı içermez.
/// </summary>
public class InputRouter : MonoBehaviour
{
    [SerializeField] private ControlScheme scheme = ControlScheme.Simple;
    [SerializeField] private StationManager stationManager;
    [SerializeField] private TankController tankController;

    private void Reset()
    {
        // Editörde component eklenince alanları otomatik doldur.
        stationManager = GetComponent<StationManager>();
        tankController = GetComponent<TankController>();
    }

    private void Update()
    {
        // Klavye yoksa (örn. build hedefi dokunmatik) sessizce çık.
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null || tankController == null)
            return;

        // Sürücü koltuğunda değilsek tank hareket etmez.
        if (stationManager != null && !stationManager.IsAt(Station.Driver))
        {
            tankController.SetTrackInput(0f, 0f);
            return;
        }

        float left, right;

        if (scheme == ControlScheme.TrackLevers)
            ReadTrackLevers(keyboard, out left, out right);
        else
            ReadSimple(keyboard, out left, out right);

        tankController.SetTrackInput(left, right);
    }

    /// <summary>W/S ileri-geri, A/D dönüş. İki eksen palet değerlerine çevrilir.</summary>
    private void ReadSimple(Keyboard keyboard, out float left, out float right)
    {
        float drive = Axis(keyboard.wKey.isPressed, keyboard.sKey.isPressed);
        float turn = Axis(keyboard.dKey.isPressed, keyboard.aKey.isPressed);

        // Sağa dönüş = sol palet ileri, sağ palet geri.
        left = Mathf.Clamp(drive + turn, -1f, 1f);
        right = Mathf.Clamp(drive - turn, -1f, 1f);
    }

    /// <summary>Q/A sol paleti, E/D sağ paleti bağımsız sürer.</summary>
    private void ReadTrackLevers(Keyboard keyboard, out float left, out float right)
    {
        left = Axis(keyboard.qKey.isPressed, keyboard.aKey.isPressed);
        right = Axis(keyboard.eKey.isPressed, keyboard.dKey.isPressed);
    }

    private static float Axis(bool positive, bool negative)
    {
        float value = 0f;
        if (positive) value += 1f;
        if (negative) value -= 1f;
        return value;
    }
}
