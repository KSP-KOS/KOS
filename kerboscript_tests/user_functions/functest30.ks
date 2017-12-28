print "Testing explicit function scope keywords override.".

run once "functest30_lib.ks".
local del is outer().

print "Should say global1: " + global1().
if (global1() <> "global1") print "!!!!!!!!!! ERROR ERRROR !!!!!!!!!!" + char(7) + char (7).
print "Should say global2: " + global2().
if (global2() <> "global2") print "!!!!!!!!!! ERROR ERRROR !!!!!!!!!!" + char(7) + char (7).
print "Should say global3: " + global3().
if (global3() <> "global3") print "!!!!!!!!!! ERROR ERRROR !!!!!!!!!!" + char(7) + char (7).
print "Should say local2: " + del().
if (del() <> "local2") print "!!!!!!!!!! ERROR ERRROR !!!!!!!!!!" + char(7) + char (7).

print " ".
print "PROGRAM SHOULD NOW FAIL WITH 'local1' NOT FOUND.".
print local1().
