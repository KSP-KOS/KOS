// useful to debug:
//
//    set o to orbit(x, v, body, time:seconds).
//    o:positionat(time:seconds + dt).
//    o:velocityat(time:seconds + dt).
//
// creates a keosynchronous orbit of an object directly above the equatorial coordinates of
// KSC and checks the position at the current time and 1/4 way around the orbit.

// x0, v0 are what we use to build the orbit
set x0 to latlng(0, -75.08333333333333333332):position - ship:body:position.
set x0:mag to 3463334.06.
set v0 to vcrs(kerbin:angularvel, x0).
set v0:mag to sqrt(kerbin:mu / x0:mag).

set keosynch to orbit(x0, v0, "kerbin", TIME:SECONDS).

print "sma    : " + keosynch:semimajoraxis.
print "ecc    : " + keosynch:eccentricity.
print "lan    : " + keosynch:lan.
print "arg    : " + keosynch:argumentofperiapsis.
print "period : " + keosynch:period.

set sidereal_day to 5*3600 + 59*60 + 9.4.

// x1, v1 should recover the same, while x2, v2 should be 1/4 around the orbit
set x1 to keosynch:positionat(time:seconds) - ship:body:position.
set v1 to keosynch:velocityat(time:seconds):orbit.
set x2 to keosynch:positionat(time:seconds + sidereal_day/4) - ship:body:position.
set v2 to keosynch:velocityat(time:seconds + sidereal_day/4):orbit.

print "x1 : " + x1:mag.
print "v1 : " + v1:mag.
print "x2 : " + x2:mag.
print "v2 : " + v2:mag.

// some handy vectors for debugging in case something goes wrong.
set xvec1 to vecdraw(ship:body:position, x1, rgb(1,0,0), "x1", 1.0, true, 0.2).
set vvec1 to vecdraw(ship:body:position + x1, 1000 * v1, rgb(1,0,0), "v1", 1.0, true, 0.2).
set xvec2 to vecdraw(ship:body:position, x2, rgb(0,1,0), "x2", 1.0, true, 0.2).
set vvec2 to vecdraw(ship:body:position + x2, 1000 * v2, rgb(0,1,0), "v2", 1.0, true, 0.2).

// assertions

if abs(keosynch:period - sidereal_day) > 1
  exit.

if abs(keosynch:eccentricity) > 0.000001
  exit.

if abs(keosynch:semimajoraxis - 3463334.06) > 0.1
  exit.

if abs(x0:mag - 3463334.06) > 0.1
  exit.
if abs(x1:mag - 3463334.06) > 0.1
  exit.
if abs(x2:mag - 3463334.06) > 0.1
  exit.

if abs(v0:mag - 1009.80) > 0.1
  exit.
if abs(v1:mag - 1009.80) > 0.1
  exit.
if abs(v2:mag - 1009.80) > 0.1
  exit.

if (x1 - x0):mag > 1
  exit.

if (v1 - v0):mag > 1
  exit.

// x2 should point in the v1 direction
if abs(3463334.06 - vdot(x2, v1:normalized)) > 0.1
  exit.

// v2 should point in the -x1 direction
if abs(1009.80 - vdot(v2, -x1:normalized)) > 0.1
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
