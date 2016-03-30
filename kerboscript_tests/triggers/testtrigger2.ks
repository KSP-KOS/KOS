// Testing a trigger that takes a long time happening
// in the same code as a trigger that happens fast and often.

set nextThirdSecond to 0.
set next2Seconds to time:seconds+2.

// A trigger that happens every third of a second:
when time:seconds > nextThirdSecond then {
  print "(short-trigger)".
  preserve.
  set nextThirdSecond to time:seconds + 0.33333.
}

// A trigger that happens only once every 2 seconds, but lasts
// about a second when it does fire, and then is
// removed from the triggers:
when time:seconds > next2Seconds then {
  print "(long trigger begin)".
  
  // loop in place for about a half second, proving
  // along the way that fixedUpdate IPU limits are happening,
  // by noticing when the time:seconds moves:
  local prevTime is time:seconds.
  local startTime is time:seconds.
  until time:seconds > startTime + 1 {
    if time:seconds > prevtime {
      print "(in long trigger, time is now "+ time:seconds + ").".
      set prevTime to time:seconds.
    }
  }
  print "(long trigger end)".
  set next2Seconds to time:seconds + 2.
}

// The main loop that should also keep running while all the above stuff keeps interrupting it:
from {local i is 0.} until i > 3000 step {set i to i + 1.} do {
  if mod(i,500) = 0 {
    print "In main loop, i = " + i.
  }
}
