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
