using CI.QuickSave;
using UnityEngine;

namespace TCCHD2D.Code
{
    public class BossEncounter : MonoBehaviour
    {
        public bool fought;

        private void Awake()
        {
            var reader = QuickSaveReader.Create("GameSave");
            fought = reader.Exists(name+"_BossEncountered") && reader.Read<bool>(name+"_BossEncountered");
            if (fought)
                Destroy(gameObject);
        }
        
        public void FightStarted()
        {
            fought = true;
            QuickSaveWriter.Create("GameSave").Write(name+"_BossEncountered", true).Commit();
        }
    }
}