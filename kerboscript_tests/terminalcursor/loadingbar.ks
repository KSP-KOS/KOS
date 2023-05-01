@lazyglobal off.

//do a 25 s loading bar in the current line

local row is terminal:cursorrow.
terminal:putln(""). //linefeed the cursor

from {local progress is 0.} until progress = 100 step { set progress to progress+1. } do {
    local bar is "".
    local prog_per_bar is (terminal:width - 4) / 100.0.
    from { local pos is 0.} until pos/prog_per_bar >= progress step { set pos to pos+1. } do {
        terminal:putat("-", pos, row).
    }
    terminal:putat(progress:tostring():padleft(3), terminal:width-4, row).
    terminal:putat("%", terminal:width-1, row).
    wait 0.25.
}