using UnityEngine;
using UnityEngine.EventSystems; // 1. Added the EventSystems namespace

// We still require a Collider because this specific face needs to be clicked
[RequireComponent(typeof(Collider))]
public class RolodexClickHandler : MonoBehaviour, IPointerClickHandler // 2. Added the interface
{
    [SerializeField] private Animator parentAnimator;

    [Header("Click Settings")]
    [Tooltip("How many seconds to ignore clicks after the rolodex is clicked.")]
    public float clickCooldown = 0.5f; // Set this to roughly the length of your animation
    
    private float nextAllowedClickTime = 0f;

    public void OnPointerClick(PointerEventData eventData)
    {
        // 1. THE GATEKEEPER: Has enough time passed since the last click?
        if (Time.time < nextAllowedClickTime)
        {
            // If we haven't reached the allowed time yet, stop here. Ignore the click.
            Debug.Log("Click ignored! Animation is still playing.");
            return;
        }

        // 2. RESET THE CLOCK: Lock out future clicks until the cooldown finishes
        nextAllowedClickTime = Time.time + clickCooldown;
        
        Debug.Log("Rolodex clicked via EventSystem!");
        
        // Safety check: Make sure you didn't forget to assign the animator in the Inspector!
        if (parentAnimator == null)
        {
            Debug.LogWarning("Hey! You forgot to assign the Parent Animator on " + gameObject.name);
            return;
        }

        // Check what state the animator is currently in on the base layer (0)
        AnimatorStateInfo stateInfo = parentAnimator.GetCurrentAnimatorStateInfo(0);
        
        if (!stateInfo.IsName("nextCardAnimation")) 
        {
            parentAnimator.SetTrigger("NextCardClicked_trigger");
        }
    }
}