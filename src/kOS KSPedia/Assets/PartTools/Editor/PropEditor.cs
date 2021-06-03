using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(KSPPartTools.Prop))]
public class PropEditor : Editor
{
    KSPPartTools.PropInspector owner;

    public PropEditor()
    {
        owner = new KSPPartTools.PropInspector(this);
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