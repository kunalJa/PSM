using UnityEngine;
using System.Threading.Tasks;
using System.Collections.Generic; // Added this for the Dictionary
using Supabase.Gotrue; 

public class SupabaseTester : MonoBehaviour
{
    async void Start()
    {
        // Wait for the Manager to be ready
        while (AppManager.Instance == null || AppManager.Instance.SupabaseClient == null)
        {
            await Task.Yield();
        }

        Debug.Log("Tester: Manager ready. Attempting to create user with username...");

        // Change these for every test run because Supabase won't allow duplicate emails!
        await CreateTestUser("cozy_seagull_2@mailinator.com", "SafePassword123", "BeachWalker99");
    }

    private async Task CreateTestUser(string email, string password, string username)
    {
        try
        {
            var client = AppManager.Instance.SupabaseClient;

            // 1. Pack the username into the "UserMetadata" dictionary
            // This is exactly what your database function NEW.raw_user_meta_data->>'username' looks for
            var options = new SignUpOptions
            {
                Data = new Dictionary<string, object>
                {
                    { "username", username }
                }
            };

            // 2. Pass the 'options' into the SignUp method
            var session = await client.Auth.SignUp(email, password, options);

            if (session != null && session.User != null)
            {
                Debug.Log($"<color=green>SUCCESS!</color> Created user: {username}");
                Debug.Log($"User ID: {session.User.Id}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"<color=red>SIGN UP FAILED:</color> {e.Message}");
        }
    }
}