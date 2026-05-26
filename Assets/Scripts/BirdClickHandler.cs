using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Click relay for a 3D bird. Uses IPointerClickHandler so it works on iOS/Android
/// touch as well as desktop click. Requires the scene to have an EventSystem and
/// a PhysicsRaycaster on the camera that views the birds.
/// </summary>
[RequireComponent(typeof(Collider))]
public class BirdClickHandler : MonoBehaviour, IPointerClickHandler
{
    [Tooltip("The BirdVisual this clickable bird represents. Usually the same GameObject.")]
    [SerializeField] private BirdVisual birdVisual;

    void Reset()
    {
        // Auto-wire if BirdVisual is on the same GameObject
        birdVisual = GetComponent<BirdVisual>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (LetterUIController.IsOpen) return;
        MessengerBirdManager.Instance?.OnBirdClicked(birdVisual);
    }
}
