using System.Reflection;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyCopyright("Copyright ©  2019")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("324da3fe-391b-421d-93b7-29499dcf9ef3")]

[assembly: KSPAssemblyDependency("kOS.Safe", 0, 0)]

// The name-tag part module lives in KSPCommunityPartModules, so KSP must load that mod before kOS.
// This is a hard dependency: KSP refuses to load kOS unless it is present.
[assembly: KSPAssemblyDependency("KSPCommunityPartModules", 0, 5)]