// Created by Sérgio Murillo da Costa Faria
// Date: 07/03/2023

#if UNITY_EDITOR
using UnityEditor;
#endif
using Sirenix.OdinInspector;
using UnityEngine;

public abstract class ScriptableVariable<T> : ScriptableObject
{
    [FoldoutGroup("Settings")]
    [SerializeField]
    private bool resetOnExit;

    [TitleGroup("Values", Alignment = TitleAlignments.Centered)]
    [HorizontalGroup("Values/Split")]
    [BoxGroup("Values/Split/Default")]
    [SerializeField, HideLabel]
    private T defaultValue;

    [BoxGroup("Values/Split/Current")]
    [SerializeField, HideLabel]
    private T value;

    public T Value
    {
        get => value;
        set => this.value = value;
    }

#if UNITY_EDITOR

    [Button("Reset Value")]
    [FoldoutGroup("Settings")]
    private void ResetValue()
    {
        value = defaultValue;
    }

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