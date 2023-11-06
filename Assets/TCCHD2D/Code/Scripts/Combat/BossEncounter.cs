// Created by Sérgio Murillo da Costa Faria

using CI.QuickSave;
using Sirenix.OdinInspector;
using UnityEngine;

namespace TCCHD2D.Code
{
    [HideMonoScript]
    public class BossEncounter : MonoBehaviour
    {
        #region === Variables ===============================================================
        [FoldoutGroup("Boss Encounter")]
        public bool fought;
        #endregion ==========================================================================
        
        #region === Unity Methods ===========================================================
        public void FightStarted()
        {
            fought = true;
            QuickSaveWriter.Create("ItemInfo").Write(name + "_BossEncountered", true).Commit();
        }

        public void Check()
        {
            var infoReader = QuickSaveReader.Create("ItemInfo");
            var saveReader = QuickSaveReader.Create("GameSave");

            if (saveReader.Exists(name + "_BossEncountered"))
            {
                saveReader.Read<bool>(name + "_BossEncountered");
                if (fought)
                    Destroy(gameObject);
            }
            if (infoReader.Exists(name + "_BossEncountered"))
            {
                fought = infoReader.Read<bool>(name + "_BossEncountered");
                if (fought)
                    Destroy(gameObject);
            }
        }
        #endregion ==========================================================================
    }
}