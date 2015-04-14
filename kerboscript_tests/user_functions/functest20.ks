// Testing local locks with repetition.


lock xx to "global lock x".

{
  global lock yy to "global lock y".
  local lock zz to "local lock z".

  print "inside scope part 1: xx = " + xx.
  print "inside scope part 1: yy = " + yy.
  print "inside scope part 1: zz = " + zz.

  // Now relocking the values to something new
  global lock yy to "global lock y-part2".
  local lock zz to "local lock z-part2".

  print "inside scope part 2: xx = " + xx.
  print "inside scope part 2: yy = " + yy.
  print "inside scope part 2: zz = " + zz.

}

lock xx to "global lock part 2".

print "outside scope: xx = " + xx.
print "outside scope: yy = " + yy.
print "next line should barf - zz undefined.".
print "outside scope: zz = " + zz.
