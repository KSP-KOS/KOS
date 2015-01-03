Waypoints
=========

.. contents:: Contents
    :local:
    :depth: 1

Waypoints are the location markers you can see on the map view showing
you where contracts are targeted for.  With this strucure, you can obtain
coordinate data for the locations of these waypoints.

Creation
--------

.. function:: WAYPOINT(name)

    :parameter name: (string) Name of the waypoint as it appears on the map or in the contract description
    :return: :struct:`Waypoint`

    This creates a new Waypoint from a name of a waypoint you read from the contract paramters.  Note that this only works on contracts you've accpted.  Waypoints for proposed contracts haven't accepted yet  do not actually work in kOS.

        SET spot TO WAYPOINT("herman's folly beta").

    The name match is case-insensitive.

.. function:: ALLWAYPOINTS()
    :return: :struct: `List` of `Waypoint`s

    This creates a `List` of `Waypoint` structures for all accepted contracts.  Waypoints for proposed contracts you haven't accepted yet do not appear in the list.

Structure
---------

.. structure:: Waypoint

    .. list-table:: **Members**
        :widths: 4 2 1 1
        :header-rows: 1
        
        * - Suffix
          - Type
          - Get
          - Set
          
        * - :attr:`NAME`
          - string
          - yes
          - no
        * - :attr:`BODY`
          - `BodyTarget`
          - yes
          - no
        * - :attr:`GEOPOSITION`
          - `GeoCoordinates`
          - yes
          - no
        * - :attr:`ALTITUDE`
          - scalar
          - yes
          - no
        * - :attr:`AGL`
          - scalar
          - yes
          - no
        * - :attr:`NEARSURFACE`
          - boolean
          - yes
          - no
        * - :attr:`GROUNDED`
          - boolean
          - yes
          - no


.. attribute:: Waypoint:NAME

    :type: string
    :access: Get

    Name of waypoint as it appears on the map and contract

.. attribute:: Waypoint:BODY

    :type: `BodyTarget`
    :access: Get

    Celestial body the waypoint is attached to


.. attribute:: Waypoint:GEOPOSITION

    :type: GeoCoordinates
    :access: Get

    The LATLNG of this waypoint

.. attribute:: Waypoint:ALTITUDE

    :type: scalar
    :access: Get

    Altitude of waypoint **above "sea" level**.  Warning, this a point somewhere in the midst of the contract altitude range, not the edge of the altitude range.  It corresponds towhere the marker tip hovers on the map, which is not actually at the very edge of the contract condition's range.  It represents a typical midling location inside the contract's altitude range.


.. attribute:: Waypoint:AGL

    :type: scalar
    :access: Get

    Altitude of waypoint **above ground**.  Warning, this a point somewhere in the midst of the contract altitude range, not the edge of the altitude range.  It corresponds to where the marker tip hovers on the map, which is not actually at the very edge of the contract condition's range.  It represents a typical midling location inside the contract's altitude range.


.. attribute:: Waypoint:NEARSURFACE

    :type: boolean
    :access: Get

    True if waypoint is a point near or on the body rather than high in orbit.


.. attribute:: Waypoint:GROUNDED

    :type: boolean
    :access: Get

    True if waypoint is actually glued to the ground.

