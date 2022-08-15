using System;
using System.Text;
using Unity.AI.Navigation;
using UnityEngine;

namespace Config
{
    public abstract class GenericConfig: MonoBehaviour
    {
        private string configPath => Application.streamingAssetsPath + "/" + GetType().Name + ".json";
        public void Awake()
        {
            if (!Application.isEditor)
            {
                LoadFromJson();
                ExecuteAtLoad();
            }
            else
            {
                SaveObject();
            }
        }

        protected abstract void ExecuteAtLoad();
        
        private void LoadFromJson()
        {
            try
            {
                // It seems like Unitys own File implementation is windows exclusive :clown:
                // Change only if you know it compiles for Linux
                var jsonString = System.IO.File.ReadAllText(configPath, Encoding.UTF8);
                JsonUtility.FromJsonOverwrite(jsonString, this);
            }
            catch (Exception)
            {
                Debug.LogError("Could not write setting file");
            }

        }
        
        private void SaveObject()
        {
            try
            {
                // It seems like Unitys own File implementation is windows exclusive :clown:
                // Change only if you know it compiles for Linux
                System.IO.File.WriteAllBytes( configPath,
                    Encoding.UTF8.GetBytes(JsonUtility.ToJson(this)));
            }
            catch (Exception)
            {
                Debug.LogError("Could not write setting file");
            }
        }
    }
}