// Test of from loop syntax:

doTest(). // body of test wrapped in function to prove it's local vars.

function doTest
{
  print "Nested from loop test.".
  print "Fills a 2-D array from 0,0, to 3,3, and then prints the array.".

  local arr is List().
  local count is 0.

  print "filling array:".
  from {local i is 0.} until i > 3 step {set i to i+1.} do {
    arr:add( list() ).
    from {local j is 0.} until j > 3 step {set j to j+1.} do {
      arr[i]:add( count ).
      set count to count + 1.
    }
  }

  print "printing array:".
  from {local i is 0.} until i > 3 step {set i to i+1.} do {
    from {local j is 0.} until j > 3 step {set j to j+1.} do {
      print "arr["+i+"]["+j+"] = " + arr[i][j].
    }
  }

  print "Done, now trying something that should bomb out on purpose.".
  print "proving local scope by trying to access i,j outside the loop:".
  print "If the next line errors out with unknown identifier, that's CORRECT.".
  print "i is " + i + ", and j is " + j.
  print "SHOULD NOT GET THIS FAR.".
}

