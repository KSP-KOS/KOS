// this is supposed to be called by functest21.


// Nothing more than a program designed to ensure that
// an update tick must occur while it is running because
// it executes a lot of instructions:
set counter to 0.
until counter >= 5000 {
  set counter to counter + 1.
}.
