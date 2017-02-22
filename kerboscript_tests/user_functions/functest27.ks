print "running a test to see if you can call".
print "a function from inside a KSM file.".

print "Ensuring functest27_b.ksm is deleted:".
log "" to "functest27_b.ksm". deletepath("functest27_b.ksm").

print "Trying to compile functest27_b.ks to KSM file:".
compile functest27_b.
print "Now running the compiled KSM file:".
run functest27_b.ksm.
print "Now trying to call the function that was in KSM:".
print_check().
print "If you got here without error, test passed.".

