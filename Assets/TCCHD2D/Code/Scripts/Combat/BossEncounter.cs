// Created by Sérgio Murillo da Costa Faria

using CI.QuickSave;
using Sirenix.OdinInspector;
using UnityEngine;

namespace TCCHD2D.Code
{
    [HideMonoScript]
    public class BossEncounter : MonoBehaviour
    {
        #region === Unity Methods ===========================================================
        public void FightStarted()
        {
            QuickSaveWriter.Create("ItemInfo").Write(name + "_BossEncountered", true).Commit();
        }

        public void Check()
        {
            var infoReader = QuickSaveReader.Create("ItemInfo");
            var saveReader = QuickSaveReader.Create("GameSave");

            if (saveReader.Exists(name + "_BossEncountered"))
            {
                if (saveReader.Read<bool>(name + "_BossEncountered"))
                    Destroy(gameObject);
            }
            if (infoReader.Exists(name + "_BossEncountered"))
            {
                if (infoReader.Read<bool>(name + "_BossEncountered"))
                    Destroy(gameObject);
            }
        }
        #endregion ==========================================================================
    }
}