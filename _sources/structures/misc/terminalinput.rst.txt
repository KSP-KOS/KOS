.. _terminalinput:


Terminal Input
==============

You can read the user's keyboard input into the kOS terminal
using this structure.  You obtain this structure by calling
:attr:`Terminal:INPUT`.

.. contents:: CONTENTS
    :local:
    :depth: 2

Input is buffered
-----------------

Input is buffered if the user types faster than you process
the input.  (For example, if you have code that reads 1
character per second, and the user types faster than 1
character per second, then the letters they typed "in between"
your reads are not lost.  It just takes time for your
program to catch up to the backlog and finish processing them
all.)  This buffer is active for the entire duration of the
program, which means that you must clear the buffer using
:meth:`TerminalInput:CLEAR` if you need to ensure that the
contents are in response to a prompt.

Input is blocking
-----------------

If you attempt to read a character of input and there are
none available (because the user hasn't typed anything
yet for you to read), then your program will pause and
get stuck there until the user presses a key.  If you want
to check first to find out if a key is available before
you read it, use the :attr:`HASCHAR<TerminalInput:HASCHAR>`
suffix described below.

Detecting special keys
----------------------

You can detect some special keys that don't form normal
ASCII codes, such as the Left arrow, Page Up, and so on.

Internally KOS uses its own mapping of these characters to
its own Unicode codes.  (This is part of the system it uses
to support a few different types of terminal in the telnet
module).  You can see some of these code names and use them
to test against your input characters.  Some of the
suffixes to :struct:`TerminalInput` are for this purpose.

Example::

    set ch to terminal:input:getchar().

    if ch = terminal:input:DOWNCURSORONE {
      print "You typed the down-arrow key.".
    }
    if ch = terminal:input:UPCURSORONE {
      print "You typed the up-arrow key.".
    }

Cannot read control-C
---------------------

You cannot read the control-C character in your program, because it
causes your program to break.

Cannot read "shift" or "alt"
----------------------------

You cannot read the "shift" or "alt" keypresses pressed by themselves
because they send no characters to the terminal until combined with
other characters.  (For example "shift A" sends an "A" character, while
"A" without shift sends a "a" character, but you can't just read the
shift key itself.)  This is a deliberate decision because the kOS
terminal in the game is supposed to be identical to a telnet terminal
window, and you can't "send" these sorts of keypresses as characters
down a stream.

Structure
---------

.. structure:: TerminalInput

    .. list-table:: Members
        :header-rows: 1
        :widths: 2 1 1 4

        * - Suffix
          - Type
          - Get/Set
          - Description

        * - :meth:`GETCHAR`
          - :struct:`String`
          - Get
          - (Blocking) I/O to read the next character of terminal input.

        * - :attr:`HASCHAR`
          - :struct:`Boolean`
          - Get
          - True if there is at least 1 character of input waiting.

        * - :meth:`CLEAR`
          - None
          - Method Call
          - Call this method to throw away all waiting input characters, flushing the input queue.

        * - :attr:`BACKSPACE`
          - :struct:`String`
          - Get
          - A string for testing if the character read is a backspace.

        * - :attr:`DELETERIGHT`
          - :struct:`String`
          - Get
          - A string for testing if the character read is the delete (to the right) key.

        * - :attr:`RETURN`
          - :struct:`String`
          - Get
          - A string for testing if the character read is the return key.

        * - :attr:`ENTER`
          - :struct:`String`
          - Get
          - An alias for :attr:`RETURN`

        * - :attr:`UPCURSORONE`
          - :struct:`String`
          - Get
          - A string for testing if the character read is the up-arrow key.

        * - :attr:`DOWNCURSORONE`
          - :struct:`String`
          - Get
          - A string for testing if the character read is the down-arrow key.

        * - :attr:`LEFTCURSORONE`
          - :struct:`String`
          - Get
          - A string for testing if the character read is the left-arrow key.

        * - :attr:`RIGHTCURSORONE`
          - :struct:`String`
          - Get
          - A string for testing if the character read is the right-arrow key.

        * - :attr:`HOMECURSOR`
          - :struct:`String`
          - Get
          - A string for testing if the character read is the HOME key.

        * - :attr:`ENDCURSOR`
          - :struct:`String`
          - Get
          - A string for testing if the character read is the END key.

        * - :attr:`PAGEUPCURSOR`
          - :struct:`String`
          - Get
          - A string for testing if the character read is the PageUp key.

        * - :attr:`PAGEDOWNCURSOR`
          - :struct:`String`
          - Get
          - A string for testing if the character read is the PageDown key.

.. method:: TerminalInput:GETCHAR

    :access: Get (Method call)
    :return: :struct:`String`

    Read the next character of terminal input.  If the user hasn't typed
    anything in that is still waiting to be read, then this will "block"
    (meaning it will pause the execution of the program) until there
    is a character that has been typed that can be processed.

    The character will be expressed in a string containing 1 char.

    If you need to check against "unprintable" characters such as
    backspace (control-H) and so on, you can do so with the
    :func:`unchar` function, or by using the aliases described elsewhere
    in this structure.

.. attribute:: TerminalInput:HASCHAR

    :access: Get (method call)
    :type: :struct:`Boolean`

    True if there is at least 1 character of input waiting.  If this is
    false then that would mean that an attempt to call :meth:`GETCHAR<TerminalInput:GETCHAR>`
    would block and wait for user input.  If this is true then an attempt
    to call :meth:`GETCHAR<TerminalInput:GETCHAR>` would return immediately with an answer.

    You can simulate non-blocking I/O like so::

        // Read a char if it exists, else just keep going:
        if terminal:input:haschar {
          process_one_char(terminal:input:getchar()).
        }

.. method:: TerminalInput:CLEAR

    :access: Get (method call)
    :return: None

    Call this method to throw away all waiting input characters, flushing
    the input queue.

.. attribute:: TerminalInput:BACKSPACE

    :access: Get
    :type: :struct:`String`

    A string for testing if the character read is a backspace.

.. attribute:: TerminalInput:DELETERIGHT

    :access: Get
    :type: :struct:`String`

    A string for testing if the character read is the delete (to the right) key.

.. attribute:: TerminalInput:RETURN

    :access: Get
    :type: :struct:`String`

    A string for testing if the character read is the return key.

.. attribute:: TerminalInput:ENTER

    :access: Get
    :type: :struct:`String`

.. attribute:: TerminalInput:UPCURSORONE

    :access: Get
    :type: :struct:`String`

    A string for testing if the character read is the up-arrow key.

.. attribute:: TerminalInput:DOWNCURSORONE

    :access: Get
    :type: :struct:`String`

    A string for testing if the character read is the down-arrow key.

.. attribute:: TerminalInput:LEFTCURSORONE

    :access: Get
    :type: :struct:`String`

    A string for testing if the character read is the left-arrow key.

.. attribute:: TerminalInput:RIGHTCURSORONE

    :access: Get
    :type: :struct:`String`

    A string for testing if the character read is the right-arrow key.

.. attribute:: TerminalInput:HOMECURSOR

    :access: Get
    :type: :struct:`String`

    A string for testing if the character read is the HOME key.

.. attribute:: TerminalInput:ENDCURSOR

    :access: Get
    :type: :struct:`String`

    A string for testing if the character read is the END key.

.. attribute:: TerminalInput:PAGEUPCURSOR

    :access: Get
    :type: :struct:`String`

    A string for testing if the character read is the PageUp key.

.. attribute:: TerminalInput:PAGEDOWNCURSOR

    :access: Get
    :type: :struct:`String`

    A string for testing if the character read is the PageDown key.
