using UnityEngine;
using System.Threading.Tasks;
using Supabase.Gotrue;
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
            Debug.Log("Tester: Found saved session! Restoring...");
            
            try
            {
                var savedSession = Newtonsoft.Json.JsonConvert.DeserializeObject<Session>(savedSessionJson);
                
                if (savedSession != null && !string.IsNullOrEmpty(savedSession.AccessToken) && !string.IsNullOrEmpty(savedSession.RefreshToken))
                {
                    // Step 1: Use SetAuth to initialize CurrentSession with access token
                    client.Auth.SetAuth(savedSession.AccessToken);
                    
                    // Step 2: Manually restore the refresh token on the session
                    client.Auth.CurrentSession.RefreshToken = savedSession.RefreshToken;
                    client.Auth.CurrentSession.ExpiresIn = savedSession.ExpiresIn;
                    client.Auth.CurrentSession.CreatedAt = savedSession.CreatedAt;
                    
                    // Step 3: Check if expired and refresh if needed
                    if (savedSession.ExpiresAt() <= DateTime.Now)
                    {
                        Debug.Log("Tester: Token expired, refreshing...");
                        var refreshedSession = await client.Auth.RefreshSession();
                        
                        if (refreshedSession != null)
                        {
                            string freshJson = Newtonsoft.Json.JsonConvert.SerializeObject(refreshedSession);
                            AppManager.Instance.SaveSession(freshJson);
                        }
                    }
                    
                    Debug.Log($"<color=cyan>WELCOME BACK!</color> {client.Auth.CurrentSession?.User?.Email}");
                    return;
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
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Login failed: {e.Message}");
        }
    }
}