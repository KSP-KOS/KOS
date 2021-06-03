using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(KSPPartTools.KSPParticleEmitter))]
public class KSPParticleEmitterEditor : Editor
{
    KSPPartTools.KSPParticleEmitterInspector  owner;

    public KSPParticleEmitterEditor()
    {
        owner = new KSPPartTools.KSPParticleEmitterInspector(this);
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

    public void OnSceneGUI()
    {
        owner.OnSceneGUI();
    }

    #endregion
}