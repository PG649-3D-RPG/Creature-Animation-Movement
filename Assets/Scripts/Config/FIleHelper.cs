using System;
using System.IO;
using System.Text;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Config
{
    public static class FileHelper 
    {
        public static string LoadJson(string fileName)
        {
            try
            {
                // It seems like Unitys own File implementation is windows exclusive :clown:
                // Change only if you know it compiles for Linux
                return System.IO.File.ReadAllText(Application.streamingAssetsPath + "/" + fileName, Encoding.UTF8);
            }
            catch (Exception)
            {
                Debug.LogError("Could not write setting file");
            }

            return null;
        }
        
        public static void SaveObject(Object obj, string fileName)
        {
            try
            {
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

        public static void CopyFields(Object source, Object destination)
        {
            if (source.GetType() != destination.GetType()) return;
            foreach (var sourceField in source.GetType().GetFields())
            {
                foreach (var destinationField in destination.GetType().GetFields())
                {
                    if(sourceField.Name == destinationField.Name)  
                        destinationField.SetValue(destination, sourceField.GetValue(source));
                }
            }
        }
    }
}