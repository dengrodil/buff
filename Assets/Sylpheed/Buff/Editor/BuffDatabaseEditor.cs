using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

public class BuffDatabaseEditor
{
//    private static readonly string AssetPath = "Assets/Resources/Data/BuffDatabase.asset";
//
//    [MenuItem("Tools/Buff/Build database")]
//    public static void BuildDatabase()
//    {
//        int numDuplicateTypes = 0;
//        BuffDatabase database = AssetDatabase.LoadAssetAtPath<BuffDatabase>(AssetPath);
//
//        // Create database if it doesn't exist yet
//        if (!database)
//        {
//            Debug.Log("BuffDatabase doesn't exist yet. Creating database at " + AssetPath);
//            database = ScriptableObject.CreateInstance<BuffDatabase>();
//            AssetDatabase.CreateAsset(database, AssetPath);
//        }
//
//        // Clear all buffs
//        database.Buffs.Clear();
//
//        // Load all buffs
//        List<BuffType> buffs = Resources.LoadAll<BuffType>("").ToList();
//        HashSet<string> idsChecked = new HashSet<string>();
//        foreach (BuffType buff in buffs)
//        {
//            if (idsChecked.Contains(buff.Id)) continue;
//            idsChecked.Add(buff.Id);
//
//            // Look for duplicate entries for this buff
//            List<BuffType> buffsFound = buffs.Where(b => b.Id == buff.Id).ToList();
//
//            // No duplicate found. Add it to the database
//            if (buffsFound.Count == 1)
//            {
//                // Add buff to database
//                database.Buffs.Add(buff);
//            }
//            // Duplicate found. Log error
//            else
//            {
//                // Log duplicate entries
//                numDuplicateTypes++;
//                string message = "Duplicate entries found for " + buff.Id + ": ";
//                foreach (BuffType buffFound in buffsFound)
//                {
//                    message += "\n" + AssetDatabase.GetAssetPath(buffFound);
//                }
//                Debug.LogError(message);
//            }
//        }
//
//        if (numDuplicateTypes > 0)
//        {
//            Debug.LogError("BuffDatabase creation failed: " + numDuplicateTypes + " duplicate buff types found");
//        }
//        else
//        {
//            // Sort buffs
//            database.Buffs = database.Buffs.OrderBy(b => b.Id).ToList();
//
//            // Save asset
//            EditorUtility.SetDirty(database);
//            AssetDatabase.SaveAssets();
//            Debug.Log("BuffDatabase updated successfuly");
//        }
//    }
}
