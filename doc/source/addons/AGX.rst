Action Groups Extended
======================

- Download: https://github.com/SirDiazo/AGExt/releases  
- Forum thread, including full instructions: http://forum.kerbalspaceprogram.com/threads/74195

Increase the action groups available to kOS from 10 to 250. Also adds the ability to edit actions in flight as well as the ability to name action groups so you can describe what a group does.

Includes a Script Trigger action that can be used to control a running program and visual feedback if an action group is currently activated.

**Usage:** 
Adds action groups AG11 through AG250 to kOS that are interacted with the same way as the AG1 through AG10 bindings in base kOS are.

Anywhere you use ``AG1``, you can use ``AG15`` in exactly the same way.

Caution: Sometimes, AGX will return an unexpected value for a group's state (On/Off), see below for the explination. (Action State Monitoring)
 
**Basic Quick Start:**

.. figure:: /_images/addons/AGExtQuickStart1.jpg
.. figure:: /_images/addons/AGExtQuickStart2.jpg


**Overview Walkthrough:** (Video, imagur album, animated gif, something)

Note that this mod only adds action grousp 11 through 250, it does not change how action groups 1 through 10 behave in any way.

**Known limitations:** 

- For an action group to be useable, it must have an action assigned to it. When installed, AGX adds a "Script Trigger" action to the kOS computer part that serves this purpose if you want an "empty" action group to trigger kOS scripts with. 
- Be aware that if you query an empty action group, it will always return a state of False and trying to turn an emtpy action group On will do nothing and silently fail without any sort of error message. (Groups AG11 through AG250 only. Groups AG1 through AG10 can be empty and will turn On and Off when commanded to.)
- At this point, AG11 through AG250 do not support RemoteTech. Triggering those action groups will bypass the signal delay and execute those actions immediately. (On the immediate fix list.)

**Action state monitoring**

Note that the state of action groups is tracked on a per-action basis, rather then on a per-group basis. This results in the group state being handled differently. (AG 11 through AG250 only.)

- The Script Trigger action found on the kOS computer module is not subject to the below considerations and is the recommended action to use when interacting with a running kOS script.
- The state of actions are monitored on the part and updated automatically. A closed solar panel will return a state of false for all it's actions. (Extend Panels, Retract Panels, Toggle Panels) When you extend the solar panel with either the Extend Panels or Toggle Panels action, all three actions will change to a state of True. Retract the panels and the state of all three actions will become False. Note that this state will update in any action group that contains that action, not just the action group that was activated.
- This can result in an action group have actions in a mixed state where some actions are on and some are off. In this case querying the group state will result in a state of False. For the purposes of the group state being True or False, if *all* actions in the action group are true, the group state will return true. If *any* actions in the group are false,the group state with return False.
- When an action triggers an animation, the state of the action will be uncertain until the animation finishes playing. Some parts will report True during the animation and some will report False. It depends on how the part creator set things up and not something AGX can control.
- For clarity, visual feedback can be provided of the current state of an action group. When editing action groups, find the "Toggle Grp." button just below the text entry field for the group name in the main AGX window and enable it. (It is enabled/disabled for the current action group when you click the button.) Once you do this, the text displaying that group will change from gray to colored. Green: Group is activated (state True). Red: Group is deactivated (state False). Yellow: Group is in a mixed state, will return a state False when queried.
- It is okay to activate an already activated group and deactivate a non-activated group. Actions in the group will still try to execute as normal. Exact behavior of a specific action will depend on how the action's creator set things up.

**Example code:**

- ``AG15 on.`` Activate action group 15.
- ``print AG15.`` Print action group 15's state to the terminal. (True/False)
- ``on AG15 {``
- ``print "Action group 15 clicked!".``
- ``preserve.``
- ``}`` Print to the terminal anytime you activate action group 15. Use this to change variables within a running kOS script and the "Script Trigger" action found on the kOS computer part.

If you want to use the ``on AG15`` command to monitor a part that has an animation, a cool down is required.

- ``on AG15 {``
- ``print "Solar Panel Toggled!".``
- ``preserve.``
- ``}`` will print to the terminal 3 times when the solar panel is extened.

- Player activates AG15, AG15's state goes from false to true and the actions are triggered. ``AG15 False -> True`` and prints to the terminal.
- On it's next update pass (100ms to 250ms later), AGX checks AG15's state and sees the solar panel is still deploying which means that AG15's state is false and so sets it that way. ``AG15 True -> False`` and prints to the terminal.
- A few seconds later, the solar panel finishes it's deployment animation. On it's next update pass AGX checks AG15's state and sees the solar panel is now deployed which means that AG15's state is now true and so sets it that way. ``AG15 False -> True`` and prints to the terminal a third time.

As a workaround, you need to add a cooldown:

- ``declare cooldownTimeAG15``
- ``on AG15 {``
- ``if cooldownTimeAG15 + 10 < time {``
- ``print "Solar Panel Toggled!".``
- ``set cooldownTimeAG15 to time.``
- ``}``
- ``preserve.``
- ``}``

Note the 10 in the second line, that is your cooldown time in seconds. Set this to a number of seconds that is longer then your animation time and the above code will limit AG15 so it can only activate after 10 seconds have passed since the previous activation and not activate multiple times on the same activation.




