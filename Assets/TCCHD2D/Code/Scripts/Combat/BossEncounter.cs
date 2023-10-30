using CI.QuickSave;
using UnityEngine;

namespace TCCHD2D.Code
{
    public class BossEncounter : MonoBehaviour
    {
        public bool fought;

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
    }
}