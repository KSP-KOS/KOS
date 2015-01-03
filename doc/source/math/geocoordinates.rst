.. _geocoordinates:

Geographic Coordinates
======================

.. contents:: Contents
    :local:
    :depth: 1

The ``GeoCoordinates`` object (also ``LATLNG``) represents a latitude and longitude pair, which is a location on the surface of a :ref:`Body <body>`.

Creation
--------

.. function:: LATLNG(lat,lng)

    :parameter lat: (deg) Latitude
    :parameter lng: (deg) Longitude
    :return: :struct:`GeoCoordinates`

    This function creates a :struct:`GeoCoordiantes` object with the given latitude and longitude. Once created it can't be changed. The :attr:`GeoCoordinates:LAT` and :attr:`GeoCoordinates:LNG` suffixes are get-only and cannot be set. To switch to a new location, make a new call to :func:`LATLNG()`.

    It is also possible to obtain a :struct:`GeoCoordiates` from some suffixes of some other structures. For example::

        SET spot to SHIP:GEOPOSITION.

Structure
---------

.. structure:: GeoCoordinates
        
    .. list-table::
        :widths: 2 1 4
        :header-rows: 1

        * - Suffix
          - Type
	  - Args
          - Description

        * - :attr:`LAT`
          - scalar (deg)
	  - none
          - Latitude
        * - :attr:`LNG`
          - scalar (deg)
	  - none
          - Longitude
        * - :attr:`DISTANCE`
          - scalar (m)
	  - none
          - distance from :ref:`CPU Vessel <cpu vessel>`
        * - :attr:`TERRAINHEIGHT`
          - scalar (m)
	  - none
          - above or below sea level
        * - :attr:`HEADING`
          - scalar (deg)
	  - none
          - *absolute* heading from :ref:`CPU Vessel <cpu vessel>`
        * - :attr:`BEARING`
          - scalar (deg)
	  - none
          - *relative* direction from :ref:`CPU Vessel <cpu vessel>`
        * - :attr:`POSITION`
          - `Vector` (3D Ship-Raw coords)
	  - none
          - Position of the surface point.
        * - :attr:`ALTITUDEPOSITION`
          - `Vector` (3D Ship-Raw coords)
	  - scalar (altitude above sea level)
          - Position of a point above (or below) the surface point, by giving the altitude number.

.. attribute:: GeoCoordinates:LAT

    The latitude of this position on the surface.

.. attribute:: GeoCoordinates:LNG

    The longitude of this position on the surface.

.. attribute:: GeoCoordinates:DISTANCE

    Distance from the :ref:`CPU_Vessel <cpu vessel>` to this point on the surface.

.. attribute:: GeoCoordinates:TERRAINHEIGHT

    Distance of the terrain above "sea level" at this geographical position. Negative numbers are below "sea level."

.. attribute:: GeoCoordinates:HEADING

    The *absolute* compass direction from the :ref:`CPU_Vessel <cpu vessel>` to this point on the surface.

.. attribute:: GeoCoordinates:BEARING

    The *relative* compass direction from the :ref:`CPU_Vessel <cpu vessel>` to this point on the surface. For example, if the vessel is heading at compass heading 45, and the geo-coordinates location is at heading 30, then :attr:`GeoCoordinates:BEARING` will return -15.

.. attribute:: GeoCoordinates:POSITION

    The ship-raw 3D position on the surface of the body, relative to the current ship's Center of mass.

.. attribute:: GeoCoordinates:ALTITUDEPOSITION (altitude)

    The ship-raw 3D position above or below the surface of the body, relative to the current ship's Center of mass.  You pass in an altitude number for the altitude above "sea" level of the desired location.

Examples Usage
--------------

::

    SET spot TO LATLNG(10, 20).     // Initialize point at latitude 10,
                                    // longitude 20
                                    
    PRINT spot:LAT.                 // Print 10
    PRINT spot:LNG.                 // Print 20
    
    PRINT spot:DISTANCE.            // Print distance from vessel to x
                                    // (same altitude is presumed)
    PRINT spot:HEADING.             // Print the heading to the point
    PRINT spot:BEARING.             // Print the heading to the point
                                    // relative to vessel heading
                                    
    SET spot TO SHIP:GEOPOSITION.   // Make spot into a location on the
                                    // surface directly underneath the
                                    // current ship
                                    
    SET spot TO LATLNG(spot:LAT,spot:LNG+5). // Make spot into a new
                                             // location 5  degrees east
                                             // of the old one

    // Point nose of ship at a spot 100,000 meters altitude above a
    // particular known latitude of 50 east, 20.2 north:
    LOCK STEERING TO LATLNG(50,20.2):ALTITUDEPOSITION(100000).

    // A nice complex example:
    // -------------------------
    // Drawing an debug arrow in 3D space at the spot where the Geocoordinate 'spot' is:
    // It starts at a position 100m above the ground altitude and is aimed down at
    // the spot on the ground:
    SET VD TO VECDRAWARGS(
                  spot:ALTITUDEPOSITION(spot:TERRAINHEIGHT+100),
                  spot:POSITION - spot:ALTITUDEPOSITION(TERRAINHEIGHT+100),
                  red, "THIS IS THE SPOT", 1, true).

    PRINT "THESE TWO NUMBERS SHOULD BE THE SAME:".
    PRINT (SHIP:ALTITIUDE - SHIP:GEOPOSITION:TERRAINHEIGHT).
    PRINT ALT:RADAR.

