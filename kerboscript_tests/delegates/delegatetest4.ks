print "This is a test that delegates can be".
print "called from steeringmanager context.".
print " ".
print "This must be run from a new vessel on launchpad".
print " ".
print "Creating deeply nested delegate function...".
function outer {
  parameter outerParam.

  function middle {
    local twenty is 20.

    function inner {
      parameter addMore.
      local fifteen is 15.

      // Proof that it is seeing all the nested local vars
      // when calculating the angle:
      return addMore + twenty + fifteen.
    }
    return inner@.
  }
  return middle:bind(outerParam).
}

print "launch in 3...".
wait 1.
print "launch in 2...".
wait 1.
print "launch in 1...".
wait 1.
print "If it succeeds, you should see the craft taking off".
print "with its steering locked to heading(90,45).".
print "if it's going some other direction, it didn't work.".

print "steering by delegate: ".
print "  " + outer(10).
print "which evals to " + outer(10):call().

lock steering to heading(90,outer(10):call() ).
lock throttle to 1.
until maxthrust > 0 { stage. wait 0.2. }


print "Press control-C to abort, then you'll need to revert flight".

wait until false.

