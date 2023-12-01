// Created by Sérgio Murillo da Costa Faria

using UnityEngine;

/// <summary>
/// Represents a script that controls the animation of a character named PaiDoRolf.
/// </summary>
public class PaiDoRolf : MonoBehaviour
{
    [SerializeField] private AnimationClip down, idle, up;
    private Animator animator;

    private void Start()
    {
        animator = GetComponent<Animator>();
        if (animator.GetInteger("Count").Equals(0)) animator.Play(down.name);
    }

    /// <summary>
    /// Triggers the character to get up and change its animation state.
    /// </summary>
    public void GetUp()
    {
        switch (animator.GetInteger("Count"))
        {
            case 0:
                animator.Play(up.name);
                animator.SetInteger("Count", 1);
                break;
            case 1:
                animator.Play(idle.name);
                animator.SetInteger("Count", 2);
                break;
        }
    }
}