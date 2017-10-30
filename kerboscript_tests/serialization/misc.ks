// Tests miscellaneous structures2

print "These should all be 'True':".

// Body
writejson(kerbin, "body.json").
set read to readjson("body.json").
print read:name = "Kerbin".

// GeoCoordinates
writejson(latlng(10,20), "geo.json").
set read to readjson("geo.json").
print read:lat = 10.
print read:lng = 20.
print read:heading <> 0.

// Timespan
set t to time.
set s to t:seconds.
writejson(t, "timespan.json").
set read to readjson("timespan.json").
print read:seconds = s.

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

// Message
ship:connection:sendmessage(lex("key1", 123)).
set m to ship:messages:pop.
writejson(m, "message.json").
set read to readjson("message.json").
print read:content["key1"] = 123.
print read:sender:tostring:contains(ship:name).
