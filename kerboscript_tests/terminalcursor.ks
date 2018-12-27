@lazyglobal off.

local teststring is "This is a teststring".
local words is teststring:split(" ").

local remember is terminal:width.
set terminal:width to teststring:length.
print "The following lines should all look identical".
print teststring. //comparison, do not go back to this line
print words[0].
set terminal:cursorcol to terminal:cursorcol - terminal:width + words[0]:length + 1.
print words[1].
set terminal:cursorrow to terminal:cursorrow - 1.
set terminal:cursorcol to words[0]:length + words[1]:length + 2.
print words[2] + " " + words[3].

set terminal:width to remember.