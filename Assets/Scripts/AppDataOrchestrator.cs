using UnityEngine;
using System.Threading.Tasks;
using System.Collections.Generic;
using Supabase.Gotrue;
using static Supabase.Gotrue.Constants;
using System;

public class AppDataOrchestrator : MonoBehaviour
{
    public static AppDataOrchestrator Instance { get; private set; }
    
    // Cached data from last fetch
    public List<FriendData> Friends { get; private set; } = new List<FriendData>();
    public List<SystemNotification> Notifications { get; private set; } = new List<SystemNotification>();
    public List<Post> Posts { get; private set; } = new List<Post>();
    
    // Events for UI managers to subscribe to
    public event Action<List<FriendData>> OnFriendsLoaded;
    public event Action<List<SystemNotification>> OnNotificationsLoaded;
    public event Action<List<Post>> OnPostsLoaded;
    public event Action OnAllDataLoaded;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    async void Start()
    {
        while (AppManager.Instance == null || AppManager.Instance.SupabaseClient == null)
            await Task.Yield();

        var client = AppManager.Instance.SupabaseClient;

        // // 1. CHECK FOR SAVED SESSION
        // string savedSessionJson = AppManager.Instance.GetSavedSession();

        // if (!string.IsNullOrEmpty(savedSessionJson))
        // {
        //     Debug.Log("Orchestrator: Found saved session! Rehydrating...");
            
        //     try
        //     {
        //         var savedSession = Newtonsoft.Json.JsonConvert.DeserializeObject<Session>(savedSessionJson);
                
        //         if (savedSession != null && !string.IsNullOrEmpty(savedSession.RefreshToken))
        //         {
        //             var session = await client.Auth.SignIn(SignInType.RefreshToken, savedSession.RefreshToken);
                    
        //             if (session != null)
        //             {
        //                 string freshJson = Newtonsoft.Json.JsonConvert.SerializeObject(session);
        //                 AppManager.Instance.SaveSession(freshJson);
                        
        //                 Debug.Log($"<color=cyan>WELCOME BACK!</color> {client.Auth.CurrentUser?.Email}");
        //                 await FetchAllData();
        //                 return;
        //             }
        //         }
        //     }
        //     catch (Exception e)
        //     {
        //         Debug.LogWarning($"Session restore failed: {e.Message}");
        //         AppManager.Instance.ClearSession();
        //     }
        // }

        // 2. IF NO SESSION OR EXPIRED, DO MANUAL LOGIN
        Debug.Log("Orchestrator: Logging in...");
        await LoginAndFetchData("cozy_seagull_2@mailinator.com", "SafePassword123");
    }

    private async Task LoginAndFetchData(string email, string password)
    {
        try
        {
            var session = await AppManager.Instance.SignIn(email, password);

            if (session != null)
            {
                string sessionJson = Newtonsoft.Json.JsonConvert.SerializeObject(session);
                AppManager.Instance.SaveSession(sessionJson);

                Debug.Log("<color=green>SUCCESS:</color> Logged in and Session Saved!");
                await FetchAllData();
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Login failed: {e.Message}");
        }
    }

    public async Task FetchAllData()
    {
        Debug.Log("<color=yellow>--- Fetching All Data (Parallel) ---</color>");
        
        try
        {
            // Parallel fetch all data
            var friendsTask = BeachService.Instance.GetAllFriends();
            var notificationsTask = BeachService.Instance.GetNotifications();
            var postsTask = BeachService.Instance.GetUnreadPosts();
            
            await Task.WhenAll(friendsTask, notificationsTask, postsTask);
            
            // Store results
            Friends = friendsTask.Result;
            Notifications = notificationsTask.Result;
            Posts = postsTask.Result;
            
            Debug.Log($"<color=green>Data loaded:</color> {Friends.Count} friends, {Notifications.Count} notifications, {Posts.Count} posts");
            
            // Fire events for UI managers
            OnFriendsLoaded?.Invoke(Friends);
            OnNotificationsLoaded?.Invoke(Notifications);
            OnPostsLoaded?.Invoke(Posts);
            OnAllDataLoaded?.Invoke();
        }
        catch (Exception e)
        {
            Debug.LogError($"FetchAllData failed: {e.Message}");
        }
        
        Debug.Log("<color=yellow>--- Data Fetch Complete ---</color>");
    }
    
    // Individual refresh methods for DataRefreshManager to use later
    public async Task RefreshFriends()
    {
        Friends = await BeachService.Instance.GetAllFriends();
        OnFriendsLoaded?.Invoke(Friends);
    }
    
    public async Task RefreshNotifications()
    {
        Notifications = await BeachService.Instance.GetNotifications();
        OnNotificationsLoaded?.Invoke(Notifications);
    }
    
    public async Task RefreshPosts()
    {
        Posts = await BeachService.Instance.GetUnreadPosts();
        OnPostsLoaded?.Invoke(Posts);
    }
}