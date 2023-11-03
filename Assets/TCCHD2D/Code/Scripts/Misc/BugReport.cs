// Created by Sérgio Murillo da Costa Faria

using Sirenix.OdinInspector;
using UnityEngine;

[HideMonoScript]
public class BugReport : MonoBehaviour
{
    #region === Variables ===============================================================
    [FoldoutGroup("Bug Report")]
    [SerializeField, Tooltip("The URL to the bug report form.")]
    private string url = "https://forms.gle/C91C4kAZbh1FR2FT6";
    #endregion ==========================================================================

    #region === Methods =================================================================
    public void ReportBug()
    {
        Application.OpenURL(url);
    }
    #endregion ==========================================================================
}