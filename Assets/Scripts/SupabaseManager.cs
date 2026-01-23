using UnityEngine;
using Supabase; // Make sure you've imported the Supabase DLLs/Package
using System.Threading.Tasks;

public class AppManager : MonoBehaviour
{
    // We use a "Singleton" pattern so other scripts (like seagulls) 
    // can easily find the Supabase client.
    public static AppManager Instance { get; private set; }

    public string supabaseUrl = "https://zlkykoziuifkhevbwpdx.supabase.co";
    public string supabaseKey = "sb_publishable_bmTPJV3GAvCrOByAM2ng5g_XFGEimmu";

    public Client SupabaseClient { get; private set; }

    void Awake()
    {
        // This ensures there's only ever one manager
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Keeps this alive when switching scenes
        }
        else
        {
            Destroy(gameObject);
        }
    }

    async void Start()
    {
        // Initialize Supabase
        var options = new SupabaseOptions { AutoRefreshToken = true };
        SupabaseClient = new Client(supabaseUrl, supabaseKey, options);
        await SupabaseClient.InitializeAsync();
        
        Debug.Log("Supabase is ready to bring in the mail!");
    }

    public void SaveSession(string json)
    {
        // Scribble the login token onto the phone's memory
        PlayerPrefs.SetString("supabase_session", json);
        PlayerPrefs.Save();
    }

    public string GetSavedSession()
    {
        // Look at the notepad and see if we have a token
        return PlayerPrefs.GetString("supabase_session", null);
    }

    public void ClearSession()
    {
        // Erase the notepad when the session is no longer valid
        PlayerPrefs.DeleteKey("supabase_session");
        PlayerPrefs.Save();
    }
}