using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI; // Required for the Button component

public class ProgrammaticPolaroid : MonoBehaviour
{
    [Header("Material Settings")]
    [SerializeField] private int photoMaterialIndex = 1;
    [SerializeField] private string texturePropertyName = "_BaseMap"; // Use "_MainTex" for Built-in Pipeline

    [Header("UI Components")]
    [Tooltip("Drag your 'X' Button GameObject here")]
    [SerializeField] private Button deleteButton;
    
    [Tooltip("Optional: A fallback texture (like a blank grey image) to display when empty")]
    [SerializeField] private Texture2D fallbackBlankTexture;

    private MeshRenderer _meshRenderer;
    
    // This keeps track of the texture currently in VRAM so we can destroy it later
    private Texture2D _activeRuntimeTexture;

    void Awake()
    {
        _meshRenderer = GetComponent<MeshRenderer>();
        
        // Setup the button click listener in code
        if (deleteButton != null)
        {
            deleteButton.onClick.AddListener(DeleteCurrentPhoto);
            deleteButton.gameObject.SetActive(false); // Hide the X initially since there is no photo
        }
        ApplyFetchedPhoto("https://picsum.photos/200/300");
    }

    public async void ApplyFetchedPhoto(string pathOrUrl)
    {
        Debug.Log($"Applying photo from: {pathOrUrl}");
        Texture2D fetchedTexture = await FetchTextureAsync(pathOrUrl);

        if (fetchedTexture != null)
        {
            // Clean up any previously downloaded photo before assigning the new one
            ClearActiveTextureFromVRAM();

            _activeRuntimeTexture = fetchedTexture;
            AssignTextureToSlot(_activeRuntimeTexture);

            // Show the X button now that an image exists
            if (deleteButton != null) deleteButton.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// Triggered when the 'X' button is clicked. Cleans VRAM and resets the mesh.
    /// </summary>
    public void DeleteCurrentPhoto()
    {
        // 1. Wipe the image out of the graphics card memory entirely
        ClearActiveTextureFromVRAM();

        // 2. Reset the Polaroid material back to blank/default
        AssignTextureToSlot(fallbackBlankTexture);

        // 3. Hide the X button again
        if (deleteButton != null) deleteButton.gameObject.SetActive(false);
    }

    private void ClearActiveTextureFromVRAM()
    {
        if (_activeRuntimeTexture != null)
        {
            // This is the magic line that prevents the VRAM memory leak
            Destroy(_activeRuntimeTexture); 
            _activeRuntimeTexture = null;
            
            // Forces Unity to clean up unreferenced assets out of memory pipeline immediately
            System.GC.Collect(); 
        }
    }

    private async Task<Texture2D> FetchTextureAsync(string url)
    {
        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(url))
        {
            var operation = request.SendWebRequest();

            while (!operation.isDone)
            {
                await Task.Yield();
            }

            if (request.result == UnityWebRequest.Result.Success)
            {
                return DownloadHandlerTexture.GetContent(request);
            }
            
            Debug.LogError($"Failed to fetch image: {request.error}");
            return null;
        }
    }

    private void AssignTextureToSlot(Texture2D texture)
    {
        Material[] currentMaterials = _meshRenderer.materials;

        if (photoMaterialIndex < currentMaterials.Length)
            {
            currentMaterials[photoMaterialIndex].SetTexture(texturePropertyName, texture);
            _meshRenderer.materials = currentMaterials;
        }
    }

    // Best practice: Ensure cleanup if the entire GameObject is destroyed or scene changes
    void OnDestroy()
    {
        ClearActiveTextureFromVRAM();
    }
}