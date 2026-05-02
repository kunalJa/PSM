using System.Collections.Generic;
using UnityEngine;
using ZXing;
using ZXing.QrCode;
using ZXing.Common;

public class CloudQRGenerator : MonoBehaviour
{
    // --- SINGLETON SETUP ---
    public static CloudQRGenerator Instance { get; private set; }

    private void Awake()
    {
        // If there is already an instance and it's not this one, destroy this duplicate
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            // Set the instance to this script
            Instance = this;
            
            // OPTIONAL: Uncomment the line below if you want this QR Generator 
            // to survive when loading new scenes (like going from Main Menu to the Beach)
            // DontDestroyOnLoad(this.gameObject);
        }
    }
    // -----------------------
    
    [Header("QR Settings")]    
    // The material holding your Cloud Shader Graph
    public Material cloudMaterial; 

    public string createDeepLink(string friendToken) 
    {
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        // in Player Settings -> Other Settings -> Supported URL schemes) to listen for the word personalsocialmedia.
        // Custom URI schemes fail ungracefully if the app isn't installed
        // (they just do nothing, or throw a "Safari cannot open the page" error).
        // Before you launch, you will upgrade to Universal Links (https://...
            // return "http://www.thelongestdomainnameintheworldandthensomeandthensomemoreandmore.com/record.htm";
            return "personalsocialmedia://connect?token=" + friendToken;
        #else
            return "https://personalsocialmedia.com/connect?token=" + friendToken;
        #endif
    }

    public void GenerateCloudQR(string friendToken)
    {
        string deepLinkUrl = createDeepLink(friendToken);

        // Configure the QR Writer for High Error Correction
        var hints = new Dictionary<EncodeHintType, object> {
            { EncodeHintType.ERROR_CORRECTION, ZXing.QrCode.Internal.ErrorCorrectionLevel.H },
            { EncodeHintType.MARGIN, 1 } 
        };
        var writer = new QRCodeWriter();

        // Encode! Passing 0, 0 tells the engine to auto-size it to the perfect dynamic resolution
        var bitMatrix = writer.encode(deepLinkUrl, BarcodeFormat.QR_CODE, 0, 0, hints);

        int dynamicWidth = bitMatrix.Width;
        int dynamicHeight = bitMatrix.Height;
        Texture2D qrTexture = new Texture2D(dynamicWidth, dynamicHeight, TextureFormat.RGBA32, false);
        
        qrTexture.filterMode = FilterMode.Bilinear;
        qrTexture.wrapMode = TextureWrapMode.Clamp;
        // Prevents edges from wrapping around 

        Color32[] pixels = new Color32[dynamicWidth * dynamicHeight];

        for (int y = 0; y < dynamicHeight; y++)
        {
            for (int x = 0; x < dynamicWidth; x++)
            {
                // ZXing BitMatrix is true for "Black" (data), false for "White" (empty)
                // We flip it: Data becomes White (Cloud), Empty becomes Transparent (Sky)
                bool isDataBlock = bitMatrix[x, y];
                
                // Note: Unity textures read from bottom-up, ZXing reads top-down. We invert the Y axis.
                int pixelIndex = ((dynamicHeight - 1 - y) * dynamicWidth) + x; 
                
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
        // This fake qrcode is generated before we ever look up just so that theres something there already
        // TODO: make sure the quad texture doesnt flash when becoming a new qrcode
        GenerateCloudQR("fake");
        Debug.Log("Cloud QR generated");
    }
}
