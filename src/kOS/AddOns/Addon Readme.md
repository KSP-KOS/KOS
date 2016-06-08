kOS 3rd Party Addon Framework
*****************************

#How To Implement an Addon
* The Addon should be in a separate assembly separate from kOS and from the mod
  you are trying to interact with.  kOS is functionally indifferent to the final
  file location, however you should follow normal practices to avoid conflicts
  and make it easy for users to install.
* Your AssemblyInfo.cs must define `[assembly: KSPAssemblyDependency("kOS", x, y)]`
  where x and y are the minimum major and minor version of kOS with which your
  Addon will function.
* Your project at a minimum include the following references:
  * kOS.dll
  * kOS.Safe.dll
  * UnityEngine.dll
  * Assembly-CSharp.dll
  * Assembly-CSharp-firstpass.dll
  * KSPUtil.dll
* You should have one central class which inherits from `kOS.Suffixed.Addon`
  and is decorated with the attribute `[kOSAddon(identifier)]` where identifier
  is the string that will be used to access the Addon as `addons:identifier`.
* Your Addon should override the abstract `Available` method, returning true
  only when the addon's core functionality is available.  You might choose to
  return false if another required mod is not installed, or if a required part
  is not on the current vessel.
* You may add additional kOS Structures, and then expose them using suffixes
  on your central Addon class.
* All new structures, including your Addon, must be decorated with a
  `[KOSNomenclature]` attribute, identifying it's "kOS Name" (the value returned
  by the `typename` suffix).

#Discouraged Options
While it is possible for you to provide your own bindings and functions, this is
generally not recommended.  By using a binding or function instead of a suffix
on an Addon you are adding to the global namespace.  This is a potential issue
for name collisions, and it could lead users to think that the function is part
of the stock kOS library rather than a 3rd party addition.  In some cases where
tight integration with kOS is required, this may be necessary.  We ask that
whenever possible you keep the access to your additional features limited to the
central Addon class.
