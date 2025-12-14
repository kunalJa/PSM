using UnityEngine;

public class ShoreLineFadeController : MonoBehaviour
{
    public Transform startPoint;
    public Transform endPoint;
    public Material waterMaterial;

    private void Update()
    {
        if (startPoint && endPoint && waterMaterial)
        {
            Vector3 lineStart = startPoint.position;
            Vector3 lineEnd = endPoint.position;
            Vector3 lineDirection = (lineEnd - lineStart).normalized;

            waterMaterial.SetVector("_ShorelineStart", lineStart);
            waterMaterial.SetVector("_ShorelineDirection", lineDirection);
        }
    }
}