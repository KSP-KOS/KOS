.. _trajectories:

Trajectories
==================

- Download: https://github.com/neuoy/KSPTrajectories/releases
- Forum thread, including full instructions: http://forum.kerbalspaceprogram.com/index.php?/topic/94368-trajectories

Trajectories is a mod that displays trajectory predictions, accounting for atmospheric drag, lift, etc.. See the forum thread for more details.

This addon is not associated with and not supported by the creator of Trajectories.

Because Trajectories does not have an official API this addon accesses it via Reflection. This means that future Trajectories updates may break this addon, in which case `ADDONS:TR:AVAILABLE` will return false.

Compatible with Trajectories 1.4.6.

**Important notes**

Trajectories only predicts the trajectory of the "Active Vessel," which is the vessel with the camera focused on it. `ADDONS:TR:IMPACTPOS` will throw an exception if you try to call it from an inactive vessel. You should always check `HASIMPACT` before accessing `IMPACTPOS`.

    if ADDONS:TR:AVAILABLE {
        if ADDONS:TR:HASIMPACT {
            PRINT ADDONS:TR:IMPACTPOS.
        } else {
            PRINT "Impact position is not available".
        }
    } else {
        PRINT "Trajectories is not available.".
    }
    
Trajectories does it's calculation based on the vessel's current orientation. Any tiny change in orientation will change the prediction.

Accuracy is not guaranteed.

See this repository for an example of this addon being used to land a rocket on the launch pad: https://github.com/CalebJ2/kOS-landing-script

Access structure TRAddon via `ADDONS:TR`.

.. structure:: TRAddon

    ===================================== ========================= =============
     Suffix                                Type                      Description
    ===================================== ========================= =============
     :attr:`AVAILABLE`                     bool(readonly)            True if a compatible Trajectories version is installed.
     :attr:`HASIMPACT`                     bool(readonly)            True if `IMPACTPOS` is available.
     :attr:`IMPACTPOS`                     GeoCoordinates(readonly)  Returns a 'LATLNG' with 
    ===================================== ========================= =============



.. attribute:: TRAddon:AVAILABLE

    :type: bool
    :access: Get only

    True if a compatible Trajectories version is installed.

.. attribute:: TRAddon:HASIMPACT

    :type: bool
    :access: Get only

    True if it is safe to call `ADDONS:TR:IMPACTPOS`, meaning that Trajectories has calculated an impact position for the current `vessel`.

.. attribute:: TRAddon:IMPACTPOS

    :type: GeoCoordinates
    :access: Get only

    Estimated impact position.