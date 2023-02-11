.. _running:

Running Programs
================

.. contents:: Contents
    :local:
    :depth: 2

KerboScript supports running executing code saved in files on the local or
archive disks. The terms "program" and "script" are generally used
interchangably when describing this behavior. Programs can either be run using
one of the :ref:`runfunctions` or the :ref:`run`.  Supported file types are:

    Raw Text KerboScript files
        These files contain KerboScript text commands, as if you had typed them
        directly into the terminal.  They are easily read, written, and edited
        by humans.  These files traditionally have a ``".ks"`` extension,
        however it is not required.
    Compiled Machine Language Files
        These files contain encoded and compressed machine language opcodes.
        They are not formatted to allow humans to read, write, or edit the
        files.  These files traditionally have a ``".ksm"`` extension, however
        it is not required.

.. seealso::
    :ref:`Compiling`
        An explaination of how scripts are compiled, as well as an explaination
        of how :key:`COMPILE` stores opcodes.
    :ref:`Filename Warning<filename_case_warning>`
        Warning detailing issues with case sensitive host file systems.

.. note::
    If you attempt to run the same program twice from within another script,
    the previously compiled version will be executed without attempting to
    recompile even if the original source script has been modified.  However
    once the program has finished executing and returns to the main terminal
    input, the memory containing the programs is released.  This means that
    every time that a script file is run from the terminal it is recompiled,
    even if the script file has not changed.

.. _runfunctions:

Run Functions
-------------

.. note::

    .. versionchanged:: 1.0.0

        The :func:`RUNPATH` and :func:`RUNONCEPATH` functions were added in
        version 1.0.0.  Previously, only the more limited :key:`RUN`
        command existed.

.. _runpath:
.. _runoncepath:

.. function:: RUNPATH(path)

.. function:: RUNPATH(path, commaSeperatedArgs...)

.. function:: RUNONCEPATH(path)

.. function:: RUNONCEPATH(path, commaSeperatedArgs...)

    :parameter path: Path information pointing to the script file. May be an
        :ref:`Absolute<absolute_paths>` or :ref:`Relative<relative_paths>` path.
    :paramtype path: :struct:`String` or :struct:`Path`

    :parameter commaSeperatedArgs...: A comma seperated list of arguments to
        pass to the program
    :paramtype commaSeperatedArgs...: *Optional*

    .. note::
        The :func:`RUNPATH` or :func:`RUNONCEPATH` functions are nearly
        identical to each other except for what is described below under the
        heading :ref:`run_once`.

    ``RUNPATH`` or ``RUNONCEPATH`` take a list of arguments, the first of
    which is the filename of the program to run, and must evaluate to a
    string.  Any additional arguments after that are optional, and are
    passed in to the program as its parameters it can read::

        RUNPATH( "myfile.ks" ). // Run a program called myfile.ks.
        RUNPATH( "myfile" ). // Run a program called myfile, where kOS will guess
                             // the filename extension you meant, and probably
                             // pick ".ks" for you.
        RUNPATH( "myfile.ks", 1, 2 ). // Run a program called myfile.ks, and
                                      // pass in the values 1 and 2 as its first
                                      // two parameters.

    ``RUNPATH`` or ``RUNONCEPATH`` can also work with any expression for the
    filename as long as it results in a string::

        SET file_base to "prog_num_".
        SET file_num to 3.
        SET file_ending to ".ks".
        RUNPATH( file_base + file_num + file_ending, 1, 2 ).
            // The above has the same effect as if you had done:
            RUNPATH("prog_num_3.ks", 1, 2).

.. _run:

Run Keyword
-----------

