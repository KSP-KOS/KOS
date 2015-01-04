.. _comm range:

Communication Range For Remote Updates
======================================

.. warning::
    .. deprecated:: 0.12.2
        The stock game and the future of this feature is still fuzzy, and will likely be related to RemoteTech2 in some way. If you don't use RemoteTech2, then there will be no check for range anymore as of version 0.12.2. The following shortcuts are now used.

        ``COMMRANGE``
            always returns a Very Big Number, and

        ``INRANGE``
            always returns true.

        If you are using a version of ``kOS >= 0.12.2``, then most of what this page says won't be true.

Communication Range (Deprecated)
--------------------------------

Kerbin must be within CommRange of the vessel in order for the following operations to work:

-  COPY a file from a local volume to Archive
-  COPY a file from Archive to a local volume.
-  LIST the files on the Archive.

You can always find out whether or not the vessel is within transmission range of Kerbin using the following:

-  PRINT COMMRANGE. // Shows a number, in meters.
-  PRINT INCOMMRANGE. // Shows a boolean true/false, for whether or not
   you are in range.

A future plan is to implement a feature that when the RemoteTech mod is installed, kOS will query RemoteTech to ask whether or not the vessel is in communications range, and allow RemoteTech to override the calculation described below.

The system described below is meant to be used only when RemoteTech2 is not installed.

How to calculate communications range
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

Communications range is decided by how many antennae are installed, and of what type. There are three categories of antenna:

-  longAntenna: (The Communotron 16 'stick')
-  mediumDishAntenna: (The Comms DTS-M1)
-  commDish: (The Communotron 88-88)

The number of meters of range is decided by this formula:

max range = ( (100,000 + L\ \*1,000,000) \*\ 100^M \* 200^D ) meters Where: \* L = number of longAntenna's on the vessel. \* M = number of mediumDishAntenna's on the vessel. \* D = number of commDish's on the vessel.


+----------------+----------------+-----------------------------------+
|No. of Antennae |                |                                   |
+----+------+----+Max range       |Context for comparison             |
|    |Medium|    |                |                                   |
|Long|Dish  |Dish|                |                                   |
+====+======+====+================+===================================+
|0   |0     |0   |100 km          |                                   |
+----+------+----+----------------+-----------------------------------+
|1   |0     |0   |1100 km         |                                   |
+----+------+----+----------------+-----------------------------------+
|2   |0     |0   |2100 km         |                                   |
+----+------+----+----------------+-----------------------------------+
|0   |1     |0   |10,000 km       |                                   |
+----+------+----+----------------+-----------------------------------+
|0   |0     |1   |20,000 km       |Mun-to-Kerbin = 12,000 km          |
+----+------+----+----------------+-----------------------------------+
|1   |0     |1   |220,000 km      |                                   |
+----+------+----+----------------+-----------------------------------+
|2   |0     |1   |420,000 km      |                                   |
+----+------+----+----------------+-----------------------------------+
|0   |2     |0   |1,000,000 km    |                                   |
+----+------+----+----------------+-----------------------------------+
|0   |0     |2   |4,000,000 km    |Almost the closest distance        |
|    |      |    |                |Moho gets to the Sun               |
+----+------+----+----------------+-----------------------------------+
|1   |1     |1   |22,000,000 km   |                                   |
+----+------+----+----------------+-----------------------------------+
|1   |0     |2   |44,000,000 km   |A bit bigger than the 'diameter'   |
|    |      |    |                |of Duna's orbit of the Sun         |
+----+------+----+----------------+-----------------------------------+
|2   |0     |2   |84,000,000 km   |A bit bigger than the biggest      |
|    |      |    |                |distance between Jool and the Sun  |
+----+------+----+----------------+-----------------------------------+
|3   |0     |2   |124,000,000 km  |A bit bigger than the biggest      |
|    |      |    |                |distance between Eeloo and the Sun |
+----+------+----+----------------+-----------------------------------+
|1   |2     |1   |2,200,000,000 km|                                   |
+----+------+----+----------------+-----------------------------------+
|1   |0     |3   |8,800,000,000 km|Larger than any distance in the    |
|    |      |    |                |Kerbal system                      |
+----+------+----+----------------+-----------------------------------+
