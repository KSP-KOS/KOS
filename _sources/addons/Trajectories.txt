.. _trajectories:

Trajectories
==================

- Download: https://github.com/neuoy/KSPTrajectories/releases
- Forum thread: http://forum.kerbalspaceprogram.com/index.php?/topic/94368-trajectories

Trajectories is a mod that displays trajectory predictions, accounting for atmospheric drag, lift, etc.. See the forum thread for more details.

This addon is not associated with and not supported by the creator of Trajectories.

The Trajectories API is accessed throgh C# reflection, and is designed for the
current version. This means that future Trajectories updates may break this
addon, in which case ``ADDONS:TR:AVAILABLE`` will return false.  It is also
possible for future versions of Trjectories to remain fully compatible.

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

Access structure TRAddon via ``ADDONS:TR``.

.. structure:: TRAddon

    ============================= ===================================== =============
     Suffix                        Type                                  Description
    ============================= ===================================== =============
     :attr:`AVAILABLE`             :struct:`Boolean` (readonly)          True if a compatible Trajectories version is installed.
     :attr:`HASIMPACT`             :struct:`Boolean` (readonly)          True if Trajectories has calculated an impact position for the current vessel.
     :attr:`IMPACTPOS`             :struct:`GeoCoordinates` (readonly)   Returns a :struct:`GeoCoordinates` with the predicted impact position.
     :attr:`PLANNEDVEC`            :struct:`Vector` (readonly)           Vector at which to point to follow predicted trajectory.
     :attr:`PLANNEDVECTOR`         :struct:`Vector` (readonly)           Alias for :attr:`PLANNEDVEC`
     :meth:`SETTARGET(position)`   None                                 Set Trajectories target.
     :attr:`CORRECTEDVEC`          :struct:`Vector` (readonly)           Offset plus :attr:`PLANNEDVEC` to correct path for targeted impact.
     :attr:`CORRECTEDVECTOR`       :struct:`Vector` (readonly)           Alias for :attr:`CORRECTEDVEC`
    ============================= ===================================== =============



.. attribute:: TRAddon:AVAILABLE

    :type: :struct:`Boolean`
    :access: Get

    True if a compatible Trajectories version is installed.

.. attribute:: TRAddon:HASIMPACT

    :type: :struct:`Boolean`
    :access: Get

    True if Trajectories has calculated an impact position for the current :struct:`Vessel`. You should always check this before using :attr:`impactPos<TRAddon:IMPACTPOS>`, :attr:`plannedVect<TRAddon:plannedVec>`, :meth:`setTarget<TRAddon:setTarget>`, or :attr:`correctedVect<TRAddon:correctedVec>` to avoid exceptions.

.. attribute:: TRAddon:IMPACTPOS

    :type: :struct:`GeoCoordinates`
    :access: Get

    Estimated impact position.

.. attribute:: TRAddon:PLANNEDVEC

    :type: :struct:`Vector`
    :access: Get

    Vector pointing the direction your vessel should face to follow the
    predicted trajectory, based on the angle of attack selected in the
    Trajectories user interface.

.. attribute:: TRAddon:PLANNEDVECTOR

    :type: :struct:`Vector`
    :access: Get

    Alias for :attr:`PLANNEDVEC<TRAddon:PLANNEDVEC>`

.. method:: TRAddon:SETTARGET(position)

    :parameter position: :struct:`GeoCoordinates`
    :return: None

    Sets the Trajectories target landing position to the given position.

.. attribute:: TRAddon:CORRECTEDVEC

    :type: :struct:`Vector`
    :access: Get

    A vector that applies an offset to :attr:`PLANNEDVEC<TRAddon:PLANNEDVEC>`
    intended to correct the predicted trajectory to impact at the selected
    target position.  This vector does not use any aerodynamic prediction and
    is a very simplistic representation.  Accuracy is not guaranteed, but it
    should at least help determine if you need to pitch the nose up or down.

.. attribute:: TRAddon:CORRECTEDVECTOR

    :type: :struct:`Vector`
    :access: Get

    Alias for :attr:`CORRECTEDVEC<TRAddon:CORRECTEDVEC>`
