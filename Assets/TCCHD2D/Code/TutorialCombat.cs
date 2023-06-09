using UnityEngine;

public class TutorialCombat : MonoBehaviour
{
    public void Pause()
    {
        Time.timeScale = 0;
    }
    
    public void Resume()
    {
        Time.timeScale = 1;
    }
}
