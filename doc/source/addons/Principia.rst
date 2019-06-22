.. _principia:

Principia
==================

- Download: https://github.com/mockingbirdnest/Principia/releases
- Forum thread: https://forum.kerbalspaceprogram.com/index.php?/topic/162200-wip131-14x-151-161-170-principia%E2%80%94version-fatou-released-2019-06-03%E2%80%94n-body-and-extended-body-gravitation-axial-tilt/

Principia aims to replace KSP's unstable physics integration with a higher-order symplectic integrator, adding n-body Newtonian gravitation in the process. See the forum thread for more details.

This addon is not associated with and not supported by the creators of Principia.

The Principia API is accessed through C# reflection, and is designed for the
current version. This means that future Principia updates may break this
addon, in which case ``ADDONS:AVAILABLE("Principia")`` will return false.  It is also
possible for future versions of Principia to remain fully compatible.

Principia Flight Planning Interface
-----------------------------------

.. structure:: Principia

    ============================= ===================================== =============
     Suffix                        Type                                  Description
    ============================= ===================================== =============
     :attr:`AVAILABLE`             :struct:`Boolean` (readonly)          True if a compatible Principia version is installed.
     :attr:`GEOPOTREFRADIUS`       :struct:`scalar` (readonly)           Returns the value in metres of the reference radius of the geopotential model for the home body (usually Kerbin).
     :attr:`HASMANOEUVRE`          :struct:`Boolean` (readonly)          True if Principia has a flight plan with at least one manoeuvre for this vessel.
     :attr:`NEXTMANOEUVRE`         :struct:`PRManoeuvre` (readonly)      Returns the next manoeuvre in the flight plan for this vessel.
     :attr:`ALLMANOEUVRES`         List  (readonly)                      Returns a list of all manoeuvres from the flight plan for this vessel.
    ============================= ===================================== =============



.. attribute:: Principia:AVAILABLE

    :type: :struct:`Boolean`
    :access: Get

    True if a compatible Principia version is installed.  If this
    is not true, then none of the other suffixes listed here are safe to
    call (they can cause error and program crash).

.. attribute:: Principia:GEOPOTREFRADIUS

    :type: :struct:`String`
    :access: Get

    Returns the value in metres of the reference radius of the geopotential model for the home body (usually Kerbin).

.. attribute:: Principia:HASMANOEUVRE

    :type: :struct:`Boolean`
    :access: Get

    True if Principia has a flight plan with at least one manoeuvre for this vessel. You should always check this before using :attr:`NextManoeuvre<Principia:NEXTMANOEUVRE>` to avoid exceptions.

.. attribute:: Principia:NEXTMANOEUVRE

    :type: :struct:`PRManoeuvre`
    :access: Get

    Returns the next manoeuvre in the flight plan for this vessel.
    The Principia API provides read only access to the flight plan. Attempting to change manoeuvres through this suffix will have no effect.

.. attribute:: TRAddon:ALLMANOEUVRES

    :type: List of :struct:`PRManoeuvre` objects
    :access: Get

    Returns a list of all manoeuvres from the flight plan for this vessel. Note that kOS does not automatically keep this up-to-date if the flight plan changes, so you should avoid storing this list.
    The Principia API provides read only access to the flight plan. Attempting to change manoeuvres through this suffix will have no effect.

.. structure:: PRManoeuvre

    This is very similar in structure to :struct:`ManeuverNode` to make porting scripts between them more straightforward.

    ===================================== ============================ =============
     Suffix                                Type                          Description
    ===================================== ============================ =============
     :attr:`DELTAV`                        :struct:`Vector` (m/s)        The burn vector with magnitude equal to delta-V.
     :attr:`BURNVECTOR`                    :struct:`Vector` (m/s)        Synonym for DELTAV.
     :attr:`ETA`                           :struct:`scalar` (s)          Time until this manoeuver starts.
     :attr:`DURATION`                      :struct:`scalar` (s)          Duration of burn for this manoeuvre.
    ===================================== ============================ =============

.. attribute:: PRManoeuvre:DELTAV

    :access: Get only
    :type: :struct:`Vector`

    The vector giving the total burn of the manoeuvre. The vector can be used to steer with, and its magnitude is the delta V of the burn.

.. attribute:: PRManoeuvre:BURNVECTOR

    Alias for :attr:`PRManoeuvre:DELTAV`.

.. attribute:: PRManoeuvre:ETA

    :access: Get only
    :type: :ref:`scalar <scalar>`

    The number of seconds until the expected burn time.
    
.. attribute:: PRManoeuvre:DURATION

    :access: Get only
    :type: :ref:`scalar <scalar>`

    The number of seconds the burn for this manoeuvre is expected to take.
    