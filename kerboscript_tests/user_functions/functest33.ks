// Testing that anon functions can have locks in them.
// (Test added in response to issue #1784 and #1801).

// Tests that the compiler will properly recurse into
// all the weird places an anon function might be,
// which is pretty much *any* expression, and walk
// their contents looking for LOCK statements.

function named_func { lock foo to 1. }

function func_that_invokes_delegate { parameter del.  del().  }
function func_that_returns_delegate { return {lock foo to 1.}. }

global declare_global_anon_func is { lock foo to 1. }.

set set_stmt_anon_func to { lock foo to 1. }.

print "Should see no output, no errors.  Just program end".
print "==================================================".
named_func().
declare_global_anon_func().
set_stmt_anon_func().
{ // do-nothing nested braces for local scoping.
  local declare_local_anon_func is { lock foo to 1. }.
  declare_local_anon_func().
}
func_that_invokes_delegate( { lock foo to 1. } ).
func_that_invokes_delegate( func_that_returns_delegate() ).
