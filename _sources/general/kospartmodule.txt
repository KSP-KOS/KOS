.. _kospartmodule:

KOS Processor PartModule Configuration Fields
=============================================

.. note::
    (The most important part of this page is probably the
    section below on the EcPerInstruction setting.)

When using `ModuleManager <https://github.com/sarbian/ModuleManager/wiki>`_
or directly editing the part.cfg files a mod ships with, it is useful to
know what those settings mean.  This page documents what some of the
settings in the kOS part config files mean.

These are the settings typically found in the files named:

``GameData/kOS/Parts/`` name_of_part_here ``/part.cfg``

You can add kOS to any other part in the game by adding the kOS module
to the part (although this may cause strange interactions that are not
officially supported).

Here is an example of the kOS processor module : the one that is
attached to the small disk shaped CPU part (KR-2402 b)::

    MODULE
    {
	    name = kOSProcessor
	    diskSpace = 5000
	    ECPerBytePerSecond = 0
	    ECPerInstruction = 0.000004
    }

If you add a section like that to the part.cfg, via directly editing it,
or via a ModuleManager configuration, then you cause that part to contain
a kOS computer.

When editing these values, the case is important.  You must capitalize
them and lowercase them exactly as shown here.

Here is a list of all the potential fields you could set in that section:

.. _diskSpace:

diskSpace
---------

    - **Type:** integer
    - **Default if omitted:** 1024
    - **Effect:** The disk space the part has by default if the
      adjustment slider in the VAB isn't changed by the user.

.. _baseDiskSpace:

baseDiskSpace
-------------

    - **Type:** integer
    - **Default if omitted:** copied from initial :ref:`diskSpace <diskSpace>` setting
    - **Effect:** The lowest disk space the part can have at the lowest
      end of the slider in the VAB.

The possible choices for disk space the user can select on the
slider is always one of 1x, 2x, and 4x this amount.

.. _ECPerInstruction:

ECPerInstruction:
-----------------

   - **Type:** float
   - **Default if omitted:** 0.000004
   - **Effect:** How much ElectricCharge resource is consumed per
     instruction the program executes.

This is a very small number so the electric charge can be payed
in micro-amounts as the CPU executes.  Remember that with default
Unity settings (which can be changed on the KSP game's main settings
screen at the launch of the program), the game runs 25 physical
updates per second.  So if the setting is 0.000004, and program is
executing 200 instructions per update, at 25 updates per second,
then it's consuming 0.02 Ec per second, or 1 Ec every 50 seconds.

This is the setting from which the value in the VAB/SPH info panel,
1 Electric Charge per N instructions, is derived (it's the reciprocal
of that display value).

More information about programs reducing power consumption can be
found in the section of the CPU hardware description that
:ref:`talks about electric drain<electricdrain>`.

.. _ECPerBytePerSecond:

ECPerBytePerSecond:
-------------------

   - **Type:** float
   - **Default if omitted:** 0.0
   - **Effect:** How much ElectricCharge resource is consumed per
     byte of disk space avaialable (not just used).

It is possible to make the disk cost more electricity to run the
bigger it is.  By default this ships as zero, but it can be changed
by a re-balancing mod by changing this value.  This value is
multiplied by how much available space there is total (used + free),
not just how much is currently in use.

.. _electriccharge:
