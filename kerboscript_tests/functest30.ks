print "Testing the case of a RUN in a loop.".

set i to 1.
until i > 4 {
  print "Iteration " + i.
  run functest30_inner(i).
  set i to i + 1.
}
