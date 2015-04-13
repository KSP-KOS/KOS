// Testing local locks.


lock xx to "global lock x".

{
  global lock yy to "global lock y".
  local lock zz to "local lock z".

  print "inside scope: xx = " + xx.
  print "inside scope: yy = " + yy.
  print "inside scope: zz = " + zz.
}

print "outside scope: xx = " + xx.
print "outside scope: yy = " + yy.
print "next line should barf - zz undefined.".
print "outside scope: zz = " + zz.
