// Testing basic functions with args

// Test that the declare parameter statement still
// does its job at the global level to be used with
// the RUN command like before:
declare parameter global_start_param.
declare parameter global_stop_param.

// example of use:
//   RUN FUNCTEST1(1,5). - counts from 1 to 5, and from 1^2 to 5^2.

declare function foo {
  // Test a mixture of both comma args style and separate arg
  // statements style:
  declare parameter a.
  declare parameter b, c.
  declare str to "inside foo(), ".
  set str to str + " a=" + a.
  set str to str + " b=" + b.
  set str to str + " c=" + c.
  return str.
}.


declare num to global_start_param.
print "num = " + num.
print "global_stop_param = " + global_stop_param.
until num > global_stop_param {
  print "calling FOO: " + foo(num, num^2, sqrt(num)).
  set num to num + 1.
}.

print "deliberate errror to force a dump in the log.".
set x to 1/0. 
