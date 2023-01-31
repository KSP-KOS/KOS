.. _bounds:

BOUNDS
======

kOS can return information about the "bounding box" around either a whole
vessel, or a part on a vessel.  When it does so, the structure it uses
to tell your script about it is called a "Bounds".

You can obtain a ``Bounds`` one of two ways:

* By calling :attr:`Vessel:Bounds`
* By calling :attr:`Part:Bounds`

The bounds for a whole vessel and the bounds for one part work almost
exactly the same way.  Places where they differ will be explained below
as they arise.

The reason this could be useful to a kerboscript program is that it lets
you get a partial "picture" of the size of a ship, or a part, or check
more precisely when landing "*what is the altitude of my landing leg's foot
above the ground?*" instead of "what is the altitude of my ship in general
above the ground?"

Before you use a Bounds be certain you have read the section
below on this page about :ref:`why you should re-use bounds
if you can <reuse_bounds>` (instead of re-getting them again and again
with the :BOUNDS suffix call.)

Quick start 1 - Radar altitude: the most useful thing
-----------------------------------------------------

The explanation of the "Bounds" structure might get a bit involved below.

For most players, probably the only thing you really really care deeply
about is the distance of the landing legs to the ground.

And you get that with the suffix :attr:`BOTTOMALTRADAR`.  Here's an example::

    // How to get the distance of the bottom corner of the vessel's 
    // bounding box to the ground.
    // As an example, try running this while you manually land,
    // so you can see it working as you fly the lander:

    local bounds_box is ship:bounds. // IMPORTANT! do this up front, not in the loop.
    until terminal:input:haschar {
      clearscreen.
      print "PRESS ANY KEY TO QUIT".
      print "ALT:RADAR of ship bounding box's bottom corner is:".
      print round(bounds_box:BOTTOMALTRADAR, 1).
      wait 0.
    }
    terminal:input:getchar().

It may be more useful to choose a specific landing leg part and test with it
instead of the whole vessel's box, by doing something akin to this at the
top of the above script instead::

    // DELETE THIS LINE:
    //    local bounds_box is ship:bounds. // IMPORTANT! do this up front, not in the loop.
    //
    // REPLACE WITH THESE LINES:
    // Assumes you gave the nametag "sensing leg" to one of your landing legs:
    //
    set leg_part to ship:partstagged("sensing leg")[0].
    local bounds_box is leg_part:bounds.

    // (rest of the example is the same as above).

Quick start 2 - Finding the corner you care about
-------------------------------------------------

The most universally helpful suffix of a ``Bounds``, if you want
to just learn one suffix and ignore the others, is probably the
:meth:`Bounds:FURTHESTCORNER(ray)` suffix.

It lets you say "I just want to know where is the corner of
this bounding box that is the furthest 'that way'".

For example, this gives you a position which is the bottom-most
vertex of your ship's bounding box::

    set B to ship:BOUNDS.

    // The furthest corner of the box in the downward (negative up) direction:
    set bottom to B:FURTHESTCORNER( - up:vector ).

With this one suffix you can answer a lot of the relevant "ship size"
questions like "Am i getting too close to that ship?, well, lets find
out which vertex of its bounding box is closest to me...".

Quick Start 3 - A complex visual example drawing the bounds
-----------------------------------------------------------

There is an example program in the tutorials section that
brings this all together to show you the bounds boxes
visually on screen:

:ref:`Click here for an example program that displays bounds <display_bounds>`

Trying that first (without necessarily understaand it right away)
will help give you a visual guide to what is happening here.

A Part:BOUNDS or Vessel:BOUNDS will move and rotate with the object
-------------------------------------------------------------------

If you get a ``Bounds`` from calling either :attr:`Part:BOUNDS`
or :attr:`Vessel:BOUNDS`, then that "bounds" is magically tied
to the vessel or part it came from.

Internally, kOS is doing some "magic" to ensure that the
``Bounds`` structure "remembers" the part, or vessel, that it is
associated with, and keeps itself updated to that part or
vessel's new moved position and orientation.  This means
the values for the "absolute" suffixes described in the
table below (namely :attr:`ABSMIN`, :attr:`ABSMAX`,
:attr:`ABSCENTER`, :attr:`ABSORIGIN`, :attr:`FACING`,
:attr:`FURTHESTCORNER`, :attr:`BOTTOMALT`, and :attr:`BOTTOMALTRADAR`)
will always be kept up to date every time you get their value
it will be newly calculated and correct even though
the part or vessel has been rotating or moving since you
last called the ``:BOUNDS`` suffix.