.. note:: **You should prefer RUNPATH over RUN**

    The ``RUN`` command is older, and less powerful than the newer
    :ref:`runfunctions`, and is kept around mostly for backward compatibility.
    You can't use a variable or expression to refer to the file name.
    The following examples will throw exceptions (but are compatible with
    the :ref:`runfunctions`)::

        SET filename_variable TO "myfile.ks".
        RUN filename_variable. // Error: a file called "filename_variable" not found.

        RUN "my" + "file" + ".ks".  // Syntax error - a single literal string expected,
                                    // not an expression that returns a string.

    Due to a parsing ambiguity issue, it was impossible to make ``RUN``
    work with any arbitrary expression as the filename without changing its syntax a little in
    a way that would break every old kOS script.  Therefore it was deemed better to
    just add a new function that uses the new syntax instead of changing the syntax
    of ``RUN``.

.. keyword:: RUN [ONCE] path [(commaSeperatedArgs...)]

    :parameter once:
        By using the optional ``ONCE`` keyword parameter you can modify the
        behavior of the ``RUN`` keyword to obey the :ref:`run once logic<run_once>`

    :parameter path: The string (i.e. ``"filename.ks"``) describing the path
        pointing to the script file. May be an :ref:`Absolute<absolute_paths>`
        or :ref:`Relative<relative_paths>` path.
    :paramtype path: :struct:`String` or bare word literal

    :parameter commaSeperatedArgs...: A comma seperated list of arguments to
        pass to the program, surrounded by parenthesis (i.e. ``(arg1, arg2)``)
    :paramtype commaSeperatedArgs...: *Optional*

    The ``RUN`` keyword is only capable of
    using hard-coded program names that were known at the time you wrote
    the script, and expressed as a simple bare word or literal string in
    quotes.  For example, you can do this::

        RUN "myfile.ks".
        RUN myfile.ks. // using a bare word literal string

    If you wish to pass arguments to the program, you may
    optionally add a set of parentheses with an argument list to the
    end of the syntax, like so::

        // All 3 of these work:
        RUN myfile(1,2,3).
        RUN myfile.ks(1,2,3).
        RUM "myfile.ks"(1,2,3).


Details Of Running Programs
---------------------------

.. _run_once:

Running a program "ONCE"
^^^^^^^^^^^^^^^^^^^^^^^^

If the ``RUNONCEPATH`` function is used instead of the ``RUNPATH`` function, or
the optional ``ONCE`` keyword is added to the ``RUN`` command, it means the run
will not actually occur if the program has already been run once during the
current program context.  This is intended mostly for loading library program
files that may have mainline code in them for initialization purposes that you
don't want to get run a second time just because you use the library in two
different subprograms.

``RUN ONCE`` and ``RUNONCEPATH`` mean "Run unless it's already been run, in which
case skip it."

.. warning::
    The "ONCE" component has no effect on how frequently a given program is
    compiled.  Every unique program is compiled exactly once per program context
    execution, and remains in memory until the program finishes and returns
    control to the terminal.

Automatic guessing of full filename
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

For all 3 types of run command (``RUN``, ``RUNPATH``, and ``RUNONCEPATH``),
the following filename "guess" rules are used when the filename given is
incomplete:

- 1: If no path information was present in the filename, then assume the
  file is in the current directory (that's pretty much standard for all
  filename commands).

- 2: Assume if no filename extension such as ``".ks"`` or ``".ksm"`` was given,
  and there is no file found that lacks an extension in the way the
  filename was given, then first try to find a file with the ".ksm"
  extension appended to it, and if that file is not found then try
  to find a file with the ".ks" extension appended to it.

Arguments
^^^^^^^^^

Although the syntax is a bit different for ``RUN`` versus
``RUNPATH`` (and ``RUNONCEPATH``), all 3 techniques allow you to
pass arguments into the program that it sees as its main script
:ref:`parameter <declare parameter>` values.

The following commands do equivalent things::

    RUN "AutoLaunch.ks"( 75000, true, "hello" ).
    RUNPATH("AutoLaunch.ksm", 75000, true, "hello" ).

In both of the above examples, had the program "AutoLaunch.ks"
started with these lines::

    // AutoLaunch.ks program file:
    parameter final_alt, do_countdown, message.
    //
    // rest of program not shown...
    //

Then inside AutoLaunch.ks, ``final_alt`` would be ``75000``,
and ``do_countdown`` would be ``true``, and ``message``
would be ``"hello"``.
