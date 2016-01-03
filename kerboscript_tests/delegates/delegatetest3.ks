print "Testing a parent program that runs a child program".
print "making delegates that the parent tries calling.".

{
  // Hide the parent delegate
  function triple { parameter i. return i * 3. }
  function announce { parameter i. print "  -- Announcing " + i. }

  print "== Pass delegate to child, receieve delegate back ==".
  run delegatetest3_lib.ks(triple@, true).
  print "  -- Ensure list(6,9,12) " + mapper_result:call(list(2,3,4)).

  print "== Allow delegate to run in child context".
  print "  -- Ensure announced: 2, 3, 4 on next line:".
  run delegatetest3_lib.ks(announce@, false).
}
