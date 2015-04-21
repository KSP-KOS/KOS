// Testing the case of a WHEN trigger containing a lock statement.

print "If this test operates properly, then".
print "you should see the script print '0' over ".
print "and over for 5 seconds, then start ".
print "printing the current time:seconds over ".
print "and over after that for another 5 ".
print "seconds.".

lock t to 0.
set ut to time:seconds.
when time:seconds > ut + 5 then {
	lock t to time:seconds.
}

until time:seconds > ut + 10 {
	print t.
	wait 0.5.
}
print "done with test".
