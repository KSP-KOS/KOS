CLEARSCREEN.                                                    // CORRECT OUTPUTS
SET s TO "Hello, Strings!".                                     // ---------------
PRINT "Original String:               " + s.                    // Hello, Strings!
PRINT "string[7]:                     " + s[7].                 // S
PRINT "LENGTH:                        " + s:LENGTH.             // 15
PRINT "SUBSTRING(7, 6):               " + s:SUBSTRING(7, 6).    // String
PRINT "CONTAINS(''ring''):            " + s:CONTAINS("ring").   // True
PRINT "CONTAINS(''bling''):           " + s:CONTAINS("bling").  // False
PRINT "ENDSWITH(''ings!''):           " + s:ENDSWITH("ings!").  // True
PRINT "ENDSWITH(''outs!''):           " + s:ENDSWITH("outs").   // False
PRINT "FIND(''l''):                   " + s:FIND("l").          // 2
PRINT "FINDLAST(''l''):               " + s:FINDLAST("l").      // 3
PRINT "FINDAT(''l'', 0):              " + s:FINDAT("l", 0).     // 2
PRINT "FINDAT(''l'', 3):              " + s:FINDAT("l", 3).     // 3
PRINT "FINDLASTAT(''l'', 9):          " + s:FINDLASTAT("l", 9). // 3
PRINT "FINDLASTAT(''l'', 2):          " + s:FINDLASTAT("l", 2). // 2
PRINT "INSERT(7, ''Big ''):           " + s:INSERT(7, "Big ").  // Hello, Big Strings!

PRINT " ".
PRINT "                               |------ 18 ------|".
PRINT "PADLEFT(18):                   " + s:PADLEFT(18).        //    Hello, Strings!
PRINT "PADRIGHT(18):                  " + s:PADRIGHT(18).       // Hello, Strings!
PRINT " ".

PRINT "REMOVE(1, 3):                  " + s:REMOVE(1, 3).               // Ho, Strings!
PRINT "REPLACE(''Hell'', ''Heaven''): " + s:REPLACE("Hell", "Heaven").  // Heaveno, Strings!
PRINT "STARTSWITH(''Hell''):          " + s:STARTSWITH("Hell").         // True
PRINT "STARTSWITH(''Heaven''):        " + s:STARTSWITH("Heaven").       // False
PRINT "TOUPPER:                       " + s:TOUPPER().                  // HELLO, STRINGS!
PRINT "TOLOWER:                       " + s:TOLOWER().                  // hello, strings!
PRINT " ".
PRINT "''  Hello!  '':TRIM():         " + "  Hello!  ":TRIM().          // Hello!
PRINT "''  Hello!  '':TRIMSTART():    " + "  Hello!  ":TRIMSTART().     // Hello!
PRINT "''  Hello!  '':TRIMEND():      " + "  Hello!  ":TRIMEND().       //   Hello!
PRINT " ".
PRINT "Chained: " + "Hello!":SUBSTRING(0, 4):TOUPPER():REPLACE("ELL", "ELEPHANT").  // HELEPHANT
function comparetest {
  parameter a, b.

  // This is the correct way, but it can't be run until PR #1412 is merged:
  //     local qt_a is "".
  //     if a:istype("string") { set qt_a to CHAR(34). }
  //     local qt_b is "".
  //     if b:istype("string") { set qt_b to CHAR(34). }
  // This is the less good way for now: quote everything, like it or not:
  local qt_a is CHAR(34).
  local qt_b is CHAR(34).

  PRINT "string compare: " + qt_a + a + qt_a + " <  " + qt_b + b + qt_b + " is " + (a <  b).
  PRINT "string compare: " + qt_a + a + qt_a + " <= " + qt_b + b + qt_b + " is " + (a <= b).
  PRINT "string compare: " + qt_a + a + qt_a + " >  " + qt_b + b + qt_b + " is " + (a >  b).
  PRINT "string compare: " + qt_a + a + qt_a + " >= " + qt_b + b + qt_b + " is " + (a >= b).
  PRINT "string compare: " + qt_a + a + qt_a + " =  " + qt_b + b + qt_b + " is " + (a =  b).
  PRINT "string compare: " + qt_a + a + qt_a + " <> " + qt_b + b + qt_b + " is " + (a <> b).
}
SET s1_lower to "abc def".
SET s1_upper to "ABC DEF".
SET s2_lower to "abc dfe".
SET s2_upper to "ABC DFE".
PRINT "Test: differ in case only:".
comparetest(s1_lower,s1_upper).
PRINT "Test: differ in case only:".
comparetest(s2_lower,s2_upper).
PRINT "Test: left < right, same case:".
comparetest(s1_lower,s2_lower).
PRINT "Test: left < right, different case:".
comparetest(s1_lower,s2_upper).
PRINT ":::".
PRINT "Mixing types with string to make ToString compares:".
PRINT ":::".
PRINT "Test: same value, int:".
comparetest("123", 123).
PRINT "Test: same value, double:".
comparetest("123.456", 123.456).
PRINT "Test: different value, int, note string, not number compare:".
comparetest("123", 1000).
PRINT "Test: different value, double, note string, not number compare:".
comparetest("123.456", 1000.456).
function iterateTest {
    parameter str.
    print "Iterate through the string:".
    print "  " + str.
    for ch in str {
        print ch.
    }
}
iterateTest("abcd").
