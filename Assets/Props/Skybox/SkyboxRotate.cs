using UnityEngine;

public class SkyboxRotate : MonoBehaviour
{
    public float rotateSpeed = 1.2f;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
       if (RenderSettings.skybox != null)
        {
            // Get the current rotation value and increment it
            float currentRotation = RenderSettings.skybox.GetFloat("_Rotation");
            currentRotation += rotateSpeed * Time.deltaTime;

            // Optional: wrap the rotation around 360 degrees to prevent the float from growing indefinitely
            // currentRotation %= 360f;

            // Set the new rotation value
            RenderSettings.skybox.SetFloat("_Rotation", currentRotation);
        }
        else
        {
            Debug.LogWarning("No skybox material assigned in RenderSettings.");
        } 
    }
}
