wait 1.
print "Press any key to begin input test...".
global input is terminal:input:getchar().
global done is false.
print "Input will be echoed back to you.  Press q to quit".
until done {
    if (terminal:input:haschar) {
        set input to terminal:input:getchar().
        if input = "q" {
            set done to true.
        }
        else {
            print "Input read was: " + input + " (ascii " + unchar(input) + ")".
        }
    }
}
