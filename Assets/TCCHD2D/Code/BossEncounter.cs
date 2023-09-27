using CI.QuickSave;
using UnityEngine;

namespace TCCHD2D.Code
{
    public class BossEncounter : MonoBehaviour
    {
        public bool fought;

        private void Awake()
        {
            var infoReader = QuickSaveReader.Create("ItemInfo");
            var saveReader = QuickSaveReader.Create("GameSave");
            
            if (saveReader.Exists(name+"_BossEncountered") && saveReader.Read<bool>(name+"_BossEncountered"))
            {
                if (fought)
                    Destroy(gameObject);
            }
            else if (infoReader.Exists(name+"_BossEncountered") && infoReader.Read<bool>(name+"_BossEncountered"))
            {
                if (fought)
                    Destroy(gameObject);
            }
        }
        
        public void FightStarted()
        {
            fought = true;
            QuickSaveWriter.Create("ItemInfo").Write(name+"_BossEncountered", true).Commit();
        }
    }
}