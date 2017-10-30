// Testing attempt to lock throttle and steering
// without mentioning them at the global
// scope - only in triggers.  From github issue #799
//
print "Test by launching a small rocket manually straight up ".
print "and then waiting for the script to take over at 50m up.".
print "If it works right, the craft should start deflecting down".
print "at 50m, and cut the throttle to 80%.".
print "At 500m it should just stay pointed whichever way it's ".
print "currently going at that moment.".
print " ".
print "If it fails, then the rocket will just keep going straight up.".
print "You have 30 seconds to perform the test.".

when alt:radar > 50 then {
    print "Alt:radar now >50.".
    lock steering to (up + r(0,-45,0)).
    lock throttle to 0.8. //added
    when alt:radar > 500 then {
        print "Alt:radar now >500.".
        lock steering to ship:prograde:vector.
    }
}
// lock steering to up.
// lock throttle to 1.
wait 30.
unlock steering.
unlock throttle.
