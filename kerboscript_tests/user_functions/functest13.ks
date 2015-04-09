// Testing the nesting of function calls and locks inside triggers.

declare function do_print {
  declare parameter str.
  print("doprint: printing: " + str).
}.

when x = 1 then {
  do_print("trigger body, when x = 1, y = " + y).
}.

when x = 2 then {
  do_print("trigger body, when x = 2, y = " + y).
}.

set x to 0.
// Also need to ensure that locks compile correctly here:
lock y to -x.

wait 1.
set x to 1.
wait 1.
set x to 2.
wait 1.
