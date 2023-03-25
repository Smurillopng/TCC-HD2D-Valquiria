using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// A console to display Unity's debug logs in-game.
///
/// Version: 1.3.1
/// </summary>
public class DebugConsole : MonoBehaviour
{
    #region Inspector Settings

    [SerializeField, Tooltip("Hotkey to show and hide the console.")]
#if ENABLE_INPUT_SYSTEM
    private Key toggleKey = Key.Backquote;
#else
    private KeyCode toggleKey = KeyCode.BackQuote;
#endif

    [SerializeField, Tooltip("Whether to open as soon as the game starts.")]
    private bool openOnStart;

    [SerializeField, Tooltip("Whether to keep a limited number of logs. Useful if memory usage is a concern.")]
    private bool restrictLogCount;

    [SerializeField, Tooltip("Number of logs to keep before removing old ones.")]
    private int maxLogCount = 1000;

    [SerializeField, Tooltip("Whether log messages are collapsed by default or not.")]
    private bool collapseLogOnStart;

    [SerializeField, Tooltip("Font size to display log entries with.")]
    private int logFontSize = 12;

    [SerializeField, Tooltip("Amount to scale UI by.")]
    private float scaleFactor = 1f;

    #endregion

    private static readonly GUIContent ClearLabel = new("Clear", "Clear contents of console.");
    private static readonly GUIContent OnlyLastLogLabel = new("Only Last Log", "Show only most recent log.");
    private static readonly GUIContent CollapseLabel = new("Collapse", "Hide repeated messages.");
    private const int Margin = 20;
    private const string WindowTitle = "Console";

    private static readonly Dictionary<LogType, Color> LOGTypeColors = new()
    {
        { LogType.Assert, Color.white },
        { LogType.Error, Color.red },
        { LogType.Exception, Color.red },
        { LogType.Log, Color.white },
        { LogType.Warning, Color.yellow },
    };

    private bool _isCollapsed;
    private bool _isVisible;
    private float _lastToggleTime;
    private readonly List<Log> _logs = new();
    private bool _onlyLastLog;
    private readonly ConcurrentQueue<Log> _queuedLogs = new();
    private Vector2 _scrollPosition;
    private readonly Rect _titleBarRect = new(0, 0, 10000, 20);
    private float _windowX = Margin;
    private float _windowY = Margin;

    private readonly Dictionary<LogType, bool> _logTypeFilters = new()
    {
        { LogType.Assert, true },
        { LogType.Error, true },
        { LogType.Exception, true },
        { LogType.Log, true },
        { LogType.Warning, true },
    };

    #region MonoBehaviour Messages

    private void OnDisable()
    {
        Application.logMessageReceivedThreaded -= HandleLogThreaded;
    }

    private void OnEnable()
    {
        Application.logMessageReceivedThreaded += HandleLogThreaded;
    }

    private void OnGUI()
    {
        if (!_isVisible)
            return;

        GUI.matrix = Matrix4x4.Scale(Vector3.one * scaleFactor);

        var width = (Screen.width / scaleFactor) - (Margin * 2);
        var height = (Screen.height / scaleFactor) - (Margin * 2);
        var windowRect = new Rect(_windowX, _windowY, width, height);

        var newWindowRect = GUILayout.Window(123456, windowRect, DrawWindow, WindowTitle);
        _windowX = newWindowRect.x;
        _windowY = newWindowRect.y;
    }

    private void Start()
    {
        if (collapseLogOnStart)
            _isCollapsed = true;

        if (openOnStart)
            _isVisible = true;
    }

    private void Update()
    {
        UpdateQueuedLogs();

        if (!_isVisible) return;
        if (Keyboard.current != null && Keyboard.current[toggleKey].wasPressedThisFrame)
            _isVisible = false;
    }

    public void TurnOnOff()
    {
        _isVisible = !_isVisible;
    }

    #endregion

    private void DrawLog(Log log, GUIStyle logStyle)
    {
        GUI.contentColor = LOGTypeColors[log.Type];

        if (_isCollapsed)
        {
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label(log.GetTruncatedMessage(), logStyle);
                GUILayout.FlexibleSpace();
                GUILayout.Label(log.Count.ToString(), GUI.skin.box);
            }
            GUILayout.EndHorizontal();
        }
        else
        {
            for (var i = 0; i < log.Count; i += 1)
            {
                GUILayout.Label(log.GetTruncatedMessage(), logStyle);
            }
        }

