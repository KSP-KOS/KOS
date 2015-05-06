// compiletest1
print "Tests the case where an empty script file is run".
print "from another script.  It should do nothing, rather".
print "than get stuck forever or error out.".
print " ".
print "You must 'set count to 0' before you run the test".
set count to count + 1.
if count > 1 { print 1/0. }. // force dump after a few runs
print "program started".
log "" to empty_file.
delete empty_file.
log "" to empty_file.
run empty_file.
print "If it got this far without complaint, then it passed the test.".
