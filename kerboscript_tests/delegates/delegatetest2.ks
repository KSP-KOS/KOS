function map {
  parameter operation, itemlist.
  local results is list().
  for item in itemlist {
    results:add(operation:call(item)).
  }
  return results.
}

function square {
  parameter x. return x * x.
}

print "== ensuring delegates as arguments ==".
set map_delegate to map@.
set square_list to map_delegate:bind(square@).
print "   — squares of list(1,2,3) are " + square_list:call(list(1,2,3)).

function make_greeter {
  parameter name.
  function greet {
    parameter name_to_greet.
    print "  — proof Hello, " + name_to_greet + ". I'm " + name.
  }
  return greet@.
}

print "== ensuring delegates as return values ==".
set my_greeter to make_greeter("Jebediah Kerman").
my_greeter:call("Brave kOS Dev").
print "== ensuring returned-delegates with immediate execution ==".
make_greeter("Bill Kerman"):call("Brave kOS Dev").
