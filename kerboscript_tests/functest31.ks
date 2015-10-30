//
// I am not too sure if this is a feature we want to advertise yet,
// but it does seem to be doable:
//
print "Testing varying args logic".

// A function that accepts varying args.
// the first arg is a number: how many more args to expect.
// the rest of the args are just printed for testing.
function print_var_args {
  parameter how_many.
  
  // read parameters, filling a local string.
  local my_string is "".
  local i is 1.
  until i > how_many {
    parameter next_arg.
    set my_string to my_string + " " + next_arg.
    set i to i + 1.
  }

  print my_string.
}


print "This test should work and print: A B C".
print_var_args(3, "A", "B", "C").

print "This test should work and print: 10 20 30 40 50 60".
print_var_args(6, 10, 20, 30, 40, 50, 60).

print "This test should work and print: <nothing - empty>".
print_var_args(0).

print "This test should fail and complain about not enough args:".
print_var_args(5, "A", "B", "C"). // it will try to read more than I sent.


