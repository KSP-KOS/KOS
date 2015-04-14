.. _remotetech:

RemoteTech
==========

**Warning! This documentation is incomplete!**

Access structure RTAddon via `ADDONS:RT`.

.. structure:: RTAddon

    ===================================== ========================= =============
     Suffix                                Type                      Description
    ===================================== ========================= =============
     :attr:`AVAILABLE`                     bool(readonly)            True if RT is installed and RT integration enabled.
     :meth:`DELAY(vessel)`                 double                    Get shortest possible delay to given :struct:`Vessel`
     :meth:`KSCDELAY(vessel)`              double                    Get delay from KSC to given :struct:`Vessel`
     :meth:`HASCONNECTION(vessel)`         bool                      True if given :struct:`Vessel` has any connection
     :meth:`HASKSCCONNECTION(vessel)`      bool                      True if given :struct:`Vessel` has connection to KSC
    ===================================== ========================= =============


     
.. attribute:: RTADDON:AVAILABLE

    :type: bool 
    :access: Get only

    True if RT is installed and RT integration enabled.

.. method:: RTAddon:DELAY(vessel)

    :parameter vessel: :struct:`Vessel`
    :return: (double) seconds 
    
    Returns shortest possible delay for `vessel` (Will be less than KSC delay if you have a local command post).

.. method:: RTAddon:KSCDELAY(vessel)

    :parameter vessel: :struct:`Vessel`
    :return: (double) seconds 
    
    Returns delay in seconds from KSC to `vessel`.

.. method:: RTAddon:HASCONNECTION(vessel)

    :parameter vessel: :struct:`Vessel`
    :return: bool 
    
    Returns True if `vessel` has any connection (including to local command posts).

.. method:: RTAddon:HASKSCCONNECTION(vessel)

    :parameter vessel: :struct:`Vessel`
    :return: bool 
    
    Returns True if `vessel` has connection to KSC.