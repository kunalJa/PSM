using UnityEngine;

public class MessengerBirdManager : MonoBehaviour
{
    public static MessengerBirdManager Instance { get; private set; }

    [Tooltip("Bird visuals, one per orbit slot. Order matches slot order under BirdOrbitRig.")]
    [SerializeField] private BirdVisual[] birdSlots = new BirdVisual[2];
    [SerializeField] private LetterUIController letterUI;
    // TODO: [SerializeField] private Animator birdAnimator;   uncomment when artist delivers per-bird flight anim

    private int _targetCount = 0;

    void Awake()
    {
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>
    /// Called by AppDataOrchestrator whenever the ready-letter count changes. Birds
    /// fly in to fill empty slots; busy (mid-flight) birds are left alone and the
    /// slots get re-evaluated when their flights complete.
    /// </summary>
    public void SetBirdCount(int count)
    {
        _targetCount = Mathf.Clamp(count, 0, birdSlots.Length);
        Debug.Log($"<color=cyan>MessengerBirdManager: target bird count = {_targetCount}</color>");
        SyncSlots();
    }

    private void SyncSlots()
    {
        // Count birds that are visible right now AND not in the middle of leaving.
        int currentlyHeld = 0;
        for (int i = 0; i < birdSlots.Length; i++)
        {
            var b = birdSlots[i];
            if (b == null) continue;
            if (b.IsVisible && !b.IsBusy) currentlyHeld++;
        }

        if (currentlyHeld < _targetCount)
        {
            // Need more birds: fly into empty, non-busy slots in order.
            for (int i = 0; i < birdSlots.Length && currentlyHeld < _targetCount; i++)
            {
                var b = birdSlots[i];
                if (b == null) continue;
                if (b.IsVisible || b.IsBusy) continue;
                b.FlyIn();
                currentlyHeld++;
            }
        }
        else if (currentlyHeld > _targetCount)
        {
            // Too many birds (e.g. server returned fewer unread). Hide extras from the back.
            for (int i = birdSlots.Length - 1; i >= 0 && currentlyHeld > _targetCount; i--)
            {
                var b = birdSlots[i];
                if (b == null) continue;
                if (!b.IsVisible || b.IsBusy) continue;
                b.HideImmediate();
                currentlyHeld--;
            }
        }
    }

    /// <summary>
    /// Called by BirdClickHandler when the user taps a specific bird. The clicked
    /// bird flies away to deliver. Once it lands, the letter UI is revealed (with
    /// content already populated) and the slot is re-evaluated for backfill.
    /// </summary>
    public void OnBirdClicked(BirdVisual clickedBird)
    {
        if (clickedBird == null || clickedBird.IsBusy || !clickedBird.IsVisible) return;

        var result = AppDataOrchestrator.Instance.DequeuePost();
        if (result == null)
        {
            Debug.LogWarning("MessengerBirdManager: clicked bird but no ready posts");
            return;
        }

        var (post, texture) = result.Value;
        Debug.Log($"<color=green>MessengerBirdManager: bird delivering letter from {post.AuthorId}</color>");

        clickedBird.FlyOut(() =>
        {
            if (letterUI != null)
            {
                letterUI.PrepareDroppedLetter(post, texture);
            }
            else
            {
                Debug.LogError("MessengerBirdManager: LetterUIController reference not set!");
            }

            // Slot is now empty; if the pipeline backfilled while the bird was leaving,
            // fly a fresh bird into the freed slot.
            SyncSlots();
        });
    }
}
