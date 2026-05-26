using UnityEngine;
using System.Collections;

public class DataRefreshManager : MonoBehaviour
{
    public static DataRefreshManager Instance { get; private set; }
    
    [Header("Polling Intervals (seconds)")]
    [SerializeField] private float foregroundPollInterval = 60f;
    [SerializeField] private float backgroundPollInterval = 300f; // 5 minutes
    
    private Coroutine pollingCoroutine;
    private bool isAppInForeground = true;
    private bool isInitialized = false; // Only refresh after initial data load
    private int consecutiveErrors = 0;
    private const int MAX_BACKOFF_MULTIPLIER = 8;

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

    void Start()
    {
        // Wait for orchestrator to exist, then subscribe to its events
        StartCoroutine(SubscribeWhenOrchestratorReady());
    }

    private IEnumerator SubscribeWhenOrchestratorReady()
    {
        // Wait until AppDataOrchestrator.Instance exists (handles Awake() order)
        while (AppDataOrchestrator.Instance == null)
        {
            yield return null; // Wait one frame
        }
        
        Debug.Log("DataRefreshManager: Orchestrator found, subscribing to OnAllDataLoaded...");
        
        // Subscribe to know when initial data is loaded
        AppDataOrchestrator.Instance.OnAllDataLoaded += OnInitialDataLoaded;
        
        // If data was already loaded before we subscribed, initialize now
        if (AppDataOrchestrator.Instance.Friends.Count > 0 || 
            AppDataOrchestrator.Instance.Notifications.Count > 0)
        {
            Debug.Log("DataRefreshManager: Data already loaded, initializing immediately");
            OnInitialDataLoaded();
        }
    }

    private void OnInitialDataLoaded()
    {
        if (isInitialized) return; // Prevent double initialization
        
        Debug.Log("DataRefreshManager: Initial data loaded, starting polling...");
        AppDataOrchestrator.Instance.OnAllDataLoaded -= OnInitialDataLoaded;
        isInitialized = true;
        StartPolling();
    }

    public void StartPolling()
    {
        if (pollingCoroutine != null)
        {
            StopCoroutine(pollingCoroutine);
        }
        pollingCoroutine = StartCoroutine(PollForUpdates());
    }

    public void StopPolling()
    {
        if (pollingCoroutine != null)
        {
            StopCoroutine(pollingCoroutine);
            pollingCoroutine = null;
        }
    }

    private IEnumerator PollForUpdates()
    {
        while (true)
        {
            // Calculate interval with exponential backoff on errors
            float interval = isAppInForeground ? foregroundPollInterval : backgroundPollInterval;
            if (consecutiveErrors > 0)
            {
                int backoffMultiplier = Mathf.Min(consecutiveErrors, MAX_BACKOFF_MULTIPLIER);
                interval *= backoffMultiplier;
                Debug.Log($"DataRefreshManager: Backing off, next poll in {interval}s");
            }
            
            yield return new WaitForSeconds(interval);
            
            // Only poll if app is in foreground (save battery)
            if (isAppInForeground)
            {
                yield return RefreshAllData();
            }
        }
    }

    private IEnumerator RefreshAllData()
    {
        Debug.Log("<color=blue>DataRefreshManager: Polling for updates...</color>");
        
        var task = AppDataOrchestrator.Instance.FetchAllData();
        
        // Wait for task to complete
        while (!task.IsCompleted)
        {
            yield return null;
        }
        
        if (task.IsFaulted)
        {
            consecutiveErrors++;
            Debug.LogWarning($"DataRefreshManager: Refresh failed ({consecutiveErrors} consecutive errors)");
        }
        else
        {
            consecutiveErrors = 0; // Reset on success
        }
    }

    void OnApplicationPause(bool pauseStatus)
    {
        isAppInForeground = !pauseStatus;
        
        if (!pauseStatus && isInitialized)
        {
            // App resumed from background - refresh immediately (only if logged in)
            Debug.Log("<color=cyan>DataRefreshManager: App resumed, refreshing data...</color>");
            StartCoroutine(RefreshAllData());
        }
        else if (pauseStatus)
        {
            Debug.Log("DataRefreshManager: App paused, reducing poll frequency");
        }
    }

    void OnApplicationFocus(bool hasFocus)
    {
        // Also handle focus changes (useful for editor testing)
        if (hasFocus && !isAppInForeground && isInitialized)
        {
            isAppInForeground = true;
            Debug.Log("<color=cyan>DataRefreshManager: App focused, refreshing data...</color>");
            StartCoroutine(RefreshAllData());
        }
    }

    // Manual refresh trigger (for pull-to-refresh UI)
    public void ManualRefresh()
    {
        Debug.Log("DataRefreshManager: Manual refresh triggered");
        StartCoroutine(RefreshAllData());
    }
}
