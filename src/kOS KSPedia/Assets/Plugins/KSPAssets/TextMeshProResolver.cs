
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Assets.Plugins.KSPAssets.Editor
{
    //[InitializeOnLoad]
    static class TextMeshProResolver
    {
        private const string TextMeshPro = "TextMesh Pro/Plugins";
        private const string TextMeshProDllFilter = "TextMeshPro*.dll";


        static TextMeshProResolver()
        {
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomainOnAssemblyResolve;
        }


        private static Assembly CurrentDomainOnAssemblyResolve(object sender, ResolveEventArgs args)
        {
            if (!args.Name.StartsWith("TextMeshPro")) return null;

            Assembly result;

            if (FindLoadedTMP(out result))
            {
                return result;
            }

            string path;

            if (!GetPathToTMP(out path))
                Debug.LogError("need to install TextMeshPro");
            else Debug.LogWarning("modify this function to load the DLL ourselves"); // seems to always be loaded by the time we get there, but include a reminder
            // on how to fix it in case this is ever not the case

            return null;
        }


        private static string SanitizePath(string path)
        {
            return path.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);
        }


        // ReSharper disable once InconsistentNaming
        private static bool GetPathToTMP(out string path)
        {
            path = string.Empty;

            var tmpDir = SanitizePath(Path.Combine(Application.dataPath, TextMeshPro));

            if (!Directory.Exists(tmpDir))
                return false;

            var possibleDlls = Directory.GetFiles(tmpDir, TextMeshProDllFilter);

            if (possibleDlls.Length == 0)
            {
                Debug.LogError("could not find TextMeshPro dll!");
                return false;
            }
            else if (possibleDlls.Length > 1)
            {
                Debug.LogError("multiple dlls found for TextMeshPro. Did you install multiple versions?");
                return false;
            }

            var possiblePath = SanitizePath(Path.Combine(tmpDir, possibleDlls.Single()));

            if (!File.Exists(possiblePath))
                return false;

            path = possiblePath;
            return true;
        }


        // ReSharper disable once InconsistentNaming
        private static bool FindLoadedTMP(out Assembly tmpAssembly)
        {
            string path;
            tmpAssembly = null;

            if (!GetPathToTMP(out path))
                return false;

            foreach (var a in AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => a.GetName().Name.StartsWith("TextMeshPro")))
            {
                if (0 != string.Compare(SanitizePath(a.Location), path, StringComparison.Ordinal))
                    continue;

                tmpAssembly = a;
                return true;
            }

            return false;
        }
    }
}
