using UnityEngine;

public class StopInteraction : MonoBehaviour
{
    public void DisableInteraction()
    {
        Interactable.CanInteract = false;
    }

    public void EnableInteraction()
    {
        Interactable.CanInteract = true;
    }
}