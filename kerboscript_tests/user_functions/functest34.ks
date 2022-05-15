//
// Test SETSUFFIX on the left side of a SET.
//

// as a function:
function aaa {
  return ship.
}
set aaa:name to "A".
set aaa():name to aaa:name + "B".
set aaa:name to aaa:name + "C".
if aaa:name = "ABC" {
  print "SUCCESS: " + aaa:name + " = " + "ABC".
} else {
  print "FAILURE: " + aaa:name + " <> " + "ABC".
}

// as a lock:
lock bbb to ship.
set bbb:name to "A".
set bbb():name to bbb:name + "B".
set bbb:name to bbb:name + "C".
if bbb:name = "ABC" {
  print "SUCCESS: " + bbb:name + " = " + "ABC".
} else {
  print "FAILURE: " + bbb:name + " <> " + "ABC".
}

//
// Test when it's a longer chain of suffixes:
// Using SKID because it's a thing with settable suffixes:
// 
local VoiceLex is LEX(
  "the_voices",
  LEX(
    "v0",
    GetVoice(0),
    "v1",
    GetVoice(1)
  )
).
// Using lex keys as suffix names to make a chain:
set GetVoice(0):wave to "sine".
set VoiceLex:the_voices:v0:wave to "triangle".
if voicelex:the_voices:v0:wave = "triangle" {
  print "SUCCESS: wave should be triangle and is " + voicelex:the_voices:v0:wave.
} else {
  print "FAILURE: wave should be triangle but is " + voicelex:the_voices:v0:wave.
}
// Using lex keys as array indeces to make a chain:
set GetVoice(1):wave to "sine".
set VoiceLex["the_voices"]["v1"]:wave to "sawtooth".
if voicelex["the_voices"]["v1"]:wave = "sawtooth" {
  print "SUCCESS: wave should be sawtooth and is " + voicelex["the_voices"]["v1"]:wave.
} else {
  print "FAILURE: wave should be sawtooth but is " + voicelex["the_voices"]["v1"]:wave.
}
