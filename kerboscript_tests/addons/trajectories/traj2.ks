print "This test should be run from a vessel in a circular orbit, without an impact.".
global tr is addons:tr.
// make sure that hasimpact works right
if tr:hasimpact {
    print "Has impact is true, error if current orbit does not impact".
    print "Check current orbit and then run test again.".
}
else{
    print "Orbit currently doesn't impact.".
    lock steering to retrograde.
    wait 2.
    wait until steeringmanager:angleerror < 1.
    print "Lowering periapsis...".
    lock throttle to 1.
    wait until periapsis < -5000.
    print "Periapsis at: " + round(periapsis, 2).
    lock throttle to 0.
    wait 1.
    if tr:hasimpact {
        print "Setting target impact position to current position.".
        tr:settarget(ship:geoposition).
        wait 1.
        local impactGeo is tr:impactpos.
        print "Impact found at lat:" + round(impactGeo:lat, 2) + " lng: " + round(impactGeo:lng, 2).
        global vd1 is vecdraw(v(0,0,0), tr:plannedvec * 100, red, "PLANNEDVEC", 1.0, true).
        global vd2 is vecdraw(v(0,0,0), tr:correctedvec * 100, blue, "CORRECTEDVEC", 1.0, true).
        global vd3 is vecdraw(impactGeo:position, (impactGeo:position - body:position) * altitude * 100, yellow, "IMPACTPOS", 1.0, true).
        print "Vectors drawn for suffixes.".
    }
    else {
        print "No impact found, error if periapsis is not in a canyon with no water...".
    }
}
