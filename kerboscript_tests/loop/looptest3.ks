// Additional test to make sure that a from loop can access a nested  and an outer function in a loop
set testValue to 0.
doTest().

function doTest
{
  from {local i is 0.} until i > 3 step {set i to i+1.} do {
    addOnce(i).
    addDouble(i).
  }

  function addOnce{
    parameter addMe.
    set testValue to testValue + addMe.
  }
}

function addDouble{
  parameter addMe.
  set testValue to testValue + addMe * 2.
}

if testValue <> 18 {
  print "failed: testValue should be 18 and is " + testValue.
}
print "success".
