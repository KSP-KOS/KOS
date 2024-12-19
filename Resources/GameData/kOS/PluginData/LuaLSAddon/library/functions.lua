---@param AlarmType string 
---@param UT number 
---@param Name string 
---@param Notes string 
---@return KACAlarm
---Creates alarm of type `KACAlarm:ALARMTYPE` at `UT` with `Name` and `Notes` attributes set. Attaches alarm to current :ref:`CPU Vessel <cpu vessel>`.  Returns `KACAlarm` object if creation was successful and empty string otherwise::
---
---    set na to addAlarm("Raw",time:seconds+300, "Test", "Notes").
---    print na:NAME. //prints 'Test'
---    set na:NOTES to "New Description".
---    print na:NOTES. //prints 'New Description'
function addalarm(AlarmType, UT, Name, Notes) end

---@param alarmType string 
---@return List
---If `alarmType` equals "All", returns `List` of *all* `KACAlarm` objects attached to current vessel or have no vessel attached.
---Otherwise returns `List` of all `KACAlarm` objects with `KACAlarm:TYPE` equeal to `alarmType` and attached to current vessel or have no vessel attached.::
---
---    set al to listAlarms("All").
---    for i in al
---    {
---        print i:ID + " - " + i:name.
---    }
function listalarms(alarmType) end

---@param alarmID string 
---@return boolean
---Deletes alarm with ID equal to alarmID. Returns True if successful, false otherwise::
---
---    set na to addAlarm("Raw",time:seconds+300, "Test", "Notes").
---    if (DELETEALARM(na:ID))
---    {
---        print "Alarm Deleted".
---    }
function deletealarm(alarmID) end

---@param path string 
---argument 1
---   Path of the file for editing.
---
---Edits or creates a program file described by filename :code:`PATH`.
---If the file referred to by :code:`PATH` already exists, then it will
---open that file in the built-in editor.  If the file referred to by
---:code:`PATH` does not already exist, then this command will create it
---from scratch and let you start editing it.
---
---It is important to type the command using the filename's :code:`.ks`
---extension when using this command to create a new file.  (Don't omit
---it like you sometimes can in other places in kOS).  The logic to
---automatically assume the :code:`.ks` extension when the filename has
---no extension only works when kOS can find an existing file by doing so.
---If you are creating a brand new file from scratch with the :code:`EDIT`
---command, and leave off the :code:`.ks` extension, you will get a file
---created just like you described it (without the extension).
function edit(path) end

---@param listType string | "bodies" | "targets" | "resources" | "parts" | "engines" | "rcs" | "sensors" | "elements" | "dockingports" | "files" | "volumes" | "processors" | "fonts" 
---@return List
---Build a list with elements of the specified "listType" argument
function buildlist(listType) end

---@param time TimeSpan | TimeStamp | number `TimeSpan` (ETA), `TimeStamp` (UT), or `number` (UT)
---@param radial number (m/s) Delta-V in radial-out direction
---@param normal number (m/s) Delta-V normal to orbital plane
---@param prograde number (m/s) Delta-V in prograde direction
---@return Node
---You can make a maneuver node in a variable using the :func:`NODE` function.
---The radial, normal, and prograde parameters represent the 3 axes you can
---adjust on the manuever node.  The time parameter represents when the node
---is along a vessel's path.  The time parameter has two different possible
---meanings depending on what kind of value you pass in for it.  It's either
---an absolute time since the game started, or it's a relative time (ETA)
---from now, according to the following rule:
---
---Using a TimeSpan for time means it's an ETA time offset
---relative to right now at the moment you called this function::
---
---    // Example: This makes a node 2 minutes and 30 seconds from now:
---    SET myNode to NODE( TimeSpan(0, 0, 0, 2, 30), 0, 50, 10 ).
---    // Example: This also makes a node 2 minutes and 30 seconds from now,
---    // but does it by total seconds (2*60 + 30 = 150):
---    SET myNode to NODE( TimeSpan(150), 0, 50, 10 ).
---
--- Using a TimeStamp, or a Scalar number of seconds for time means
--- it's a time expressed in absolute universal time since game
--- start::
---
---    // Example: A node at: year 5, day 23, hour 1, minute 30, second zero:
---    SET myNode to NODE( TimeStamp(5,23,1,30,0), 0, 50, 10 ).
---
---    // Using a Scalar number of seconds for time also means it's
---    // a time expressed in absolute universal time (seconds since
---    // epoch):
---    // Example: A node exactly one hour (3600 seconds) after the
---    // campaign started:
---    SET myNode to NODE( 3600, 0, 50, 10 ).
---
--- Either way, once you have a maneuver node in a variable, you use the :global:`ADD` and :global:`REMOVE` commands to attach it to your vessel's flight plan. A kOS CPU can only manipulate the flight plan of its :ref:`CPU vessel <cpu vessel>`.
---
---Once you have created a node, it's just a hypothetical node that hasn't
---been attached to anything yet. To attach a node to the flight path, you must use the command :global:`ADD` to attach it to the ship.
function node(time, radial, normal, prograde) end

---@param x number (scalar) :math:`x` coordinate
---@param y number (scalar) :math:`y` coordinate
---@param z number (scalar) :math:`z` coordinate
---@return Vector
---This creates a new vector from 3 components in :math:`(x,y,z)`::
---
---    SET vec TO V(x,y,z).
---
---Here, a new `Vector` called ``vec`` is created . The object `Vector` represents a `three-dimensional euclidean vector <http://en.wikipedia.org/wiki/Euclidean_vector>`__ To deeply understand most vectors in kOS, you have to understand a bit about the :ref:`underlying coordinate system of KSP <ref frame>`. If you are having trouble making sense of the direction the axes point in, go read that page.
function v(x, y, z) end

---@param pitch number 
---@param yaw number 
---@param roll number 
---@return Direction
---A `Direction` can be created out of a Euler Rotation, indicated with the :func:`R()` function, as shown below where the ``pitch``, ``yaw`` and ``roll`` values are in degrees::
---
---    SET myDir TO R( a, b, c ).
function r(pitch, yaw, roll) end

---@param x number 
---@param y number 
---@param z number 
---@param rot number 
---@return Direction
---A `Direction` can also be created out of a *Quaternion* tuple,
---indicated with the :func:`Q()` function, passing it the x, y, z, w
---values of the Quaternion.
---`The concept of a Quaternion <https://en.wikipedia.org/wiki/Quaternions_and_spatial_rotation>`__
---uses complex numbers and is beyond the scope of the kOS
---documentation, which is meant to be simple to understand.  It is
---best to not use the Q() function unless Quaternions are something
---you already understand.
---
---::
---
---    SET myDir TO Q( x, y, z, w ).
function q(x, y, z, rot) end

---@param inc number (`number`) inclination, in degrees.
---@param e number (`number`) eccentricity
---@param sma number (`number`) semi-major axis
---@param lan number (`number`) longitude of ascending node, in degrees.
---@param argPe number (`number`) argument of periapsis
---@param mEp number (`number`) mean anomaly at epoch, in degrees.
---@param t number (`number`) epoch
---@param body Body (`Body`) body to orbit around
---@return Orbit
---This creates a new orbit around the Mun::
---
---    SET myOrbit TO CREATEORBIT(0, 0, 270000, 0, 0, 0, 0, Mun).
function createorbit(inc, e, sma, lan, argPe, mEp, t, body) end

