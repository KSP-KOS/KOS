.. _trajectories:

Trajectories
==================

- Download: https://github.com/neuoy/KSPTrajectories/releases
- Forum thread: https://forum.kerbalspaceprogram.com/index.php?/topic/162324-131-110/

Trajectories is a mod that displays trajectory predictions, accounting for atmospheric drag, lift, etc.. See the forum thread for more details.

This addon is not associated with and not supported by the creator of Trajectories.

The Trajectories API is accessed through C# reflection, and is designed for the
current version. This means that future Trajectories updates may break this
addon, in which case ``ADDONS:TR:AVAILABLE`` will return false.  It is also
possible for future versions of Trajectories to remain fully compatible.

.. note::

    Trajectories only predicts the trajectory of the "Active Vessel," which is the vessel with the camera focused on it. :attr:`IMPACTPOS<TRAddon:IMPACTPOS>`, :attr:`PLANNEDVEC<TRAddon:PLANNEDVEC>`, :meth:`SETTARGET<TRAddon:SETTARGET>`, and :attr:`CORRECTEDVEC<TRAddon:CORRECTEDVEC>` will throw exceptions if you try to call them from an inactive vessel or if Trajectories has not calculated an impact position. You should always check if :attr:`HASIMPACT<TRAddon:HASIMPACT>` is true before accessing these suffixes.

For example::

    if ADDONS:TR:AVAILABLE {
        if ADDONS:TR:HASIMPACT {
            PRINT ADDONS:TR:IMPACTPOS.
        } else {
            PRINT "Impact position is not available".
        }
    } else {
        PRINT "Trajectories is not available.".
    }

Trajectories does its calculation based on the vessel's current orientation. Any tiny change in orientation will change the prediction.

Accuracy is not guaranteed.

See this repository for an example of this addon being used to land a rocket on the launch pad: https://github.com/CalebJ2/kOS-landing-script

Trajectories Version Compatibility
----------------------------------

The Trajectories mod went through some changes between Trajectories 1.x
and Trajectories 2.x.  These changes alter the way that the kOS
Trajectories AddOn has to communicate with the Trajectories mod, and
added new suffixes that kOS could use.  However, for backward
compatibility kOS will try to support an older version of Trajectories
if that's what's installed.  Places where a suffix only works with
newer versions of Trajectories are noted below in the suffix table.

Access structure TRAddon via ``ADDONS:TR``.

