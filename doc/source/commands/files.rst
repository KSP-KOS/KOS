.. _files:

File I/O
========

For information about where files are kept and how to deal with volumes see the
:ref:`Volumes <volumes>` page in the general topics section of this
documentation.

.. contents::
    :local:
    :depth: 2


Understanding directories
-----------------------------------

kOS, just as real life filesystems, has the ability to group files into
directories. Directories can contain other directories, which can result in
a tree-like structure.

Directories, contrary to files, do not take up space on the volume. That means
you can have as many directories on your volume as you want.

Paths
-----

kOS uses strings of a specific format as a way of describing the location
of files and directories. We will call them path strings or simply - paths.
They will look familiar to users of most real operating systems. On Windows
for example you might have seen something like this::

  C:\Program Files\Some Directory\SomeFile.exe

Linux users are probably more familiar with paths that look like this::

  /home/user/somefile

kOS's paths are quite similar, this is how a full path string might look like::

  0:/lib/launch/base.ks

There are two types of paths in kOS. Absolute paths explicitly state all data
needed to locate an item. Relative paths describe the location of an item
relative to the current directory or current volume.

Absolute paths
~~~~~~~~~~~~~~

Absolute paths have the following format::

  volumeIdOrName:[/directory/subdirectory/...]/filename

The first slash immediately after the colon is optional.

Examples of valid absolute paths::

  0:flight_data/data.json
  secondcpu: // refers to the root directory of a volume
  1:/boot.ks

You can use a special two-dot directory name - `..` - to denote the parent
of a given directory. In the following example the two paths refer to the same
file::

  0:/directory/subdirectory/../file
  0:/directory/file

A path that points to the parent of the root directory of a volume is considered
invalid. Those paths are all invalid::

  0:..
  0:/../..
  0:/directory/../..

Current directory
~~~~~~~~~~~~~~~~~

To facilitate the way you interact with volumes, directories and files kOS
has a concept of current directory. That means you can make a certain directory
a `default` one and kOS will look for files you pass on to kOS commands in that
directory. Let's say for example that you're testing a script located on the
Archive volume in the `launch_scripts` directory. Normally every time you'd like
to do something with it (edit it, run it, copy it etc) you'd have to tell kOS
exactly where that file is.  That could be troublesome, especially when it would
have to be done multiple times.

Instead you can change your current directory using :code:`cd(path)`
(as in `change directory`) command and then refer to all the files and
directories you need by using their relative paths (read more below).

You can always print out the current directory's path like this::

  PRINT PATH().

Remember that you can print the contents of the current directory using the
:code:`LIST` command (which is a shortcut for :code:`LIST FILES`).

Relative paths
~~~~~~~~~~~~~~

Relative paths are the second way you can create paths. Those paths are
transformed by kOS into absolute paths by adding them to the current directory.

Let's say that you've changed your current directory to :code:`0:/scripts`.
If you pass :code:`launch.ks` path to any command kOS will add it to current
directory and create an absolute path this way::

  CD("0:/scripts").
  DELETEPATH("launch.ks"). // this will remove 0:/scripts/launch.ks
  COPYPATH("../launch.ks", ""). // this will copy 0:/launch.ks to 0:/scripts/launch.ks

As you can see above an empty relative path results in a path pointing to the
current directory.

If a relative path starts with :code:`/` kOS will only take the current
directory's volume and add it to the relative path::

  CD("0:/scripts").
  COPYPATH("/launch.ks", "launch_scripts"). // will copy 0:/launch.ks to 0:/scripts/launch_scripts


Paths and bareword arguments
~~~~~~~~~~~~~~~~~~~~~~~~~~~~

