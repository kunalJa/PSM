using System.Collections.Generic;
using UnityEngine;
using ZXing;
using ZXing.QrCode;
using ZXing.Common;

public class CloudQRGenerator : MonoBehaviour
{
    [Header("QR Settings")]
    public int resolution = 33; // Keep this small and square (e.g., 33x33)
    
    // The material holding your Cloud Shader Graph
    public Material cloudMaterial; 

    public void GenerateCloudQR(string friendToken)
    {
        // string deepLinkUrl = "https://cozybeach.app/connect?token=" + friendToken;
        string deepLinkUrl = "https://repete.art";

        // 1. Configure the QR Writer for High Error Correction
        var hints = new Dictionary<EncodeHintType, object> {
            { EncodeHintType.ERROR_CORRECTION, ZXing.QrCode.Internal.ErrorCorrectionLevel.H },
            { EncodeHintType.MARGIN, 1 } // Minimal quiet zone, let the sky act as the margin
        };

        var writer = new QRCodeWriter();
        
        // 2. Generate the raw boolean data (BitMatrix)
        BitMatrix bitMatrix = writer.encode(deepLinkUrl, BarcodeFormat.QR_CODE, resolution, resolution, hints);

        // 3. Convert the BitMatrix into a Texture2D
        Texture2D qrTexture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
        
        // Set filter mode to Point so the shader gets hard, exact pixels to work with
        qrTexture.filterMode = FilterMode.Point; 

        Color32[] pixels = new Color32[resolution * resolution];

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                // ZXing BitMatrix is true for "Black" (data), false for "White" (empty)
                // We flip it: Data becomes White (Cloud), Empty becomes Transparent (Sky)
                bool isDataBlock = bitMatrix[x, y];
                
                // Note: Unity textures read from bottom-up, ZXing reads top-down. We invert the Y axis.
                int pixelIndex = ((resolution - 1 - y) * resolution) + x; 
                
                pixels[pixelIndex] = isDataBlock ? new Color32(255, 255, 255, 255) : new Color32(0, 0, 0, 0);
            }
        }

        // 4. Apply pixels and push to the GPU
        qrTexture.SetPixels32(pixels);
        qrTexture.Apply();

        // 5. Pass the generated texture to your Shader Graph
        if (cloudMaterial != null)
        {
            // Make sure your Shader Graph has a Texture2D property named "_QRMask"
            cloudMaterial.SetTexture("_QRMask", qrTexture);
        }
    }

    void Start()
    {   
        GenerateCloudQR("test");
        Debug.Log("Cloud QR generated");
    }
}
