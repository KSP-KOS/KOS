print "testing how arg count works with premature return.".

function foo {
  parameter a.
  if a < 1 {
    print "abort function foo, invalid arg count given".
    return.
  }
  print "reading " + a + " args.".
  from {local i is 1.} until i > a step {set i to i + 1.} do {
    parameter temp.
    print "arg " + i + " is " + temp.
  }

  return.
}


foo(1,"a").
foo(3,"a","b","c").
print "Premature return test, with no extra args passed. Should be okay.".
foo(0).
print "Premature return test, with some extra args passed. Should be okay.".
foo(0,1,1,1). // check that premature return stmt consumes args properly.
print "Trying again after the premature return cases.".
foo(3,"aaa","bbb","ccc"). // what happens after the premature abort case?
print " ".
print "BUT, THE FOLLOWING SHOULD STILL FAIL.".
print "BECAUSE IT HAS WRONG ARGS BUT NOT A PREMATURE RETURN.".
foo(1,"a","b","c"). // will only read it up to the "a".
