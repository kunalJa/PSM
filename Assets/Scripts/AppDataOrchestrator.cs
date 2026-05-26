using UnityEngine;
using System.Threading.Tasks;
using System.Collections.Generic;
using Supabase.Gotrue;
using static Supabase.Gotrue.Constants;
using System;
using System.Linq;

public class AppDataOrchestrator : MonoBehaviour
{
    public static AppDataOrchestrator Instance { get; private set; }
    
    // Cached data from last fetch
    public List<FriendData> Friends { get; private set; } = new List<FriendData>();
    public List<SystemNotification> Notifications { get; private set; } = new List<SystemNotification>();
    // Posts are now managed by the prefetch pipeline below
    
    // Events for UI managers to subscribe to
    public event Action<List<FriendData>> OnFriendsLoaded;
    public event Action<List<SystemNotification>> OnNotificationsLoaded;
    public event Action OnAllDataLoaded;

    // ===== MESSENGER BIRD PREFETCH PIPELINE =====
    private HashSet<string> _knownPostIds = new HashSet<string>();
    private Queue<Post> _pendingPosts = new Queue<Post>();
    private Queue<(Post post, Texture2D tex)> _readyPosts = new Queue<(Post, Texture2D)>();
    private string _inFlightPostId = null;
    private bool _isPrefetching = false;
    private const int PREFETCH_SLOTS = 3;
    private const string MOCK_MEDIA_URL = "https://picsum.photos/200/300";

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
            SmartMergeUnreadPosts(postsTask.Result);
            
            Debug.Log($"<color=green>Data loaded:</color> {Friends.Count} friends, {Notifications.Count} notifications, {_pendingPosts.Count + _readyPosts.Count} unread posts");
            
            // Fire events for UI managers
            OnFriendsLoaded?.Invoke(Friends);
            OnNotificationsLoaded?.Invoke(Notifications);
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
        var freshPosts = await BeachService.Instance.GetUnreadPosts();
        SmartMergeUnreadPosts(freshPosts);
    }

    private void SmartMergeUnreadPosts(List<Post> freshPosts)
    {
        int addedCount = 0;

        foreach (var post in freshPosts)
        {
            if (post.Id == _inFlightPostId) continue;
            if (_knownPostIds.Contains(post.Id)) continue;

            if (string.IsNullOrEmpty(post.MediaUrl))
            {
                post.MediaUrl = MOCK_MEDIA_URL;
                post.MediaType = "image";
            }

            _knownPostIds.Add(post.Id);
            _pendingPosts.Enqueue(post);
            addedCount++;
        }

        if (addedCount > 0)
        {
            Debug.Log($"<color=cyan>SmartMerge: Added {addedCount} new posts to pending queue</color>");
        }

        StartPrefetchIfNeeded();
    }

    private void StartPrefetchIfNeeded()
    {
        if (_isPrefetching) return;
        if (_readyPosts.Count >= PREFETCH_SLOTS) return;
        if (_pendingPosts.Count == 0) return;

        _ = PrefetchPipeline();
    }

    private async Task PrefetchPipeline()
    {
        _isPrefetching = true;

        while (_pendingPosts.Count > 0 && _readyPosts.Count < PREFETCH_SLOTS)
        {
            var post = _pendingPosts.Dequeue();

            string url = !string.IsNullOrEmpty(post.MediaUrl) ? post.MediaUrl : MOCK_MEDIA_URL;
            Texture2D tex = await TextureFetchUtility.FetchAsync(url);

            _readyPosts.Enqueue((post, tex));
            MessengerBirdManager.Instance?.SetBirdCount(Mathf.Min(_readyPosts.Count, 2));

            Debug.Log($"<color=green>Prefetch complete for post {post.Id}, ready queue: {_readyPosts.Count}</color>");

            await Task.Yield();
        }

        _isPrefetching = false;
        StartPrefetchIfNeeded();
    }

    public (Post post, Texture2D tex)? DequeuePost()
    {
        if (_readyPosts.Count == 0) return null;

        var (post, tex) = _readyPosts.Dequeue();
        _inFlightPostId = post.Id;

        MessengerBirdManager.Instance?.SetBirdCount(Mathf.Min(_readyPosts.Count, 2));
        StartPrefetchIfNeeded();

        return (post, tex);
    }

    public async Task MarkPostRead(string postId)
    {
        _inFlightPostId = null;
        await BeachService.Instance.MarkAsRead(postId);
        Debug.Log($"<color=green>Post {postId} marked as read</color>");
    }
}