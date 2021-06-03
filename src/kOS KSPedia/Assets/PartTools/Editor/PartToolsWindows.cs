using UnityEngine;
using UnityEditor;

public class PartToolsWindows
{
    [MenuItem("Tools/KSP Part Tools")]
    public static void ShowPartToolsWindow()
    {
        KSPPartTools.PartToolsWindow.ShowWindow();
    }
}