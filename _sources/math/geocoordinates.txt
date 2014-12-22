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
          - Description

        * - :attr:`LAT`
          - scalar (deg)
          - Latitude
        * - :attr:`LNG`
          - scalar (deg)
          - Longitude
        * - :attr:`DISTANCE`
          - scalar (m)
          - distance from :ref:`CPU Vessel <cpu vessel>`
        * - :attr:`TERRAINHEIGHT`
          - scalar (m)
          - above or below sea level
        * - :attr:`HEADING`
          - scalar (deg)
          - *absolute* heading from :ref:`CPU Vessel <cpu vessel>`
        * - :attr:`BEARING`
          - scalar (deg)
          - *relative* direction from :ref:`CPU Vessel <cpu vessel>`

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

    PRINT "THESE TWO NUMBERS SHOULD BE THE SAME:".
    PRINT (SHIP:ALTITIUDE - SHIP:GEOPOSITION:TERRAINHEIGHT).
    PRINT ALT:RADAR.

