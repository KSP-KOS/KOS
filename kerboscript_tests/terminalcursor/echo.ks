@lazyglobal off.

terminal:putln("enter text to echo:").
local line is "".
until false {
    local c is terminal:input:getchar().
    if(c = terminal:input:enter) {
        terminal:putln(""). //linefeed
        if(line:length > 0) {
            terminal:putln(line).
            set line to "".
        } else {
            break.
        }
    } else {
        terminal:put(c).
        set line to line + c.
    }
}