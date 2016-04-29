function test_comms {
  parameter connection.
  parameter messages.

  print connection:delay = 0.
  print connection:isconnected.

  messages:clear.
  print messages:empty.
  set m to lex("key1", 1, "key2", "value2", true, ship).
  print connection:sendmessage(m).
  print not messages:empty.

  set received to messages:pop.
  print messages:empty.
  print received:sender = ship.
  print received:content:typename = "Lexicon".
  print received:content[true] = ship.

  print connection:sendmessage(4).
  set received to messages:pop.
  print received:content = 4.

  print connection:sendmessage(true).
  set received to messages:pop.
  print received:content = true.

  print connection:sendmessage("testmessage").
  set received to messages:pop.
  print received:content = "testmessage".
}

// Inter-vessel tests, this doesn't do any long-range tests, vessel simply sends messages to itself

print ship:connection:tostring:contains(ship:name).
test_comms(ship:connection, ship:messages).

// Inter-CPU tests, as before the cpu sends messages to itself

print core:connection:tostring:contains(core:tag).
test_comms(core:connection, core:messages).
