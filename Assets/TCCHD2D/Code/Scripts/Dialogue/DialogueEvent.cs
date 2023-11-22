using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class DialogueEvent : MonoBehaviour
{
    public List<Event> events = new();
    public static string currentEvent;

    private void OnEnable()
    {
        DialogueManager.OnDialogueEnd += OnDialogueEnd;
    }

    private void OnDisable()
    {
        DialogueManager.OnDialogueEnd -= OnDialogueEnd;
        currentEvent = string.Empty;
    }

    public void StartEvent(string eventName)
    {
        var evento = events.Find(e => e.eventName == eventName);
        currentEvent = eventName;
        evento.onEventStart?.Invoke();
    }

    private void OnDialogueEnd()
    {
        var evento = events.Find(e => e.eventName == currentEvent);
        evento.onEventEnd?.Invoke();
    }

    public void EndEvent(string eventName)
    {
        var evento = events.Find(e => e.eventName == eventName);
        evento.onEventEnd?.Invoke();
    }
}

[Serializable]
public struct Event
{
    public string eventName;
    public UnityEvent onEventStart;
    public UnityEvent onEventEnd;
}