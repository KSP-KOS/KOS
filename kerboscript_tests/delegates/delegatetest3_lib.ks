
parameter op, yield_results.

function make_mapper {
  function map {
    parameter operation, collection.
    local result is list().
    for item in collection result:add(operation:call(item)).
    return result.
  }
  return map@.
}

function make_eacher {
  function for_each {
    parameter operation, collection.
    for item in collection {
      operation:call(item).
    }
  }
  return for_each@.
}

if yield_results {
  global mapper_result is make_mapper():bind(op).
} else {
  global mapper_result is make_eacher():bind(op).
  mapper_result:call(list(2,3,4)).
}


