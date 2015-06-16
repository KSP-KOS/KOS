// Tests the case of a short triggers that finish easily within
// a few IPU's.

set t_start to time:seconds.

when time:seconds > t_start+3 then {
  print "trigger 1 after 3 seconds!".
}
when time:seconds > t_start+3 then {
  print "trigger 2 after 3 seconds!".
}

when time:seconds > t_start+6 then {
  print "trigger 1 after 6 seconds!".
}
when time:seconds > t_start+6 then {
  print "trigger 2 after 6 seconds!".
  print "annoyingly staying around for a second.".
  if time:seconds < t_start+7 { 
    preserve. 
  }
}

print "waiting a few seconds to ensure the triggers happen.".
print "You should see messages after 3 and 6 seconds.".
until time:seconds > t_start+8 {
  print "mainline waiting...".
  wait 0.333.
}
