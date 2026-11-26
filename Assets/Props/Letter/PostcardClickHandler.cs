using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

public class PostcardClickHandler : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private Animator parentAnimator;
    private bool isLetterOpen = false;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    void OnEnable()
    {
        EnhancedTouchSupport.Enable();
    }

    void OnDisable()
    {
        EnhancedTouchSupport.Disable();
    }

    // Update is called once per frame
    void Update()
    {
     // If the trigger hasn't been set yet, stop here and do nothing
        if (!isLetterOpen) return;

        Vector2? inputPosition = null;

        // Check for mouse input (Desktop)
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            inputPosition = Mouse.current.position.ReadValue();
        }
        // Check for touch input (iOS/Android)
        else if (Touch.activeTouches.Count > 0 && Touch.activeTouches[0].phase == UnityEngine.InputSystem.TouchPhase.Began)
        {
            inputPosition = Touch.activeTouches[0].screenPosition;
        }

        if (inputPosition.HasValue)
        {
            Ray ray = Camera.main.ScreenPointToRay(inputPosition.Value);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                if (hit.collider.gameObject != gameObject)
                {
                    UnFocusLetter();
                }
            }
            else
            {
                UnFocusLetter();
            }
        }
    }

    void UnFocusLetter()
    {
        if (parentAnimator == null)
        {
            Debug.LogError("Parent Animator is not assigned!");
            return;
        }
        
        parentAnimator.ResetTrigger("LetterOpen");
        parentAnimator.SetTrigger("LetterClose");

        isLetterOpen = false; 
    }
    
    public void OnPointerClick(PointerEventData eventData)
    {
        if (parentAnimator == null)
        {
            Debug.LogError("Parent Animator is not assigned!");
            return;
        }
        
        parentAnimator.ResetTrigger("LetterClose");
        parentAnimator.SetTrigger("LetterOpen");
        isLetterOpen = true;

    }
}
