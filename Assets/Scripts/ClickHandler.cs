using UnityEngine;
using UnityEngine.EventSystems;

public class ClickHandler : MonoBehaviour, IPointerClickHandler
{
    // Drag your Main Camera into this slot in the Unity Inspector
    public Animator anim;
    public void OnPointerClick(PointerEventData eventData)
    {
        if (anim != null)
        {
            AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.IsName("Looking at Table"))
            {
                anim.SetTrigger("camera_from_table_trigger");
            }
            else if (stateInfo.IsName("Looking at Beach"))
            {
                anim.SetTrigger("camera_to_table_trigger");

            }
        }
        else
        {
            Debug.LogError("Hey! You forgot to drag the Main Camera into the 'Anim' slot on " + gameObject.name);
        }
    }
}
