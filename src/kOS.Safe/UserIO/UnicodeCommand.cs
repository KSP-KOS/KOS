namespace kOS.Safe.UserIO
{
    /// <summary>
    /// A list of extra Unicode command characters we use to generalize command codes, abstracting away
    /// the differences between different terminal models.
    /// <br/>
    /// These are all stored in the Unicode "private use" area in range [0xE000..0xF8FF].
    /// The Unicode Consortium promises that they will never make any official meanings for
    /// these characters, so they are free to use for whatever local private meanings you like.
    /// The only "public" standards for this character range are unofficial ones for
    /// very specialized interest groups.   (For example, by using this range, we have
    /// precluded people from being able to write kOS scripts in Klingon.)
    /// <br/>
    /// WARNING:  EVERY TIME you use these you must always cast them to (char).  This is because
    /// C# does not allow us to do the correct thing here, which is this:
    ///     public enum UnicodeCommand : char { stuff, stuff, stuff,....}
    /// For some reason I cannot fathom, C# lets you pick the type of any enum EXCEPT char, even
    /// though a Unicode char is still a well defined narrow known number of bits and should
    /// effectively work just as well for enums as, say, a ushort.
    /// <br/>
    /// If anyone does have the ambition to get rid of the need for all the casting, they can
    /// go through here and turn all these into const chars if they like.  But that means having
    /// to manually type in their number value for each one instead of just letting the enum
    /// syntax auto-increment it for each successive one.
    /// </summary>
    public enum UnicodeCommand
    {
        //          NOTE ON COMMENTED OUT CODES:
        //          ----------------------------
        // NOTE: Several of these are here for future expansion ideas but are not yet implemented
        // in the individual terminal-specific mappers (TerminalXtermMapper, TerminalVt100Mapper, etc).
        //
        // To avoid the confusion of accidentally using the ones that are not implemented, the unused
        // ones are commented out.  But they're still here for possible future plans.
        //
        
        /// <summary>
        /// Indicates an emergency low-level break or interrupt process signal.  Often a telnet client will send this
        /// character out-of-band immediately, queue-barging in front of whatever else is in the
        /// input queue:
        /// </summary>
        BREAK = 0xE000,
        
        /// <summary>
        /// Indicates that when this character is seen on the output stream, the connection should
        /// close down at that point, after the characters prior to it have been sent out:
        /// </summary>
        DIE,
        
        /// <summary>
        /// Clear the screen and move the cursor to the upper left corner:
        /// </summary>
        CLEARSCREEN,
        
        /// <summary>
        /// (input only) keypress that means "please do a full redraw for me".
        /// </summary>
        REQUESTREPAINT,

        
        /// <summary>
        /// This begin/end character pair indicates that a string is to follow which is meant to be used to tell the terminal
        /// what it should set its titlebar to.  Output-only.<br/>
        ///     Example:  To set the terminal's title to "Vessel A, CPU 1":<br/>
        ///         TITLEBEGIN V e s s e l   A ,   C P U   1 TITLEEND<br/>
        /// </summary>
        TITLEBEGIN, TITLEEND,

        /// <summary>
        /// Begins a cursor move to an exact position.  Expects exactly 2 more<br/>
        /// Unicode chars to follow, interpreted as binary number data (not as characters)<br/>
        /// for the column num and row num, respectively.<br/>
        /// <br/>
        /// Example:   To move the cursor to the 33rd column, 16th row:<br/>
        ///     TELEPORTCURSOR  (char)0x20  (char)0x0f<br/>
        /// Remember that they start counting at zero so col 0x20 is the 33rd column, not the 32nd.<br/>
        /// similarly, row 0x0f is the 16th row, not the 15th.<br/>
        /// 
        /// <br/>
        /// The pretend Unicode terminal counts rows and columns starting from 0.  Many commercial<br/>
        /// terminals use a reckoning starting at 1, so you may have to offset this value when<br/>
        /// implementing a TerminalMapper class.
        /// </summary>
        TELEPORTCURSOR,

        /// <summary>
        /// Indicates moving a cursor up one row.  Can be used both on output to
        /// move the cursor, or on input to encode that the arrow key was pressed.
        /// </summary>
        UPCURSORONE,

        /// <summary>
        /// Indicates moving a cursor down one row.  Can be used both on output to
        /// move the cursor, or on input to encode that the arrow key was pressed.
        /// </summary>
        DOWNCURSORONE, 


        /// <summary>
        /// Indicates moving a cursor left one column.  Can be used both on output to
        /// move the cursor, or on input to encode that the arrow key was pressed.
        /// </summary>
        LEFTCURSORONE, 


        /// <summary>
        /// Indicates moving a cursor right one column.  Can be used both on output to
        /// move the cursor, or on input to encode that the arrow key was pressed.
        /// </summary>
        RIGHTCURSORONE, 

        
        /// <summary>
        /// Indicates moving a cursor to the home position of the row.  Also can be seen
        /// on input to indicate the home key being pressed.
        /// </summary>
        HOMECURSOR,

        /// <summary>
        /// Indicates moving a cursor to the end position of the row.  Also can be seen
        /// on input to indicate the end key being pressed.
        /// </summary>
        ENDCURSOR,

        /// <summary>
        /// Indicates moving a cursor one page up.  Also can be seen on input to indicate the
        /// PgUp key being pressed.
        /// </summary>
        PAGEUPCURSOR,

        /// <summary>
        /// Indicates moving a cursor one page down.  Also can be seen on input to indicate the
        /// PgDn key being pressed.
        /// </summary>
        PAGEDOWNCURSOR,

        /// <summary>
        /// Delete the character to the left of the cursor, and move the cursor left one
        /// space, wrapping to the end of the previous line if at the left edge of the screen.
        /// Also used for input to represent the keypress that does that (the backspace).
        /// </summary>
        DELETELEFT,
        
        /// <summary>
        /// Delete the character the cursor is currently on, and shift the characters to
        /// the right of it one space left to fill the gap.  (Does not wrap because the
        /// terminal doesn't know where the wraparound lines versus true end of lines are.)
        /// Also used for input to represent the keypress that does that (the delete button).
        /// </summary>
        DELETERIGHT,
        
        /// <summary>
        /// Abstracts away all that CR/LF vs LF only versus CR only nonsense.  In the pretend
        /// Unicode terminal we are referring to, we'll map them all to the same character,
        /// this one.  This character means go to the start of the next line.
        /// Also used on input to represent hitting either the return or the enter key.
        /// </summary>
        STARTNEXTLINE,
        
        /// <summary>
        /// Perform a linefeed straight down (the official ASCII definition of a LF char,
        /// where it doesn't go to the start of a line).  You can probably just map this
        /// directly to a LF in most cases and in fact the base mapper class will do that.
        /// </summary>
        LINEFEEDKEEPCOL,
        
        /// <summary>
        /// Go to the start of the current line. withiout line-feeding.  (the official ASCII
        /// definition of the CR character).  You can probably just map this directly
        /// to a CR in most cases, and in fact the base mapper class will do that.
        /// </summary>
        GOTOLEFTEDGE,
        
        /// <summary>
        /// Scroll the screen up one line (like what happens when you hit 'return' when
        /// the cursor is at the bottom left of the screen), but leave the cursor where it is.
        /// </summary>
        SCROLLSCREENUPONE,
        
        /// <summary>
        /// Scroll the screen down one line (like what might happen in a text editor when
        /// you push the cursor up past the top row), but leave the cursor where it is.
        /// </summary>
        SCROLLSCREENDOWNONE,
        
        // /// <summary>
        // /// Indicates moving a cursor [count] rows up.  Expects exactly 1 more Unicode
        // /// char to follow, interpreted as binary number data (not as character) for
        // /// the number of spaces to move.
        // /// </summary>
        // UPCURSORNUM,
        
        // /// <summary>
        // /// Indicates moving a cursor [count] rows down.  Expects exactly 1 more Unicode
        // /// char to follow, interpreted as binary number data (not as character) for
        // /// the number of spaces to move.
        // /// </summary>
        // DOWNCURSORNUM,
        
        // /// <summary>
        // /// Indicates moving a cursor [count] spaces left.  Expects exactly 1 more Unicode
        // /// char to follow, interpreted as binary number data (not as character) for
        // /// the number of spaces to move.
        // /// </summary>
        // LEFTCURSORNUM,
        
        // /// <summary>
        // /// Indicates moving a cursor [count] spaces right.  Expects exactly 1 more Unicode
        // /// char to follow, interpreted as binary number data (not as character) for
        // /// the number of spaces to move.
        // /// </summary>
        // RIGHTCURSORNUM,        

        /// <summary>
        /// Enable or disable actually displaying the cursor.
        /// This is basicly a passthrough of ScreenBuffer.CursorVisible.
        /// </summary>
        SHOWCURSOR, HIDECURSOR,

        /// <summary>
        /// Tell the terminal to resize itself to a new row/col size.  Not all terminals will be capable of doing this.
        /// This can be communicated in either direction - for the client telling the server it has been resized, or
        /// for the server telling the client to it needs to resize itself.
        /// Expects a sequence of 3 characters as follows: <br/>
        ///     RESIZESCREEN Binary_Width_Num Binary_Height_Num <br/>
        /// Where Width_num and Height_num are the numbers directly transcoded into Unicode chars in a binary way.
        /// (For example a height of 66, which is hex 0x32 would end up being sent as the capital letter 'B' which is Unicode 0x0032.).
        /// </summary>
        RESIZESCREEN,        
        
        /// <summary>
        /// Our homemade unicode char that maps to the ascii BEL character that causes a terminal to beep.
        /// </summary>
        BEEP,
        
        /// <summary>
        /// Send this char to put the terminal into reversed color mode.
        /// </summary>
        REVERSESCREENMODE,

        /// <summary>
        /// Send this char to put the terminal into normal foreground color mode.
        /// </summary>
        NORMALSCREENMODE,
        
        /// <summary>
        /// Send this char to put the terminal into visual beep (beeps flash the screen) mode.
        /// NOTE that most terminals refuse to implement an escape code for this, setting the beep mode
        /// is purely a client-side thing on the setup screen for them, so there's a good chance this will get ignored.
        /// As of this writing, none of the Terminal Mappers in kOS actually use this, but it's here in case we
        /// ever support a more modern terminal class in the future that does implement it.
        /// </summary>
        VISUALBEEPMODE,

        /// <summary>
        /// Send this char to put the terminal into normal audio beep mode (beeps make a sound and don't flash the screen).
        /// NOTE that most terminals refuse to implement an escape code for this, setting the beep mode
        /// is purely a client-side thing on the setup screen for them, so there's a good chance this will get ignored.
        /// As of this writing, none of the Terminal Mappers in kOS actually use this, but it's here in case we
        /// ever support a more modern terminal class in the future that does implement it.
        /// </summary>
        AUDIOBEEPMODE
        
    }
    
    // For tracking multiple-character input sequences to remember where it is in the sequence:
    public enum ExpectNextChar {
        NORMAL, RESIZEWIDTH, RESIZEHEIGHT, INTITLE, TELEPORTCURSORCOL, TELEPORTCURSORROW
    }

}