.. warning::

  kOS has historically always allowed you to omit quotes for file names in certain
  cases. Although it is still possible (explanation below) we recommend against
  it now. kOS 1.0 has introduced directory support and as a result the number of
  cases in which omitting quotes would be fine is less than before. Paths like
  :code:`../file` make things very confusing to the kOS parser because
  kerboscript uses a dot to denote the end of an expression. If you're used
  to skipping quotes you might find that now you will often have to add them to make
  the path understandable to kOS. The only case in which you can reliably omit
  quotes is when you want to use simple, relative paths:
  :code:`RUN script.`, :code:`CD(dir.ext)`.

Any of the commands below which use path arguments follow these rules:

-  A path may be an expression which evaluates to a string.
-  A path may also be an undefined identifier
   which does not match a variable name, in which case the bare word
   name of the identifier will be used as the path. If the
   identifier does match a variable name, then it will be evaluated as
   an expression and the variable's contents will be used as the
   path.
-  A bareword path may contain file extensions with dots, provided
   it does not end in a dot.
-  Bareword filenames containing any characters other than A-Z, 0-9, underscore,
   and the period extension separator ('.'), can only be referred to
   using a string expression (with quotes), and cannot be used as a
   bareword expression (without quotes). This makes it impossible to construct
   valid kOS paths that contain slashes using bareword paths - you will
   need to use quotes.
-  If your filesystem is case-sensitive (Linux and sometimes Mac OSX, or
   even Windows if using some kinds of remote network drives), then
   bareword filenames will only work properly on filenames that are all
   lowercase. If you try to use a file with capital letters in the name
   on these systems, you will only be able to do so by quoting it.

Putting the above rules together, you can create paths in any of
the following ways:

-  COPYPATH(myfilename, "1:"). // This is an example of a bareword filename.
-  COPYPATH("myfilename", "1:"). // This is an example of an EXPRESSION
   filename.
-  COPYPATH(myfilename.ks, "1:"). // This is an example of a bareword
   filename.
-  COPYPATH(myfilename.txt, "1:"). // This is an example of a bareword
   filename.
-  COPYPATH("myfilename.ks", "1:"). // This is an example of an EXPRESSION
   filename
-  SET str TO "myfile" + "name" + ".ks". COPYPATH(str, "1:"). // This is an
   example of an EXPRESSION filename


Other data types as paths
~~~~~~~~~~~~~~~~~~~~~~~~~

Whenever kOS expects a path string as an argument you can actually pass
one of the following data types instead:

- :struct:`Path`
- :struct:`Volume` - will use volume's root path
- :struct:`VolumeFile` - will use file's path
- :struct:`VolumeDirectory` - will use directory's path


.. _path_command:

path(pathString)
~~~~~~~~~~~~~~~~

Will create a :struct:`Path` structure representing the given path string. You
can omit the argument to create a :struct:`Path` for the current directory.


scriptpath()
~~~~~~~~~~~~

Will return a :struct:`Path` structure representing the path to the currently
running script.

Volumes
-------

volume(volumeIdOrName)
~~~~~~~~~~~~~~~~~~~~~~

Will return a :struct:`Volume` structure representing the volume with a given
id or name. You can omit the argument to create a :struct:`Volume`
for the current volume.

SWITCH TO Volume|volumeId|volumeName.
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

Changes the current directory to the root directory of the specified volume.
Volumes can be referenced by instances of :struct:`Volume`, their ID numbers
or their names if they've been given one. Understanding how
:ref:`volumes work <volumes>` is important to understanding this command.

Example::

    SWITCH TO 0.                        // Switch to volume 0.
    SET VOLUME(1):NAME TO AwesomeDisk.  // Name volume 1 as AwesomeDisk.
    SWITCH TO AwesomeDisk.              // Switch to volume 1.
    PRINT VOLUME:NAME.                  // Prints "AwesomeDisk".


Files and directories
---------------------

