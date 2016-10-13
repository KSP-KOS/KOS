print "YOU SHOULD SEE THE MESSAGES".
print "IN THE RIGHT ORDER AND TIMING:".
print "These are a complex set of triggers with waits.".
print "that should print the messages in order despite".
print "them being from different triggers and main code.".
print " ".

set start_time to time:seconds.

function timestamp_msg {
  parameter msg.
  print round(time:seconds - start_time, 2) + ": " + msg. 
}

set rightaway to true.
when rightaway then {
  timestamp_msg("msg02: Should be after one 'tick'.").
  wait 3.
  timestamp_msg("msg03: Should be after 3s+1tick.").  
}
set twosecondslater to time:seconds+2.
when time:seconds > twosecondslater then {
  timestamp_msg("msg04: Despite trigger call getting put on stack at 2s, it won't start until the other trigger's wait is over, so this should be after 3s.").
  wait 2.
  timestamp_msg("msg05: Should be after 5s.").
}

timestamp_msg("msg01: Program starting.").
wait 6.
timestamp_msg("msg06: Program ending after 6s."). 
