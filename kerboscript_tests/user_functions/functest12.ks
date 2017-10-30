// Testing the case of a function that contains a WHEN trigger
// inside of itself:

set x to 0.

// Try this twice to ensure it gets re-enabled in the second 
// function call too.
reinit_triggers().
print "pass 1: triggers initialized... ".
wait 1.
set x to 1.
wait 1.
set x to 2.

wait 1.

reinit_triggers().
print "pass 2: triggers initialized... ".
wait 1.
set x to 1.
wait 1.
set x to 2.
wait 1.

print "done with test".

declare function reinit_triggers {
  set x to 0.

  when x = 1 then {
    print "When x = 1 trigger has been invoked.".
  }.

  when x = 2 then {
    print "When x = 2 trigger has been invoked.".
  }.
}.