.. warning::

    .. versionchanged:: 1.0.0

        **COPY, RENAME and DELETE are now deprecated**

        Previously you could use the aforementioned commands to manipulate files.
        Currently using them will result in a deprecation message being shown.
        After subdirectories were introduced in kOS 1.0 it was necessary to add
        more flexible commands that could deal with both files and directories.
        The old syntax was not designed with directories in mind. It would also
        make it difficult for the kOS parser to properly handle paths.

        Please update your scripts to use the new commands:
        :ref:`movepath(frompath, topath) <movepath>`,
        :ref:`copypath(frompath, topath) <copypath>` and
        :ref:`deletepath(path) <deletepath>`.
        :ref:`runpath(path) <runpath>`.

LIST
~~~~

Shows a printed list of the files and subdirectories in
the current working directory.

This is actually a shorthand for the longer :code:`LIST FILES` command.

To get the files into a :struct:`LIST` structure you can read in a script (rather
than just printed to the screen), use the :ref:`list files in ... <list files>`
command.

CD(PATH)
~~~~~~~~

Changes the current directory to the one pointed to by the :code:`PATH`
argument. This command will fail if the path is invalid or does not point
to an existing directory.

.. _copypath:

COPYPATH(FROMPATH, TOPATH)
~~~~~~~~~~~~~~~~~~~~~~~~~~

Copies the file or directory pointed to by :code:`FROMPATH` to the location
pointed to :code:`TOPATH`. Depending on what kind of items both paths point
to the exact behaviour of this command will differ:

1. :code:`FROMPATH` points to a file

   - :code:`TOPATH` points to a directory

     The file from :code:`FROMPATH` will be copied to the directory.

   - :code:`TOPATH` points to a file

     Contents of the file pointed to by :code:`FROMPATH` will overwrite
     the contents of the file pointed to by :code:`TOPATH`.

   - :code:`TOPATH` points to a non-existing path

     New file will be created at :code:`TOPATH`, along with any parent
     directories if necessary. Its contents will be set to the contents of
     the file pointed to by :code:`FROMPATH`.

2. :code:`FROMPATH` points to a directory

   If :code:`FROMPATH` points to a directory kOS will copy recursively all
   contents of that directory to the target location.

   - :code:`TOPATH` points to a directory

     The directory from :code:`FROMPATH` will be copied inside the
     directory pointed to by :code:`TOPATH`.

   - :code:`TOPATH` points to a file

     The command will fail.

   - :code:`TOPATH` points to a non-existing path

     New directory will be created at :code:`TOPATH`, along with any
     parent directories if necessary. Its contents will be set to the
     contents of the directory pointed to by :code:`FROMPATH`.

3. :code:`FROMPATH` points to a non-existing path

   The command will fail.

.. _movepath:

MOVEPATH(FROMPATH, TOPATH)
~~~~~~~~~~~~~~~~~~~~~~~~~~

Moves the file or directory pointed to by :code:`FROMPATH` to the location
pointed to :code:`TOPATH`. Depending on what kind of items both paths point
to the exact behaviour of this command will differ, see :code:`COPYPATH` above.

.. _deletepath:

DELETEPATH(PATH)
~~~~~~~~~~~~~~~~

Deleted the file or directory pointed to by :code:`FROMPATH`. Directories are
removed along with all the items they contain.

EXISTS(PATH)
~~~~~~~~~~~~

Returns true if there exists a file or a directory under the given path,
otherwise returns false. Also see :meth:`Volume:EXISTS`.

CREATE(PATH)
~~~~~~~~~~~~

Creates a file under the given path. Will create parent directories if needed.
It will fail if a file or a directory already exists under the given path.
Also see :meth:`Volume:CREATE`.

CREATEDIR(PATH)
~~~~~~~~~~~~~~~

Creates a directory under the given path. Will create parent directories
if needed. It will fail if a file or a directory already exists under the
given path. Also see :meth:`Volume:CREATEDIR`.

OPEN(PATH)
~~~~~~~~~~

Will return a :struct:`VolumeFile` or :struct:`VolumeDirectory` representing the item
pointed to by :code:`PATH`. It will return a :struct:`Boolean` false if there's
nothing present under the given path. Also see :meth:`Volume:OPEN`.


JSON
----

.. _writejson:

