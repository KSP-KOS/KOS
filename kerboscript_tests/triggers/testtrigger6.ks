// Testing triggers with scopes remembered with closure.

function outer {
  inner().

  local start_time is time:seconds.
  function inner {
    local delay_time is 3.
    print "will trigger in " + delay_time + " seconds".
    // Note the trigger's conditional check can use
    // local scope vars too:
    when time:seconds > start_time + delay_time then {
      print "trigger happened after " + delay_time + " seconds.".
      return 0. // fire once only.
    }
  }
  local b is 2. // despite being after function 'inner', this should still
                // be in the same scope as variable 'a'.
}

outer().
wait 5.
print "done".
