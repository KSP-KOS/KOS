using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace kOS.Safe.Utilities
{
    [System.AttributeUsage((System.AttributeTargets.Class | System.AttributeTargets.Struct), Inherited = false, AllowMultiple = false)]
    public class AssemblyWalkAttribute : Attribute
    {
        public Type AttributeType { get; set; }
        public Type InterfaceType { get; set; }
        public Type InherritedType { get; set; }
        public string StaticWalkMethod { get; set; }
        public string StaticRegisterMethod { get; set; }
        public int LoadedCount { get; set; }

        public static readonly Dictionary<AssemblyWalkAttribute, Type> AllWalkAttributes = new Dictionary<AssemblyWalkAttribute, Type>();
        public static readonly Dictionary<Type, Type> AttributesToWalk = new Dictionary<Type, Type>();

        public AssemblyWalkAttribute()
        {
            LoadedCount = 0;
        }

        public override string ToString()
        {
            if (AttributeType != null)
                return AttributeType.FullName;
            if (InterfaceType != null)
                return InterfaceType.FullName;
            if (InherritedType != null)
                return InherritedType.FullName;
            return "StaticWalk";
        }

        public static void Walk()
        {
            LoadWalkAttributes();
            WalkAllAssemblies();
        }

        public static void LoadWalkAttributes()
        {
            AllWalkAttributes.Clear();
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (Assembly assembly in assemblies)
            {
                if (!assembly.GlobalAssemblyCache)
                {
                    SafeHouse.Logger.LogWarning("Loading assembly: " + assembly.FullName);
                    try
                    {
                        LoadWalkAttribute(assembly);
                    }
                    catch
                    {
                        SafeHouse.Logger.LogWarning("Error while loading assembly: " + assembly.FullName + ", skipping assembly.");
                    }
                }
            }
        }

        public static void LoadWalkAttribute(Assembly assembly)
        {
            foreach (Type type in assembly.GetTypes())
            {
                var attr = type.GetCustomAttributes(typeof(AssemblyWalkAttribute), true).FirstOrDefault() as AssemblyWalkAttribute;
                if (attr != null)
                {
                    AllWalkAttributes.Add(attr, type);
                }
            }
        }

        public static void WalkAllAssemblies()
        {
            SafeHouse.Logger.LogWarning("Begin walking assemblies.");
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (Assembly assembly in assemblies)
            {
                if (!assembly.GlobalAssemblyCache)
                {
                    SafeHouse.Logger.LogWarning("Walking assembly: " + assembly.FullName);
                    try
                    {
                        WalkAssembly(assembly);
                    }
                    catch
                    {
                        SafeHouse.Logger.LogWarning("Error while loading assembly: " + assembly.FullName + ", skipping assembly.");
                    }
                }
            }
            SafeHouse.Logger.LogWarning("Begin static walk methods.");
            foreach (AssemblyWalkAttribute walkAttribute in AllWalkAttributes.Keys)
            {
                if (!string.IsNullOrEmpty(walkAttribute.StaticWalkMethod))
                {
                    Type managerType = AllWalkAttributes[walkAttribute];
                    var walkMethod = managerType.GetMethod(walkAttribute.StaticWalkMethod, BindingFlags.Static | BindingFlags.Public);
                    walkMethod.Invoke(null, null);
                }
                SafeHouse.Logger.LogWarning("Attribute " + walkAttribute.ToString() + " loaded " + walkAttribute.LoadedCount + " objects.");
            }
            SafeHouse.Logger.LogWarning("Finish walking assemblies.");
        }

        public static void WalkAssembly(Assembly assembly)
        {
            foreach (Type type in assembly.GetTypes())
            {
                foreach (AssemblyWalkAttribute walkAttribute in AllWalkAttributes.Keys)
                {
                    if (!string.IsNullOrEmpty(walkAttribute.StaticRegisterMethod))
                    {
                        Type managerType = AllWalkAttributes[walkAttribute];
                        var managerRegisterMethod = managerType.GetMethod(walkAttribute.StaticRegisterMethod, BindingFlags.Static | BindingFlags.Public);
                        if (managerRegisterMethod != null)
                        {
                            if (walkAttribute.AttributeType != null)
                            {
                                var attr = type.GetCustomAttributes(walkAttribute.AttributeType, false).FirstOrDefault();
                                if (attr != null)
                                {
                                    managerRegisterMethod.Invoke(null, new[] { attr, type });
                                    walkAttribute.LoadedCount++;
                                }
                            }
                            else if (walkAttribute.InterfaceType != null)
                            {
                                bool isInterface = !type.IsInterface && type.GetInterfaces().Contains(walkAttribute.InterfaceType);
                                if (isInterface)
                                {
                                    managerRegisterMethod.Invoke(null, new[] { type });
                                    walkAttribute.LoadedCount++;
                                }
                            }
                            else if (walkAttribute.InherritedType != null)
                            {
                                if (!type.IsAbstract && walkAttribute.InherritedType.IsAssignableFrom(type))
                                {
                                    managerRegisterMethod.Invoke(null, new[] { type });
                                    walkAttribute.LoadedCount++;
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}