SETTING a suffix of Bounds can break the link to its object
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

This "magically keep updating things" is only guaranteed to
keep happening if you restrict yourself to only using GET access
on Bounds suffixes.  If you ever SET the :attr:`ABSORIGIN`
or :attr:`FACING` suffixes to some other value, then Bounds will
no longer keep updating that suffix to match the object it came from.
(and consequently the other suffixes won't be updating themselves
properly either, as they depend on calculations from these two).
This is not a bug.  It's intentional.  When you SET a suffix of a
Bounds, you are explicitly telling it to use your new value
instead of its usual practice of always re-calculating it from
the part or vessel it came from.

.. _reuse_bounds:

A Bounds structure is re-usable.  Please do re-use it.
------------------------------------------------------

While it may seem like these two examples below are the same, the second
example is MUCH less of a burden on the KSP game than the first one::

    // Example 1: An expensive example using :BOUNDS again and again:
    //                (THIS IS A BAD PRACTICE):
    // Please set the ship rotating before doing this, to prove that
    // it is indeed seeing the new rotated positions:
    //
    print "100 samples of my min/max corners as I rotate:".
    for i in range(0,100) {
      print i + ": absmin=" + SHIP:bounds:absmin + ", absmax=" + SHIP:bounds:absmax.
      wait 0.
    }

::

    // Example 2: The exact same thing, done more efficiently:
    //                (THIS IS BETTER PRACTICE):
    // Please set the ship rotating before doing this, to prove that
    // it is indeed seeing the new rotated positions:
    //
    local B is SHIP:bounds. // get the :bounds suffix ONCE.
    print "100 samples of my min/max corners as I rotate:".
    for i in range(0,100) {
      print i + ": absmin=" + B:absmin + ", absmax=" + B:absmax.
      wait 0.
    }

The reason example 1 is more expensive is that every time you call
the :bounds suffix, you make kOS under the hood re-run some coordinate
transformations, and the ``Bounds`` structure is explicitly designed
to make it so you don't have to keep doing that to make it accurate.
It "remembers" which object's orientation it needs to be using, and
it keeps re-correcting itself to that objects orientation for you
every time you use it.

The expense of calling ``Part:BOUNDS`` isn't that bad and calling it
repeatedly probably won't really make your script suffer noticably.
But when you do it for the whole vessel, calling ``Vessel:BOUNDS``
repeatedly, that can definitely result in noticable unnecessary
computations being done by your computer.

For a more in-depth explanation of why it's expensive to re-call
the Bounds suffix over and over, if you care, see
:ref:`The bottom of this page <bounds_expense>`.  For now, it is
sufficient to say "it's expensive, don't do it".


.. _bounds_invalidate:

Things that will invalidate an existing Bounds
----------------------------------------------

As explained elsewhere on this page, it is much faster
and less taxing on the KSP game to re-use a ``Bounds``
instead of obtaining a new one.  The ``Bounds`` object
contains some internal logic to track rotation and movement
of the ship so the bounds box will rotate properly with it,
and the bounds boxes of individual parts will rotate if
the part rotates.

However, be aware of these following situations that can
cause a previously - obtained ``Bounds`` to become incorrect,
and require you to get a new Bounds with the suffix.  Because
doing so is expensive, don't fall to the temptation of just
making your script easy to write by unconditionally re-getting
the ``Bounds`` suffix all the time.  Instead be aware of what
makes you have to get a new Bounds, and don't do so if these
events aren't happening:

A list of events that can make a ``Bounds`` become incorrect:

* A Part Bounds will need to be recalculated if the part shrinks
  or grows through actions such as these:

    * Extending or retracting solar panels.
    * Extending or retracting Landing Gear.
    * Opening or closing cargo bay doors.
    * Moving robotic parts from the Breaking Ground DLC.
    * etc.