---@param pos Vector (`Vector`) position (relative to center of body, NOT the usual relative to current ship most positions in kOS use.  Remember to offset a kOS position from the body's position when calculating what to pass in here.)
---@param vel Vector (`Vector`) velocity
---@param body Body (`Body`) body to orbit around
---@param ut number (`number`) time (universal)
---@return Orbit
---This creates a new orbit around Kerbin::
---
---    SET myOrbit TO CREATEORBIT(V(2295.5, 0, 0), V(0, 0, 70000 + Kerbin:RADIUS), Kerbin, 0).
function createorbit(pos, vel, body, ut) end

---@param fromVec Vector 
---@param toVec Vector 
---@return Direction
---A `Direction` can be created with the ``ROTATEFROMTO`` function.  It is *one of the infinite number of* rotations that could rotate vector *fromVec* to become vector *toVec* (or at least pointing in the same direction as toVec, since fromVec and toVec need not be the same magnitude).  Note the use of the phrase "**infinite number of**".  Because there's no guarantee about the roll information, there are an infinite number of rotations that could qualify as getting you from one vector to another, because there's an infinite number of roll angles that could result and all still fit the requirement::
---
---    SET myDir to ROTATEFROMTO( v1, v2 ).
function rotatefromto(fromVec, toVec) end

---@param lookAt Vector 
---@param lookUp Vector 
---@return Direction
---A `Direction` can be created with the LOOKDIRUP function by using two vectors.   This is like converting a vector to a direction directly, except that it also provides roll information, which a single vector lacks.   *lookAt* is a vector describing the Direction's FORE orientation (its local Z axis), and *lookUp* is a vector describing the direction's TOP orientation (its local Y axis).  Note that *lookAt* and *lookUp* need not actually be perpendicualr to each other - they just need to be non-parallel in some way.  When they are not perpendicular, then a vector resulting from projecting *lookUp* into the plane that is normal to *lookAt* will be used as the effective *lookUp* instead::
---
---    // Aim up the SOI's north axis (V(0,1,0)), rolling the roof to point to the sun.
---    LOCK STEERING TO LOOKDIRUP( V(0,1,0), SUN:POSITION ).
---    //
---    // A direction that aims normal to orbit, with the roof pointed down toward the planet:
---    LOCK normVec to VCRS(SHIP:BODY:POSITION,SHIP:VELOCITY:ORBIT).  // Cross-product these for a normal vector
---    LOCK STEERING TO LOOKDIRUP( normVec, SHIP:BODY:POSITION).
function lookdirup(lookAt, lookUp) end

---@param degrees number 
---@param axisVector Vector 
---@return Direction
---A `Direction` can be created with the ANGLEAXIS function.  It represents a rotation of *degrees* around an axis of *axisVector*.  To know which way a positive or negative number of degrees rotates, remember this is a left-handed coordinate system::
---
---    // Pick a new rotation that is pitched 30 degrees from the current one, taking into account
---    // the ship's current orientation to decide which direction is the 'pitch' rotation:
---    //
---    SET pitchUp30 to ANGLEAXIS(-30,SHIP:STARFACING).
---    SET newDir to pitchUp30*SHIP:FACING.
---    LOCK STEERING TO newDir.
function angleaxis(degrees, axisVector) end

---@param lat number (deg) Latitude
---@param lng number (deg) Longitude
---@return GeoCoordinates
---This function creates a `GeoCoordinates` object with the given
---latitude and longitude, assuming the current SHIP's Body is the body
---to make it for.
---
---Once created it can't be changed. The :attr:`GeoCoordinates:LAT` and
---:attr:`GeoCoordinates:LNG` suffixes are get-only (they cannot be
---set.) To switch to a new location, make a new call to :func:`LATLNG()`.
---
---If you wish to create a `GeoCoordinates` object for a latitude
---and longitude around a *different* body than the ship's current sphere
---of influence body, see :meth:`Body:GEOPOSITIONLATLNG` for a means to do that.
---
---It is also possible to obtain a `GeoCoordinates` from some suffixes of some other structures. For example::
---
---    SET spot to SHIP:GEOPOSITION.
function latlng(lat, lng) end

---@param name string 
---@return Vessel
---Get vessel with the specified name
function vessel(name) end

---@param name string 
---@return Body
---Get body with the specified name
function getbody(name) end

---@param name string 
---@return boolean
function bodyexists(name) end

---@param name string 
---@return Atmosphere
---Passing in a string (``name``) parameter, this function returns the
---:attr:`ATM <Body:ATM>` of the body that has that name.  It's identical
---to calling ``BODY(name):ATM``, but accomplishes the goal in fewer steps.
---
---It will crash with an error if no such body is found in the game.
function bodyatmosphere(name) end

---@param absOrigin Vector 
---@param facing Direction 
---@param relMin Vector 
---@param relMax Vector 
---@return Bounds
function bounds(absOrigin, facing, relMin, relMax) end

---@param dir number 
---@param pitch number 
---@param roll? number 
---@return Direction
---A `Direction` can be created out of a :func:`HEADING()` function. The first parameter is the compass heading, and the second parameter is the pitch above the horizon::
---
---    SET myDir TO HEADING(degreesFromNorth, pitchAboveHorizon).
---
---The third parameter, *roll*, is optional. Roll indicates rotation about the longitudinal axis.
function heading(dir, pitch, roll) end

---@param frequency number | string 
---@param endFrequency number | string 
---@param duration number 
---@param keyDownLength? number 
---@param volume? number 
---@return Note
---This global function creates a note object that makes a sliding note
---that changes linearly from the start frequency to the end frequency
---across the duration of the note.
---
---where:
---
---``frequency``
---    **Mandatory**: This is the frequency the sliding note begins at.
---    If it is a number, then it is the frequency in hertz (Hz).
---    If it is a string, then it's using the letter notation
---    :ref:`described here <skid_letter_frequency>`.
---``endFrequency``
---    **Mandatory**: This is the frequency the sliding note ends at.
---    If it is a number, then it is the frequency in hertz (Hz).
---    If it is a string, then it's using the letter notation
---    :ref:`described here <skid_letter_frequency>`.
---``duration``
---    **Mandatory**: Same as the duration for the :func:`NOTE()`
---    built-in function.  If it is missing it will be the same thing
---    as the keyDownLength.
---``keyDownLength``
---    **Optional**: Same as the keyDownLength for the :func:`NOTE()`
---    built-in function.
---``volume``
---    **Optional**: Same as the volume for the :func:`NOTE()`
---    built-in function.
---
---The note's frequency will change linearly from the starting to
---the ending frequency over the note's duration.  (For example, If the
---duration is shorter, but all the other values are the kept the same,
---that makes the frequency change go faster so it can all fit within the
---given duration.)
---
---You can make the note pitch up over time or pitch down over time
---depending on whether the endFrequency is higher or lower than
---the initial frequency.
---
---This is an example of it being used in conjunction with the Voice's
---PLAY() suffix method::
---
---    SET V1 TO GETVOICE(0).
---    // A fast "whoop" sound that pitches up from 300 Hz to 600 Hz quickly:
---    V1:PLAY( SLIDENOTE(300, 600, 0.2, 0.25, 1) ).
function slidenote(frequency, endFrequency, duration, keyDownLength, volume) end

---@param frequency number | string 
---@param duration number 
---@param keyDownLength? number 
---@param volume? number 
---@return Note
---This global function creates a note object from the given values.
---
---where:
---
---``frequency``
---    **Mandatory**: The frequency can be given as either a number or a
---    string.  If it is a number, then it is the frequency in hertz (Hz).
---    If it is a string, then it's using the letter notation
---    :ref:`described here <skid_letter_frequency>`.
---``duration``
---    **Mandatory**: The total amount of time the note takes up before
---    the next note can begin, *including* the small gap between the end
---    of its keyDownLength and the start of the next note.
---    Note that the value here gets multiplied by the voice's
---    :meth:`TEMPO<Voice:TEMPO>` to decide the actual duration in seconds when
---    it gets played.
---``keyDownLength``
---    **Optional**: The amount of time the note takes up before the
---    "synthesizer key" is released.  In terms of the
---    :ref:`ADSR Envelope <skid_envelope>`, this is the portion of
---    the note's time taken up by the Attack, Decay, and Sustain part
---    of the note, but not including the Release part of the note.  In
---    order to hear the note fade away during its Release portion, the
---    keyDownLength must be shorter than the Duration, or else there's
---    no gap of time to fit the release in before the next note starts.
---    By default, if you leave the KeyDownLength off, you get a default
---    KeyDownLength of 90% of the Duration, leaving 10% of the Duration
---    left to hear the "Release" time before the next note starts.
---    If you wish to force the notes to immediately blend from one to the
---    next with no audible gaps between them, then for each note you
---    need to specify a keyDownLength that is equal to the Duration.
---    Note that the value here gets
---    multiplied by the voice's :meth:`TEMPO<Voice:TEMPO>` to decide the actual
---    duration in seconds when it gets played.
---``volume``
---    **Optional**: If present, then the note can be given a different
---    volume than the default for the voice it's being played on, to
---    make it louder or quieter than the other notes this voice is
---    playing.  This setting is a relative multiplier applied to the
---    voice's volume. (i.e. 1.0 means play at the same volume as the
---    voice's setting, 1.1 means play a bit louder than the voice's
---    setting, and 0.9 means play a bit quieter than the voice's
---    setting).
---
---This is an example of it being used in conjunction with the Voice's
---:meth:`PLAY()<Voice:PLAY>` suffix method::
---
---    SET V1 TO GETVOICE(0).
---    V1:PLAY( NOTE(440, 0.2, 0.25, 1) ).
function note(frequency, duration, keyDownLength, volume) end

