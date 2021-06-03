using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(KSPPartTools.PartTools))]
public class PartToolsEditor : Editor
{
    KSPPartTools.PartToolsInspector owner;

    public PartToolsEditor()
    {
        owner = new KSPPartTools.PartToolsInspector(this);
    }

    #region EditorInspector Methods

    public override void OnInspectorGUI()
    {
        owner.OnInspectorGUI();
    }

    public override bool HasPreviewGUI()
    {
        return owner.HasPreviewGUI();
    }

    public override void OnPreviewGUI(Rect r, GUIStyle background)
    {
        owner.OnPreviewGUI(r, background);
    }

    public override void OnPreviewSettings()
    {
        owner.OnPreviewSettings();
    }

    #endregion
}