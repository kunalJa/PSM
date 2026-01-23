using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

public class BeachService : MonoBehaviour
{
    public static BeachService Instance { get; private set; }

    private Supabase.Client Client => AppManager.Instance.SupabaseClient;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

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

    public async Task AddFriendByUsername(string username)
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
}
