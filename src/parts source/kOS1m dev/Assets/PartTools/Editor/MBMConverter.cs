using UnityEngine;
using UnityEditor;
using System.Collections;
using System.IO;
using System;
using System.Text;

public class MBMConverter : EditorWindow {
	
	static string filename = "";
	static string directoryname = "";
	StringBuilder sb = new StringBuilder();
	int logdepth = 0;
	Vector2 scrollPos;

	
	void Log(string msg)
	{
		sb.Append("\n");
		for(int i = 0; i < logdepth; i++)			
			sb.Append("+ ");
		sb.Append(msg);
		scrollPos.y = float.MaxValue;
	}
	
	Texture2D loadFromMBMFile(string filepath)
	{
		try{			
			BinaryReader binRead = new BinaryReader(File.Open(filepath, FileMode.Open));
			
			string magicletter = binRead.ReadString();		
			if (magicletter != "KSP") return null;
			
			
			int width = binRead.ReadInt32();		
			int height = binRead.ReadInt32();
			Log("Texture size " + height.ToString() + "x" + width.ToString());
			
			binRead.ReadInt32();
			
			int textureDepth = binRead.ReadInt32();
			
			TextureFormat textureFormat;
			if(textureDepth == 24)
				textureFormat = TextureFormat.RGB24;
			else if(textureDepth == 32)
				textureFormat = TextureFormat.RGBA32;
			else return null;
			
			
			Texture2D texture = new Texture2D(width, height, textureFormat, false);
			Color32[] colors32 = new Color32[width*height];
			
			if(textureFormat == TextureFormat.RGBA32)
			{
				byte r,g,b,a;
				
				for(int currentPixel = 0; currentPixel < width*height; currentPixel++)
				{
					r = binRead.ReadByte();
					g = binRead.ReadByte();
					b = binRead.ReadByte();
					a = binRead.ReadByte();
					
					colors32[currentPixel].r = r;
					colors32[currentPixel].g = g;
					colors32[currentPixel].b = b;
					colors32[currentPixel].a = a;
				}
			}
			else
			{
				byte r,g,b;
				
				for(int currentPixel = 0; currentPixel < width*height; currentPixel++)
				{
					r = binRead.ReadByte();
					g = binRead.ReadByte();
					b = binRead.ReadByte();
					
					colors32[currentPixel].r = r;
					colors32[currentPixel].g = g;
					colors32[currentPixel].b = b;
				}
			}
			texture.SetPixels32(colors32);
			binRead.Close();
			return texture;
		}
		catch (Exception e)
		{
			logdepth--;
			Log("+ FAIL: an Exception occured!");
			Log("+ " + e.Message);
			return null;
		}
	}

    [UnityEditor.MenuItem("Tools/MBM Converter")]
    static void ShowWindow()
    {
        MBMConverter window = (MBMConverter)EditorWindow.GetWindow(typeof(MBMConverter));
        window.title = "MBM Converter";
		window.minSize = new Vector2(300f, 260f);
    }
		
	void ConvertTexture(string file)
	{
		if(file == null || file == "" || !File.Exists(file))
		{
			Log("That file doesn't exist!");
			return;
		}
		Log("+ Converting Texture: " + file);
		logdepth++;
		
		Texture2D convertTex;
		convertTex = loadFromMBMFile(file);
		
		if(convertTex == null)
			return;
		
		string newpath = file.Substring(0, file.Length - 4) + ".png";
		
		try
		{
			byte[] pngByteArray = convertTex.EncodeToPNG();
			
			FileStream fileStream = new FileStream(newpath, FileMode.Create);
			BinaryWriter binWrite = new BinaryWriter(fileStream);
			
			binWrite.Write(pngByteArray);
			
			binWrite.Close();
		}
		catch(Exception e)
		{
			logdepth--;
			Log("+ FAIL: an Exception occured!");
			Log("+ " + e.Message);
			return;	
		}
		logdepth--;
		Log("+ SUCCESS");
		
	}
	
	void ConvertAllTextures(string directory)
	{
		if(directoryname == null || directoryname == "" || !Directory.Exists(directoryname))
		{
			Log("That directory doesn't exist!");
			return;
		}
		
		Log("+ Directory: " + directory);
		logdepth++;
		
		foreach (string dir in Directory.GetDirectories(directory))
			ConvertAllTextures(dir);
		foreach (string file in Directory.GetFiles(directory))
			if(file.EndsWith(".mbm"))
				ConvertTexture(file);
				
		logdepth--;
		Log("+ Directory Finished");
	}
	
    void OnGUI()
    {
        GUILayout.BeginVertical();		
			GUILayout.Space(8f);
	        GUILayout.BeginHorizontal();			
				GUILayout.Label("Texture File:");			
	        GUILayout.EndHorizontal();
			GUILayout.Space(4f);
	        GUILayout.BeginHorizontal();								
				if(GUILayout.Button("Set", GUILayout.Width(45)))
				{
					string file1 = EditorUtility.OpenFilePanel("Select texture file", filename, "mbm");
					if(file1 != null && file1 != "" && file1.EndsWith(".mbm"))
					{
						filename = file1;
					}
				}
				filename = GUILayout.TextField(filename, GUILayout.ExpandWidth(true));			
	        GUILayout.EndHorizontal();
	        GUILayout.BeginHorizontal();			
				GUI.enabled = (filename != null && filename != "" && filename.EndsWith(".mbm"));
				if(GUILayout.Button("Convert Texture", GUILayout.ExpandWidth(true)))
				{
					sb.Remove(0, sb.Length);
					ConvertTexture(filename);
				}
				GUI.enabled = true;			
	        GUILayout.EndHorizontal();
			GUILayout.Space(8f);
	        GUILayout.BeginHorizontal();			
				GUILayout.Label("Texture Directory:");			
	        GUILayout.EndHorizontal();
			GUILayout.Space(4f);
	        GUILayout.BeginHorizontal();							
				if(GUILayout.Button("Set", GUILayout.Width(45)))
				{
					string file1 = EditorUtility.OpenFolderPanel("Select texture folder", directoryname,"");
					if(file1 != null && file1 != "")
					{
						directoryname = file1;
					}
				}			
				directoryname = EditorGUILayout.TextField(directoryname, GUILayout.ExpandWidth(true));			
	        GUILayout.EndHorizontal();
	        GUILayout.BeginHorizontal();			
				GUI.enabled = (directoryname != null && directoryname != "");
				if(GUILayout.Button("Convert All Textures", GUILayout.ExpandWidth(true)))
				{
					sb.Remove(0, sb.Length);
					ConvertAllTextures(directoryname);
				}
				GUI.enabled = true;			
	        GUILayout.EndHorizontal();
			GUILayout.Space(8f);		
	        GUILayout.BeginHorizontal();
				GUILayout.Label("Log:", GUILayout.ExpandWidth(true));	
	        GUILayout.EndHorizontal();	
	        GUILayout.BeginHorizontal();
				scrollPos = 
				EditorGUILayout.BeginScrollView(scrollPos, false, true, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
					string logstring = sb.ToString();
					foreach(string loglines in logstring.Split('\n'))
						GUILayout.Label(loglines);
				EditorGUILayout.EndScrollView();
	        GUILayout.EndHorizontal();
		GUILayout.EndVertical();		
    }
}
