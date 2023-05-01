@lazyglobal off.

local teststring is "This is a teststring".
local words is teststring:split(" ").

print "The following lines should all look identical".
print teststring. //comparison, do not go back to this line
print words[0].
set terminal:cursorcol to words[0]:length + 1.
set terminal:cursorrow to terminal:cursorrow - 1.
terminal:putln(words[1]).
set terminal:cursorrow to terminal:cursorrow - 1.
set terminal:cursorcol to words[0]:length + words[1]:length + 2.
terminal:putln(words[2] + " " + words[3]).
print words[0] + " " + words[1].
terminal:putat(words[0], 0, terminal:cursorrow - 1).
terminal:movecursor(words[0]:length + words[1]:length +2, terminal:cursorrow - 2).
terminal:put(words[2]).
set terminal:cursorrow to terminal:cursorrow + 1.
set terminal:cursorcol to terminal:cursorcol - words[2]:length.
terminal:put(words[2]).
print " " + words[3]. 
