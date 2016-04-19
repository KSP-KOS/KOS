// Tests miscellaneous structures

print "These should all be 'True':".

// Body
writejson(kerbin, "misc.json").
set read to readjson("misc.json").
print read:name = "Kerbin".

// GeoCoordinates
writejson(latlng(10,20), "misc.json").
set read to readjson("misc.json").
print read:lat = 10.
print read:lng = 20.
print read:heading <> 0.

// Timespan
set t to time.
writejson(t, "misc.json").
set read to readjson("misc.json").
print read:seconds = t:seconds.

// Vector
set vec to v(1,2,3).
writejson(vec, "vector.json").
set read to readjson("vector.json").
print read:x = 1.
print read:y = 2.
print read:z = 3.

// Vessel
writejson(ship, "vessel.json").
set read to readjson("vessel.json").
print read:name = ship:name.
