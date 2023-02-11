.. _career_limits:


Career Limits
=============

The theme of KSP's "career mode" is that some features of your space program don't work until after you've made some upgrades.
kOS now supports enforcing these checks, as described below.

.. contents::
    :local:
    :depth: 2

The rules being enforced
------------------------

These are rules inherited from the stock KSP game that kOS is simply adhering to:

-  If your tracking center is not upgraded far enough, then you cannot see
   future orbit patches.

-  If your mission control center AND tracking center are not upgraded enough,
   then you cannot add manuever nodes.

The following are rules invented by kOS that are thematically very similar to stock KSP, intended to be as close to the meaning of the stock game's rules as possible:

-  In order to be allowed to execute the :struct:`PartModule` :DOACTION method, either
   your VAB or your SPH must be upgraded to the point where it will allow access to
   custom action groups.  This is because otherwise the :DOACTION method would be a
   backdoor "cheat" past the restricted access to the action group feaures of various
   PartModules that the game is meant to have.

-  In order to be allowed to add a ``nametag`` to parts inside the editor so they get
   saved to the craft file, you must have upgraded your current editor building (VAB or
   SPH, depending) to the point where it allows at least stock action groups.  This is
   because name tags are a sort of semi-advanced feature.

You can see some of these settings by reading the values of the Career() global object,
for example::

    print Career():PATCHLIMIT.
    print Career():CANDOACTIONS.

Structure
---------

.. structure:: Career

    .. list-table:: **Members**
        :widths: 4 2 1 1
        :header-rows: 1
        
        * - Suffix
          - Type
          - Get
          - Set
          
        * - :attr:`CANTRACKOBJECTS`
          - :ref:`Boolean <boolean>`
          - yes
          - no
        * - :attr:`PATCHLIMIT`
          - :ref:`scalar <scalar>`
          - yes
          - no
        * - :attr:`CANMAKENODES`
          - :ref:`Boolean <boolean>`
          - yes
          - no
        * - :attr:`CANDOACTIONS`
          - :ref:`Boolean <boolean>`
          - yes
          - no

.. attribute:: Career:CANTRACKOBJECTS

    :type: :ref:`Boolean <boolean>`
    :access: Get

    If your tracking center allows the tracking of unnamed objects (asteroids, mainly) then this will return true.

.. attribute:: Career:PATCHLIMIT

    :type: :ref:`scalar <scalar>`
    :access: Get

    If your tracking center allows some patched conics predictions, then this number will be greater than zero.
    The number represents how many patches beyond the current one that you are allowed to see.  It influences
    attempts to call SHIP:PATCHES and SHIP:OBT:NEXTPATCH.

.. attribute:: Career:CANMAKENODES

    :type: :ref:`Boolean <boolean>`
    :access: Get

    If your tracking center and mission control buildings are both upgraded enough, then the game allows
    you to make manuever nodes (which the game calls "flight planning").  This will return true if you
    can make maneuver nodes.

.. attribute:: Career:CANDOACTIONS

    :type: :ref:`Boolean <boolean>`
    :access: Get

    If your VAB or SPH are upgraded enough to allow custom action groups, then you will also be allowed
    to execute the :DOACTION method of PartModules.  Otherwise you can't.  This will return a boolean
    letting you know if the condition has been met.

