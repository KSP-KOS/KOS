// A generic PID controller routine to be used by other scripts.
// This controller operates without being aware of the math
// to perform the integral or derivative operations.  Instead
// you just keep updating it with the position information and
// it derives them from it as it goes.

@LAZYGLOBAL off.

// Make a list of pid tuning parameters Kp, Ki, Kd.
declare function pid_init {
  parameter
    Kp, // gain of position
    Ki, // gain of integral
    Kd. // gain of derivative

  local SeekP is 0. // desired value for P (will get set later).
  local P is 0.     // phenomenon P being affected.
  local I is 0.     // crude approximation of Integral of P.
  local D is 0.     // crude approximation of Derivative of P.
  local oldT is -1. // (old time) start value flags the fact that it hasn't been calculated
  local oldInput is 0. // previous return value of PID controller.

  // Because we don't have proper user structures in kOS (yet?)
  // I'll store the pid tracking values in a list like so:
  //
  local pid_array is list().
  pid_array:add(Kp).    // [0]
  pid_array:add(Ki).    // [1]
  pid_array:add(Kd).    // [2]
  pid_array:add(SeekP). // [3]
  pid_array:add(P).     // [4]
  pid_array:add(I).     // [5]
  pid_array:add(D).     // [6]
  pid_array:add(oldT).  // [7]
  pid_array:add(oldInput). // [8].

  return pid_array.
}.

// Given a list of the tuning params made with pid_init, and
// the desired value of the phenomenon, and the current value
// for the phenomenon, it will automatically return what you should
// set the controlling thing for that phenomenon to.
declare function pid_seek {
  parameter
    pid_array, // array built with pid_init, and updated with each call to me (pid_seek).
    seekVal,   // value we want.
    curVal.    // value we currently have.

  // We have no "static locals" so I'm doing a trick here.
  // Since LIST()s are passed by ref instead of by value,
  // I'm going to store the data needed between calls in the
  // LIST(), so it gets persisted between calls of me.

  local Kp   is pid_array[0].
  local Ki   is pid_array[1].
  local Kd   is pid_array[2].
  local oldS is pid_array[3]. 
  local oldP is pid_array[4].
  local oldI is pid_array[5].
  local oldD is pid_array[6].
  local oldT is pid_array[7]. // Old Time
  local oldInput is pid_array[8]. // prev return value, just in case we have to do nothing and return it again.

  local P is seekVal - curVal.
  local D is 0. // default if we do no work this time.
  local I is 0. // default if we do no work this time.
  local newInput is oldInput. // default if we do no work this time.

  local t is time:seconds.
  local dT is t - oldT.

  if oldT < 0 {
    // I have never been called yet - so don't trust any
    // of the settings yet.
  } else {
    if dT = 0 { // Do nothing if no physics tick has passed from prev call to now.
      set newInput to oldInput.
    } else {
      set D to (P - oldP)/dT. // crude fake derivative of P
      set I to oldI + P*dT. // crude fake integral of P
      set newInput to Kp*P + Ki*I + Kd*D.
    }.
  }.

  // remember old values for next time.
  set pid_array[3] to seekVal.
  set pid_array[4] to P.
  set pid_array[5] to I.
  set pid_array[6] to D.
  set pid_array[7] to t.
  set pid_array[8] to newInput.

  return newInput.
}.
