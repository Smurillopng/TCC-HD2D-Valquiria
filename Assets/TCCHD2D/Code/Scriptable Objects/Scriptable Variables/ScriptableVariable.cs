// Created by Sérgio Murillo da Costa Faria
// Date: 07/03/2023

#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

public abstract class ScriptableVariable<T> : ScriptableObject
{
    [SerializeField] private bool resetOnExit;
    [SerializeField] private T defaultValue;
    [SerializeField] private T value;

    public T Value
    {
        get => value;
        set => this.value = value;
    }

#if UNITY_EDITOR
    protected void OnEnable()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }
    protected void OnDisable()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
    }
    protected void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingPlayMode && resetOnExit)
        {
            value = defaultValue;
        }
    }
#else
    protected void OnDisable()
    {
        if (resetOnExit)
        {
            value = defaultValue;
        }
    }
#endif
}