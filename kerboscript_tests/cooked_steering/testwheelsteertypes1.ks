print "This needs to be run on a rover, on ground at KSC".
print " ".
print "-------------------------------------------------".

brakes off.
print "going north by integer compass heading 0".
lock wheelsteering to 0.
lock wheelthrottle to 1.0.
wait 3.
lock wheelthrottle to 0.5.
wait 7.
brakes on.
print "stopping".
wait 5.

print "going westish by float compass heading 270.5".
brakes off.
lock wheelsteering to 270.5.
lock wheelthrottle to 1.0.
wait 3.
lock wheelthrottle to 0.5.
wait 7.
lock wheelthrottle to 0.
brakes on.
print "stopping".
wait 5.

print "going south by integer compass heading 180".
brakes off.
lock wheelsteering to 180.
lock wheelthrottle to 1.0.
wait 3.
lock wheelthrottle to 0.5.
wait 7.
lock wheelthrottle to 0.
brakes on.
print "stopping".
wait 5.

print "going east by integer compass heading 90".
brakes off.
lock wheelsteering to 90.
lock wheelthrottle to 1.0.
wait 3.
lock wheelthrottle to 0.5.
wait 7.
lock wheelthrottle to 0.
brakes on.
print "stopping".
wait 5.

print "going south again, now by float compass heading 180.1".
brakes off.
lock wheelsteering to 180.1.
lock wheelthrottle to 1.0.
wait 3.
lock wheelthrottle to 0.5.
wait 7.
lock wheelthrottle to 0.
brakes on.
print "stopping".
wait 5.

print "Going toward a LATLNG position that is southeast of here".
set newlat to latitude + 1.
set newlng to longitude + 1.
brakes off.
lock wheelsteering to LATLNG(newlat,newlng).
lock wheelthrottle to 1.0.
wait 3.
lock wheelthrottle to 0.5.
wait 7.
lock wheelthrottle to 0.
brakes on.
print "stopping".
wait 5.


print "okay that's enough.  done testing".
