using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(KSPPartTools.InternalSpace))]
public class InternalSpaceEditor : Editor
{
    KSPPartTools.InternalSpaceInspector owner;

    public InternalSpaceEditor()
    {
        owner = new KSPPartTools.InternalSpaceInspector(this);
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