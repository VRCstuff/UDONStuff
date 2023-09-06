#if UNITY_EDITOR
using UnityEngine;
using System.Collections;
using UnityEditor;
using com.vrcstuff.controls.Dial;

[CustomEditor(typeof(Dial))]
public class ObjectBuilderEditor : Editor
{
    public override void OnInspectorGUI()
    {
        EditorGUI.BeginChangeCheck();
        DrawDefaultInspector();
        if (EditorGUI.EndChangeCheck())
        {
            Debug.Log("Text field has changed.");
            Dial myScript = (Dial)target;
            myScript._RefreshDialInEditor();
        }
    }
}
#endif