using UnityEngine;

public class PlayerTutorialController : MonoBehaviour
{
    private void Start()
    {
        PlayerControls.Instance.ToggleDefaultControls(false);
    }
}
