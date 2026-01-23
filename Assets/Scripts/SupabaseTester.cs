using UnityEngine;
using System.Threading.Tasks;
using Supabase.Gotrue;
using static Supabase.Gotrue.Constants;
using System;

public class SupabaseTester : MonoBehaviour
{
    async void Start()
    {
        while (AppManager.Instance == null || AppManager.Instance.SupabaseClient == null)
            await Task.Yield();

        var client = AppManager.Instance.SupabaseClient;

        // 1. CHECK FOR SAVED SESSION
        string savedSessionJson = AppManager.Instance.GetSavedSession();

        if (!string.IsNullOrEmpty(savedSessionJson))
        {
            Debug.Log("Tester: Found saved session! Rehydrating via SDK...");
            
            try
            {
                var savedSession = Newtonsoft.Json.JsonConvert.DeserializeObject<Session>(savedSessionJson);
                
                if (savedSession != null && !string.IsNullOrEmpty(savedSession.RefreshToken))
                {
                    // Use the SDK's proper rehydration method: SignIn with RefreshToken
                    // This internally sets CurrentSession, CurrentUser, and updates API headers
                    var session = await client.Auth.SignIn(SignInType.RefreshToken, savedSession.RefreshToken);
                    
                    if (session != null)
                    {
                        // Save the fresh session with new tokens
                        string freshJson = Newtonsoft.Json.JsonConvert.SerializeObject(session);
                        AppManager.Instance.SaveSession(freshJson);
                        
                        Debug.Log($"<color=cyan>WELCOME BACK!</color> {client.Auth.CurrentUser?.Email}");
                        await RunBeachTests();
                        return;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Session restore failed: {e.Message}");
                AppManager.Instance.ClearSession();
            }
        }

        // 2. IF NO SESSION OR EXPIRED, DO MANUAL LOGIN
        Debug.Log("Tester: Logging in...");
        await LoginAndSave("cozy_seagull_2@mailinator.com", "SafePassword123");
    }

    private async Task LoginAndSave(string email, string password)
    {
        try
        {
            var client = AppManager.Instance.SupabaseClient;
            var session = await client.Auth.SignIn(email, password);

            if (session != null)
            {
                // Convert the session to a string and save it to the notepad!
                string sessionJson = Newtonsoft.Json.JsonConvert.SerializeObject(session);
                AppManager.Instance.SaveSession(sessionJson);

                Debug.Log("<color=green>SUCCESS:</color> Logged in and Session Saved!");
                await RunBeachTests();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Login failed: {e.Message}");
        }
    }

    private async Task RunBeachTests()
    {
        Debug.Log("<color=yellow>--- Running Beach Service Tests ---</color>");
        
        if (BeachService.Instance == null)
        {
            Debug.LogWarning("BeachService not found. Create a GameObject with BeachService attached.");
            return;
        }

        // Test: Unread posts workflow
        Debug.Log("<color=magenta>--- Testing Unread Posts Workflow ---</color>");
        
        // Step 1: Get unread posts (should show all friend posts if none marked as read)
        Debug.Log("Step 1: Getting unread posts...");
        await BeachService.Instance.GetUnreadPosts();
        
        // Step 2: Mark the first unread post as read (you'll need to manually set a post ID here)
        // For now, we'll skip this step - you can uncomment and add a real post ID later
        string testPostId = "df548d59-a135-4109-9b82-731e18429301";
        Debug.Log($"Step 2: Marking post {testPostId} as read...");
        await BeachService.Instance.MarkAsRead(testPostId);
        
        // Step 3: Get unread posts again (should show one fewer post)
        Debug.Log("Step 3: Getting unread posts again...");
        await BeachService.Instance.GetUnreadPosts();

        Debug.Log("<color=yellow>--- Beach Tests Complete ---</color>");
    }
}