print "THIS IS A DELIBERATE TEST OF INFINITE RECURSION.".
print "Let's see how long this goes before KSP barfs.".
print "And let's make sure KSP barfs in a 'clean' way.".

// Give some time to hit ctrl-C before the big spew of scrolling starts:
print "Starting recurse in....".
print "5 seconds".
wait 1. 
print "4 seconds".
wait 1. 
print "3 seconds".
wait 1. 
print "2 seconds".
wait 1. 
print "1 seconds".
wait 1. 
print "now".

recurse(0).

declare function recurse {
  declare parameter depth.

  print "  Recurse depth="+depth.

  return recurse(depth+1).
}.
