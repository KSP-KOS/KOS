PRINT " ".
PRINT "TESTING 'UNSET' NESTING AND SILENT FAILS.".
PRINT " ".
PRINT "EXPECTED   ACTUAL".
PRINT "---------  ---------".

set AAA to 1.
set BBB to 1.
set CCC to 1.
if (true) {
  local BBB is 2. // hides higher BBB.
  if (true) {
    local BBB is 3. // hides higher BBB.
    print "3          " + (Choose BBB if defined(BBB) else "undefined").
    unset BBB.
    print "2          " + (Choose BBB if defined(BBB) else "undefined").
    unset BBB.
    print "1          " + (Choose BBB if defined(BBB) else "undefined").
    unset BBB.
    print "undefined  " + (Choose BBB if defined(BBB) else "undefined").
    unset BBB. // Should fail silently and not complain.
    print "undefined  " + (Choose BBB if defined(BBB) else "undefined").

    unset AAA.
  }
}
print "undefined  " + (Choose AAA if defined(AAA) else "undefined").
unset AAA. // should fail silently and not complain.

print "1          " + (Choose CCC if defined(CCC) else "undefined").
unset CCC.
print "undefined  " + (Choose CCC if defined(CCC) else "undefined").
unset CCC. // should fail silently and not complain.
print "undefined  " + (Choose CCC if defined(CCC) else "undefined").
PRINT " ".
print "IF THE ABOVE 'EXPECTEDS' MATCH THE 'ACTUALS',".
print "AND IT GOT THIS FAR, THEN ISSUE #2752 IS FIXED.".
