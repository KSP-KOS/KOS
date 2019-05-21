using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace kOS.Safe.Utilities
{
    /// <summary>
    /// Classes decorated with this attribute will be treated as "manager" classes. You must either
    /// define a StaticWalk method, or define one of the 3 Type properties and a
    /// StaticRegisterMethod. If you are defining the AttributeType property you may optionally
    /// define either the InterfaceType or InherritedType as well. When Walk() is called every Type
    /// in the currently loaded assemblies (excluding the GAC) will be checked for this attribute,
    /// and then a 2nd time for the type conditions of each AssemblyWalkAttribute found. Each Type
    /// matching a condition of the attribute will be passed to the StaticRegisterMethod. Optionally,
    /// defining StaticWalkMethod will let the manager walk the assemblies itself, allowing for more
    /// complicated conditions
    /// </summary>
    [AttributeUsage((AttributeTargets.Class | AttributeTargets.Struct), Inherited = false, AllowMultiple = false)]
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

        private const string toStringFormat = "AssemblyWalkAttribute({0})";
        private const string assemblyLoadErrorFormat = "Error while loading assembly: {0}, skipping assembly.\nFull path: {1}";

        public AssemblyWalkAttribute()
        {
            LoadedCount = 0;
        }

        public override string ToString()
        {
            if (AttributeType != null)
                return string.Format(toStringFormat, AttributeType.FullName);
            if (InterfaceType != null)
                return string.Format(toStringFormat, InterfaceType.FullName);
            if (InherritedType != null)
                return string.Format(toStringFormat, InherritedType.FullName);
            if (!string.IsNullOrEmpty(StaticWalkMethod))
                return string.Format(toStringFormat, StaticWalkMethod);
            return string.Format(toStringFormat, "<None>");
        }

        public static void Walk()
        {
            SafeHouse.Logger.Log("AssemblyWalkAttribute begin walking");
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            Walk(assemblies);
        }

        public static void Walk(Assembly[] assemblies)
        {
            SafeHouse.Logger.Log("AssemblyWalkAttribute begin walking");
            LoadWalkAttributes(assemblies);
            WalkAssemblies(assemblies);
        }

        public static void LoadWalkAttributes(Assembly[] assemblies)
        {
            SafeHouse.Logger.SuperVerbose("Begin loading AssemblyWalkAttributes.");
            foreach (Assembly assembly in assemblies)
            {
                // Don't check the assembly if it's in the GAC.
                // This is mostly to save time, since there are a number of assemblies
                // in the GAC, and it should not be possible for them to at all implement
                // this attribute.
                if (!assembly.GlobalAssemblyCache)
                {
                    SafeHouse.Logger.SuperVerbose("Loading assembly: " + assembly.FullName);
                    try
                    {
                        LoadWalkAttribute(assembly);
                    }
                    catch
                    {
                        SafeHouse.Logger.LogWarning(string.Format(assemblyLoadErrorFormat, assembly.FullName, assembly.Location));
                    }
                }
            }
            SafeHouse.Logger.SuperVerbose("Loaded " + AllWalkAttributes.Count + " attributes.");
        }

        public static bool CheckMethodParameters(Type baseType, string methodName, params Type[] parameterTypes)
        {
            var managerRegisterMethod = baseType.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public);
            if (managerRegisterMethod != null)
            {
                // Check the register method to make sure it supports both parameters.
                var parameters = managerRegisterMethod.GetParameters();
                if (parameters.Length != parameterTypes.Length)
                {
                    return false;
                }
                // Check the type of each parameter to make sure they can be assigned properly
                for (int i = 0; i < parameters.Length; i++)
                {
                    if (!parameters[i].ParameterType.IsAssignableFrom(parameterTypes[i]))
                    {
                        return false;
                    }
                }
                return true;
            }
            return false;
        }

        public static void LoadWalkAttribute(Assembly assembly)
        {
            foreach (Type type in ReflectUtil.GetLoadedTypes(assembly))
            {
                var attr = type.GetCustomAttributes(typeof(AssemblyWalkAttribute), true).FirstOrDefault() as AssemblyWalkAttribute;
                if (attr != null)
                {
                    SafeHouse.Logger.LogWarning(string.Format("Found attribute on type {0}", type.Name));
                    if (!string.IsNullOrEmpty(attr.StaticRegisterMethod))
                    {
                        if (attr.AttributeType != null)
                        {
                            // the register method should support parameters of <AttributeType> and Type
                            if (CheckMethodParameters(type, attr.StaticRegisterMethod, attr.AttributeType, typeof(Type)))
                            {
                                AllWalkAttributes.Add(attr, type);
                                SafeHouse.Logger.SuperVerbose(string.Format("Add attribute on type {0}", type.Name));
                            }
                            else
                            {
                                string message = string.Format("Found AssemblyWalkAttribute on type {0} but the specified StaticRegisterMethod does not accept parameters of types {1} and Type",
                                    type.Name,
                                    attr.AttributeType.Name);
                                SafeHouse.Logger.LogError(message);
                                Debug.AddNagMessage(Debug.NagType.NAGFOREVER, message);
                            }
                        }
                        else if (attr.InherritedType != null || attr.InterfaceType != null)
                        {
                            // the register method should support only a parameter of Type
                            if (CheckMethodParameters(type, attr.StaticRegisterMethod, typeof(Type)))
                            {
                                AllWalkAttributes.Add(attr, type);
                                SafeHouse.Logger.SuperVerbose(string.Format("Add attribute on type {0}", type.Name));
                            }
                            else
                            {
                                string message = string.Format("Found AssemblyWalkAttribute on type {0} but the specified StaticRegisterMethod does not accept parameters of type Type",
                                    type.Name);
                                SafeHouse.Logger.LogError(message);
                                Debug.AddNagMessage(Debug.NagType.NAGFOREVER, message);
                            }
                        }
                    }
                    else if (!string.IsNullOrEmpty(attr.StaticWalkMethod))
                    {
                        // the static walk method should accept no parameters
                        if (CheckMethodParameters(type, attr.StaticWalkMethod))
                        {
                            AllWalkAttributes.Add(attr, type);
                            SafeHouse.Logger.SuperVerbose(string.Format("Add attribute on type {0}", type.Name));
                        }
                        else
                        {
                            string message = string.Format("Found AssemblyWalkAttribute on type {0} but the specified StaticWalkMethod does not accept zero parameters",
                                type.Name,
                                attr.AttributeType.Name);
                            SafeHouse.Logger.LogError(message);
                            Debug.AddNagMessage(Debug.NagType.NAGFOREVER, message);
                        }
                    }
                    else
                    {
                        string message = string.Format("Found AssemblyWalkAttribute on type {0} but neither StaticRegisterMethod nor StaticWalkMethod are specified",
                            type.Name);
                        SafeHouse.Logger.LogError(message);
                        Debug.AddNagMessage(Debug.NagType.NAGFOREVER, message);
                    }
                }
            }
        }

        public static void WalkAssemblies(Assembly[] assemblies)
        {
            SafeHouse.Logger.SuperVerbose("Begin walking assemblies.");
            foreach (Assembly assembly in assemblies)
            {
                if (!assembly.GlobalAssemblyCache)
                {
                    SafeHouse.Logger.SuperVerbose("Walking assembly: " + assembly.FullName);
                    try
                    {
                        WalkAssembly(assembly);
                    }
                    catch (Exception ex)
                    {
                        string message = string.Format(assemblyLoadErrorFormat, assembly.FullName, assembly.Location);
                        SafeHouse.Logger.LogError(message);
                        Debug.AddNagMessage(Debug.NagType.NAGONCE, message);
                        SafeHouse.Logger.LogError(string.Format("Exception: {0}\nStack Trace:\n{1}", ex.Message, ex.StackTrace));
                    }
                }
            }
            SafeHouse.Logger.SuperVerbose("Begin static walk methods.");
            foreach (AssemblyWalkAttribute walkAttribute in AllWalkAttributes.Keys)
            {
                if (!string.IsNullOrEmpty(walkAttribute.StaticWalkMethod))
                {
                    Type managerType = AllWalkAttributes[walkAttribute];
                    var walkMethod = managerType.GetMethod(walkAttribute.StaticWalkMethod, BindingFlags.Static | BindingFlags.Public);
                    walkMethod.Invoke(null, null);
                }
                SafeHouse.Logger.Log("Attribute " + walkAttribute.ToString() + " loaded " + walkAttribute.LoadedCount + " objects.");
            }
            SafeHouse.Logger.SuperVerbose("Finish walking assemblies.");
        }

        public static void WalkAssembly(Assembly assembly)
        {
            foreach (Type type in ReflectUtil.GetLoadedTypes(assembly))
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
                                    if (walkAttribute.InterfaceType != null)
                                    {
                                        bool isInterface = !type.IsInterface && type.GetInterfaces().Contains(walkAttribute.InterfaceType);
                                        if (isInterface)
                                        {
                                            managerRegisterMethod.Invoke(null, new[] { attr, type });
                                            walkAttribute.LoadedCount++;
                                        }
                                        else
                                        {
                                            string message = string.Format("Attribute {0} found on type {1}, but type does not implement the required interface {2}",
                                                walkAttribute.AttributeType.Name,
                                                type.Name,
                                                walkAttribute.InterfaceType.Name);
                                            SafeHouse.Logger.LogError(message);
                                            Debug.AddNagMessage(Debug.NagType.NAGONCE, message);
                                        }
                                    }
                                    else if (walkAttribute.InherritedType != null)
                                    {
                                        if (walkAttribute.InherritedType.IsAssignableFrom(type))
                                        {
                                            managerRegisterMethod.Invoke(null, new[] { attr, type });
                                            walkAttribute.LoadedCount++;
                                        }
                                        else
                                        {
                                            string message = string.Format("Attribute {0} found on type {1}, but type does not inherrit from the required type {2}",
                                                walkAttribute.AttributeType.Name,
                                                type.Name,
                                                walkAttribute.InherritedType.Name);
                                            SafeHouse.Logger.LogError(message);
                                        }
                                    }
                                    else
                                    {
                                        managerRegisterMethod.Invoke(null, new[] { attr, type });
                                        walkAttribute.LoadedCount++;
                                    }
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
                                if (walkAttribute.InherritedType.IsAssignableFrom(type))
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