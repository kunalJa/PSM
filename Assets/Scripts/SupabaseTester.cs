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

        // Test: Notifications workflow
        Debug.Log("<color=magenta>--- Testing Notifications Workflow ---</color>");
        
        // Step 1: Get all notifications
        Debug.Log("Step 1: Fetching notifications...");
        var notifications = await BeachService.Instance.GetNotifications();
        
        // Step 2: Mark the first unread notification as read (if any exist)
        if (notifications.Count > 0)
        {
            var firstNotif = notifications[0];
            if (!firstNotif.IsRead)
            {
                Debug.Log($"Step 2: Marking notification {firstNotif.Id} as read...");
                await BeachService.Instance.MarkNotificationAsRead(firstNotif.Id);
                
                // Step 3: Fetch notifications again to confirm the marked one is filtered out
                Debug.Log("Step 3: Fetching notifications again to confirm...");
                var updatedNotifications = await BeachService.Instance.GetNotifications();
                
                if (updatedNotifications.Count < notifications.Count)
                {
                    Debug.Log($"<color=green>SUCCESS:</color> Notification count reduced from {notifications.Count} to {updatedNotifications.Count}");
                }
                else
                {
                    Debug.LogWarning($"<color=orange>UNEXPECTED:</color> Notification count did not decrease (was {notifications.Count}, now {updatedNotifications.Count})");
                }
            }
            else
            {
                Debug.Log("Step 2: First notification already read, skipping mark as read test.");
            }
        }
        else
        {
            Debug.Log("Step 2-3: No notifications to test with.");
        }

        // Test: Delete friend (commented out - uncomment with a real friend_id to test)
        // Debug.Log("<color=magenta>--- Testing Delete Friend ---</color>");
        // string testFriendId = "some-friend-uuid-here";
        // await BeachService.Instance.DeleteFriend(testFriendId);

        Debug.Log("<color=yellow>--- Beach Tests Complete ---</color>");
    }
}