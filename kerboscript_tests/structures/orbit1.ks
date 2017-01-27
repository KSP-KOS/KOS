// useful to debug:
//
//    set o to orbit(x, v, body, time:seconds).
//    o:positionat(time:seconds + dt).
//    o:velocityat(time:seconds + dt).
//
// creates a keosynchronous orbit of an object directly above the equatorial coordinates of
// KSC and checks the position at the current time and 1/4 way around the orbit.


wait until ship:unpacked.
CORE:PART:GETMODULE("kOSProcessor"):DOEVENT("Open Terminal").

local sidereal_day to 5*3600 + 59*60 + 9.4.

// x0, v0 are what we use to build the orbit
local x0 to latlng(0, -75.08333333333333333332):position - ship:body:position.
set x0:mag to (sqrt(kerbin:mu) * sidereal_day / (2 * constant:pi))^(2/3).  // 3463331.36.
local v0 to vcrs(kerbin:angularvel, x0).
set v0:mag to sqrt(kerbin:mu / x0:mag).

local keosynch to orbit(x0 + ship:body:position, v0, ship:body, time:seconds).

print "sma    : " + keosynch:semimajoraxis.
print "ecc    : " + keosynch:eccentricity.
print "lan    : " + keosynch:lan.
print "arg    : " + keosynch:argumentofperiapsis.
print "period : " + keosynch:period.

// x1, v1 should recover the same, while x2, v2 should be 1/4 around the orbit
local xnow to keosynch:position - ship:body:position.
local vnow to keosynch:velocity:orbit.
local x1 to keosynch:positionat(time:seconds) - ship:body:position.
local v1 to keosynch:velocityat(time:seconds):orbit.
local x2 to keosynch:positionat(time:seconds + sidereal_day/4) - ship:body:position.
local v2 to keosynch:velocityat(time:seconds + sidereal_day/4):orbit.

// this should recover the same orbit
local keosynch2 to orbit(keosynch:positionat(time:seconds + 100), keosynch:velocityat(time:seconds + 100):orbit, ship:body, time:seconds + 100).

// should be the same as xnow/x1
local x3 to keosynch2:positionat(time:seconds) - ship:body:position.
local v3 to keosynch2:velocityat(time:seconds):orbit.


print "x1 : " + x1:mag.
print "v1 : " + v1:mag.
print "x2 : " + x2:mag.
print "v2 : " + v2:mag.

// some handy vectors for debugging in case something goes wrong.
set xvec0 to vecdraw(ship:body:position, x0, rgb(1,0,0), "x0", 1.0, true, 0.2).
set vvec0 to vecdraw(ship:body:position + x0, 1000 * v0, rgb(1,0,0), "v0", 1.0, true, 0.2).
set xvec1 to vecdraw(ship:body:position, x1, rgb(1,0,0), "x1", 1.0, true, 0.2).
set vvec1 to vecdraw(ship:body:position + x1, 1000 * v1, rgb(1,0,0), "v1", 1.0, true, 0.2).
set xvec2 to vecdraw(ship:body:position, x2, rgb(0,1,0), "x2", 1.0, true, 0.2).
set vvec2 to vecdraw(ship:body:position + x2, 1000 * v2, rgb(0,1,0), "v2", 1.0, true, 0.2).

// assertions

if abs(keosynch:period - sidereal_day) > 1
  exit.
if abs(keosynch:eccentricity) > 0.000001
  exit.
if abs(keosynch:semimajoraxis - 3463331.36) > 0.1
  exit.

if abs(keosynch2:period - sidereal_day) > 1
  exit.
if abs(keosynch2:eccentricity) > 0.000001
  exit.
if abs(keosynch2:semimajoraxis - 3463331.36) > 0.1
  exit.

if abs(x0:mag - 3463331.36) > 0.1
  exit.
if abs(x1:mag - 3463331.36) > 0.1
  exit.
if abs(x2:mag - 3463331.36) > 0.1
  exit.
if abs(x3:mag - 3463331.36) > 0.1
  exit.

if abs(v0:mag - 1009.81) > 0.1
  exit.
if abs(v1:mag - 1009.81) > 0.1
  exit.
if abs(v2:mag - 1009.81) > 0.1
  exit.
if abs(v3:mag - 1009.81) > 0.1
  exit.

// these all recover the same vector at time:seconds ("now")
if (x1 - xnow):mag > 1
  exit.
if (v1 - vnow):mag > 1
  exit.
if (x1 - x0):mag > 1
  exit.
if (v1 - v0):mag > 1
  exit.
if (x1 - x3):mag > 1
  exit.
if (v1 - v3):mag > 1
  exit.

// x2 should point in the v1 direction
if abs(3463331.36 - vdot(x2, v1:normalized)) > 0.1
  exit.

// v2 should point in the -x1 direction
if abs(1009.81 - vdot(v2, -x1:normalized)) > 0.1
  exit.

// all vectors should be normal to the angular vel of kerbin's rotation
if vdot(x1:normalized, kerbin:angularvel:normalized) > 0.001
  exit.
if vdot(v1:normalized, kerbin:angularvel:normalized) > 0.001
  exit.
if vdot(x2:normalized, kerbin:angularvel:normalized) > 0.001
  exit.
if vdot(v2:normalized, kerbin:angularvel:normalized) > 0.001
  exit.
if vdot(x3:normalized, kerbin:angularvel:normalized) > 0.001
  exit.
if vdot(v3:normalized, kerbin:angularvel:normalized) > 0.001
  exit.
