// Simple test of lock closures
//  Test 2: nested scope levels, not in a function:

set x to 1.
set indent to "  ".
lock testlock to x.

declare function printindent{
   declare parameter indent.
   print indent + "testlock = " + testlock.
}

printindent(indent + " before, ").

// The 'if true' commands are just here to allow valid braces to exist:
if true {
    declare local_x to 2.
    declare indent to indent + "  ". // indent with a longer indent string

    lock testlock to x + local_x.
    printindent(indent + " before, ").

    if true {
        declare localer_x to 3.
        declare indent to indent + "  ".

	lock testlock to x + local_x + localer_x.
        printindent(indent + " before, ").

        if true {
            declare localest_x to 4.
            declare indent to indent + "  ".

            lock testlock to x + local_x + localer_x + localest_x.
            printindent(indent + " innermost, ").
        }.
	printindent(indent + " after, ").
    }.
    printindent(indent + " after, ").
}.
printindent(indent + " after, ").
