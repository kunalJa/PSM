using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using DateTime = System.DateTime;
using Newtonsoft.Json;

public class BeachService
{
    // --- PURE C# SINGLETON SETUP --- this is no longer a monobehavior but is now a instance
    private static BeachService _instance;
    public static BeachService Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new BeachService();
            }
            return _instance;
        }
    }

    // A private constructor prevents any other script from accidentally typing 'new BeachService()'
    private BeachService() { }
    // -------------------------------

    private Supabase.Client Client => AppManager.Instance.SupabaseClient;

    public async Task CreatePost(string text, string mediaUrl = null)
    {
        try
        {
            var currentUserId = Client.Auth.CurrentUser?.Id;
            if (string.IsNullOrEmpty(currentUserId))
            {
                Debug.LogError("BeachService: Not logged in!");
                return;
            }

            var post = new Post
            {
                AuthorId = currentUserId,
                ContentText = text,
                MediaUrl = mediaUrl,
                MediaType = string.IsNullOrEmpty(mediaUrl) ? "none" : "image"
            };

            var response = await Client.From<Post>().Insert(post);
            Debug.Log($"<color=green>Letter sent to the beach!</color> Content: {text}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"CreatePost failed: {e.Message}");
        }
    }

    public async Task GetUnreadPosts()
    {
        try
        {
            var currentUserId = Client.Auth.CurrentUser?.Id;
            if (string.IsNullOrEmpty(currentUserId))
            {
                Debug.LogError("BeachService: Not logged in!");
                return;
            }

            // Get all read post IDs for current user
            var readResponse = await Client.From<ReadReceipt>()
                .Filter("user_id", Postgrest.Constants.Operator.Equals, currentUserId)
                .Get();

            var readPostIds = readResponse.Models.Select(r => (object)r.PostId).ToList();

            // Get friend IDs
            var friendships = await Client.From<Friendship>()
                .Filter("user_id", Postgrest.Constants.Operator.Equals, currentUserId)
                .Get();

            var friendIds = friendships.Models.Select(f => (object)f.FriendId).ToList();

            if (friendIds.Count == 0)
            {
                Debug.Log("<color=yellow>No friends yet. The beach is quiet...</color>");
                return;
            }

            // Get posts from friends, excluding already-read posts (server-side)
            var query = Client.From<Post>()
                .Filter("author_id", Postgrest.Constants.Operator.In, friendIds);
            
            if (readPostIds.Count > 0)
            {
                query = query.Not("id", Postgrest.Constants.Operator.In, readPostIds);
            }
            
            var postsResponse = await query
                .Order("created_at", Postgrest.Constants.Ordering.Descending)
                .Get();

            var unreadPosts = postsResponse.Models;

            if (unreadPosts.Count == 0)
            {
                Debug.Log("<color=cyan>No new letters today. The seagulls rest.</color>");
                return;
            }

            Debug.Log($"<color=yellow>You have {unreadPosts.Count} unread letter(s)!</color>");
            foreach (var post in unreadPosts)
            {
                Debug.Log($"  <color=white>From {post.AuthorId}: {post.ContentText}</color>");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"GetUnreadPosts failed: {e.Message}");
        }
    }

    public async Task<string> GetQRToken()
    {
        try
        {
            var currentUserId = Client.Auth.CurrentUser?.Id;
            if (string.IsNullOrEmpty(currentUserId))
            {
                Debug.LogError("BeachService: Not logged in!");
                return null;
            }

            // Check if we have a stored token, and if it is valid
            string storedToken = PlayerPrefs.GetString("QRToken", null);
            string storedTokenExpiresAt = PlayerPrefs.GetString("QRTokenExpiresAt", null);
            if (string.IsNullOrEmpty(storedToken) || string.IsNullOrEmpty(storedTokenExpiresAt) || System.DateTimeOffset.UtcNow > System.DateTimeOffset.Parse(storedTokenExpiresAt))
            {
                // Generate a new token
                var rpcResponse = await Client.Rpc("refresh_my_qr_token", null);
                Debug.Log($"RPC Response: {rpcResponse?.Content ?? "NULL"}");
                
                if (rpcResponse != null && !string.IsNullOrEmpty(rpcResponse.Content))
                {
                    // RPC returns an array with one row: [{"v_new_token": "...", "v_expiry": "..."}]
                    var responseArray = JsonConvert.DeserializeObject<List<QRTokenResponse>>(rpcResponse.Content);
                    if (responseArray != null && responseArray.Count > 0)
                    {
                        var response = responseArray[0];
                        PlayerPrefs.SetString("QRToken", response.token);
                        // Store expiry as UTC ISO string
                        PlayerPrefs.SetString("QRTokenExpiresAt", response.expires_at.ToUniversalTime().ToString("o"));
                        PlayerPrefs.Save();
                        return response.token;
                    }
                }
            }
            else
            {
                Debug.Log($"Using stored QR token, it expires at {storedTokenExpiresAt}");
            }

            return storedToken;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"GetQRToken failed: {e.Message}");
            return null;
        }
    }

    public async Task<bool> AddFriendByToken(string token)
    {
        try
        {
            var currentUserId = Client.Auth.CurrentUser?.Id;
            if (string.IsNullOrEmpty(currentUserId))
            {
                Debug.LogError("BeachService: Not logged in!");
                return false;
            }

            var rpcResponse = await Client.Rpc("add_friend_via_qr", new Dictionary<string, object> { { "p_qr_token", token } });
            Debug.Log($"RPC Response: {rpcResponse?.Content ?? "NULL"}");
            
            // RPC returns a single boolean: true
            if (rpcResponse != null && !string.IsNullOrEmpty(rpcResponse.Content))
            {
                return JsonConvert.DeserializeObject<bool>(rpcResponse.Content);
            }

            return false;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"AddFriendByToken failed: {e.Message}");
            return false;
        }
    }

    private async Task AddFriendByUsername(string username)
    {
        try
        {
            var currentUserId = Client.Auth.CurrentUser?.Id;
            if (string.IsNullOrEmpty(currentUserId))
            {
                Debug.LogError("BeachService: Not logged in!");
                return;
            }

            // Find the profile with this username
            var profileResponse = await Client.From<Profile>()
                .Filter("username", Postgrest.Constants.Operator.Equals, username)
                .Single();

            if (profileResponse == null)
            {
                Debug.LogWarning($"<color=orange>No user found with username: {username}</color>");
                return;
            }

            var friendId = profileResponse.Id;

            // Check if already friends
            var existingFriendship = await Client.From<Friendship>()
                .Filter("user_id", Postgrest.Constants.Operator.Equals, currentUserId)
                .Filter("friend_id", Postgrest.Constants.Operator.Equals, friendId)
                .Get();

            if (existingFriendship.Models.Count > 0)
            {
                Debug.Log($"<color=cyan>Already friends with {username}!</color>");
                return;
            }

            // Create friendship
            var friendship = new Friendship
            {
                UserId = currentUserId,
                FriendId = friendId
            };

            await Client.From<Friendship>().Insert(friendship);
            Debug.Log($"<color=green>Added {username} as a friend!</color>");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"AddFriendByUsername failed: {e.Message}");
        }
    }

    public async Task GetAllFriends()
    {
        try
        {
            var currentUserId = Client.Auth.CurrentUser?.Id;
            if (string.IsNullOrEmpty(currentUserId))
            {
                Debug.LogError("BeachService: Not logged in!");
                return;
            }

            // Get all friendships for current user
            var friendships = await Client.From<Friendship>()
                .Filter("user_id", Postgrest.Constants.Operator.Equals, currentUserId)
                .Get();

            if (friendships.Models.Count == 0)
            {
                Debug.Log("<color=yellow>No friends on the beach yet...</color>");
                return;
            }

            Debug.Log($"<color=cyan>Friends on the beach ({friendships.Models.Count}):</color>");

            foreach (var friendship in friendships.Models)
            {
                Debug.Log($"  Looking up friend_id: {friendship.FriendId}");
                
                try
                {
                    // Fetch each friend's profile
                    var profileResponse = await Client.From<Profile>()
                        .Filter("id", Postgrest.Constants.Operator.Equals, friendship.FriendId)
                        .Get();

                    if (profileResponse.Models.Count > 0)
                    {
                        var profile = profileResponse.Models[0];
                        Debug.Log($"  <color=white>{profile.Username}</color>");
                    }
                    else
                    {
                        Debug.LogWarning($"  No profile found for id: {friendship.FriendId}");
                    }
                }
                catch (System.Exception profileEx)
                {
                    Debug.LogError($"  Profile lookup failed: {profileEx.Message}");
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"GetAllFriends failed: {e.Message}");
        }
    }

    public async Task MarkAsRead(string postId)
    {
        try
        {
            var currentUserId = Client.Auth.CurrentUser?.Id;
            if (string.IsNullOrEmpty(currentUserId))
            {
                Debug.LogError("BeachService: Not logged in!");
                return;
            }

            var receipt = new ReadReceipt
            {
                UserId = currentUserId,
                PostId = postId
            };

            await Client.From<ReadReceipt>().Insert(receipt);
            Debug.Log($"<color=green>Letter {postId} tucked away safely.</color>");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"MarkAsRead failed: {e.Message}");
        }
    }

    public async Task<List<SystemNotification>> GetNotifications()
    {
        try
        {
            var currentUserId = Client.Auth.CurrentUser?.Id;
            if (string.IsNullOrEmpty(currentUserId))
            {
                Debug.LogError("BeachService: Not logged in!");
                return new List<SystemNotification>();
            }

            var response = await Client.From<SystemNotification>()
                .Filter("recipient_id", Postgrest.Constants.Operator.Equals, currentUserId)
                .Filter("is_read", Postgrest.Constants.Operator.Is, "false")
                .Order("created_at", Postgrest.Constants.Ordering.Descending)
                .Get();

            var notifications = response.Models;

            if (notifications.Count == 0)
            {
                Debug.Log("<color=cyan>No notifications. All quiet on the beach.</color>");
            }
            else
            {
                Debug.Log($"<color=yellow>You have {notifications.Count} notification(s):</color>");
                foreach (var notif in notifications)
                {
                    var readStatus = notif.IsRead ? "[Read]" : "[Unread]";
                    Debug.Log($"  <color=white>{readStatus} [{notif.Type}] {notif.Content}</color>");
                }
            }

            return notifications;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"GetNotifications failed: {e.Message}");
            return new List<SystemNotification>();
        }
    }

    public async Task MarkNotificationAsRead(string notificationId)
    {
        try
        {
            var currentUserId = Client.Auth.CurrentUser?.Id;
            if (string.IsNullOrEmpty(currentUserId))
            {
                Debug.LogError("BeachService: Not logged in!");
                return;
            }

            await Client.From<SystemNotification>()
                .Filter("id", Postgrest.Constants.Operator.Equals, notificationId)
                .Filter("recipient_id", Postgrest.Constants.Operator.Equals, currentUserId)
                .Set(x => x.IsRead, true)
                .Update();

            Debug.Log($"<color=green>Notification {notificationId} marked as read.</color>");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"MarkNotificationAsRead failed: {e.Message}");
        }
    }

    public async Task DeleteFriend(string friendId)
    {
        try
        {
            var currentUserId = Client.Auth.CurrentUser?.Id;
            if (string.IsNullOrEmpty(currentUserId))
            {
                Debug.LogError("BeachService: Not logged in!");
                return;
            }

            await Client.From<Friendship>()
                .Filter("user_id", Postgrest.Constants.Operator.Equals, currentUserId)
                .Filter("friend_id", Postgrest.Constants.Operator.Equals, friendId)
                .Delete();

            Debug.Log($"<color=green>Friendship with {friendId} removed. The mirror deletion is handled by the database.</color>");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"DeleteFriend failed: {e.Message}");
        }
    }
}
