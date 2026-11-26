using UnityEngine;
using UnityEngine.EventSystems;

public class EnvelopeClickHandler : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private Animator parentAnimator;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    public void OnPointerClick(PointerEventData eventData)
    {
        if (parentAnimator == null)
        {
            Debug.LogError("Parent Animator is not assigned!");
            return;
        }
        
        parentAnimator.SetTrigger("Open");
    }
}