        GUI.contentColor = Color.white;
    }

    private void DrawLogList()
    {
        var logStyle = GUI.skin.label;
        logStyle.fontSize = logFontSize;

        _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);

        GUILayout.BeginVertical();
        {
            if (_onlyLastLog)
            {
                var lastVisibleLog = GetLastVisibleLog();

                if (lastVisibleLog.HasValue)
                    DrawLog(lastVisibleLog.Value, logStyle);
            }
            else
            {
                foreach (var log in _logs.Where(IsLogVisible))
                    DrawLog(log, logStyle);
            }
        }
        GUILayout.EndVertical();

        var innerScrollRect = GUILayoutUtility.GetLastRect();
        GUILayout.EndScrollView();
        var outerScrollRect = GUILayoutUtility.GetLastRect();

        if (Event.current.type == EventType.Repaint && IsScrolledToBottom(innerScrollRect, outerScrollRect))
        {
            ScrollToBottom();
        }
    }

    private void DrawToolbar()
    {
        GUILayout.BeginHorizontal();
        {
            if (GUILayout.Button(ClearLabel))
                _logs.Clear();

            foreach (LogType logType in Enum.GetValues(typeof(LogType)))
            {
                var currentState = _logTypeFilters[logType];
                var label = logType.ToString();
                _logTypeFilters[logType] = GUILayout.Toggle(currentState, label, GUILayout.ExpandWidth(false));
                GUILayout.Space(20);
            }

            _isCollapsed = GUILayout.Toggle(_isCollapsed, CollapseLabel, GUILayout.ExpandWidth(false));
            _onlyLastLog = GUILayout.Toggle(_onlyLastLog, OnlyLastLogLabel, GUILayout.ExpandWidth(false));
        }
        GUILayout.EndHorizontal();
    }

    private void DrawWindow(int windowID)
    {
        DrawLogList();
        DrawToolbar();

        GUI.DragWindow(_titleBarRect);
    }

    private void UpdateQueuedLogs()
    {
        while (_queuedLogs.TryDequeue(out var log))
        {
            ProcessLogItem(log);
        }
    }

    private Log? GetLastVisibleLog()
    {
        for (var i = _logs.Count - 1; i >= 0; i--)
        {
            var log = _logs[i];

            if (IsLogVisible(log))
                return log;
        }
        return null;
    }

    private void HandleLogThreaded(string message, string stackTrace, LogType type)
    {
        var log = new Log
        {
            Count = 1,
            Message = message,
            StackTrace = stackTrace,
            Type = type,
        };

        _queuedLogs.Enqueue(log);
    }

    private void ProcessLogItem(Log log)
    {
        var lastLog = _logs.Count > 0 ? _logs[^1] : (Log?)null;
        var isDuplicateOfLastLog = lastLog.HasValue && log.Equals(lastLog.Value);

        if (isDuplicateOfLastLog)
        {
            log.Count = lastLog.Value.Count + 1;
            _logs[^1] = log;
        }
        else
        {
            _logs.Add(log);
            TrimExcessLogs();
        }
    }

    private bool IsLogVisible(Log log)
    {
        return _logTypeFilters[log.Type];
    }

    private bool IsScrolledToBottom(Rect innerScrollRect, Rect outerScrollRect)
    {
        var innerScrollHeight = innerScrollRect.height;

        var outerScrollHeight = outerScrollRect.height - GUI.skin.box.padding.vertical;

        if (outerScrollHeight > innerScrollHeight)
            return true;

        return Mathf.Approximately(innerScrollHeight, _scrollPosition.y + outerScrollHeight);
    }

    private void ScrollToBottom()
    {
        _scrollPosition = new Vector2(0, Int32.MaxValue);
    }

    private void TrimExcessLogs()
    {
        if (!restrictLogCount)
            return;

        var amountToRemove = _logs.Count - maxLogCount;

        if (amountToRemove <= 0)
            return;

        _logs.RemoveRange(0, amountToRemove);
    }
}

