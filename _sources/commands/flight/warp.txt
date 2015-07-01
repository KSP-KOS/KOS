.. _warp:

Time Warping
============


.. global:: WARP

    You may use the WARPTO(time) function to automatically warp to the specified time (given in seconds game universal time).  If you need more precise control, you can use the other options below.::

        WARPTO(TIME:SECONDS + 60 * 10). // warp to a time 10 minutes in the future

    The :global:`WARP` global variable can be set to change the game warp to a value between 0 and 7 (for rails warp) or 0 to 3 (for physics warp)::

        SET WARP TO 5. // Sets warp to 1000x
        SET WARP TO 0. // Sets warp to 1x (real time)

    You may also choose which warp mode you wish the WARP command
    to invoke- physics warp (capped at 4x) or rails warp::

        SET WARPMODE TO "PHYSICS".
        SET WARPMODE TO "RAILS".

    WARPMODE can be set to a string to choose the warp.  It must be
    one of the two strings mentioned above.

    The difference is the same as that experienced in the game between
    physics warp and 'rails' warp (sometimes called 'time warp') although
    that term is confusingly ambiguous.

.. table:: RAILS WARP MODES

    ==== =======
    MODE MEANING
    ==== =======
    0    1x
    1    5x
    2    10x
    3    50x
    4    100x
    5    1000x
    6    10000x
    7    100000x
    ==== =======

.. table::  PHYSICS WARP MODES

    ==== =======
    MODE MEANING
    ==== =======
    0    1x
    1    2x
    2    3x
    3    4x
    ==== =======

