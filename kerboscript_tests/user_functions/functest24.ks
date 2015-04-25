// Testing calling a built-in function from inside a double-nested fucntion.

function outer_clamp {
  function inner_clamp{
    parameter x.
    parameter minval.
    parameter maxval.

    return max( min( x, maxval), minval).
  }.
  print "clamp of 10 to rage 4,9 is ".
  print inner_clamp(10,4,9).
}.

outer_clamp().
