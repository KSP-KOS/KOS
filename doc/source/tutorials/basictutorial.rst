********************************
KOS and programming introduction
********************************

This tutorial is for people who are new to programming and want to start programming using KOS.

.. contents:: Contents
    :local:
    :depth: 2

==========================
Accessing the KOS terminal
==========================

To use KOS you need to have a KOS processor on your vessel, they can be found under the 'control' tab in the editor and are under
the same tab as the RCS thrusters and the reaction wheels.
Right clicking the KOS processor in the editor after adding it to your vessel shows a screen where you can tweak settings of the
KOS processor.

There are two ways you can create KOS scripts: either you make them in game and edit them in game or you use a text editor.

Making scripts in-game
______________________

First we'll look at making and editing scripts in-game.
Put your vessel with KOS processor on the launchpad and right click the processor.
Press the button that says 'open terminal' and click on the window that just popped up.

typing the following will create a file called 'hello', to enter the command press ``ENTER`` (don't forget the period at the end):

::

	edit hello.


you can now edit the file and make your script for the processor to execute, the other tutorials will show how you can make a
working script. Keep in mind that files made in game dissapear after the vessel is gone. This happens because you locally made a file on the ship's processor.

To run the file, type:

::

	run hello.


Making scrips with an editor
____________________________

To use a text editor instead go to your KSP folder and go to ``Ships/Script/`` and create an empty file and name it whatever you want
(be cautious using spaces in filenames because this might mess stuff up, also, avoid using capital letters in .ks files if you are running Linux). Not sure where your KSP folder is? If you used steam to install KSP, go to your steam library and right click Kerbal Space Program in the list of games. Next click on ``properties`` and go to the ``local files`` tab. Finally, press on the ``browse local files`` button. (KSP doesn't require you to use steam, you can copy the KSP folder from the Steam folder to your desktop so you can play without using Steam).

All KOS script files should end with ``.ks``.
An example of a valid KOS script file name would be:

``hello.ks``

Running files you made using text editors is a bit more complex than if you'd make a file in game.
This is because when you make a script in game the script is locally stored on the ship.
Files you made with a text editor are saved in the archive, can be accessed from different saved games and won't be lost after
the vessel is gone. You could compare it with ground control, which has infinite data storage and a spaceship which has limited
space for data.

To access the archive type:

::

	switch to 0.

To run ``hello.ks`` type the same as above:

::

	run hello.

=============
Print and set
=============

Before we can send ships to space we first need to know the basic features of KOS.
Let's begin with the ``print`` command. Type the following in your terminal and press ``ENTER`` to enter the piece of code. Keep in mind that **ALL** lines of code require a period
at the end of the line, there are some exceptions but more about that later. ::

	print "hello world".  // shows: hello world

See those two forward slashes? That means that a comment has been made, anything
after the double slashes won't be read by the script so you don't have to worry
about what it says. Here's an example of what happens if you make something a comment: ::

	// print "hello world 1".
	print "hello world 2".

If you'd run these two lines of code you'd **ONLY** see:
``hello world 2``

You'd might be wondering why you'd want to use the forward slashes if the script doesn't read them.
The computer ignores them, but they're there to help leave explanations for the humans to see who might have to read your program.

To clear the screen type: ::

	clearscreen.

Now say that we really like to use our hello world command but don't want to type
the entire sentence every time, we could use the set command. ::

	set x to "hello world".

The set command 'sets' a certain value to the given variable. Everytime we refer
to ``x`` we will actually refer to ``"hello world"``. ::

	set x to "hello world".
	print x. // shows: hello world

Of course we can choose other things to print other than ``hello world``.
Keep in mind that 'normal' text requires ``""`` and variables you made using the
set command, numbers and booleans (true or false) don't need ``""``. ::

	set x to "hello world".
	set y to true.
	set z to 123.

	print x.   // shows: hello world
	print "x". // shows: x
	print y.   // shows: true
	print "y". // shows: y
	print z.   // shows: 123
	print "z". // shows: z

You can also replace a variable you've made: ::

	set x to "hello world".
	set x to "updated text".
	print x. // shows: updated text

	set x to "hello world".
	print x. // shows: hello world
	set x to "updated text".
	print x. // shows: updated text

As you can see ``hello world`` doesn't exist anymore. If you'd want to print
both you could do: ::

	set x to "hello world".
	set y to x.
	set x to "updated text".

	print y. // shows: hello world
	print x. // shows: updated text

Variables don't just have to be one letter you could also use a word as a variable, don't use spaces when naming variables. ::

	set WhateverThisVariableIs to false.
	print WhateverThisVariableIs. // shows: false

=============
If statements
=============

Now we know how to set certain text to a variable we can explore more stuff.
For instance ``if``, this checks if a certain value matches the given value. ::

  set x to 1.

  if x = 1 {
    print "x is one".
  }

This will show: ``x is one``.

You've probably noticed the curly brackets ``{ }`` after an ``if`` statement. You don't need a period at the end of an ``if`` statement but you need these brackets if you have more than one statement in the body, otherwise they're optional.

This is valid: ::

  if x = 1
    print "x is one".

You could cover the piece the code within the curly brackets with your hand and say: if ``x`` is equal to ``1``, then do whatever
I covered with my hand. ``If`` statements can also be used for booleans: ::

  set SomeBoolean to true.

  if SomeBoolean = true {
    print "this is a true".
  }

This will show: ``this is true``.

Ofcourse the equals sign isn't the only symbol you can use, other symbols are:


Equals to or bigger than:

``1 >= 1``

``2 >= 1``

Equals to or smaller than

``1 <= 1``

``1 <= 2``

Is not equal to:

``1 <> 2``

So as you have seen, we created some commands that will only happen if a condition is true, otherwise nothing happens and we move
on. But what if you want to do some commands when the condition is true and instead of doing nothing when it's false,
you'd give it commands to do instead. The ``else`` statement also requires curly brackets ``{ }``. ::

  set SomeAnimal to "Dog".

  if SomeAnimal = "Cat" {
    print "this is a cat".
  } else {
    print "this is not a cat".
  }

Since ``SomeAnimal`` isn't ``Cat``, it skips whatever would happen if ``SomeAnimal`` would be ``Cat``. Then it checks what else to do, which is
to print ``this is not a cat``. You could expand this by using ``else if``, which means that if the first ``if`` statement isn't true
then check the following ``if`` statement. ::

  set SomeAnimal to "Dog".

  if SomeAnimal = "Cat" {
    print "this is a cat".
  } else if SomeAnimal = "Dog" {
    print "this is a dog".
  } else {
    print "this is neither a cat nor a dog".
  }

This would print ``this is a dog``.

=====================
``if`` vs ``else if``
=====================

Hopefully you now know the basics of how ``if`` works. You might be wondering why use ``else if`` if it's the same as ``if``.

Example 1, using ``else if``
____________________________
::

	if distance <= 1 {
  	  print "Distance is within a meter.".
	} else if distance <= 100 {
  	  print "Distance is within 100 meters.".
	} else {
	  print "Distance is farther than 100 m.".
	}

Example 2, only using ``if``
____________________________
::

	if distance <= 1 {
	  print "Distance is within a meter.".
	}
	if distance <= 100 {
  	  print "Distance is within 100 meters.".
	}
	if distance > 1000 {
  	  print "Distance is farther than 1 kilometer.".
	}

Using example 1, if your distance is less than a meter you'll get the following message: ::

	Distance is within a meter.

Using example 2, if your distance is less than a meter you'll get the following messages: ::

	Distance is within a meter.
	Distance is within 100 meters.

As you can imagine the second example isn't good. If we're at less than a meter away from something and the messages for if we would be farther
than 100 meters show up we have a big problem. This could be fixed by doing the following, but **THIS IS UNNECESSARILY COMPLEX**: ::

 set Done to false.

 if Done = false {
   if distance <= 1 {
     print "Distance is within a meter.".
     set Done to true.
   }
 }

 if Done = false {
   if distance <= 100 {
     print "Distance is within 100 meters.".
     set Done to true.
   }
 }

 if Done = false {
   if distance > 1000 {
     print "Distance is farther than 1 kilometer.".
     set Done to true.
   }
 }

Now this essentially does the same as the ``else if`` script but it's way more confusing and complicating.

====================
Until, lock and wait
====================

The ``wait`` command is pretty straight forward: ::

  wait 10.
  print "done waiting".


It will take 10 seconds before ``done waiting`` shows up.
Using ``wait 0`` will let the script wait for one physics tick (a physics tick is the time it takes for KSP to update its physics), this can be handy for when you're doing stuff with maneuvers. Maneuvers don't show up instantly but show up
after one physics tick. More about maneuvers in a future tutorial.

The ``until`` command will keep looping a piece of code until the given value has been met. Here's a simple example of what you can do with an ``until`` command: ::

  set x to 0.
  until x > 100 {
    print x.
    set x to x + 1.
  }

This first sets ``x`` to 0 and until ``x`` is bigger than 100 it does whatever happens within the brackets.
In this case it prints ``x`` and then it increases ``x`` by 1. This loop repeats itself until ``x`` is bigger than 100.
Before we can talk about more complex until loops let's first talk about ``time:seconds`` and the ``lock`` command. ::

  print time:seconds.

Will print the current time in seconds. Let's say the in-game time is 1 minute.
It would print ``60``. You can also set the current in-game time as a variable: ::

  set CurrentTime to time:seconds.

The variable ``CurrentTime`` will stay 60 seconds. Eventhough the in-game time changes: ::

  set CurrentTime to time:seconds.
  print CurrentTime. // shows: 60
  wait 10.
  print CurrentTime. // shows: 60

As you can see, eventhough the in-game time has changed the variable ``CurrentTime`` is still 60. The ``set`` command does **NOT** constantly update the variable. If you want a constantly updating variable you have to use the ``lock`` command.
Here's an example of what the ``lock`` command can do: ::

  lock TimeSecondsPlusTen to time:seconds + 10.

If you print ``TimeSecondsPlusTen`` at 60 seconds it will show 70, if you print
``TimeSecondsPlusTen`` at 4000 seconds it will show 4010.

Using until, lock and wait in an example
_________________________________________

If we now combine all the command we can make the following piece of code: ::

  set Adder to 0.
  lock Multiplier to Adder * 2.
  set TimePlusFive to time:seconds + 5.

  until time:seconds > TimePlusFive {
    print Multiplier.
    set Adder to Adder + 1.
    wait 1.
  }

So an easy way to read the until loop is to cover what ever is inside of the curly brackets
and say: until ``time:seconds`` is bigger than our current time plus 5 seconds, repeat whatever I covered with my hand.
In this case that'd be: print ``Multiplier``, increase the value of ``Adder`` and wait 1 second.

The outcome of this piece of code is: ::

  0
  2
  4
  6
  8
  10

Wait until
__________

You can also use the ``wait until`` command, this pauses the current script until the
conditions have been met. ::

  set TimePlusFive to time:seconds + 5.
  wait until time:seconds > TimePlusFive.
  print "done waiting".

It will take 5 seconds for ``done waiting`` to show up.
Note: the ``wait until`` command only checks the condition once per physics tick.  Using ``wait until`` for a fraction of a physics tick will round up to the start of a new physics tick.

==================
Lists and lexicons
==================

::

  set Value1 to 0.
  set Value2 to 5.
  set Value3 to 10.
  set Value4 to 15.
  set Value5 to 20.

Let's say we want to put these values in a list we want to edit later we can put them into a list by typing the following: ::

  set ValueList to list(Value1, Value2, Value3, Value4, Value5).
  print ValueList.

This will show: ::

  [0] = 0
  [1] = 5
  [2] = 10
  [3] = 15
  [4] = 20

As you can see the list goes from 0-4 instead of 1-5. So if you'd want to access ``Value3`` you'd need to look for ``[2]``.
This can be done as follows: ::

  print ValueList[2]. // shows 10

But let's say you want to print every item in the list you could do: ::

  print ValueList[0]. // shows 0
  print ValueList[1]. // shows 5
  print ValueList[2]. // shows 10
  print ValueList[3]. // shows 15
  print ValueList[4]. // shows 20

But the problem with this is that you have to know how big the list is and it'd take up a lot space when dealing with big lists. ::

  for Whatever in ValueList {
    print Whatever.
  }

  for Value in ValueList {
    print Value.
  }

Both pieces of code do **EXACTLY** the same.
This checks each item in a given list (now called ``Whatever``) and does what the curly brackets contains.
(For each item in the list called ``ValueList``, which we call ``Whatever``, do whatever is inside of the brackets).

In this case it prints: ::

  0
  5
  10
  15
  20

You can also use variables to check an item in a list: ::

  set x to 3.
  print ValueList[x]. // shows 15

Does the same as: ::

  print ValueList[3]. // also shows 15

Lexicons
________

Lexicons are in a way the same as lists but they have some crucial differences.
Lexicons can store a pair of information, for example: ::

  set MyLexicon to lexicon("MyValue1", 100, "MyValue2", 200, "MyValue3", 300).

The following piece of code acts **EXACTLY** the same as the piece of code above but is easier to read: ::

  set MyLexicon to lexicon(
    "MyValue1", 100,
    "MyValue2", 200,
    "MyValue3", 300
  ).

::

  print MyLexicon["MyValue1"]. // shows 100
  print MyLexicon["MyValue2"]. // shows 200
  print MyLexicon["MyValue3"]. // shows 300

NOTE: print ``MyLexicon[100]``. will NOT work.

=========
Functions
=========

Imagine you're driving in a manual shift car for with an instructor for the first time.
He helps you getting into first gear and tells you the following when you want to accelerate: ::

  Let go of the gas pedal.
  Press in the clutch pedal.
  Shift the gear stick from first to second.
  Let go of the clutch pedal.
  Press in the gas pedal.

After a while he tells you: ::

  Let go of the gas pedal.
  Press in the clutch pedal.
  Shift the gear stick from second to third.
  Let go of the clutch pedal.
  Press in the gas pedal.

Not long after that he tells you: ::

  Let go of the gas pedal.
  Press in the clutch pedal.
  Shift the gear stick from third to fourth.
  Let go of the clutch pedal.
  Press in the gas pedal.

Wouldn't it be easier if instead of telling you the entire procedure he'd tell you the following: ::

  Shift from first to second.
  And after a after he tells you:
  Shift from second to third.
  And not long after that he tells you:
  Shift from third to fourth.

As you can see you only need to know how to shift once (if you're a quick learner) and after that telling the whole process is
repetitive. The same goes for code in KOS, you might want to use a piece of code more than once without typing it out everytime.
This is called a ``function`` and functions often have ``parameters`` (similar to starting conditions).

Keep in mind that the following piece of code is pseudo-code and is not actual working code but an example of what functions
are like: ::

  Function ShiftGearFirstToSecond {
    Let go of the gas pedal.
    Press in the clutch pedal.
    Shift the gear stick from first gear to second gear.
    Let go of the clutch pedal.
    Press in the gas pedal.
  }

Your instructor could now say ``ShiftGearFirstToSecond()`` and you'd know how to go from the first gear to the second.
But this is only about going from the first gear to the second and not from the second gear to the third.
To do that you'd need to have blank spaces for you to fill in with your desired gears. ::

  Function ShiftGear {
    Let go of the gas pedal.
    Press in the clutch pedal.
    Shift the gear stick from ____ to ____.
    Let go of the clutch pedal.
    Press in the gas pedal.
  }

On paper this sounds like a great idea but if your instructor tells you ``ShiftGear()`` ``first gear``, ``second gear``. But you're not sure where to
put ``first gear`` and where to put ``second gear``. Wouldn't it be handy if you made rule that the first word your instructor says is the
gear you start in and the second word he says is the gear you end in? Well luckily there's a way to apply that rule.
This is were ``parameters`` come into play, all functions get called using ``()`` after the function name and inside of the brackets
you put the parameters. ::

  Function ShiftGear {
    Parameter StartGear.
    Parameter EndGear.

    Let go of the gas pedal.
    Press in the clutch pedal.
    Shift the gear stick from StartGear to EndGear.
    Let go of the clutch pedal.
    Press in the gas pedal.
  }

As you can see we replaced the blank spaces with variables (parameters are also variables).
So to go from first gear to second gear you'd use:
``ShiftGear(first, second)``.
To go from second to third you'd use:
``ShiftGear(second, third)``.
To go from third to second you'd use:
``ShiftGear(third, second)``.

A working example of a function
_______________________________

Here's an example of a simple function which works in KOS: ::

  Function OneThroughFivePrint {
    print 1.
    print 2.
    print 3.
    print 4.
    print 5.
    }

Functions can have any name but avoid making functions and variables the same name as this will very likely cause problems.
A function will do anything that's inside of the curly brackets. To use this function type the following: ::

 OneThroughFivePrint().

This will show: ::

  1
  2
  3
  4
  5

A more complex function
________________________

Here's an example of a more complex function which has a parameter and will also work in KOS:

Let's say we're in a perfectly circular orbit around kerbin, we can use the following formula:
``velocity = (2 * pi * radius) / orbital period``
(https://en.wikipedia.org/wiki/Circular_motion#Formulas)

Ignore how ``ship:orbit:period`` works for now, that will be discussed in the next chapter. ::

  Function VelocityCalculator {
    Parameter OrbitHeight.

    set KerbinRadius to 600000.
    set TotalRadius to OrbitHeight + KerbinRadius.
    set OrbitalPeriod to ship:orbit:period.
    print (2 * 3.1416 * TotalRadius) / OrbitalPeriod.
  }

If you're in a 400 km circular orbit and type: ::

  VelocityCalculator(400000).

Will show your orbital velocity.

Now what if you want to use the velocity for other calculations, is that possible? Yes of course that's possible!
The ``return`` command is very helpful is these situations. The ``return`` function returns a value, piece of text, boolean etc and ends
the function it is in. ::

  Function VelocityCalculator {
    Parameter OrbitHeight.

    set KerbinRadius to 600000.
    set TotalRadius to OrbitHeight + KerbinRadius.
    set OrbitalPeriod to ship:orbit:period.
    return (2 * 3.1416 * TotalRadius) / OrbitalPeriod.
    // everything after the return command will be skipped because a return command ends a function.
    print "this will be skipped".
  }

  set CurrentVelocity to VelocityCalculator(400000).
  print CurrentVelocity.

Will show your orbital velocity for a circular orbit at 400 kilometers.

Suffixes
========

In KOS you can access information about orbits using special structures.
Let's start with things we can check about our ship's orbit. ::

  print ship:orbit:apoapsis. // shows the ship's apoapsis
  print kerbin:orbit:apoapsis. // shows kerbin's apoapsis
  print ship:body:orbit:apoapsis. // shows kerbin's apoapsis if you're currently orbiting kerbin

You could compare these structures to a fill in the blanks story: ::

  print ___:orbit:apoapsis. // shows the apoapsis of whatever you fill in the blank

There are also other things you can get instead of just apoapsis, for example: ::

  print ship:orbit:periapsis. // shows the ship's periapsis
  print ship:orbit:period. // shows the ship's period
  print ship:orbit:inclination. // shows the ship's inclination
  print ship:orbit:eccentricity. // shows the ship's eccentricity
  print ship:orbit:semimajoraxis. // shows the ship's semimajoraxis

The full list of things you can add after :orbit can be found here:
https://ksp-kos.github.io/KOS/structures/orbits/orbit.html

Taking a step back, you can also look up values of planets ::

  print kerbin:name. // shows kerbin
  print kerbin:mass. // shows kerbin's mass
  print kerbin:radius // shows kerbin's radius
  print kerbin:mu // shows kerbin's gravitational parameter

If you're currently orbiting kerbin, the following is true: ::

  print ship:body:name. // shows kerbin
  print ship:body:mass. // shows kerbin's mass
  print ship:body:radius // shows kerbin's radius
  print ship:body:mu // shows kerbin's gravitational parameter

More information about that here:
https://ksp-kos.github.io/KOS/structures/orbits/orbitable.html
