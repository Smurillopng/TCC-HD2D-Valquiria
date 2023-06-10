using System;
using CI.QuickSave;
using UnityEngine;

namespace TCCHD2D.Code
{
    public class BossEmergency : MonoBehaviour
    {
        public bool fought;

        private void Awake()
        {
            var reader = QuickSaveReader.Create("GameSave");
            fought = reader.Exists("BossEncountered") && reader.Read<bool>("BossEncountered");
            if (fought)
                Destroy(gameObject);
        }
        
        public void Fight()
        {
            fought = true;
            QuickSaveWriter.Create("GameSave").Write("BossEncountered", true).Commit();
        }
    }
}