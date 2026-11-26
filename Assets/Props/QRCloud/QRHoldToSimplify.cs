using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Shows the stylized cloud QR by default. While the user presses and holds on the quad,
/// swaps to the high-contrast simple material so a scanner can read it reliably.
/// Uses pointer events so it works with iOS touch and desktop mouse.
/// Requires an EventSystem and a PhysicsRaycaster on the camera, plus a Collider on this object.
/// </summary>
[RequireComponent(typeof(Renderer))]
[RequireComponent(typeof(Collider))]
public class QRHoldToSimplify : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [SerializeField] private Material cloudMaterial;
    [SerializeField] private Material simpleMaterial;

    private Renderer _renderer;
    private bool _isHeld;

    void Awake()
    {
        _renderer = GetComponent<Renderer>();
        ShowCloud();
    }

    void OnDisable()
    {
        _isHeld = false;
        ShowCloud();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        _isHeld = true;
        if (simpleMaterial != null)
            _renderer.sharedMaterial = simpleMaterial;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        Release();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (_isHeld) Release();
    }

    private void Release()
    {
        _isHeld = false;
        ShowCloud();
    }

    private void ShowCloud()
    {
        if (_renderer != null && cloudMaterial != null)
            _renderer.sharedMaterial = cloudMaterial;
    }
}
