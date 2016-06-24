print " ==== Anonymous functions. Test 1 ====".

// Just a dumb test that prints everything
// in the collection that matches the boolean
// test function:
function print_hits {
  parameter things, func.

  for thing in things {
    if func(thing)
      print thing.
  }
}

function is_even {
  parameter num.
  return (mod(num,2)=0).
}

local test_list is list(1,2,3,4,5, 50,51,52,53, 6, 7, 8).

print "Calling print_hits with named function.".
print_hits(test_list, is_even@).
print "Calling print_hits with anonymous function.".
print "Should give the same results as above.".
print_hits(test_list, { parameter num. return mod(num,2)=0. } ).