.. structure:: TRAddon

    ==================================== ===================================== =============
     Suffix                               Type                                  Description
    ==================================== ===================================== =============
     :attr:`AVAILABLE`                    :struct:`Boolean` (readonly)          True if a compatible Trajectories version is installed.
     :attr:`GETVERSION`                   :struct:`String` (readonly)           Trajectories version string.
     :attr:`GETVERSIONMAJOR`              :struct:`ScalarValue` (readonly)      Trajectories version Major.
     :attr:`GETVERSIONMINOR`              :struct:`ScalarValue` (readonly)      Trajectories version Minor.
     :attr:`GETVERSIONPATCH`              :struct:`ScalarValue` (readonly)      Trajectories version Patch.
     :attr:`ISVERTWO`                     :struct:`Boolean` (readonly)          True if Trajectories version is 2.0.0 or above.
     :attr:`ISVERTWOTWO`                  :struct:`Boolean` (readonly)          True if Trajectories version is 2.2.0 or above.
     :attr:`ISVERTWOFOUR`                 :struct:`Boolean` (readonly)          True if Trajectories version is 2.4.0 or above.
     :attr:`HASIMPACT`                    :struct:`Boolean` (readonly)          True if Trajectories has calculated an impact position for the current vessel.
     :attr:`IMPACTPOS`                    :struct:`GeoCoordinates` (readonly)   Returns a :struct:`GeoCoordinates` with the predicted impact position.
     :attr:`TIMETILLIMPACT`               :struct:`ScalarValue` (readonly)      **(only TR 2.2.0 and up)** Seconds until impact.
     :meth:`RESETDESCENTPROFILE(AoA)`     None                                  **(only TR 2.4.0 and up)** Reset all the Descent profile nodes.
     :attr:`DESCENTANGLES`                :struct:`List<ScalarValue>`           **(only TR 2.4.0 and up)** Descent profile angles.
     :attr:`DESCENTGRADES`                :struct:`List<Boolean>`               **(only TR 2.4.0 and up)** Descent profile grades (Retro or Pro).
     :attr:`DESCENTMODES`                 :struct:`List<Boolean>`               **(only TR 2.4.0 and up)** Descent profile modes (AoA or Horizon).
     :attr:`PROGRADE`                     :struct:`Boolean`                     **(only TR 2.2.0 and up** Descent profile all prograde.
     :attr:`RETROGRADE`                   :struct:`Boolean`                     **(only TR 2.2.0 and up** Descent profile all retrograde.
     :attr:`PLANNEDVEC`                   :struct:`Vector` (readonly)           Vector at which to point to follow predicted trajectory.
     :attr:`PLANNEDVECTOR`                :struct:`Vector` (readonly)           Alias for :attr:`PLANNEDVEC`
     :meth:`SETTARGET(position)`          None                                  Set Trajectories target.
     :attr:`HASTARGET`                    :struct:`Boolean` (readonly)          **(only TR 2.0.0 and up)** True if Trajectories target position has been set.
     :attr:`GETTARGET`                    :struct:`GeoCoordinates` (readonly)   **(only TR 2.4.0 and up)** Returns a :struct:`GeoCoordinates` with the Trajectories target position.
     :meth:`CLEARTARGET()`                None                                  **(only TR 2.4.0 and up)** Clear Trajectories target.
     :attr:`CORRECTEDVEC`                 :struct:`Vector` (readonly)           Offset plus :attr:`PLANNEDVEC` to correct path for targeted impact.
     :attr:`CORRECTEDVECTOR`              :struct:`Vector` (readonly)           Alias for :attr:`CORRECTEDVEC`
    ==================================== ===================================== =============



.. attribute:: TRAddon:AVAILABLE

    :type: :struct:`Boolean`
    :access: Get

    True if a compatible Trajectories version is installed.  If this
    is not true, then none of the other suffixes listed here are safe to
    call (they can cause error and program crash).

.. attribute:: TRAddon:GETVERSION

    :type: :struct:`String`
    :access: Get

    **Only gives the correct answer for Trajectries version >= 2.2.0**

    *For earlier versions, it gives a hardcoded fixed answer, as follows:*

    - For any Trajectories version earlier than 2.0.0,
      this returns the empty string "".
    - For any Trajectories version at least 2.0.0 but
      below 2.2.0, this returns the 'rounded off' answer "2.0.0"
      regardless of the precise version number within that range.
    - If your Trajectories version is at least 2.2.0 or above,
      this returns the specific version string correctly.

    For cases where you need to check for a known minimum Trajectories
    version, it is probably better to use the specific boolean suffix
    for that version (for example, :attr:`TRAddon:ISVERTWO`, or
    :attr:`TRAddon:ISVERTWOTWO` etc.)

.. attribute:: TRAddon:GETVERSIONMAJOR

    :type: :struct:`Scalar`
    :access: Get

    **Only gives the correct answer for Trajectries version >= 2.0.0**

    *For earlier versions, it gives a hardcoded fixed answer, as follows:*

    - For any Trajectories version earlier than 2.0.0,
      this returns "0".
    - If your Trajectories version is at least 2.0.0 or above,
      this returns the specific version major value correctly.

    For cases where you need to check for a known minimum Trajectories
    version, it is probably better to use the specific boolean suffix
    for that version (for example, :attr:`TRAddon:ISVERTWO`, or
    :attr:`TRAddon:ISVERTWOTWO` etc.)

.. attribute:: TRAddon:GETVERSIONMINOR

    :type: :struct:`Scalar`
    :access: Get

    **Only gives the correct answer for Trajectries version >= 2.2.0**

    *For earlier versions, it gives a hardcoded fixed answer, as follows:*

    - For any Trajectories version below 2.2.0, this returns
      "0" regardless of the precise version number within that range.
    - If your Trajectories version is at least 2.2.0 or above,
      this returns the specific version minor value correctly.

    For cases where you need to check for a known minimum Trajectories
    version, it is probably better to use the specific boolean suffix
    for that version (for example, :attr:`TRAddon:ISVERTWO`, or
    :attr:`TRAddon:ISVERTWOTWO` etc.)

.. attribute:: TRAddon:GETVERSIONPATCH

    :type: :struct:`Scalar`
    :access: Get

    **Only gives the correct answer for Trajectries version >= 2.2.0**

    *For earlier versions, it gives a hardcoded fixed answer, as follows:*

    - For any Trajectories version below 2.2.0, this returns
      "0" regardless of the precise version number within that range.
    - If your Trajectories version is at least 2.2.0 or above,
      this returns the specific version patch value correctly.

    For cases where you need to check for a known minimum Trajectories
    version, it is probably better to use the specific boolean suffix
    for that version (for example, :attr:`TRAddon:ISVERTWO`, or
    :attr:`TRAddon:ISVERTWOTWO` etc.)

.. attribute:: TRAddon:ISVERTWO

    :type: :struct:`Boolean`
    :access: Get

    True if the Trajectories mod is at least version 2.0.0 or above.

.. attribute:: TRAddon:ISVERTWOTWO

    :type: :struct:`Boolean`
    :access: Get

    True if the Trajectories mod is at least version 2.2.0 or above.

.. attribute:: TRAddon:ISVERTWOFOUR

    :type: :struct:`Boolean`
    :access: Get

    True if the Trajectories mod is at least version 2.4.0 or above.

.. attribute:: TRAddon:HASIMPACT

    :type: :struct:`Boolean`
    :access: Get

    True if Trajectories has calculated an impact position for the current :struct:`Vessel`. You should always check this before using :attr:`impactPos<TRAddon:IMPACTPOS>`, :attr:`plannedVect<TRAddon:plannedVec>`, :meth:`setTarget<TRAddon:setTarget>`, or :attr:`correctedVect<TRAddon:correctedVec>` to avoid exceptions.

.. attribute:: TRAddon:IMPACTPOS

    :type: :struct:`GeoCoordinates`
    :access: Get

    Estimated impact position.

.. attribute:: TRAddon:TIMETILLIMPACT

    :type: :struct:`Scalar`
    :access: Get

    **Did Not Exist in Trajectories before 2.2.0!**

    *If :attr:`TRAddons:ISVERTWOTWO` is false, using this suffix will cause
    a runtime error.*

    Gives you Trajectories prediction of how many seconds until impact
    on ground or water.

.. method:: TRAddon:RESETDESCENTPROFILE(AoA)

    :parameter AoA: :struct:`Scalar`
    :return: None

    **Did Not Exist in Trajectories before 2.4.0!**

    *If :attr:`TRAddons:ISVERTWOFOUR` is false, using this suffix will cause
    a runtime error.*

    Resets all the Trajectories descent profile nodes to the passed AoA value (in Degrees),
    also sets Retrograde if AoA value is greater than 90 degrees (PI/2 radians)
    otherwise sets to Prograde.

.. attribute:: TRAddon:DESCENTANGLES

    :type: :struct:`List<Scalar>`
    :access: Get/Set

    **Did Not Exist in Trajectories before 2.4.0!**

    *If :attr:`TRAddons:ISVERTWOFOUR` is false, using this suffix will cause
    a runtime error.*

    Returns or sets all the Trajectories descent profile AoA values (in Degrees),
    also sets a node to Retrograde if it's passed AoA is greater than 90 degrees
    (PI/2 radians)
    Note. also use with :attr:`TRAddons:DESCENTGRADES` to set a nodes grade
    if needed and passing AoA values as displayed in the gui with max 90 degrees
    (PI/2 radians).

    List<Scalar>(atmospheric entry, high altitude, low altitude, final approach).

.. attribute:: TRAddon:DESCENTGRADES

    :type: :struct:`List<Boolean>`
    :access: Get/Set

    **Did Not Exist in Trajectories before 2.4.0!**

    *If :attr:`TRAddons:ISVERTWOFOUR` is false, using this suffix will cause
    a runtime error.*

    Returns or sets all the Trajectories descent profile grades,
    True = Retrograde, False = Prograde.

    List<Boolean>(atmospheric entry, high altitude, low altitude, final approach).

.. attribute:: TRAddon:DESCENTMODES

    :type: :struct:`List<Boolean>`
    :access: Get/Set

    **Did Not Exist in Trajectories before 2.4.0!**

    *If :attr:`TRAddons:ISVERTWOFOUR` is false, using this suffix will cause
    a runtime error.*

    Returns or sets all the Trajectories descent profile modes,
    True = AoA, False = Horizon.

    List<Boolean>(atmospheric entry, high altitude, low altitude, final approach).

.. attribute:: TRAddon:PROGRADE

    :type: :struct:`Boolean`
    :access: Get/Set

    **Did Not Exist in Trajectories before 2.2.0!**

    *If :attr:`TRAddons:ISVERTWOTWO` is false, using this suffix will cause
    a runtime error.*

    For Trajectories 2.2.0 True if all the descent profile AoA values are 0.
    For Trajectories 2.4.0 True if all the descent profile nodes are 'prograde'

    You can set this to have the same effect as clicking on prograde mode
    in the trajectories GUI. Setting this value to true causes
    :attr:`TRAddon:RETROGRADE` to become false. (They cannot both be
    true at the same time.)

    Setting this causes all Trajectories descent profile nodes
    to be set to 'prograde' mode if True or 'retrograde' mode if False.
    Also resets all AoA values to 0.

.. attribute:: TRAddon:RETROGRADE

    :type: :struct:`Boolean`
    :access: Get/Set

    **Did Not Exist in Trajectories before 2.2.0!**

    *If :attr:`TRAddons:ISVERTWOTWO` is false, using this suffix will cause
    a runtime error.*

    For Trajectories 2.2.0 True if all the descent profile AoA values are 180.
    For Trajectories 2.4.0 True if all the descent profile nodes are 'retrograde'

    You can set this to have the same effect as clicking on retrograde mode
    in the trajectories GUI. Setting this value to true causes
    :attr:`TRAddon:PROGRADE` to become false. (They cannot both be
    true at the same time.)

    Setting this causes all Trajectories descent profile nodes
    to be set to 'retrograde' mode if True or 'prograde' mode if False.
    Also resets all AoA values to 0.

.. attribute:: TRAddon:PLANNEDVEC

    :type: :struct:`Vector`
    :access: Get

    Vector pointing the direction your vessel should face to follow the
    predicted trajectory, based on the angle of attack selected in the
    Trajectories descent profile.

.. attribute:: TRAddon:PLANNEDVECTOR

    :type: :struct:`Vector`
    :access: Get

    Alias for :attr:`PLANNEDVEC<TRAddon:PLANNEDVEC>`

.. method:: TRAddon:SETTARGET(position)

    :parameter position: :struct:`GeoCoordinates`
    :return: None

    Sets the Trajectories target landing position to the given position.

.. attribute:: TRAddon:HASTARGET

    :type: :struct:`Boolean`
    :access: Get

    **Did Not Exist in Trajectories before 2.0.0!**

    *If :attr:`TRAddons:ISVERTWO` is false, using this suffix will cause
    a runtime error.*

    The Trajectories Addon can be given a target position.
    This is true if such a position is set, or false if it is not.

.. attribute:: TRAddon:GETTARGET

    :type: :struct:`GeoCoordinates`
    :access: Get

    **Did Not Exist in Trajectories before 2.4.0!**

    *If :attr:`TRAddons:ISVERTWOFOUR` is false, using this suffix will cause
    a runtime error.*

    Returns the Trajectories target position if one is set.

.. method:: TRAddon:CLEARTARGET()

    :parameter None
    :return: None

    **Did Not Exist in Trajectories before 2.4.0!**

    *If :attr:`TRAddons:ISVERTWOFOUR` is false, using this suffix will cause
    a runtime error.*

    Clears the Trajectories target position.

.. attribute:: TRAddon:CORRECTEDVEC

    :type: :struct:`Vector`
    :access: Get

    A vector that applies an offset to :attr:`PLANNEDVEC<TRAddon:PLANNEDVEC>`
    intended to correct the predicted trajectory to impact at the selected
    target position.  This vector does not use any aerodynamic prediction and
    is a very simplistic representation.  It is also just a unit vector.  It
    contains no magnitude information about how far off the selected target is
    from the predicted impact - just the way the offset points. Accuracy is
    not guaranteed, but it should at least help determine if you need to
    pitch the nose up or down.

.. attribute:: TRAddon:CORRECTEDVECTOR

    :type: :struct:`Vector`
    :access: Get

    Alias for :attr:`CORRECTEDVEC<TRAddon:CORRECTEDVEC>`
