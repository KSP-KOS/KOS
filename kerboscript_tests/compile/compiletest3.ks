// Verify old parse cases work:

set xxxx to 1. // These two differ in case only so they should
set Xxxx to 2. // be the same identifier being overwritten.
print "should be equal: " + Xxxx + " = " + xxxx. 

set _xxX to 3. print _xxX.
set X_xxX to 4. print X_xxX.
set XXX to 5. print XXX.
print XXX-xxxx.  // ensure the '-' isn't an ident char.
print list(XXX,xxxx). // ensure the "," isn't an ident char.
set X0 to 123. print X0.
set X1 to 123456789. print X1.
set X2 to 123.46789. print X2.
set X3 to .456789. print X3.
set X4 to 1.23456789e3. print X4.
set X5 to 1.23456789e-3. print X5.
set X6 to 1.23456789e+3. print X6.

print " ".

// Verify new numbers with underscores work:
set Y0 to 123_456_789. print Y0.
set Y1 to 123_456.78_9. print Y1.
set Y2 to 123_456.78_9e-5. print Y2. // 1.23456789
set Y3 to 12.3_456_78_9e+5. print Y3. // 1234567.89
set Y4 to 12.3_456_78_9e5. print Y4. // 1234567.89
set Y5 to 12.3_456_78_9 e5. print Y5. // 1234567.89
set Y6 to 12.3_456_78_9 e 5 . print Y6. // 1234567.89
set Y7 to 12.3_456_78_9 e - 3 . print Y7. // 0.0123456789
set Y8 to 12.3_456_78_9 e + 5 . print Y8. // 1234567.89
set Y9 to 12.3_456_78_9 e -3 . print Y9. // 0.0123456789
set Y10 to 12.3_456_78_9 e- 3 . print Y10. // 0.0123456789

print " ".

// Verify that other unicode letters work as identifiers:
// Cyrllic (tends to be always uppercase, I *think*)
set БНЯД to 1. print БНЯД.
set БНЯ_Д to 2. print БНЯ_Д.
set _БНЯД to 3. print _БНЯД.

// Accented Latin chars:

// These only differ in case, so they should be the same identifier:
set GARÇON to "this value should be overwritten on next line".
set garçon to "boy".
print "Same identifier? " + GARÇON + " = " + garçon.

set ÂÊÔ to "this value should be overwritten on the next line".
set âêô to "yummy carets".
print "Same identifier? " + ÂÊÔ + " = " + âêô.

set ÆØÅ to "this value should be overwritten on the next line".
set æøå to "Pining for the Fjords".
print "Same identifier? " + ÆØÅ + " = " + æøå.

// No idea if "uppercase" even means anything here in this context:
set シ佅ヂ乶 to "I have no clue about Kanji".
print シ佅ヂ乶.

print " ".

// Also test case-insensitivity in strings themselves:
print "Does ÆØÅ = æøå? " +
      ( "ÆØÅ" = "æøå" ).
set myLex to Lexicon().
set myLex["GARÇON"] to "value that should get clobbered on next line".
set myLex["garçon"] to "newer value".
print "Does myLex[GARÇON] = myLex[garçon]? " +
    ( myLex["GARÇON"] = myLex["garçon"] ).