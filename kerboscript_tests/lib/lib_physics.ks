// A library of routines to help do basic physics calculations

@LAZYGLOBAL off.

// Return the gravity acceleration at SHIP's current location.
declare function g_here {
  return constant():G * ((ship:body:mass)/((ship:altitude + body:radius)^2)).
}.

// Return the force on SHIP due to gravity acceleration at SHIP's current location.
declare function Fg_here {
  return ship:mass*g_here().
}.

