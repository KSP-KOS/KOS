print "This test from issue #1117 excercises the".
print "bug with trying to LOCK THROTTLE inside".
print "a FROM loop".
print "It should move the throttle at T -3 seconds.".
print "--------------------------------------------".
print " ".
FROM { LOCAL countdown IS 10. } UNTIL countdown = 0 STEP { SET countdown TO countdown - 1. } DO {
    PRINT "T -" + countdown.
    IF countdown = 3 {
        print "Throttle should now become 1.".
        LOCK THROTTLE TO 1.0.
    }
    WAIT 1.
}