* A Vessel Bounds will need to be recalculated if any Part Bounds
  inside the vessel needs to be recalculated (see above list).
  In addition, the items on the following list will require a
  Vessel's Bounds (but not individual parts' bounds) to be
  recaculated:

    * Anything that adds/removes parts obviously alters the
      bounding box of the vessel.  These are examples but not an
      exhuastive list:

	* Docking and Undocking
	* Decoupling stages
	* Explosions
	* Using the asteroid grabber claw.

    * Anything that changes the vessel's "control" orientation.
      (As in anything that makes the navball jump to a new
      orientation all at once).  That invalidates the old bounding box
      because it swaps the meaning of which axis of the ship is
      the "fore" and which is the "starboard" and so on.  These are
      examples but not an exhaustive list:

	* Right-clicking a docking port and saying "control from here".
	* Right-clicking a lander can and choosing a new control orientation.
	* Entering IVA view (which has the side effect of making the game
	  do a "control from here" on the cockpit part).

Also, be aware that getting a new :attr:`Part:BOUNDS` is a LOT
less expensive than getting a whole new :attr:`Vessel:BOUNDS`,
so if your script task does need to constantly re-get the
bounds, try writing it in such a way that it only needs to
re-get the bounds of one or two parts, not the whole vessel.
(For example, for a landing script, maybe try to have your script learn
which part of your vessel is the bottom-most part you'll be landing on,
and just use that one part's bounds to test the height to the ground
instead of the entire vessel's bounds.)


Making your own Bounds
----------------------

There are a few suffixes of Bounds that are settable.
Doing so isn't very useful for Bounds coming from the vessel
or a part.  But the reason they are settable is so you can make
your own bounds objects if you feel the need to.

At minimum to make your own bounds you will need these pieces
of information:

  * The ABSORIGIN of the bounds.
  * The FACING of the bounds.
  * The RELMIN of the bounds.
  * The RELMAX of the bounds.

The following function will let you construct your own Bounds,
although it's not clear what use this would have yet::

    local my_bounds is BOUNDS( ABSORIGIN, FACING, RELMIN, RELMAX ).

The other suffixes are derived from calculations based on these.

Example::

    
    // Makes a bounds that is centered around a flag,
    // oriented in that flag's UP direction, which
    // goes a lot further up into the sky than it does down
    // into the ground (to demonstrate that the bounds box
    // doesn't have to span equally far in all directions
    // around the origin, and thus why the origin isn't always
    // the center of the box):
    local my_flag is vessel("that flag").
    local my_bounds is BOUNDS(
      my_flag:position,
      my_flag:up, // In this facing, Z = up/down, X = north/south, and Y = east/west.
      
      // box is 20x20x502 meters, centered in east/west/north/south terms, but
      // extending higher up in the +Z direction than down in the -Z direction:
      V(-10,-10, -2),
      V(10, 10, 500)
      ).

Again, it's unclear how a script might use this, but it's there
for completeness.

Obviously, a bounds box you make manually yourself this way does not
have the "magic" linkage to a vessel or part that the ones kOS makes have,
and therefore its position is more fixed in space unless your script
manually re-assignes its properties.

Diagram
-------

When looking at the suffix explanations below, these diagrams may help
illustrate what is being talked about:

.. figure:: /_images/structures/vessels/bounding_vessel.png
  :alt: Showing bounding box around a Vessel

  What some of the terms mean for a bounding box around a vessel.


.. figure:: /_images/structures/vessels/bounding_part.png
  :alt: Showing bounding box around a Part

  What some of the terms mean for a bounding box around a part.

.. structure:: Bounds

    .. list-table::
        :header-rows: 1
        :widths: 2 1 1 4

        * - Suffix
          - Type
          - Access
          - Description

        * - :attr:`ABSORIGIN`
          - :struct:`Vector`
          - Get/Set
          - origin point of box, in absolute ship-raw coords.
        * - :attr:`FACING`
          - :struct:`Direction`
          - Get/Set
          - The orientation of the box's own reference frame.
        * - :attr:`RELMIN`
          - :struct:`Vector`
          - Get/Set
          - a corner of the box in box's own reference frame.
        * - :attr:`RELMAX`
          - :struct:`Vector`
          - Get/Set
          - opposite corner of the box from RELMIN, in box's own reference frame.
        * - :attr:`ABSMIN`
          - :struct:`Vector`
          - Get only
          - a corner of the box in absolute (ship-raw) reference frame.
        * - :attr:`ABSMAX`
          - :struct:`Vector`
          - Get only
          - opposite corner of the box from RELMIN, in absolute (ship-raw) reference frame.
        * - :attr:`ABSCENTER`
          - :struct:`Vector`
          - Get only
          - center of the box (not its origin), in absolute (ship-raw) frame.
        * - :attr:`RELCENTER`
          - :struct:`Vector`
          - Get only
          - center of the box (not its origin), in box's own reference frame.
        * - :attr:`EXTENTS`
          - :struct:`Vector`
          - Get/Set
          - A vector from box center to max corner, in box's reference frame.
        * - :attr:`SIZE`
          - :struct:`Vector`
          - Get/Set
          - Exactly 2 times EXTENTS - the vector from min corner to max, in box's reference frame.
        * - :meth:`FURTHESTCORNER(Vector ray)`
          - :struct:`Vector`
          - Get only
          - Position (in absolute ship-raw coords) of the box corner most "that-a-way".
        * - :attr:`BOTTOMALT`
          - :struct:`Scalar`
          - Get Only
          - Sea-level altitude of bottom-most corner of box.
        * - :attr:`BOTTOMALTRADAR`
          - :struct:`Scalar`
          - Get Only
          - Radar altitude of bottom-most corner of box.
        * - RELORIGIN is missing
          - n/a
          - DOES NOT EXIST
          - This suffix is deliberately missing because it would always be V(0,0,0).

.. attribute:: Bounds:ABSORIGIN

    :type: :struct:`Vector`
    :access: Get/Set but read the note below before you SET it.

    The position of the origin point of the bounding box, expressed
    in absolute coordinates (what kOS calls the ship-raw reference
    frame, that the rest of the position vectors in kOS use.)

    If this bounding box came from a Part, this will be the same as
    that part's ``Part:POSITION``, and will keep being "magically"
    updated to stay with that part's position if it moves or rotates
    (but see note below).

    If this bounding box came from a vessel, this will be the same as
    that vessel's ``Vessel:PARTS[0]:POSITION`` (the position of its
    root part) and be "magically" updated to stay with that part's
    position if it moves or rotates (but see note below).  It is
    NOT ``Vessel:position``, which is important.  ``Vessel:position``
    is the *center of mass* of a vessel.  While kOS prefers to use
    CoM as the official position of a vessel most of the time, the fact
    that using fuel shifts the position of the CoM within the vessel made
    it impractical to use CoM for the vessel's bounding box origin.

    **WARNING about using SET with this suffix:** *If this bounds box
    was obtained using :attr:`Part:BOUNDS` or :attr:`Vessel:BOUNDS`,
    then this suffix keeps changing its value to remain correct as the
    vessel rotates or moves.  But ONLY if you restrict your use of this
    suffix to GET-only.  If you ever SET this suffix, kOS stops that
    auto-updating so it won't override the value you gave.  Generally,
    using SET on this suffix was only ever intended for Bounds you
    created manually with the BOUNDS() function.*

