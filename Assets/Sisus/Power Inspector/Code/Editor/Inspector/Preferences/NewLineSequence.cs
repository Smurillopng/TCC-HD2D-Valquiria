using UnityEngine;

namespace Sisus.CreateScriptWizard
{
    public enum NewLineSequence
    {
        [Tooltip("CR LF")]
        WindowsStyle = 0,

        [Tooltip("LF")]
        UnixStyle = 1
    }
}