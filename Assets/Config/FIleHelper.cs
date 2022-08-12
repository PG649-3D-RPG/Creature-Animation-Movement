using System;
using System.Text;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Config
{
    public static class FileHelper 
    {
        public static void SaveObject(Object obj, string fileName)
        {
            try
            {
                Debug.Log( Application.streamingAssetsPath + "/" + fileName);
                // It seems like Unitys own File implementation is windows exclusive :clown:
                // Change only if you know it compiles for Linux
                System.IO.File.WriteAllBytes( Application.streamingAssetsPath + "/" + fileName,
                    Encoding.UTF8.GetBytes(JsonUtility.ToJson(obj)));
            }
            catch (Exception)
            {
                Debug.LogError("Could not write setting file");
            }
        }

    }
}