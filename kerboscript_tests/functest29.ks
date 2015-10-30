function wants_three_args {
  parameter p1, p2, p3.

  print "args were: " + p1 + ", " + p2 + ", " + p3.
}.

print "Testing function call with too many args.".
print "You should expect an error when this runs.".
print " ".

wants_three_args("arg1", "arg2", "arg3", "arg4" ). // arg4 uncalled for.