/// <summary>
/// A basic container for log details.
/// </summary>
internal struct Log
{
    public int Count;
    public string Message;
    public string StackTrace;
    public LogType Type;

    /// <summary>
    /// The max string length supported by UnityEngine.GUILayout.Label without triggering this error:
    /// "String too long for TextMeshGenerator. Cutting off characters."
    /// </summary>
    private const int MaxMessageLength = 16382;

    public bool Equals(Log log)
    {
        return Message == log.Message && StackTrace == log.StackTrace && Type == log.Type;
    }

    /// <summary>
    /// Return a truncated Message if it exceeds the max Message length.
    /// </summary>
    public string GetTruncatedMessage()
    {
        if (string.IsNullOrEmpty(Message))
            return Message;

        return Message.Length <= MaxMessageLength ? Message : Message.Substring(0, MaxMessageLength);
    }
}

/// <summary>
/// Alternative to System.Collections.Concurrent.ConcurrentQueue
/// (It's only available in .NET 4.0 and greater)
/// </summary>
/// <remarks>
/// It's a bit slow (as it uses locks), and only provides a small subset of the interface
/// Overall, the implementation is intended to be simple & robust
/// </remarks>
internal class ConcurrentQueue<T>
{
    private readonly Queue<T> _queue = new();
    private readonly object _queueLock = new();

    public void Enqueue(T item)
    {
        lock (_queueLock)
        {
            _queue.Enqueue(item);
        }
    }

    public bool TryDequeue(out T result)
    {
        lock (_queueLock)
        {
            if (_queue.Count == 0)
            {
                result = default(T);
                return false;
            }

            result = _queue.Dequeue();
            return true;
        }
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(Console))]
class ConsoleEditor : Editor
{
    SerializedProperty toggleKey;
    SerializedProperty openOnStart;
    SerializedProperty shakeToOpen;
    SerializedProperty shakeRequiresTouch;
    SerializedProperty shakeAcceleration;
    SerializedProperty toggleThresholdSeconds;
    SerializedProperty restrictLogCount;
    SerializedProperty maxLogCount;
    SerializedProperty collapseLogOnStart;
    SerializedProperty logFontSize;
    SerializedProperty scaleFactor;

    void OnEnable()
    {
        toggleKey = serializedObject.FindProperty("toggleKey");
        openOnStart = serializedObject.FindProperty("openOnStart");
        shakeToOpen = serializedObject.FindProperty("shakeToOpen");
        shakeRequiresTouch = serializedObject.FindProperty("shakeRequiresTouch");
        shakeAcceleration = serializedObject.FindProperty("shakeAcceleration");
        toggleThresholdSeconds = serializedObject.FindProperty("toggleThresholdSeconds");
        restrictLogCount = serializedObject.FindProperty("restrictLogCount");
        maxLogCount = serializedObject.FindProperty("maxLogCount");
        collapseLogOnStart = serializedObject.FindProperty("collapseLogOnStart");
        logFontSize = serializedObject.FindProperty("logFontSize");
        scaleFactor = serializedObject.FindProperty("scaleFactor");
    }

    public override void OnInspectorGUI()
    {
        EditorGUILayout.PropertyField(toggleKey);
        EditorGUILayout.PropertyField(openOnStart);
        EditorGUILayout.PropertyField(shakeToOpen);

        using (new EditorGUI.DisabledScope(!shakeToOpen.boolValue))
        using (new EditorGUI.IndentLevelScope())
        {
            EditorGUILayout.PropertyField(shakeRequiresTouch);
            EditorGUILayout.PropertyField(shakeAcceleration);
        }

        EditorGUILayout.PropertyField(toggleThresholdSeconds);
        EditorGUILayout.PropertyField(restrictLogCount);

        using (new EditorGUI.DisabledScope(!restrictLogCount.boolValue))
        using (new EditorGUI.IndentLevelScope())
        {
            EditorGUILayout.PropertyField(maxLogCount);
        }

        EditorGUILayout.PropertyField(collapseLogOnStart);
        EditorGUILayout.PropertyField(logFontSize);
        EditorGUILayout.PropertyField(scaleFactor);
        serializedObject.ApplyModifiedProperties();
    }
}

#endif