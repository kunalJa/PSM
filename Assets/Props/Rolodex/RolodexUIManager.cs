using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class RolodexUIManager : MonoBehaviour
{
    [Header("UI Elements in Scene")]
    public TextMeshProUGUI friendNameText; 
    public Button deleteButton;            

    // Internal tracking - now stores FriendData with IDs
    private List<FriendData> myFriends = new List<FriendData>();
    private int currentIndex = 0;

    void Start()
    {
        // Subscribe to the orchestrator's friends loaded event
        if (AppDataOrchestrator.Instance != null)
        {
            AppDataOrchestrator.Instance.OnFriendsLoaded += InitializeFriendsList;
            
            // If friends already loaded, initialize now
            if (AppDataOrchestrator.Instance.Friends.Count > 0)
            {
                InitializeFriendsList(AppDataOrchestrator.Instance.Friends);
            }
        }
    }

    void OnDestroy()
    {
        // Unsubscribe to prevent memory leaks
        if (AppDataOrchestrator.Instance != null)
        {
            AppDataOrchestrator.Instance.OnFriendsLoaded -= InitializeFriendsList;
        }
    }

    // Called when friends are loaded from API
    public void InitializeFriendsList(List<FriendData> friendsFromAPI)
    {
        myFriends = friendsFromAPI.ToList(); // Copy to avoid modifying the original
        currentIndex = 0;
        UpdateCardUI();
        Debug.Log($"RolodexUI: Initialized with {myFriends.Count} friends");
    }

    private void UpdateCardUI()
    {
        if (myFriends == null || myFriends.Count == 0)
        {
            friendNameText.text = "No friends yet!";
            deleteButton.gameObject.SetActive(false);
            return;
        }

        deleteButton.gameObject.SetActive(true);
        
        var currentFriend = myFriends[currentIndex];
        friendNameText.text = currentFriend.DisplayName;

        deleteButton.onClick.RemoveAllListeners();
        deleteButton.onClick.AddListener(() => OnDeleteButtonClicked(currentFriend));
    }

    // WE WILL CALL THIS FROM THE ANIMATION!
    public void GoToNextFriend()
    {
        if (myFriends.Count == 0) return;

        currentIndex++;
        if (currentIndex >= myFriends.Count)
        {
            currentIndex = 0;
        }

        UpdateCardUI();
    }

    private async void OnDeleteButtonClicked(FriendData friend)
    {
        Debug.Log($"Deleting friend: {friend.DisplayName} (ID: {friend.Id})");
        
        // Call the actual API
        await BeachService.Instance.DeleteFriend(friend.Id);
        
        // Remove from local list
        myFriends.Remove(friend);
        
        // Adjust index if needed
        if (currentIndex >= myFriends.Count) currentIndex = 0;
        
        UpdateCardUI();
    }
}