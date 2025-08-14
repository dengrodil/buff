using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;  
using System.Linq;

[CustomEditor(typeof(BuffReceiver))]
public class BuffsViewer : Editor {

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        BuffReceiver receiver = target as BuffReceiver;

        // Split into id
        List<string> ids = receiver.Buffs.Select(b => b.Id).Distinct().ToList();

        // Display description based on BuffApplication
        foreach (string id in ids)
        {
            Buff[] buffs = receiver.Buffs.Where(b => b.Id == id).ToArray();
            if (buffs[0].Application == BuffApplication.StackingIntensity)
            {
                int stacks = buffs.Sum(b => b.Stacks);
                GUILayout.Label(buffs[0].Id + " [Stacks: " + stacks + "]");
            }
            else
            {
                GUILayout.Label(buffs[0].Id + " [Duration: " + buffs[0].TimeRemaining + "s]");
            }
        }
    }
}
