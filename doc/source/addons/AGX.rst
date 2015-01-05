Action Groups Extended
=======

- Download: https://github.com/SirDiazo/AGExt/releases  
- Forum thread, including full instructions: http://forum.kerbalspaceprogram.com/threads/74195

Increase the action groups available to kOS from 10 to 250 and add the ability to edit actions in flight and the ability to name action groups so you describe what a group does.

Includes a Script Trigger action that can be used to control a running program and visual feedback if an action group is currently activated.

**Usage:** 
Adds action groups AG11 through AG250 to kOS that are interacted with the same way as the AG1 through AG10 bindings in base kOS are.

**Basic Quick Start:**
![](http://members.shaw.ca/diazo/AGExtQuickStart1.jpg)
![](http://members.shaw.ca/diazo/AGExtQuickStart2.jpg)

**Overview Walkthrough:**
(Video, imagur album, animated gif, something)

**Known limitations:**

- The following limitations apply only to AG11 through AG250. AG1 through AG10 will as they have regardless of whether AGX is installed or not.
- For an action group to be useable, it must have an action assigned to it. When installed, AGX adds a "Script Trigger" action to the kOS computer part that serves this purpose if you want an "empty" action group to trigger kOS scripts with.
- Be aware that if you query an emtpy action group, it will always return a state of False and trying to turn an emtpy action group On will do nothing and silently fail without any sort of error message.
- The state of action groups is tracked on a per-action basis, rather then on a per-group basis. Note the following for actions that are assigned to multiple action groups:
-- The state of actions are monitored on the part. 

