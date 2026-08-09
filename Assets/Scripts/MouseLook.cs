using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Fareyle kamera çevirme. Dönüş parent'a göre yerel olarak uygulanır:
/// kamera bir aracın child'ıysa araç döndükçe bakış da onunla döner,
/// fare girdisi bunun üstüne biner.
///
/// Sadece bakış yapar; hareket ettirmez.
/// </summary>
public class MouseLook : MonoBehaviour
{
    [Header("Hassasiyet")]
    [Tooltip("Fare girdisi kare başına piksel farkı olarak gelir; " +
             "Time.deltaTime ile ÇARPILMAZ. Çarpılırsa hassasiyet kare " +
             "hızına bağlı olur ve oyun farklı ekranlarda başka hissedilir.")]
    [SerializeField] private float sensitivity = 0.12f;

    [SerializeField] private bool invertY;

    [Header("Dikey sınır")]
    [Range(-89f, 0f)]
    [SerializeField] private float minPitch = -40f;

    [Range(0f, 89f)]
    [SerializeField] private float maxPitch = 30f;

    [Header("Yatay sınır")]
    [Tooltip("Kapalı: kamera serbestçe tam tur döner. " +
             "Açık: aşağıdaki açılarla sınırlanır. Sürücü koltuğunda " +
             "sınırlı bakış hem gerçekçi hem oyunun dar görüş hissini korur.")]
    [SerializeField] private bool limitYaw = true;

    [Range(-180f, 0f)]
    [SerializeField] private float minYaw = -55f;

    [Range(0f, 180f)]
    [SerializeField] private float maxYaw = 55f;

    [Header("İmleç")]
    [Tooltip("Açık: imleç ekrana kilitlenir. Esc bırakır, sol tık geri kilitler.")]
    [SerializeField] private bool lockCursor = true;

    private float yaw;
    private float pitch;

    private void Awake()
    {
        // Objenin sahnedeki başlangıç açısını koru; sıfırlamak kamerayı sıçratır.
        Vector3 startAngles = transform.localEulerAngles;
        pitch = NormalizeAngle(startAngles.x);
        yaw = NormalizeAngle(startAngles.y);
    }

    private void OnEnable()
    {
        if (lockCursor)
            SetCursorLocked(true);
    }

    private void OnDisable()
    {
        SetCursorLocked(false);
    }

    private void Update()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null)
            return;

        HandleCursorToggle(mouse);

        // İmleç serbestken bakış dönmesin; pencereye tıklarken kamera savrulur.
        if (lockCursor && Cursor.lockState != CursorLockMode.Locked)
            return;

        Vector2 delta = mouse.delta.ReadValue() * sensitivity;

        yaw += delta.x;
        pitch += invertY ? delta.y : -delta.y;

        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        if (limitYaw)
            yaw = Mathf.Clamp(yaw, minYaw, maxYaw);

        // Yerel dönüş: araç döndükçe bakış da onunla gider.
        transform.localRotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    private void HandleCursorToggle(Mouse mouse)
    {
        if (!lockCursor)
            return;

        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            SetCursorLocked(false);

        // Tarayıcılar imleç kilidini ancak kullanıcı tıklamasıyla verir;
        // WebGL'de kilit bu yüzden ilk tıklamada kurulur.
        if (mouse.leftButton.wasPressedThisFrame && Cursor.lockState != CursorLockMode.Locked)
            SetCursorLocked(true);
    }

    private static void SetCursorLocked(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }

    /// <summary>eulerAngles 0-360 döner; -40 gibi değerler 320 olarak gelir.</summary>
    private static float NormalizeAngle(float angle)
    {
        return angle > 180f ? angle - 360f : angle;
    }
}
