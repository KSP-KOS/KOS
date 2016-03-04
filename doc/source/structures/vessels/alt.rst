.. _alt:

ALT
===

ALT is a special object that exists just to help you get the
altitudes of interest for a vessel future.  It is grandfathered
in for the sake of backward compatibility, but this information
is also available on the Vessel structure as well, which is
the better new way to do it:


.. structure:: ALT

    .. list-table::
        :header-rows: 1
        :widths: 2 1 4

        * - Suffix
          - Type
          - Description

        * - :attr:`APOAPSIS`
          - :ref:`scalar <scalar>`, meters
          - altitude in meters of SHIP's apoapsis.  Same as SHIP:APOAPSIS.

        * - :attr:`PERIAPSIS`
          - :ref:`scalar <scalar>`, meters
          - altitude in meters of SHIP's periapsis.  Same as SHIP:PERIAPSIS.

        * - :attr:`RADAR`
          - :ref:`scalar <scalar>`, meters
          - Altitude of SHIP above the ground terrain, rather than above sea level.
		  