WRITEJSON(OBJECT, PATH)
~~~~~~~~~~~~~~~~~~~~~~~

Serializes the given object to JSON format and saves it under the given path.

Go to :ref:`Serialization page <serialization>` to read more about serialization.

Usage example::

    SET L TO LEXICON().
    SET NESTED TO QUEUE().

    L:ADD("key1", "value1").
    L:ADD("key2", NESTED).

    NESTED:ADD("nestedvalue").

    WRITEJSON(l, "output.json").

READJSON(PATH)
~~~~~~~~~~~~~~

Reads the contents of a file previously created using ``WRITEJSON`` and deserializes them.

Go to :ref:`Serialization page <serialization>` to read more about serialization.

Example::


    SET L TO READJSON("output.json").
    PRINT L["key1"].

Miscellaneous
-------------


.. _running:

RUN "program".
~~~~~~~~~~~~~~

When you want to run another kerboscript program, you can do so
with one of these 3 variations of the :code:`RUN` command:

- ``RUNPATH(`` *program_file* *[* ``,`` *comma-separated-arguments*. *]* ``).``
- ``RUNONCEPATH(`` *program_file* *[* ``,`` *comma-separated-arguments*. *]* ``).``
- ``RUN`` *[* ``ONCE`` *]* *program_file that must be a bare word or literal string in quotes* *[* ``(`` *comma-separated-arguments* ``)``.

All of these 3 variations run the specified file as a program, optionally passing
information to the program in the form of a comma-separated list of arguments.

.. note::

    .. versionchanged:: 1.0.0

        The ``RUNPATH`` and ``RUNONCEPATH`` functions were added in
        version 1.0.0.  Previously, only the more limited ``RUN``
        command existed.

You should prefer RUNPATH over RUN
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

The ``RUN`` command is older, and less powerful than the newer ``RUNPATH`` (or
``RUNONCEPATH``) functions, and is kept around mostly for backward compatibility.
See below for what this difference is and how they work:

*(Due to a parsing ambiguity issue, it was impossible to make ``RUN`` work with
any arbitrary expression as the filename without changing its syntax a little in
a way that would break every old kOS script.  Therefore it was deemed better to
just add a new function that uses the new syntax instead of changing the syntax of
``RUN``.)*

.. _runpath:
.. _runoncepath:

RUNPATH and RUNONCEPATH
^^^^^^^^^^^^^^^^^^^^^^^

(The ``RUNPATH`` or ``RUNONCEPATH`` functions are identical to each other
except for what is described below under the heading "The ONCE Keyword".)

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

RUN
^^^

On the other hand, the older ``RUN`` command is only capable of
using hard-coded program names that were known at the time you wrote
the script, and expressed as a simple bare word or literal string in
quotes.  For example, you can do this::

    RUN "myfile.ks".
    RUN myfile.ks. // or this works too.

But you can't do this::

    SET filename_variable TO "myfile.ks".
    RUN filename_variable. // Error: a file called "filename_variable" not found.

    RUN "my" + "file" + ".ks".  // Syntax error - a single literal string expected,
                                // not an expression that returns a string.

(The above techniques would work with the ``RUNONCE`` command.)

Once upon a time this limitation was built into kOS - kOS had to know the names
of every program you're going to invoke and it had to know them when it was
compiling your script, before your script began executing the first statement.
Thus it couldn't come from an expression evaluated at run-time.

For ``RUN``, if you wish to pass arguments to the program, you may
optionally add a set of parentheses with an argument list to the
end of the syntax, like so::

    // All 3 of these work:
    RUN myfile(1,2,3).
    RUN myfile.ks(1,2,3).
    RUM "myfile.ks"(1,2,3).

.. _run_once:

THE "ONCE" KEYWORD
^^^^^^^^^^^^^^^^^^

If the ``RUNONCEPATH`` function is used instead of the ``RUNPATH`` function, or
the optional ``ONCE`` keyword is added to the ``RUN`` command, it means the run
will not actually occur if the program has already been run once during the
current program context.  This is intended mostly for loading library program
files that may have mainline code in them for initialization purposes that you
don't want to get run a second time just because you use the library in two
different subprograms.

``RUN ONCE`` and ``RUNONCEPATH`` mean "Run unless it's already been run, in which
case skip it."

.. note::

    *Limitations on file names used for programs*

    On case-sensitive filesystems typically found on Linux and Mac, you should
    name program files used with the ``run`` command entirely with
    lowercase-only filenames or the system may fail to find them when you
    use the ``run`` command.

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

Full path information is allowed
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

You are allowed to use directory path information
in the string filename you are running (even with
the older ``RUN`` command)::

    // Absolute path names are allowed.
    RUN "0:/archive_lib/myfile.ks".
    RUN ONCE "0:/archive_lib/myfile.ks".
    RUNPATH("0:/archive_lib/myfile.ks").
    RUNONCEPATH("0:/archive_lib/myfile.ks").

    // Relative path names are also allowed.
    RUN "../dir1/myfile.ks"(arg1, arg2).
    RUNPATH("../dir1/myfile.ks", arg1, arg2).

Automatic guessing of full filename
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

For all 3 types of run command (``RUN``, ``RUNPATH``, and ``RUNONCEPATH``),
the following filename "guess" rules are used when the filename given is
incomplete:

- 1: If no path information was present in the filename, then assume the
  file is in the current directory (that's pretty much standard for all
  filename commands).

- 2: Assume if no filename extension such as ".ks" or ".ksm" was given,
  and there is no file found that lacks an extension in the way the
  filename was given, then first try to find a file with the ".ksm"
  extension appended to it, and if that file is not found then try
  to find a file with the ".ks" extension appended to it.

LOG TEXT TO PATH
~~~~~~~~~~~~~~~~

Logs the selected text to a file. Can print strings, or the result of an expression.

Arguments
^^^^^^^^^

-  argument 1: Value you would like to log.
-  argument 2: Path pointing to the file to log into.

Example::

    LOG "Hello" to mylog.txt.    // logs to "mylog.txt".
    LOG 4+1 to "mylog" .         // logs to "mylog.ks" because .ks is the default extension.
    LOG "4 times 8 is: " + (4*8) to mylog.   // logs to mylog.ks because .ks is the default extension.


COMPILE PROGRAM (TO COMPILEDPROGRAM)
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

**(experimental)**

Arguments:

    argument 1
        Path to the source file.
    argument 2
        Path to the destination file. If the optional argument 2 is missing, it will assume it's the same as argument 1, but with a file extension changed to ``*.ksm``.

Pre-compiles a script into an :ref:`Kerboscript ML Executable
image <compiling>` that can be used
instead of executing the program script directly.

The RUN, RUNPATH, or RUNONCEPATH commands (mentioned elsewhere
on this page) can work with either \*.ks script files or \*.ksm
compiled files.

The full details of this process are long and complex enough to be
placed on a separate page.

Please see :ref:`the details of the Kerboscript ML
Executable <compiling>`.

EDIT PATH
~~~~~~~~~

Edits a program pointed to by :code:`PATH`.

Arguments
^^^^^^^^^

-  argument 1: Path of the file for editing.

.. note::

    The Edit feature was lost in version 0.11 but is back again after version
    0.12.2 under a new guise. The new editor is unable to show a monospace
    font for a series of complex reasons involving how Unity works and how
    Squad bundled the KSP game. The editor works, but will be in a proportional
    width font, which isn't ideal for editing code. The best way to edit code
    remains to use a text editor external to KSP, however for a fast peek at
    the code during play, this editor is useful.

Example::

    EDIT filename.       // edits filename.ks
    EDIT filename.ks.    // edits filename.ks
    EDIT "filename.ks".  // edits filename.ks
    EDIT "filename".     // edits filename.ks
    EDIT "filename.txt". // edits filename.txt