.. attribute:: Bounds:RELORIGIN

    :type: Nonexistent
    :access: Nonexistent

    **This suffix does not exist**.  It is mentioned here simply because you
    might be trying to look up a suffix with this name, thinking
    it should exist, wondering "well, there's an ABSMIN and a
    RELMIN, an ABSCENTER and a RELCENTER... where's the RELORIGIN that
    should go with ABSORIGIN?".

    The reason there is no RELORIGIN is that given how a ``Bounds`` stores
    values, by its very definition the RELORIGIN would be V(0,0,0), always.
    It's the origin of the bounding box, in the bounding box's own reference
    frame - a reference frame with this spot as its origin.

.. attribute:: Bounds:FACING

    :type: :struct:`Direction`
    :access: Get/Set but read the note below before you SET it.

    This defines the orientation of this bounding box's local
    reference frame, by providing a rotation that will get you
    from the bounding-box relative orientation (in which the
    X, Y, and Z axes are parallel to the bounding box's edges)
    to the absolute orientation (the ship-raw orientation the
    rest of kOS uses).

    If this bounding box came from a Part, this will be the same as
    that part's ``Part:FACING``, and will keep being "magically"
    updated to stay aligned with that part's facing if it moves or
    rotates (but see note below).

    If this bounding box came from a Vessel, this will be the same as
    that Vessel's ``Vessel:FACING``, and will keep being "magically"
    updated to stay aligned with that part's facing if it moves or
    rotates (but see note below).

    **WARNING about using SET with this suffix:** *If this bounds box
    was obtained using :attr:`Part:BOUNDS` or :attr:`Vessel:BOUNDS`,
    then this suffix keeps changing its value to remain correct as the
    vessel rotates or moves.  But ONLY if you restrict your use of this
    suffix to GET-only.  If you ever SET this suffix, kOS stops that
    auto-updating so it won't override the value you gave.  Generally,
    using SET on this suffix was only ever intended for Bounds you
    created manually with the BOUNDS() function.*
    
.. attribute:: Bounds:RELMIN

    :type: :struct:`Vector` **in bounding-box relative reference frame**
    :access: Get/Set

    A vector expressed in the bounding-box-relative reference frame
    (where the XYZ axes are parallel to the bounding box's edges).

    This defines one corner of the bounding box.  It is the
    "negative-most" corner of the box.  If you drew a vector from
    the box's origin point to its "negative-most" corner, that would
    be this vector.  By "negative-most" that simply means the corner
    where the X, Y, and Z coordinates have their smallest values.
    (again, in the bounding box's own reference frame, not the absolute
    world (ship-raw) frame.)

    This corner will always be the diagonally opposite corner from
    :attr:`Bounds:RELMAX`.

    If you SET this value, you are changing the size of the
    bounding box, making it larger (or smaller), as well as
    stretching or shrinking it, depending on the new value
    you pick.  Doing so doesn't *actually* change the size of
    a part or vessel, and is really only useful if you are
    working with your own ``Bounds`` you created manually with
    the ``Bounds()`` built-in function.

    Be careful when trying to "add" the RELMIN vector to other
    vectors in the game.  It's not oriented in ship-raw coords.
    To rotate it into ship-raw coords you can multiply it by
    the bounds facing like so: ``MyBounds:FACING * MyBounds:RELMIN``.

.. attribute:: Bounds:RELMAX

    :type: :struct:`Vector` **in bounding-box relative reference frame**
    :access: Get/Set

    A vector expressed in the bounding-box-relative reference frame
    (where the XYZ axes are parallel to the bounding box's edges).

    This defines one corner of the bounding box.  It is the
    "positive-most" corner of the box.  If you drew a vector from
    the box's origin point to its "positive-most" corner, that would
    be this vector.  By "positive-most" that simply means the corner
    where the X, Y, and Z coordinates have their greatest values.
    (again, in the bounding box's own reference frame, not the absolute
    world (ship-raw) frame.)

    This corner will always be the diagonally opposite corner from
    :attr:`Bounds:RELMIN`.

    If you SET this value, you are changing the size of the
    bounding box, making it larger (or smaller), as well as
    stretching or shrinking it, depending on the new value
    you pick.  Doing so doesn't *actually* change the size of
    a part or vessel, and is really only useful if you are
    working with your own ``Bounds`` you created manually with
    the ``Bounds()`` built-in function.

    Be careful when trying to "add" the RELMAX vector to other
    vectors in the game.  It's not oriented in ship-raw coords.
    To rotate it into ship-raw coords you can multiply it by
    the bounds facing like so: ``MyBounds:FACING * MyBounds:RELMAX``.

.. attribute:: Bounds:ABSMIN

    :type: :struct:`Vector`
    :access: Get

    This is the same point as :attr:`Bounds:RELMIN`, except it has
    been rotated and translated into absolute coordinates (what 
    kOS calls the ship-raw reference frame, that the rest of the
    position vectors in kOS use.)

    You cannot SET this value, because it is generated
    from the ABSORIGIN, the FACING, and the RELMIN.

    Calculating the ABSMIN could be done in kerboscript from the
    other Bounds suffixes (see example below), but this is provided
    for convenience::

        // The following two print lines should print
        // the same vector, within reason.  (There may be a
        // small floating point precision variance between them):
        set B to ship:bounds.
        print B:ABSMIN.
        print B:ABSORIGIN + (B:FACING * B:RELMIN).


.. attribute:: Bounds:ABSMAX

    :type: :struct:`Vector`
    :access: Get

    This is the same point as :attr:`Bounds:RELMAX`, except it has
    been rotated and translated into absolute coordinates (what 
    kOS calls the ship-raw reference frame, that the rest of the
    position vectors in kOS use.)

    You cannot SET this value, because it is generated
    from the ABSORIGIN, the FACING, and the RELMAX.

    Calculating the ABSMAX could be done in kerboscript from the
    other Bounds suffixes (see example below), but this is provided
    for convenience::

        // The following two print lines should print
        // the same vector, within reason.  (There may be a
        // small floating point precision variance between them):
        set B to ship:bounds.
        print B:ABSMAX.
        print B:ABSORIGIN + (B:FACING * B:RELMAX).

.. attribute:: Bounds:RELCENTER

    :type: :struct:`Vector` **in bounding-box relative reference frame**
    :access: Get-only

    The center of the bounding box, in its own relative reference frame.
    (Not the absolute ship-raw reference frame the rest of kOS uses.)

    This is the offset between the bounding box's origin and its center.

    The origin of a bounding box is often not at its center because a
    bounding box can extend further in one direction than the other.
    For example a vessel's root part is often up at the top of the rocket,
    such a vessel's bounding box will extend much further in the "aft"
    direction than it does in the "fore" direction.  The wing parts in the
    game are often defined with their origin point at the base where they
    glue to the fuselage, not out in the middle of the wing.

    Instead of being provided directly, this value could be calculated
    from the RELMIN and RELMAX.  It's simply the point exactly halfway
    between those two opposite corners.

.. attribute:: Bounds:ABSCENTER

    :type: :struct:`Vector`
    :access: Get-only

    This is just the same thing as :attr:`Bounds:RELCENTER`, but
    in the absolute (ship-raw) reference frame which scripts might find
    more useful.

    It's exactly equivalent to doing this::

        MyBounds:ABSORIGIN + (MyBounds:FACING * MyBounds:RELCENTER).

    Instead of being provided directly, this value could be calculated
    from the ABSMIN and ABSMAX.  It's simply the point exactly halfway
    between those two opposite corners.

.. attribute:: Bounds:EXTENTS

    :type: :struct:`Vector` **in bounding-box relative reference frame**
    :access: Get-only

    A vector (in bounding-box relative reference frame, NOT the
    absolute (ship-raw) reference frame the rest of kOS uses)
    that describes where :attr:`Bounds:RELMAX` is, relative to
    to the box's center (rather than to its origin).

    Note that the vector in the inverse direction of this one (that you'd
    get by multiplying it by -1), points from the center to
    the oppposite corner, the :attr:`Bounds:RELMIN`.

.. attribute:: Bounds:SIZE

    :type: :struct:`Vector` **in bounding-box relative reference frame**
    :access: Get-only

    A vector (in bounding-box relative reference frame, NOT the
    absolute (ship-raw) reference frame the rest of kOS uses)
    that describes the ray from RELMIN to RELMAX that goes diagonally
    across the whole box.  It's always just the same thing you'd
    get if you took the :attr:`Bounds:EXTENTS` vector and multiplied
    it by the scalar 2.

.. method:: Bounds:FURTHESTCORNER(ray)

    :parameter ray: The "that-a-way" :struct:`Vector` in absolute (ship-raw) reference frame.
    :return: :struct:`Vector` in absolute (ship-raw) referece frame.

    Returns the position (in absolute (ship-raw) reference frame) of
    whichever of the 8 corners of this bounding box is "furthest" in
    the direction the ray vector is pointing.  Useful when you want
    to know the furthest a bounding box extends in some gameworld
    direction.

    Examples::

        // Assume other_vessel has been set to some vessel nearby
        // other than the current SHIP:
        //
        local ves_box is other_vessel:bounds.
        local top is ves_box:furthestcorner(up:vector).
        local bottom is ves_box:furthestcorner(-up:vector).

        local from_other_to_me is ship:position - other_vessel:position.
        local nearest is ves_box:furthestcorner(from_other_to_me).
        print "The closest point on the other vessel's bounds box is this far away:".
        print nearest:mag.

        // A more complex example showing how you might use bounds boxes
        // when trying to figure out how big a target vessel is so you
        // know how to go around it:
        //
        local my_left is -ship:facing:starvector.
        local leftmost is ves_box:furthestcorner(my_left).
        print "In order to go around the other vessel, to the left, ".
        pritn "I would need to shift myself this far to my left:".
        print vdot(-ship:facing:starvector, leftmost).
    
.. attribute:: Bounds:BOTTOMALT

    :type: :struct:`Scalar`
    :access: Get-only

    The above-sea-level altitude reading from the bottom-most corner of
    this bounding box, toward whichever Body the current CPU vessel is
    orbiting.
    
    Note that it's always using the CPU vessel's *current*
    body to decide which body is the one that defines the bounding
    box's "downward" direction for picking its bottom-most corner,
    and it uses that same body to decide what counts as "altitude",
    regardless of wether the bounds box is a bounds box of the current
    CPU vessel or something else.

    To put it another way: You can't "read" what the altitude of a
    bounding box above the Mun is if your ship is currently
    in Kerbin's sphere of influence.  If you are currently orbiting
    Kerbin, it will assume that the "bottom" of any Bounds box you
    refer to means "corner closest to Kerbin" and "altitude" means
    "distance from Kerbin's Sea level".  Once your CPU vessel moves
    into the Mun's sphere of influence this will change it it will
    now assume that the "bottom" of a Bounds is the corner closest
    to the Mun and the altitude you care about is the altitude above
    the Mun.

    This may seem like a limitation, but it really isn't, since you
    wouldn't be able to query a vessel or a part for its bounding
    box if that vessel was far enough away to be outside the loading
    distance and thus its full set of parts isn't "there".
    It would only be a limitation for cases where you are inventing
    your own bounds boxes from scratch.

.. attribute:: Bounds:BOTTOMALTRADAR

    :type: :struct:`Scalar`
    :access: Get-only

    The radar-altitude reading from the bottom-most corner of
    this bounding box, toward whichever Body the current CPU vessel is
    orbiting.  Same as :attr:`Bounds:BOTTOMALT` except for the
    difference between above-sea-level altitude versus radar altitude.


Side topic - what is a bounding box and how does kOS know about it?
-------------------------------------------------------------------

A bounding box is a rectangular box around an item that represents the
smallest space that contains all verteces of the item yet is still
shaped like a box.  It is common in graphics and video games for the
GPU and/or game engine to maintain information about the bounding boxes
of items in the game.  Knowing this information is part of what they
do to reduce their large workload.  When checking if two items are
colliding, or checking if part of object A blocks the line of sight
between the camera and part of object B it's trying to draw, a check
against the bounding box first is quick and simple.  If the thing
you're checking isn't even intersecting the bounding box, then it's
impossible for it to be a hit on the actual complex shape inside it.
The expensive check that looks at the exact shape of the object's
mesh can be skipped when you're not even intersecting the bounding
box around it.

.. _bounds_expense:

Why is calling :bounds repeatedly a slow thing to do?
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

The bounding box Unity tracks for items is aligned with the XYZ axes
of the game's world coordinates, what kOS calls the "ship-raw" reference
frame.  This is vital for the game engine's needs, as the speedy quick
bounding box intersection tests work by doing simple greater-than
and less-than tests of the coordinates.  This means the box will also be
"too big" if the item is rotated from the world's XYZ axes, as the
world-aligned box has to accomodate the item's "diagonal" corners pushing
the box bigger.  For the purposes of a graphics engine, that's fine, since
erring on the side of a too-big bounding box is okay, since it's nothing
more than a time savings to short-circuit work, and not the final say-so
on whether item A is touching item B.

To get a rotated box like is needed for kOS's needs, where its tightly
snug against the object in question, kOS has to go a bit more low-level
and look at the actual meshes that make up the object, and look at
*their* bounding boxes, which are aligned in the mesh's own locally
rotated XYZ axes, rather than world axes.  Some ugly transforms
of each of the 8 vertices of the mesh bounding box Unity knows about are
needed, and there isn't really a good way to do this without running a
loop across all vertices of the box, which is what kOS does internally.
For the whole vessel's bounding box, this means doing those 8 vertices
per part, on every part on the ship.

Why then is it faster to re-use the BOUNDS suffix?
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

Internally, kOS stores the *relative* bounds *in* the reference
frame of the object (the *facing* suffix), and uses a delegate to
keep getting the new orientation and origin of that object every
time you ask for a Bounds' suffix.  Thus it doesn't have to keep
re-doing the transformations described above as the part rotates
or shifts.  It only has to apply the rotation and translation to
the existing relative bounds it already calculated the expensive
way the first time.

Credit to kRPC
~~~~~~~~~~~~~~
The messy problem described above wasn't solved until peeking at the
code inside the kRPC mod, which also offers bounding box information.
That peek showed a hint at the similar steps kOS would need to do to
support the same thing, and gave an explanation why the "raw" bounding
box that Unity gave was always too big and seemed wrong.  That led to a
lot of looking at answers on the Unity forum from other people who had
the same problem, and did similar solutions to what kRPC was doing,
indicating that despite looking like a lot of work, this was really
the only way to do it.  (Since kRPC and kOS are both GPL3, this peeking
is totally legit.)
