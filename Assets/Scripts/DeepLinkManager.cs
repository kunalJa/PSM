using UnityEngine;
using System;
using System.Collections;

public class DeepLinkManager : MonoBehaviour
{
    public static DeepLinkManager Instance { get; private set; }
    public string deeplinkURL;
    private string pendingFriendToken = null;
    private bool isSubscribed = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;                
            Application.deepLinkActivated += onDeepLinkActivated;
            if (!String.IsNullOrEmpty(Application.absoluteURL))
            {
                // Cold start and Application.absoluteURL not null so process Deep Link.
                onDeepLinkActivated(Application.absoluteURL);
            }
            // Initialize DeepLink Manager global variable.
            else deeplinkURL = "[none]";
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
 
    private void onDeepLinkActivated(string url)
    {
        // Update DeepLink Manager global variable, so URL can be accessed from anywhere.
        deeplinkURL = url;
        // In this example, the app expects a link formatted like this:
        // "https://personalsocialmedia.com/connect?token=<TOKEN>"
        if (url.Contains("/connect?token="))
        {
            string token = url.Split(new string[] { "?token=" }, StringSplitOptions.None)[1];
            ProcessFriendRequest(token);
        }
        else 
        {
            Debug.LogError("Invalid deep link format: " + url);
        }
    }

    private void ProcessFriendRequest(string token)
    {
        Debug.Log("Deep link received with token: " + token);
        
        // START loading here - covers both waiting for login AND the RPC call
        // UIManager.Instance.ShowLoading("Connecting...");

        // If user is already logged in, process immediately
        if (AppManager.Instance != null && AppManager.Instance.IsUserReady)
        {
            ExecuteAddFriend(token);
        }
        else
        {
            // Queue the token and safely subscribe when AppManager exists
            Debug.Log("User not ready yet, queuing friend request...");
            pendingFriendToken = token;
            StartCoroutine(SubscribeWhenReady());
        }
    }

    private IEnumerator SubscribeWhenReady()
    {
        // Wait until AppManager.Instance exists (handles Awake() order issue)
        while (AppManager.Instance == null)
        {
            yield return null; // Wait one frame
        }
        
        // Now safe to subscribe
        if (!isSubscribed)
        {
            isSubscribed = true;
            AppManager.Instance.OnUserReady += OnUserReady;
            Debug.Log("Subscribed to OnUserReady event");
        }
    }

    private void OnUserReady()
    {
        AppManager.Instance.OnUserReady -= OnUserReady;
        isSubscribed = false;
        
        if (!string.IsNullOrEmpty(pendingFriendToken))
        {
            Debug.Log("User now ready, processing queued friend request...");
            ExecuteAddFriend(pendingFriendToken);
            pendingFriendToken = null;
        }
    }

    private async void ExecuteAddFriend(string token)
    {
        // Loading already showing from ProcessFriendRequest()

        try
        {
            bool success = await BeachService.Instance.AddFriendByToken(token);

            if (success)
            {
                // Update the UI for success
                Debug.Log("Friend added successfully!");
            }
            else
            {
                // Update the UI for failure
                // NotificationManager.Instance.ShowNotification("Failed to add friend!");
                Debug.LogError("Failed to add friend!");
            }
        }
        catch (Exception e)
        {
            Debug.LogError("QR Scan Failed: " + e.Message);
        }
        finally
        {
            // STOP loading here - always runs whether success, failure, or exception
            // UIManager.Instance.HideLoading();
        }
    }
}