// Program to test that triggers can contain wait's in them now:
//

on ag1 {
  print "(You pressed AG1.. thinking....)".
  wait 1.
  print "(Still thinking about how you pressed AG1...)".
  wait 1.
  print "(Okay, done thinking about AG1 now.)".
  print "(You may press AG1 again if you like.)".
  preserve.
}

print "Test trigger wait using AG1.".
print "AG1 will last a long time with WAIT.".
print "use CTRL-C to quit.".
wait until false.

