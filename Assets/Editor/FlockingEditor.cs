using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Flock))]
public class FlockingEditor : Editor {
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (GUILayout.Button("Reset"))
        {
            ((Flock)target).Reset();
        }
    }
}