---@param num number 
---@return Voice
---To access one of the :ref:`voices <skid_voice>` of the
---:ref:`SKID <skid>` chip, you use the ``GetVoice(num)`` built-in
---function.
---
---Where ``num`` is the number of the hardware voice you're interested
---in accessing.  (The numbering starts with the first voice being
---called 0).
function getvoice(num) end

---This will stop all voices.  If the voice is scheduled to play additional
---notes, they will not be played. If the voice in the middle of playing a note,
---that note will be stopped.
function stopallvoices() end

---@param universal_time? number (`number`)
---@return TimeStamp
---:return: A :struct`TimeStamp` of the time represented by the seconds passed in.
---This creates a `TimeStamp` given a "universal time",
---which is a number of seconds since the current game began,
---IN GAMETIME.  example: ``TIME(3600)`` will give you a
---`TimeSpan` representing the moment exactly 1 hour
---(3600 seconds) since the current game first began.
---
---The parameter is OPTIONAL.  If you leave it off,
---and just call ``TIMESTAMP()``, then you end up getting
---the current time, which is the same thing that :global:`TIME`
---gives you (without the parentheses).
function timestamp(universal_time) end

---@param year number (`number`)
---@param day number (`number`)
---@param hour? number (`number`) [optional]
---@param min? number (`number`) [optional]
---@param sec? number (`number`) [optional]
---@return TimeStamp
---:return: A :struct`TimeStamp` of the time represented by the values passed in.
---This creates a `TimeStamp` given a year, day, hour-hand,
---minute-hand, and second-hand.
---
---Because a `TimeStamp` is a calendar reckoning, the values
---you use for the year and the day should start counting at 1, not
---at 0.  (The hour, minute, and second still start at zero).
---
---In other words::
---
---  // Notice these are equal because year and day start at 1 not 0:
---  set t1 to TIMESTAMP(0).
---  set t2 to TIMESTAMP(1,1,0,0,0).
---  print t1:full.
---  print t2:full. // Prints same as above.
---
---Note that the year and day are mandatory, but the remaining
---parameters are optional and if you leave them off it assumes you
---meant them to be zero (meaning it will give you a timestamp at
---the very start of that date, right at midnight 0:00:00 O'clock).
function timestamp(year, day, hour, min, sec) end

---@param universal_time? number (`number`)
---@return TimeSpan
---:return: A :struct`TimeSpan` of the time represented by the seconds passed in.
---This creates a `TimeSpan` equal to the number of seconds
---passed in. Fractional seconds are allowed for more precise spans.
---
---The parameter is OPTIONAL.  If you leave it off, and just call
---``TIMESPAN()``, then you end up getting a timespan of zero duration.
function timespan(universal_time) end

---@param year number (`number`)
---@param day number (`number`)
---@param hour? number (`number`) [optional]
---@param min? number (`number`) [optional]
---@param sec? number (`number`) [optional]
---@return TimeSpan
---:return: A :struct`TimeSpan` of the time represented by the values passed in.
---This creates a `TimeSpan` that lasts this number of years
---plus this number of days plus this number of hours plus this number
---of minutes plus this number of seconds.
---
---Because a `TimeSpan` is NOT a calendar reckoning, but
---an actual duration, the values you use for the year and the day
---should start counting at 0, not at 1.
---
---In other words::
---
---  // Notice these are equal because year and day start at 0 not 1:
---  set span1 to TIMESPAN(0).
---  set span2 to TIMESPAN(0,0,0,0,0).
---  print span1:full.
---  print span2:full. // Prints same as above.
---
---Note that the year and day are mandatory in this function, but the
---remaining parameters are optional and if you leave them off it
---assumes you meant them to be zero (meaning it will give you a
---timespan exactly equal to that many years and days, with no leftover
---hours or minutes or seconds.)
function timespan(year, day, hour, min, sec) end

---@param h number 
---@param s number 
---@param v number 
---@return HSVA
---This global function creates a color from hue, saturation and value::
---
---    SET myColor TO HSV(h,s,v).
---            
---    `More Information about HSV <http://en.wikipedia.org/wiki/HSL_and_HSV>`_,
---
---where:
---
---``h``
---    A floating point number from 0.0 to 1.0 for the hue component.
---``s``
---    A floating point number from 0.0 to 1.0 for the saturation component.
---``v``
---    A floating point number from 0.0 to 1.0 for the value component.
function hsv(h, s, v) end

---@param h number 
---@param s number 
---@param v number 
---@param a number 
---@return HSVA
---Same as :func:`HSV()` but with an alpha (transparency) channel::
---
---    SET myColor TO HSVA(h,s,v,a).
---
---``h, s, v`` are the same as above.
---
---``a``
---    A floating point number from 0.0 to 1.0 for the alpha component. (1.0 means opaque, 0.0 means invisibly transparent).
function hsva(h, s, v, a) end

---@param r number 
---@param g number 
---@param b number 
---@return RGBA
---This global function creates a color from red green and blue values::
---
---    SET myColor TO RGB(r,g,b).
---
---where:
---
---``r``
---    A floating point number from 0.0 to 1.0 for the red component.
---``g``
---    A floating point number from 0.0 to 1.0 for the green component.
---``b``
---    A floating point number from 0.0 to 1.0 for the blue component.
function rgb(r, g, b) end

---@param r number 
---@param g number 
---@param b number 
---@param a number 
---@return RGBA
---Same as :func:`RGB()` but with an alpha (transparency) channel::
---
---    SET myColor TO RGBA(r,g,b,a).
---
---``r, g, b`` are the same as above.
---
---``a``
---    A floating point number from 0.0 to 1.0 for the alpha component. (1.0 means opaque, 0.0 means invisibly transparent).
function rgba(r, g, b, a) end

---@param start? Vector | Delegate 
---@param vec? Vector | Delegate 
---@param color? RGBA | Delegate 
---@param label? string 
---@param scale? number 
---@param show? boolean 
---@param width? number 
---@param pointy? boolean 
---@param wiping? boolean 
---@return Vecdraw
---Both these two function names do the same thing.  For historical
---reasons both names exist, but now they both do the same thing.
---They create a new ``vecdraw`` object that you can then manipulate
---to show things on the screen.
---
---For an explanation what the parameters start, vec, color, label, scale, show,
---width, pointy, and wiping mean, they correspond to the same suffix names
---below in the table.
---
---Here are some examples::
---
---    SET anArrow TO VECDRAW(
---          V(0,0,0),
---          V(a,b,c),
---          RGB(1,0,0),
---          "See the arrow?",
---          1.0,
---          TRUE,
---          0.2,
---          TRUE,
---          TRUE
---        ).
---
---    SET anArrow TO VECDRAWARGS(
---          V(0,0,0),
---          V(a,b,c),
---          RGB(1,0,0),
---          "See the arrow?",
---          1.0,
---          TRUE,
---          0.2,
---          TRUE,
---          TRUE
---        ).
---
---Vector arrows can also be created with dynamic positioning and color.  To do
---this, instead of passing static values for the first three arguments of
---``VECDRAW()`` or ``VECDRAWARGS()``, you can pass a
---:ref:`Delegate <delegates>` for any of them, which returns a value of the
---correct type.  Here's an example where the Start, Vec, and Color are all
---dynamically adjusted by anonymous delegates that kOS will frequently call
---for you as it draws the arrow::
---
---    // Small dynamically moving vecdraw example:
---    SET anArrow TO VECDRAW(
---      { return (6-4*cos(100*time:seconds)) * up:vector. },
---      { return (4*sin(100*time:seconds)) * up:vector.  },
---      { return RGBA(1, 1, RANDOM(), 1). },
---      "Jumping arrow!",
---      1.0,
---      TRUE,
---      0.2,
---      TRUE,
---      TRUE
---    ).
---    wait 20. // Give user time to see it in motion.
---    set anArrow:show to false. // Make it stop drawing.
---
---In the above example, ``VECDRAW()`` detects that the first argument
---is a delegate, and it uses this information to decide to assign
---it into :attr:`VecDraw:STARTUPDATER`, instead of into :attr:`VecDraw:START`.
---Similarly it detects that the second argument is a delegate, so it
---assigns it into :attr:`VecDraw:VECUPDATER` instead of into :attr:`VecDraw:VEC`.
---And it does the same thing with the third argument, assigning it into
---:attr:`VecDraw:COLORUPDATER`, instead of :attr:`VecDraw:COLOR`.
---
---All the parameters of the ``VECDRAW()`` and ``VECDRAWARGS()`` are
---optional.  You can leave any of the lastmost parameters off and they
---will be given a default::
---
---    Set anArrow TO VECDRAW().
---
---Causes it to have these defaults:
---
---.. list-table:: Defaults
---        :header-rows: 1
---        :widths: 1 3
---
---        * - Suffix
---          - Default
---
---        * - :attr:`START`
---          - V(0,0,0)  (center of the ship is the origin)
---        * - :attr:`VEC`
---          - V(0,0,0)  (no length, so nothing appears)
---        * - :attr:`COLO[U]R`
---          - White
---        * - :attr:`LABEL`
---          - Empty string ""
---        * - :attr:`SCALE`
---          - 1.0
---        * - :attr:`SHOW`
---          - false
---        * - :attr:`WIDTH`
---          - 0.2
---        * - :attr:`POINTY`
---          - true
---        * - :attr:`WIPING`
---          - true
---
---Examples::
---
---    // Makes a red vecdraw at the origin, pointing 5 meters north,
---    // with defaults for the un-mentioned
---    // paramters LABEL, SCALE, SHOW, and WIDTH.
---    SET vd TO VECDRAW(V(0,0,0), 5*north:vector, red).
---
---To make a `VecDraw` disappear, you can either set its :attr:`VecDraw:SHOW` to false or just :ref:`UNSET <unset>` the variable, or re-assign it. An example using `VecDraw` can be seen in the documentation for :func:`POSITIONAT()`.
function VECDRAW(start, vec, color, label, scale, show, width, pointy, wiping) end

---@param start? Vector | Delegate 
---@param vec? Vector | Delegate 
---@param color? RGBA | Delegate 
---@param label? string 
---@param scale? number 
---@param show? boolean 
---@param width? number 
---@param pointy? boolean 
---@param wiping? boolean 
---@return Vecdraw
---Both these two function names do the same thing.  For historical
---reasons both names exist, but now they both do the same thing.
---They create a new ``vecdraw`` object that you can then manipulate
---to show things on the screen.
---
---For an explanation what the parameters start, vec, color, label, scale, show,
---width, pointy, and wiping mean, they correspond to the same suffix names
---below in the table.
---
---Here are some examples::
---
---    SET anArrow TO VECDRAW(
---          V(0,0,0),
---          V(a,b,c),
---          RGB(1,0,0),
---          "See the arrow?",
---          1.0,
---          TRUE,
---          0.2,
---          TRUE,
---          TRUE
---        ).
---
---    SET anArrow TO VECDRAWARGS(
---          V(0,0,0),
---          V(a,b,c),
---          RGB(1,0,0),
---          "See the arrow?",
---          1.0,
---          TRUE,
---          0.2,
---          TRUE,
---          TRUE
---        ).
---
---Vector arrows can also be created with dynamic positioning and color.  To do
---this, instead of passing static values for the first three arguments of
---``VECDRAW()`` or ``VECDRAWARGS()``, you can pass a
---:ref:`Delegate <delegates>` for any of them, which returns a value of the
---correct type.  Here's an example where the Start, Vec, and Color are all
---dynamically adjusted by anonymous delegates that kOS will frequently call
---for you as it draws the arrow::
---
---    // Small dynamically moving vecdraw example:
---    SET anArrow TO VECDRAW(
---      { return (6-4*cos(100*time:seconds)) * up:vector. },
---      { return (4*sin(100*time:seconds)) * up:vector.  },
---      { return RGBA(1, 1, RANDOM(), 1). },
---      "Jumping arrow!",
---      1.0,
---      TRUE,
---      0.2,
---      TRUE,
---      TRUE
---    ).
---    wait 20. // Give user time to see it in motion.
---    set anArrow:show to false. // Make it stop drawing.
---
---In the above example, ``VECDRAW()`` detects that the first argument
---is a delegate, and it uses this information to decide to assign
---it into :attr:`VecDraw:STARTUPDATER`, instead of into :attr:`VecDraw:START`.
---Similarly it detects that the second argument is a delegate, so it
---assigns it into :attr:`VecDraw:VECUPDATER` instead of into :attr:`VecDraw:VEC`.
---And it does the same thing with the third argument, assigning it into
---:attr:`VecDraw:COLORUPDATER`, instead of :attr:`VecDraw:COLOR`.
---
---All the parameters of the ``VECDRAW()`` and ``VECDRAWARGS()`` are
---optional.  You can leave any of the lastmost parameters off and they
---will be given a default::
---
---    Set anArrow TO VECDRAW().
---
---Causes it to have these defaults:
---
---.. list-table:: Defaults
---        :header-rows: 1
---        :widths: 1 3
---
---        * - Suffix
---          - Default
---
---        * - :attr:`START`
---          - V(0,0,0)  (center of the ship is the origin)
---        * - :attr:`VEC`
---          - V(0,0,0)  (no length, so nothing appears)
---        * - :attr:`COLO[U]R`
---          - White
---        * - :attr:`LABEL`
---          - Empty string ""
---        * - :attr:`SCALE`
---          - 1.0
---        * - :attr:`SHOW`
---          - false
---        * - :attr:`WIDTH`
---          - 0.2
---        * - :attr:`POINTY`
---          - true
---        * - :attr:`WIPING`
---          - true
---
---Examples::
---
---    // Makes a red vecdraw at the origin, pointing 5 meters north,
---    // with defaults for the un-mentioned
---    // paramters LABEL, SCALE, SHOW, and WIDTH.
---    SET vd TO VECDRAW(V(0,0,0), 5*north:vector, red).
---
---To make a `VecDraw` disappear, you can either set its :attr:`VecDraw:SHOW` to false or just :ref:`UNSET <unset>` the variable, or re-assign it. An example using `VecDraw` can be seen in the documentation for :func:`POSITIONAT()`.
function vecdrawargs(start, vec, color, label, scale, show, width, pointy, wiping) end

---Sets all visible vecdraws to invisible, everywhere in this kOS CPU.
---This is useful if you have lost track of the handles to them and can't
---turn them off one by one, or if you don't have the variable scopes
---present anymore to access the variables that hold them.  The system
---does attempt to clear any vecdraws that go "out of scope", however
---the "closures" that keep local variables alive for LOCK statements
---and for other reasons can keep them from every truely going away
---in some circumstances.  To make the arrow drawings all go away, just call
---CLEARVECDRAWS() and it will have the same effect as if you had
---done ``SET varname:show to FALSE`` for all vecdraw varnames in the
---entire system.
function CLEARVECDRAWS() end

---If you want to conveniently clear away all GUI windows that you
---created from this CPU, you can do so with the ``CLEARGUIS()``
---built-in function.  It will call :meth:`GUI:HIDE` and :meth:`GUI:DISPOSE`
---for all the gui windows that were made using this particular CPU part.
---(If you have multiple kOS CPUs, and some GUIs are showing that were made
---by other kOS CPUs, those will not be cleared by this.)
---
---.. note::
---
---    This built-in function was added mainly so you have a way
---    to easily clean up after a program has crashed which left
---    behind some GUI windows that are now unresponsive because
---    the program isn't running anymore.
function clearguis() end

---@param width number 
---@param height? number 
---@return GUI
---This is the first place any GUI control panel starts.
---
---The GUI built-in function creates a new `GUI` object that you can then
---manipulate to build up a GUI. If no height is specified, it will resize
---automatically to fit the contents you put inside it.  The width can be set
---to 0 to force automatic width resizing too::
---
---        SET my_gui TO GUI(200).
---        SET button TO my_gui:ADDBUTTON("OK").
---        my_gui:SHOW().
---        UNTIL button:TAKEPRESS WAIT(0.1).
---        my_gui:HIDE().
---
---See the "ADD" functions in the `BOX` structure for
---the other widgets you can add.
---
---Warning: Setting BOTH width and height to 0 to let it choose automatic
---resizing in both dimensions will often lead to a look you won't like.
---You may find that to have some control over the layout you will need to
---specify one of the two dimensions and only let it resize the other.
function gui(width, height) end

---@param orbitable Orbitable A `Vessel`, `Body` or other `Orbitable` object
---@param time TimeStamp | number Time of prediction
---@return Vector
---:type orbitable:  `Orbitable`
---:type time:     `TimeStamp` or `number` universal seconds
---:return:        A position `Vector` expressed as the coordinates in the :ref:`ship-center-raw-rotation <ship-raw>` frame
---
---Returns a prediction of where the `Orbitable` will be at some :ref:`universal Time <universal_time>`. If the `Orbitable` is a `Vessel`, and the `Vessel` has planned :ref:`maneuver nodes <maneuver node>`, the prediction assumes they will be executed exactly as planned.
---
---*Refrence Frame:* The reference frame that the future position
---gets returned in is the same reference frame as the current position
---vectors use.  In other words it's in ship:raw coords where the origin
---is the current ``SHIP``'s center of mass.
---
---*Prerequisite:*  If you are in a career mode game rather than a
---sandbox mode game, This function requires that you have your space
---center's buildings advanced to the point where you can make maneuver
---nodes on the map view, as described in :struct:`Career:CANMAKENODES`.
function positionat(orbitable, time) end

---@param orbitable Orbitable A `Vessel`, `Body` or other `Orbitable` object
---@param time TimeStamp | number Time of prediction
---@return OrbitableVelocity
---:type orbitable:  `Orbitable`
---:type time:     `TimeStamp` or `number` universal seconds
---:return: An :ref:`ObitalVelocity <orbitablevelocity>` structure.
---
---Returns a prediction of what the :ref:`Orbitable's <orbitable>` velocity will be at some :ref:`universal Time <universal_time>`. If the `Orbitable` is a `Vessel`, and the `Vessel` has planned :struct:`maneuver nodes <Node>`, the prediction assumes they will be executed exactly as planned.
---
---*Prerequisite:*  If you are in a career mode game rather than a
---sandbox mode game, This function requires that you have your space
---center's buildings advanced to the point where you can make manuever
---nodes on the map view, as described in :struct:`Career:CANMAKENODES`.
---
---*Refrence Frame:* The reference frame that the future velocity gets
---returned in is the same reference frame as the current velocity
---vectors use.  In other words it's relative to the ship's CURRENT
---body it's orbiting just like ``ship:velocity`` is.  For example,
---if the ship is currently in orbit of Kerbin, but will be in the Mun's
---SOI in the future, then the ``VELOCITYAT`` that future time will return
---is still returned relative to Kerbin, not the Mun, because that's the
---current reference for current velocities.  Here is an example
---illustrating that::
---
---    // This example imagines you are on an orbit that is leaving
---    // the current body and on the way to transfer to another orbit:
---
---    // Later_time is 1 minute into the Mun orbit patch:
---    local later_time is time:seconds + ship:obt:NEXTPATCHETA + 60.
---    local later_ship_vel is VELOCITYAT(ship, later_time):ORBIT.
---    local later_body_vel is VELOCITYAT(ship:obt:NEXTPATCH:body, later_time):ORBIT.
---
---    local later_ship_vel_rel_to_later_body is later_ship_vel - later_body_vel.
---
---    print "My later velocity relative to this body is: " + later_ship_vel.
---    print "My later velocity relative to the body I will be around then is: " +
---      later_ship_vel_rel_to_later_body.
function velocityat(orbitable, time) end

---@param p Part | List | Element 
---@param c RGBA 
---@return HIGHLIGHT
---This global function creates a part highlight::
---
---    SET foo TO HIGHLIGHT(p,c).
---
---where:
---
---``p``
---    A single :ref:`part <part>`, a list of parts or an :ref:`element <element>`
---``c``
---    A :ref:`color <color>`
function highlight(p, c) end

---@param orbitable Orbitable A :Ref:`Vessel <vessel>`, `Body` or other `Orbitable` object
---@param time TimeStamp | number Time of prediction
---@return Orbit
---:type orbitable:  `Orbitable`
---:type time:     `TimeStamp` or `number` universal seconds
---:return: An `Orbit` structure.
---
---Returns the :ref:`Orbit patch <orbit>` where the `Orbitable` object is predicted to be at some :ref:`universal Time <universal_time>`. If the `Orbitable` is a `Vessel`, and the `Vessel` has planned :ref:`maneuver nodes <maneuver node>`, the prediction assumes they will be executed exactly as planned.
---
---*Prerequisite:*  If you are in a career mode game rather than a
---sandbox mode game, This function requires that you have your space
---center's buildings advanced to the point where you can make maneuver
---nodes on the map view, as described in :struct:`Career:CANMAKENODES`.
function orbitat(orbitable, time) end

---@return Career
function career() end

---@return List
---:return: `List` of `Waypoint`
---
---This creates a `List` of `Waypoint` structures for all accepted contracts.  Waypoints for proposed contracts you haven't accepted yet do not appear in the list.
function allwaypoints() end

---@param name string (`string`) Name of the waypoint as it appears on the map or in the contract description
---@return Waypoint
---This creates a new Waypoint from a name of a waypoint you read from the contract paramters.  Note that this only works on contracts you've accpted.  Waypoints for proposed contracts haven't accepted yet  do not actually work in kOS.
---
---SET spot TO WAYPOINT("herman's folly beta").
---
---The name match is case-insensitive.
function waypoint(name) end

---@param resourceName string 
---@param from Part | Element | List 
---@param to Part | Element | List 
---@return Transfer
function transferall(resourceName, from, to) end

---@param resourceName string 
---@param from Part | Element | List 
---@param to Part | Element | List 
---@param amount number 
---@return Transfer
function transfer(resourceName, from, to, amount) end

---Clears the screen and places the cursor at the top left
function clearscreen() end

---@param text string The message to show to the user on screen
---@param delay number How long to make the message remain onscreen before it goes away
---@param style number Where to show the message on the screen:; - 1 = upper left; - 2 = upper center; - 3 = upper right; - 4 = lower center
---@param size number 
---@param colour RGBA 
---@param doEcho boolean 
---You can make text messages appear on the heads-up display, in the
---same way that the in-game stock messages appear, by calling the
---HUDTEXT function, as follows::
---
---HUDTEXT( string Message, 
---         integer delaySeconds,
---         integer style,
---         integer size,
---         RGBA colour,
---         boolean doEcho).
---
---Message
---  The message to show to the user on screen
---delaySeconds
---  How long to make the message remain onscreen before it goes away.
---  If another message is drawn while an old message is still displaying,
---  both messages remain, the new message scrolls up the old message.
---style
---  Where to show the message on the screen:
---  - 1 = upper left
---  - 2 = upper center
---  - 3 = upper right
---  - 4 = lower center
---  Note that all these locations have their own defined slightly
---  different fonts and default sizes, enforced by the stock KSP game.
---size
---  A number describing the font point size: NOTE that the actual size
---  varies depending on which of the above styles you're using.  Some
---  of the locations have a magnifying factor attached to their fonts.
---colour
---  The colour to show the text in, using `one of the built-in colour names
---  or the RGB constructor to make one up <../structures/misc/colors.html>`__
---doEcho
---  If true, then the message is also echoed to the terminal as "HUD: message".
---
---Examples::
---
---  HUDTEXT("Warning: Vertical Speed too High", 5, 2, 15, red, false).
---  HUDTEXT("docking mode begun", 8, 1, 12, rgb(1,1,0.5), false).
function hudtext(text, delay, style, size, colour, doEcho) end

---Activates the next stage if the cpu vessel is the active vessel.  This will
---trigger engines, decouplers, and any other parts that would normally be
---triggered by manually staging.  The default equivalent key binding is the
---space bar.  As with other parameter-less functions, both ``STAGE.`` and
---``STAGE().`` are acceptable ways to call the function.
---
---.. note::
---    .. versionchanged:: 1.0.1
---
---        The stage function will automatically pause execution until the next
---        tick.  This is because some of the results of the staging event take
---        effect immediately, while others do not update until the next time
---        that physics are calculated.  Calling ``STAGE.`` is essentially
---        equivalent to::
---
---            STAGE.
---            WAIT 0.
---
---.. warning::
---    Calling the :global:`Stage` function on a vessel other than the active
---    vessel will throw an exception.
function stage() end

---@param node Node 
---To put a maneuver node into the flight plan of the current :ref:`CPU vessel <cpu vessel>` (i.e. ``SHIP``), just :global:`ADD` it like so::
---
---    SET myNode to NODE( TIME:SECONDS+200, 0, 50, 10 ).
---    ADD myNode.
---
---You should immediately see it appear on the map view when you do this. The :global:`ADD` command can add nodes anywhere within the flight plan. To insert a node earlier in the flight than an existing node, simply give it a smaller :attr:`ETA <ManeuverNode:ETA>` time and then :global:`ADD` it.
---
---.. warning::
---    As per the warning above at the top of the section, ADD won't work on vessels that are not the active vessel.
function add(node) end

---@param node Node 
---To remove a maneuver node from the flight path of the current :ref:`CPU vessel <cpu vessel>` (i.e. ``SHIP``), just :global:`REMOVE` it like so::
---
---    REMOVE myNode.
---
---.. warning::
---    As per the warning above at the top of the section, REMOVE won't work on vessels that are not the active vessel.
function remove(node) end

---@param timestamp number 
---This is identical to :meth:`WARPTO<TimeWarp:WARPTO>` above.
---::
---
---    // These two do the same thing:
---    WARPTO(time:seconds + 60*60). // warp 1 hour into the future.
---    KUNIVERSE:TIMEWARP:WARPTO(time:seconds + 60*60).
function warpto(timestamp) end

---@param volumeOrNameTag Volume | string (`Volume` | `String`) can be either an instance of `Volume` or a string
---@return KOSProcessor
---Depending on the type of the parameter value will either return the processor associated with the given
---`Volume` or the processor with the given name tag.
function processor(volumeOrNameTag) end

---@param file VolumeItem | string 
---@param volumeOrNameTag Volume | string 
function runfileon(file, volumeOrNameTag) end

---@param command string 
---@param volumeOrNameTag Volume | string 
function runcommandon(command, volumeOrNameTag) end

---@param listType string | "bodies" | "targets" | "resources" | "parts" | "engines" | "rcs" | "sensors" | "elements" | "dockingports" | "files" | "volumes" | "processors" | "fonts" 
---Display elements of the specified "listType" argument
function printlist(listType) end

---@param v1 Vector (`Vector`)
---@param v2 Vector (`Vector`)
---@return Vector
---:return: The `vector cross-product <https://mathinsight.org/cross_product>`__
---The vector `cross product <https://mathinsight.org/cross-product/>`__ of two vectors in the order ``(v1,v2)`` returning a new `Vector`::
---
---    SET vec1 TO V(1,2,3).
---    SET vec2 TO V(2,3,4).
---
---    // These will both print: V(-1,2,-1)
---    PRINT VCRS(vec1, vec2).
---    PRINT VECTORCROSSPRODUCT(vec1, vec2).
---
---When visualizing the direction that a vector cross product will
---point, remember that KSP is using a :ref:`left-handed <left-handed>`
---coordinate system, and this means a cross-product of two vectors
---will point in the opposite direction of what it would had KSP been
---using a right-handed coordinate system.
function vcrs(v1, v2) end

---@param v1 Vector (`Vector`)
---@param v2 Vector (`Vector`)
---@return Vector
---:return: The `vector cross-product <https://mathinsight.org/cross_product>`__
---The vector `cross product <https://mathinsight.org/cross-product/>`__ of two vectors in the order ``(v1,v2)`` returning a new `Vector`::
---
---    SET vec1 TO V(1,2,3).
---    SET vec2 TO V(2,3,4).
---
---    // These will both print: V(-1,2,-1)
---    PRINT VCRS(vec1, vec2).
---    PRINT VECTORCROSSPRODUCT(vec1, vec2).
---
---When visualizing the direction that a vector cross product will
---point, remember that KSP is using a :ref:`left-handed <left-handed>`
---coordinate system, and this means a cross-product of two vectors
---will point in the opposite direction of what it would had KSP been
---using a right-handed coordinate system.
function vectorcrossproduct(v1, v2) end

---@param v1 Vector (`Vector`)
---@param v2 Vector (`Vector`)
---@return number
---:return: The `vector dot-product <https://mathinsight.org/dot_product>`__
---This is the `dot product <https://mathinsight.org/dot_product>`__ of two vectors returning a scalar number. This is the same as :ref:`v1 * v2 <Vector *>`::
---
---    SET vec1 TO V(1,2,3).
---    SET vec2 TO V(2,3,4).
---
---    // These are different ways to perform the same operation.
---    // All of them will print the value: 20
---    // -------------------------------------------------------
---    PRINT VDOT(vec1, vec2).
---    PRINT VECTORDOTPRODUCT(vec1, vec2).
---    PRINT vec1 * vec2. // multiplication of two vectors with asterisk "*" performs a VDOT().
function vdot(v1, v2) end

---@param v1 Vector (`Vector`)
---@param v2 Vector (`Vector`)
---@return number
---:return: The `vector dot-product <https://mathinsight.org/dot_product>`__
---This is the `dot product <https://mathinsight.org/dot_product>`__ of two vectors returning a scalar number. This is the same as :ref:`v1 * v2 <Vector *>`::
---
---    SET vec1 TO V(1,2,3).
---    SET vec2 TO V(2,3,4).
---
---    // These are different ways to perform the same operation.
---    // All of them will print the value: 20
---    // -------------------------------------------------------
---    PRINT VDOT(vec1, vec2).
---    PRINT VECTORDOTPRODUCT(vec1, vec2).
---    PRINT vec1 * vec2. // multiplication of two vectors with asterisk "*" performs a VDOT().
function vectordotproduct(v1, v2) end

---@param v1 Vector 
---@param v2 Vector 
---@return Vector
---This is a vector, ``v2`` with all of ``v1`` excluded from it. In other words, the projection of ``v2`` onto the plane that is normal to ``v1``.
function vxcl(v1, v2) end

---@param v1 Vector 
---@param v2 Vector 
---@return Vector
---This is a vector, ``v2`` with all of ``v1`` excluded from it. In other words, the projection of ``v2`` onto the plane that is normal to ``v1``.
function vectorexclude(v1, v2) end

---@param v1 Vector (`Vector`)
---@param v2 Vector (`Vector`)
---@return number
---:return: Angle between two vectors
---This returns the angle between v1 and v2. It is the same result as:
---
---.. math::
---
---    \arccos\left(
---        \frac{
---            \vec{v_1}\cdot\vec{v_2}
---        }{
---            \left|\vec{v_1}\right|\cdot\left|\vec{v_2}\right|
---        }
---    \right)
---
---or in **KerboScript**::
---
---    arccos( (VDOT(v1,v2) / (v1:MAG * v2:MAG) ) )
function vang(v1, v2) end

---@param v1 Vector (`Vector`)
---@param v2 Vector (`Vector`)
---@return number
---:return: Angle between two vectors
---This returns the angle between v1 and v2. It is the same result as:
---
---.. math::
---
---    \arccos\left(
---        \frac{
---            \vec{v_1}\cdot\vec{v_2}
---        }{
---            \left|\vec{v_1}\right|\cdot\left|\vec{v_2}\right|
---        }
---    \right)
---
---or in **KerboScript**::
---
---    arccos( (VDOT(v1,v2) / (v1:MAG * v2:MAG) ) )
function vectorangle(v1, v2) end

---@param volumeIdOrName? number | string 
---@return Volume
---Will return a `Volume` structure representing the volume with a given
---id or name. You can omit the argument to create a `Volume`
---for the current volume.
function volume(volumeIdOrName) end

---@param path string | VolumeItem | Volume 
---@return Path
---Will create a `Path` structure representing the given path string. You
---can omit the argument to create a `Path` for the current directory.
function path(path) end

---@param ...  
---@return List
---Creates a List structure with arguments as elements
function list(...) end

---@param ...  
---@return Lexicon
---Creates a Lexicon structure with odd arguments as keys and even arguments as values
function lex(...) end

---@param ...  
---@return Lexicon
---Creates a Lexicon structure with odd arguments as keys and even arguments as values
function lexicon(...) end

---@param ...  
---@return Queue
---Creates a Queue structure with arguments as elements
function queue(...) end

---@param ...  
---@return UniqueSet
---Creates a UniqueSet structure with arguments as elements
function uniqueset(...) end

---@param kp number 
---@param ki number 
---@param kd number 
---@param minOutput number 
---@param maxOutput number 
---@param epsilon number 
---@return PIDLoop
---Creates a new PIDLoop
function pidloop(kp, ki, kd, minOutput, maxOutput, epsilon) end

---@param kp number 
---@param ki number 
---@param kd number 
---@param minOutput number 
---@param maxOutput number 
---@return PIDLoop
---Creates a new PIDLoop
function pidloop(kp, ki, kd, minOutput, maxOutput) end

---@param kp number 
---@param ki number 
---@param kd number 
---@return PIDLoop
---Creates a new PIDLoop
function pidloop(kp, ki, kd) end

---@param kp number 
---@return PIDLoop
---Creates a new PIDLoop
function pidloop(kp) end

---@return PIDLoop
---Creates a new PIDLoop
function pidloop() end

---@param ...  
---@return Stack
---Creates a Stack structure with arguments as elements
function stack(...) end

---@param volume Volume | number | string 
---Changes the current directory to the root directory of the specified volume.
---Volumes can be referenced by instances of `Volume`, their ID numbers
---or their names if they've been given one. Understanding how
---:ref:`volumes work <volumes>` is important to understanding this command.
function switch(volume) end

---@param path string | VolumeItem | Volume 
---Changes the current directory to the one pointed to by the :code:`PATH`
---argument. This command will fail if the path is invalid or does not point
---to an existing directory.
function cd(path) end

---@param path string | VolumeItem | Volume 
---Changes the current directory to the one pointed to by the :code:`PATH`
---argument. This command will fail if the path is invalid or does not point
---to an existing directory.
function chdir(path) end

---@param fromPath string | VolumeItem | Volume 
---@param toPath string | VolumeItem | Volume 
---@return boolean
---Copies the file or directory pointed to by :code:`FROMPATH` to the location
---pointed to :code:`TOPATH`. Depending on what kind of items both paths point
---to the exact behaviour of this command will differ:
---
---1. :code:`FROMPATH` points to a file
---
---   - :code:`TOPATH` points to a directory
---
--- The file from :code:`FROMPATH` will be copied to the directory.
---
---   - :code:`TOPATH` points to a file
---
--- Contents of the file pointed to by :code:`FROMPATH` will overwrite
--- the contents of the file pointed to by :code:`TOPATH`.
---
---   - :code:`TOPATH` points to a non-existing path
---
--- New file will be created at :code:`TOPATH`, along with any parent
--- directories if necessary. Its contents will be set to the contents of
--- the file pointed to by :code:`FROMPATH`.
---
---2. :code:`FROMPATH` points to a directory
---
---   If :code:`FROMPATH` points to a directory kOS will copy recursively all
---   contents of that directory to the target location.
---
---   - :code:`TOPATH` points to a directory
---
--- The directory from :code:`FROMPATH` will be copied inside the
--- directory pointed to by :code:`TOPATH`.
---
---   - :code:`TOPATH` points to a file
---
--- The command will fail.
---
---   - :code:`TOPATH` points to a non-existing path
---
--- New directory will be created at :code:`TOPATH`, along with any
--- parent directories if necessary. Its contents will be set to the
--- contents of the directory pointed to by :code:`FROMPATH`.
---
---3. :code:`FROMPATH` points to a non-existing path
---
---   The command will fail.
function copypath(fromPath, toPath) end

---@param fromPath string | VolumeItem | Volume 
---@param toPath string | VolumeItem | Volume 
---@return boolean
---Moves the file or directory pointed to by :code:`FROMPATH` to the location
---pointed to :code:`TOPATH`. Depending on what kind of items both paths point
---to the exact behaviour of this command will differ, see :code:`COPYPATH` above.
function movepath(fromPath, toPath) end

---@param path string | VolumeItem 
---@return boolean
---Deleted the file or directory pointed to by :code:`FROMPATH`. Directories are
---removed along with all the items they contain.
function deletepath(path) end

---@param object  
---@param path string | VolumeItem | Volume 
---@return VolumeFile
---Serializes the given object to JSON format and saves it under the given path.
---
---Go to :ref:`Serialization page <serialization>` to read more about serialization.
function writejson(object, path) end

---@param path string | VolumeItem 
---Reads the contents of a file previously created using ``WRITEJSON`` and deserializes them.
---
---Go to :ref:`Serialization page <serialization>` to read more about serialization.
function readjson(path) end

---@param path string | VolumeItem | Volume 
---@return boolean
---Returns true if there exists a file or a directory under the given path,
---otherwise returns false. Also see :meth:`Volume:EXISTS`.
function exists(path) end

---@param path string | VolumeItem | Volume 
---Will return a `VolumeFile` or `VolumeDirectory` representing the item
---pointed to by :code:`PATH`. It will return a `boolean` false if there's
---nothing present under the given path. Also see :meth:`Volume:OPEN`.
function open(path) end

---@param path string | VolumeItem 
---@return VolumeFile
---Creates a file under the given path. Will create parent directories if needed.
---It will fail if a file or a directory already exists under the given path.
---Also see :meth:`Volume:CREATE`.
function create(path) end

---@param path string | VolumeItem 
---@return VolumeDirectory
---Creates a directory under the given path. Will create parent directories
---if needed. It will fail if a file or a directory already exists under the
---given path. Also see :meth:`Volume:CREATEDIR`.
function createdir(path) end

---@param a number (deg) angle
---@return number
---:return: sine of the angle
---
---::
---
---    PRINT SIN(6). // prints 0.10452846326
function sin(a) end

---@param a number (deg) angle
---@return number
---:return: cosine of the angle
---
---::
---
---    PRINT COS(6). // prints 0.99452189536
function cos(a) end

---@param a number (deg) angle
---@return number
---:return: tangent of the angle
---
---::
---
---    PRINT TAN(6). // prints 0.10510423526
function tan(a) end

---@param x number (`number`)
---@return number
---:return: (deg) angle whose sine is x
---
---::
---
---    PRINT ARCSIN(0.67). // prints 42.0670648
function arcsin(x) end

---@param x number (`number`)
---@return number
---:return: (deg) angle whose cosine is x
---
---::
---
---    PRINT ARCCOS(0.67). // prints 47.9329352
function arccos(x) end

---@param x number (`number`)
---@return number
---:return: (deg) angle whose tangent is x
---
---::
---
---    PRINT ARCTAN(0.67). // prints 33.8220852
function arctan(x) end

---@param y number (`number`)
---@param x number (`number`)
---@return number
---:return: (deg) angle whose tangent is :math:`\frac{y}{x}`
---
---::
---
---    PRINT ARCTAN2(0.67, 0.89). // prints 36.9727625
---
---The two parameters resolve ambiguities when taking the arctangent. See the `wikipedia page about atan2 <http://en.wikipedia.org/wiki/Atan2>`_ for more details.
function arctan2(y, x) end

---@param a1 number 
---@param a2 number 
---@return number
---Gets the minimal angle between two angles
function anglediff(a1, a2) end

---@param from number 
---@param to number 
---@param step number 
---@return Range
function range(from, to, step) end

---@param from number 
---@param to number 
---@return Range
function range(from, to) end

---@param to number 
---@return Range
function range(to) end

---@param item  
---@param column number 
---@param row number 
function printat(item, column, row) end

---@param paramName string | "steering" | "throttle" | "wheelsteering" | "wheelthrottle" | "flightcontrol" 
---@param enabled boolean 
function toggleflybywire(paramName, enabled) end

---@param mode string | "maneuver" | "prograde" | "retrograde" | "normal" | "antinormal" | "radialin" | "radialout" | "target" | "antitarget" | "stability" | "stabilityassist" 
function selectautopilotmode(mode) end

---@param value  
---@param path VolumeFile | string 
function logfile(value, path) end

---Reboots the core
function reboot() end

---Shutsdown the core
function shutdown() end

---@param milliseconds number 
---Deliberately cause physics lag by making the main game thread sleep.
function debugfreezegame(milliseconds) end

---@param a number 
---@return number
---Returns absolute value of input::
---
---    PRINT ABS(-1). // prints 1
function abs(a) end

---@param a number 
---@param b number 
---@return number
---Returns remainder from integer division.
---Keep in mind that it's not a traditional mathematical Euclidean division where the result is always positive. The result has the same absolute value as mathematical modulo operation but the sign is the same as the sign of dividend::
---
---    PRINT MOD(21,6). // prints 3
---    PRINT MOD(-21,6). // prints -3
function mod(a, b) end

---@param a number 
---@return number
---Rounds down to the nearest whole number::
---
---    PRINT FLOOR(1.887). // prints 1
function floor(a) end

---@param a number 
---@param b number 
---@return number
---Rounds down to the nearest place value::
---
---    PRINT CEILING(1.887,2). // prints 1.88
function floor(a, b) end

---@param a number 
---@return number
---Rounds up to the nearest whole number::
---
---    PRINT CEILING(1.887). // prints 2
function ceiling(a) end

---@param a number 
---@param b number 
---@return number
---Rounds up to the nearest place value::
---
---    PRINT CEILING(1.887,2). // prints 1.89
function ceiling(a, b) end

---@param a number 
---@return number
---Rounds to the nearest whole number::
---
---    PRINT ROUND(1.887). // prints 2
function round(a) end

---@param a number 
---@param b number 
---@return number
---Rounds to the nearest place value::
---
---    PRINT ROUND(1.887,2). // prints 1.89
function round(a, b) end

---@param a number 
---@return number
---Returns square root::
---
---    PRINT SQRT(7.89). // prints 2.80891438103763
function sqrt(a) end

---@param a number 
---@return number
---Gives the natural log of the provided number::
---
---    PRINT LN(2). // prints 0.6931471805599453
function ln(a) end

---@param a number 
---@return number
---Gives the log base 10 of the provided number::
---
---    PRINT LOG10(2). // prints 0.30102999566398114
function log10(a) end

---@param a number 
---@param b number 
---@return number
---Returns The lower of the two values::
---
---    PRINT MIN(0,100). // prints 0
function min(a, b) end

---@param a number 
---@param b number 
---@return number
---Returns The higher of the two values::
---
---    PRINT MAX(0,100). // prints 100
function max(a, b) end

---@param key?  
---@return number
---Returns the next random floating point number from a random
---number sequence.  The result is always in the range [0..1]
---
---This uses what is called a `pseudo-random number generator
---<https://en.wikipedia.org/wiki/Pseudorandom_number_generator>`_.
---
---For basic usage you can leave the ``key`` parameter off and it
---works fine, like so:
---
---Example, basic usage::
---
---    PRINT RANDOM(). //prints a random number
---    PRINT "Let's roll a 6-sided die 10 times:".
---    FOR n in range(0,10) {
---
---      // To make RANDOM give you an integer in the range [0..n-1], you do this:
---      // floor(n*RANDOM()).
---
---      // So for example : a die giving values from 1 to 6 is like this:
---      print (1 + floor(6*RANDOM())).
---    }
---
---The parameter ``key`` is a string, and it's used when you want
---to track separate psuedo-random number sequences by name and 
---have them be deterministically repeatable. *Like other
---string keys in kOS, this key is case-insensitive.*
---
---* If you leave the parameter ``key`` off, you get the next number
---  from a default unnamed random number sequencer.
---* If you supply the parameter ``key``, you get the next number
---  from a named random number sequencer.  You can invent however
---  many keys you like and each one is a new random number sequencer.
---  Supplying a key probably only means something if you have
---  previously used :func:`RANDOMSEED(key, seed)`.
---
---The following example is more complex and shows the repeatability
---of the "random" sequence using seeds.  For most simple uses you
---probably don't need to bother with this.  If words like "random
---number seed" are confusing, you can probably skip this part and 
---get by just fine with the basic usage shown above.  (Explaining
---how pseudorandom number generators work is a bit beyond this
---page - check the wikipedia link above to learn more.)
---
---Example, deterministic usage::
---
---    // create two different random number sequencers, both starting
---    // with seed 12345 so they should have the same exact values.
---    RANDOMSEED("sequence1",12345).
---    RANDOMSEED("sequence2",12345).
---
---    PRINT "5 coin flips from SEQUENCE 1:".
---    FOR n in range(0,5) {
---      print choose "heads" if RANDOM("sequence1") < 0.5 else "tails".
---    }
---
---    PRINT "5 coin flips from SEQUENCE 2, which should be the same:".
---    FOR n in range(0,5) {
---      print choose "heads" if RANDOM("sequence2") < 0.5 else "tails".
---    }
---
---    PRINT "5 more coin flips from SEQUENCE 1:".
---    FOR n in range(0,5) {
---      print choose "heads" if RANDOM("sequence1") < 0.5 else "tails".
---    }
---
---    PRINT "5 more coin flips from SEQUENCE 2, which should be the same:".
---    FOR n in range(0,5) {
---      print choose "heads" if RANDOM("sequence2") < 0.5 else "tails".
---    }
function random(key) end

---@param key  
---@param seed number 
---No Return Value.
---
---Initializes a new random number sequence from a seed, giving it a
---key name you can use to refer to it in future calls to :func:`RANDOM(key)`
---
---Using this you can make psuedo-random number sequences that can be
---re-run using the same seed to get the same result a second time.
---
---Parameter ``key`` is a string - a name you can use to refer to this
---random series later.  Calls to ``RANDOMSEED`` that use different
---keys actually cause different new random number sequences to be
---created that are tracked separately from each other. *Like other
---string keys in kOS, this key is case-insensitive.*
---
---Parameter ``seed`` is an integer - an initial value to cause a
---deterministic series of random numbers to come out of the random
---function.
---
---Whenever you call ``RANDOMSEED(key, seed)``, it starts a new
---random number sequence using the integer seed you give it, and names
---that sequence with a string key you can use later to retrive
---values from that random number sequence.
---
---Example::
---
---  RANDOMSEED("generator A",1000).
---  RANDOMSEED("generator B",1000).
---  PRINT "Generators A and B should emit identical ".
---  PRINT "sequences because they both started at seed 1000.".
---  PRINT "3 numbers from Generator A:".
---  PRINT floor(RANDOM("generator A")*100).
---  PRINT floor(RANDOM("generator A")*100).
---  PRINT floor(RANDOM("generator A")*100).
---  PRINT "3 numbers from Generator B - they should ".
---  PRINT "be the same as above:".
---  PRINT floor(RANDOM("generator B")*100).
---  PRINT floor(RANDOM("generator B")*100).
---  PRINT floor(RANDOM("generator B")*100).
---
---  PRINT "Resetting generator A but not Generator B:".
---  RANDOMSEED("generator A",1000).
---
---  PRINT "3 more numbers from Generator A which got reset".
---  PRINT "so they should match the first ones again:".
---  PRINT floor(RANDOM("generator A")*100).
---  PRINT floor(RANDOM("generator A")*100).
---  PRINT floor(RANDOM("generator A")*100).
---  PRINT "3 numbers from Generator B, which didn't get reset:".
---  PRINT floor(RANDOM("generator B")*100).
---  PRINT floor(RANDOM("generator B")*100).
---  PRINT floor(RANDOM("generator B")*100).
---
---
---If you call ``RANDOMSEED`` using the same key as a key you already used
---before, it just forgets the previous random number sequence and starts
---a new one using the new seed.  You can use this to reset the sequence.
function randomseed(key, seed) end

---@param a number (number)
---@return string
---:return: (string) single-character string containing the unicode character specified
---
---::
---
---    PRINT CHAR(34) + "Apples" + CHAR(34). // prints "Apples"
function char(a) end

---@param a string (string)
---@return number
---:return: (number) unicode number representing the character specified
---
---::
---
---    PRINT UNCHAR("A"). // prints 65
function unchar(a) end

