Action Groups Extended
=======

- Download: https://github.com/SirDiazo/AGExt/releases  
- Forum thread, including full instructions: http://forum.kerbalspaceprogram.com/threads/74195

Increase the action groups available to kOS from 10 to 250 and add the ability to edit actions in flight as well as the ability to name action groups so you describe what a group does.

Includes a Script Trigger action that can be used to control a running program and visual feedback if an action group is currently activated.

**Usage:** 
Adds action groups AG11 through AG250 to kOS that are interacted with the same way as the AG1 through AG10 bindings in base kOS are.

- ``AG15 on.``  Activate action group 13.
- ``AG15 off.`` Deactivate action group 13.
- ``print AG15.`` Prints True or False based on action group 15's state.
- And however else you used AG1 can be used for AG15.
- Note: When you issue the ``AG15 on.`` command, all actions in action group 15 are trigged in the activate direction regardless of the current state of the action group. How an action handles being activated when already activated will depend on how the original creator of the action set things up. 

Caution:
 

    ON AG15 {
    print"AG15 triggered!".
    preserve.
    }
    
will behave unexpected if a action triggered by action group 15 has an animation. Because AGX montiors group state per action, the above code will most likely trigger three times (assuming AG15 has a Solar Panel Toggle Action in it):
  1. AG15 is triggered by the player somehow. (AG15 Off -> On)
  2. Panel starts deploying, but AGX sees the action as off while the animation plays and turns the action back off. (AG15 On -> Off)
  3. Animation finishes playing, AGX now sees the action as on and turns the action on (AG15 Off -> On)
If you need to do this, you have to add a delay that is long then the animation time as so:
``Add code snippet``


**Basic Quick Start:**
![](http://members.shaw.ca/diazo/AGExtQuickStart1.jpg)
![](http://members.shaw.ca/diazo/AGExtQuickStart2.jpg)

**Overview Walkthrough:**
(Video, imagur album, animated gif, something)

**Known limitations:**

- The following limitations apply only to AG11 through AG250. AG1 through AG10 will behave as they have regardless of whether AGX is installed or not.
- For an action group to be useable, it must have an action assigned to it. When installed, AGX adds a "Script Trigger" action to the kOS computer part that serves this purpose if you want an "empty" action group to trigger kOS scripts with. 
- Be aware that if you query an empty action group, it will always return a state of False and trying to turn an emtpy action group On will do nothing and silently fail without any sort of error message. (Groups AG11 through AG250 only. Groups AG1 through AG10 can be empty and will turn On and Off when commanded to.)
- At this point, AG11 through AG250 do not support RemoteTech. Triggering those action groups will bypass the signal delay and execute those actions immediately. (On the immediate fix list.)

**Action state monitoring**

Note that the state of action groups is tracked on a per-action basis, rather then on a per-group basis. This results in the group state being handled differently.

- The Script Trigger action found on the kOS computer module is not subject to the below considerations and is the recommended action to use when interacting with a running kOS script.
- The state of actions are monitored on the part and updated automatically. A closed solar panel will return a state of false for all it's actions. (Extend Panels, Retract Panels, Toggle Panels) When you extend the solar panel with either the Extend Panels or Toggle Panels action, all three actions will change to a state of True. Retract the panels and the state of all three actions will become False. Note that this state will update in any action group that contains that action, not just the action group that was activated.
- This can result in an action group have actions in a mixed state where some actions are on and some are off. In this case querying the group state will result in a state of False. For the purposes of the group state being True or False, if *all* actions in the action group are true, the group state will return true. If *any* actions in the group are false,the group state with return False.
- When an action triggers an animation, the state of the action will be uncertain until the animation finishes playing. Some parts will report True during the animation and some will report False. It depends on how the part creator set things up and not something AGX can control.
- For clarity, visual feedback can be provided of the current state of an action group. When editing action groups, find the "Toggle Grp." button just below the text entry field for the group name in the main AGX window and enable it. (It is enabled/disabled for the current action group when you click the button.) Once you do this, the text displaying that group will change from gray to colored. Green: Group is activated (state True). Red: Group is deactivated (state False). Yellow: Group is in a mixed state, will return a state False when queried.
- It is okay to activate an already activated group and deactivate a non-activated group. Actions in the group will stil try to execute as normal. Exact behavior of a specific action will depend on how the action's creator set things up.

