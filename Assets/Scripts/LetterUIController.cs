using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

public class LetterUIController : MonoBehaviour
{
    [SerializeField] private Animator letterAnimator;       // Animator on Letter_w_postcard (or its parent)
    [SerializeField] private TextMeshProUGUI contentText;
    [SerializeField] private TextMeshProUGUI fromText;
    [SerializeField] private TextMeshProUGUI dateText;
    [SerializeField] private ProgrammaticPolaroid polaroid;
    [SerializeField] private Button closeButton;
    [SerializeField] private ScrollRect contentScrollRect;

    public static bool IsOpen { get; private set; }

    private string _currentPostId;

    void Awake()
    {
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(OnClose);
        }
    }

    public void PrepareDroppedLetter(Post post, Texture2D texture)
    {
        _currentPostId = post.Id;
        IsOpen = true;
        gameObject.SetActive(true);

        if (contentText != null)
            contentText.text = post.ContentText ?? "";
        else
            Debug.LogWarning("LetterUIController: contentText is not assigned in the Inspector!");

        if (contentScrollRect != null)
            StartCoroutine(ScrollToTop());
        
        if (fromText != null)
            fromText.text = ResolveAuthorName(post.AuthorId);
        
        if (dateText != null)
            dateText.text = post.CreatedAt.ToLocalTime().ToString("MMM d, yyyy");

        if (polaroid == null)
        {
            Debug.LogError("LetterUIController: polaroid reference is not assigned!");
        }
        else if (texture == null)
        {
            Debug.LogWarning($"LetterUIController: texture is null for post {post.Id}, skipping polaroid assignment");
        }
        else
        {
            polaroid.ApplyPreloadedTexture(texture);
        }
        // TODO: if original post.MediaUrl was null → spawn Letter_without_polaroid instead (next feature)

        Debug.Log($"<color=yellow>LetterUIController: Prepared dropped letter {post.Id}</color>");
    }

    public void OnClose()
    {
        // Trigger close animation
        if (letterAnimator != null)
        {
            letterAnimator.ResetTrigger("LetterOpen");
            letterAnimator.SetTrigger("LetterClose");
        }

        // Mark as read (fire-and-forget)
        if (!string.IsNullOrEmpty(_currentPostId))
        {
            _ = AppDataOrchestrator.Instance.MarkPostRead(_currentPostId);
        }

        Debug.Log($"<color=yellow>LetterUIController: Closed letter {_currentPostId}</color>");
        _currentPostId = null;
        IsOpen = false;
    }

    private System.Collections.IEnumerator ScrollToTop()
    {
        yield return new WaitForEndOfFrame();
        contentScrollRect.content.anchoredPosition = new Vector2(
            contentScrollRect.content.anchoredPosition.x, 0f);
    }

    private string ResolveAuthorName(string authorId)
    {
        if (AppDataOrchestrator.Instance == null) return "A friend";
        
        var friend = AppDataOrchestrator.Instance.Friends.FirstOrDefault(f => f.Id == authorId);
        return friend != null ? friend.DisplayName : "A friend";
    }
}
