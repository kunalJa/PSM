using UnityEngine;
using TMPro;

public class InGameConsole : MonoBehaviour
{
    public TextMeshProUGUI logDisplay; // Assign your UI Text here
    public int maxLines = 10;
    private System.Collections.Generic.List<string> logs = new System.Collections.Generic.List<string>();

    void OnEnable() {
        // Subscribe to Unity's log event
        Application.logMessageReceived += HandleLog;
    }

    void OnDisable() {
        // Unsubscribe when the object is disabled
        Application.logMessageReceived -= HandleLog;
    }

    void HandleLog(string logString, string stackTrace, LogType type) {
        // Format the log (e.g., add colors for errors)
        string color = type == LogType.Error || type == LogType.Exception ? "red" : "white";
        logs.Add($"<color={color}>{logString}</color>");

        // Keep the log list within a specific size
        if (logs.Count > maxLines) logs.RemoveAt(0);

        // Update the UI
        logDisplay.text = string.Join("\n", logs);
    }
}
