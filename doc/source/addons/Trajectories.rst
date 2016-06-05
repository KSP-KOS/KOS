.. _trajectories:

Trajectories
==================

- Download: https://github.com/neuoy/KSPTrajectories/releases
- Forum thread: http://forum.kerbalspaceprogram.com/index.php?/topic/94368-trajectories

Trajectories is a mod that displays trajectory predictions, accounting for atmospheric drag, lift, etc.. See the forum thread for more details.

This addon is not associated with and not supported by the creator of Trajectories.

Because Trajectories does not have an official API this addon accesses it via Reflection. This means that future Trajectories updates may break this addon, in which case `ADDONS:TR:AVAILABLE` will return false.

Compatible with Trajectories 1.4.6.

**Important notes**

Trajectories only predicts the trajectory of the "Active Vessel," which is the vessel with the camera focused on it. `IMPACTPOS`, `PLANNEDVECT`, `SETTARGET`, and `CORRECTEDVECT` will throw exceptions if you try to call them from an inactive vessel or if Trajectories has not calculated an impact position. You should always check if `HASIMPACT` is true before accessing these suffixes.

For example:

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

Access structure TRAddon via `ADDONS:TR`.

.. structure:: TRAddon

    ===================================== ========================= =============
     Suffix                                Type                      Description
    ===================================== ========================= =============
     :attr:`AVAILABLE`                     bool(readonly)            True if a compatible Trajectories version is installed.
     :attr:`HASIMPACT`                     bool(readonly)            True if Trajectories has calculated an impact position for the current vessel.
     :attr:`IMPACTPOS`                     GeoCoordinates(readonly)  Returns a `LATLNG` with the predicted impact position.
     :attr:`PLANNEDVECT`                   Vector(readonly)          Direction to point to follow predicted trajectory.
     :attr:`SETTARGET(position)`           void                      Set Trajectories target.
     :attr:`CORRECTEDVECT`                 Vector(readonly)          A hint about the direction you should go to reach the target.
    ===================================== ========================= =============



.. attribute:: TRAddon:AVAILABLE

    :type: bool
    :access: Get

    True if a compatible Trajectories version is installed.

.. attribute:: TRAddon:HASIMPACT

    :type: bool
    :access: Get

    True if Trajectories has calculated an impact position for the current `vessel`. You should always check this before using `impactPos`, `plannedVect`, `setTarget`, or `correctedVect` to avoid exceptions.

.. attribute:: TRAddon:IMPACTPOS

    :type: GeoCoordinates
    :access: Get

    Estimated impact position.

.. attribute:: TRAddon:PLANNEDVECT

    :type: Vector
    :access: Get

    Direction to point to follow the currently predicted trajectory.

.. attribute:: TRAddon:SETTARGET(position)

    :parameter position: (GeoPosition) Position to set Trajectories target to.
    :return: void

    Direction to point to follow predicted trajectory.

.. attribute:: TRAddon:CORRECTEDVECT

    :type: Vector
    :access: Get

    A hint about the direction you should go to adjust your trajectory to reach the target. This is not necessarily where you need to point, what's important is the direction between this and `PLANNEDVECT`, and the angle between them indicates how far you are from the perfect trajectory.