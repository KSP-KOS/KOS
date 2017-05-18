// Testing triggers with scopes remembered with closure.

function outerA {

  local start_time is time:seconds.
  local myname is "A".
  function inner {
    local delay_time is 3.
    print "Trigger " + myname + " will happen in " + delay_time + " seconds".
    // Note the trigger's conditional check can use
    // local scope vars too:
    when time:seconds > start_time + delay_time then {
      print "trigger A happened after " + delay_time + " seconds.".
      return 0. // fire once only.
    }
  }
  inner().
}

// This will use the same variable names but becasue of scope they
// should be different instances with different values:
function outerB {

  local start_time is time:seconds.
  local myname is "B".
  function inner {
    local sustain_time is 1.
    print "Trigger " + myname + " will now continue for " + sustain_time + " seconds".
    when true then {
      print "trigger B now happening.".
      return time:seconds < start_time + sustain_time.
    }
  }
  inner().
}


outerA().
wait 1.5.
outerB().
wait 5.
print "done".
