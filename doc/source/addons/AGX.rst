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
- Be aware that if you query an empty action group, it will always return a state of False and trying to turn an emtpy action group On will do nothing and silently fail without any sort of error message.
- At this point, AG11 through AG250 do not support RemoteTech. Triggering those action groups will bypass the signal delay and execute those actions immediately. (On the immediate fix list.)

**Action state monitoring**
Note that the state of action groups is tracked on a per-action basis, rather then on a per-group basis. This results in the group state being handled differently. (Note the following applies only to AG11 through AG250, AG1 through AG10 are not affected by this.)

- The Script Trigger action found on the kOS computer module is not subject to the below considerations and is the recommended action to use when interacting with a running kOS script.
- The state of actions are monitored on the part and updated automatically. A closed solar panel will return a state of false for all it's actions. (Extend Panels, Retract Panels, Toggle Panels) When you extend the solar panel with either the Extend Panels or Toggle Panels action, all three actions will change to a state of True. Retract the panels and the state of all three actions will become False.
- This can result in an action group have actions in a mixed state where some actions are on and some are off. In this case querying the group state will result in a state of False. For the purposes of the group state being True or False, if *all* actions in the action group are true, the group state will return true. If *any* actions in the group are false,the group state with return False.

