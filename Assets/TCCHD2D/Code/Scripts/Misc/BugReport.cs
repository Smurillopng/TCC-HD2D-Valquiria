using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BugReport : MonoBehaviour
{
    public void ReportBug()
    {
        Application.OpenURL("https://forms.gle/C91C4kAZbh1FR2FT6");
    }
}
