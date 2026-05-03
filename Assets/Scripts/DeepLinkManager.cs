using UnityEngine;
using System;

public class DeepLinkManager : MonoBehaviour
{
    public static DeepLinkManager Instance { get; private set; }
    public string deeplinkURL;

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

    private async void ProcessFriendRequest(string token)
    {
        Debug.Log("Attempting to add friend with token: " + token);

        // 1. IMMEDIATE FEEDBACK
        // Call whatever UI manager you have to show a loading spinner
        // UIManager.Instance.ShowLoading("Catching cloud...");

        try
        {
            bool success = await BeachService.Instance.AddFriendByToken(token);

            // 3. THE RESOLUTION (SUCCESS)
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
            // THE RESOLUTION (FAILURE)
            Debug.LogError("QR Scan Failed: " + e.Message);
            // NotificationManager.Instance.ShowNotification("Scan failed. Try generating a new Cloud!");
        }
        finally
        {
            // 4. CLEANUP
            // This runs no matter what (success or fail), ensuring the loader always goes away.
            // UIManager.Instance.HideLoading();
        }
    }
}