.. _files:

File I/O
========

For information about where files are kept and how to deal with volumes see the :ref:`Volumes <volumes>` page in the general topics section of this documentation.

.. contents::
    :local:
    :depth: 2

.. note::

    *Limitations on file names used for programs*

    All file names used as program names with the ``run`` command must be
    valid identifiers.  They can not contain spaces or special characters. For
    example, you can't have a program named ``this is my-file.ks``.  This rule
    does not necessarily apply to other filenames such as log files.  However
    to use a filename that contains spaces, you will have to put quotes around
    it.

    On case-sensitive filesystems typically found on Linux and Mac, you should
    name program files used with the ``run`` command entirely with
    lowercase-only filenames or the system may fail to find them when you
    use the ``run`` command.

.. warning::

    .. versionchanged:: 0.15

        **Archive location and file extension change**

        The Archive where KerboScript files are kept has been changed from ``Plugins/PluginData/Archive`` to ``Ships/Script``, but still under the top-level **KSP** installation directory. The file name extensions have also changes from ``.txt`` to ``.ks``.

Volume and Filename arguments
-----------------------------

Any of the commands below which use filename arguments, \*\*with the
exception
of the RUN command\*\*, follow these rules:

-  (expression filenames) A filename may be an expression which
   evaluates to a string.
-  (bareword filenames) A filename may also be an undefined identifier
   which does not match a variable name, in which case the bare word
   name of the identifier will be used as the filename. If the
   identifier does match a variable name, then it will be evaluated as
   an expression and the variable's contents will be used as the
   filename.
-  A bareword filename may contain file extensions with dots, provided
   it does not end in a dot.
-  If the filename does not contain a file extension, kOS will pad it
   with a ".ks" extension and use that.

Putting the above rules together, you can refer to filenames in any of
the following ways:

-  copy myfilename to 1. // This is an example of a bareword filename.
-  copy "myfilename" to 1. // This is an example of an EXPRESSION
   filename.
-  copy myfilename.ks to 1. // This is an example of a bareword
   filename.
-  copy myfilename.txt to 1. // This is an example of a bareword
   filename.
-  copy "myfilename.ks" to 1. // This is an example of an EXPRESSION
   filename
-  set str to "myfile" + "name" + ".ks". copy str to 1. // This is an
   example of an EXPRESSION filename

**Limits:**

The following rules apply as limitations to the bareword filenames:

-  The **RUN command only works with bareword filenames**, not
   expression filenames. Every other command works with either type of
   filename.
-  Filenames containing any characters other than A-Z, 0-9, underscore,
   and the period extension separator ('.'), can only be referred to
   using a string expression (with quotes), and cannot be used as a
   bareword expression (without quotes).
-  If your filesystem is case-sensitive (Linux and sometimes Mac OSX, or
   even Windows if using some kinds of remote network drives), then
   bareword filenames will only work properly on filenames that are all
   lowercase. If you try to use a file with capital letters in the name
   on these systems, you will only be able to do so by quoting it.

**Volumes too:**

The rules for filenames also apply to volumes. You may do this for
example:

-  set volNum to 1. copy "myfile" to volNum.


``COMPILE program (TO compiledProgram).``
-----------------------------------------

**(experimental)**

Arguments:

    argument 1
        Name of source file.
    argument 2
        Name of destination file. If the optional argument 2 is missing, it will assume it's the same as argument 1, but with a file extension changed to ``*.ksm``.

Pre-compiles a script into an :ref:`Kerboscript ML Exceutable
image <compiling>` that can be used
instead of executing the program script directly.

The RUN command (elsewhere on this page) can work with either \*.ks
script files or \*.ksm compiled files.

The full details of this process are long and complex enough to be
placed on a separate page.

Please see :ref:`the details of the Kerboscript ML
Executable <compiling>`.

``COPY programFile FROM/TO Volume|volumeId|volumeName.``
--------------------------------------------------------

Arguments
^^^^^^^^^

-  argument 1: Name of target file.
-  argument 2: Target volume.

Copies a file to or from another volume. Volumes can be referenced by
instances of :struct:`Volume`, their ID numbers or their names if they’ve been given one. See LIST,
SWITCH and RENAME.

Understanding how :ref:`volumes
work <volumes>` is important to
understanding this command.

Example::

    SWITCH TO 1.                      // Makes volume 1 the active volume
    COPY file1 FROM 0.                // Copies a file called file1.ks from volume 0 to volume 1
    COPY file2 TO 0.                  // Copies a file called file2.ks from volume 1 to volume 0
    COPY file1.ks FROM 0.             // Copies a file called file1.ks from volume 0 to volume 1
    COPY file2.ksm TO 0.              // Copies a file called file2.ksm from volume 1 to volume 0
    COPY "file1.ksm" FROM 0.          // Copies a file called file1.ksm from volume 0 to volume 1
    COPY "file1" + "." + "ks" FROM 0. // Copies a file called file1.ks from volume 0 to volume 1
    COPY file2.ksm TO CORE:VOLUME.    // Copies a file called file2.ksm to active processor's volume
    COPY file2.ksm TO "other".        // Copies a file called file2.ksm to volume named 'other'


``DELETE filename FROM Volume|volumeId|volumeName.``
----------------------------------------------------

Deletes a file. Volumes can be referenced by instances of :struct:`Volume`, their ID numbers or their names
if they’ve been given one.

Arguments
^^^^^^^^^

-  argument 1: Name of target file.
-  argument 2: (optional) Target volume.

Example::

    DELETE file1.                   // Deletes file1.ks from the active volume.
    DELETE "file1".                 // Deletes file1.ks from the active volume.
    DELETE file1.txt.               // Deletes file1.txt from the active volume.
    DELETE "file1.txt".             // Deletes file1.txt from the active volume.
    DELETE file1 FROM 1.            // Deletes file1.ks from volume 1
    DELETE file1 FROM CORE:VOLUME.  // Deletes file1.ks from active processor's volume
    DELETE file1 FROM "other".      // Deletes file1.ks from volume name 'other'


``EDIT program.``
-----------------

Edits a program on the currently selected volume.

Arguments
^^^^^^^^^

-  argument 1: Name of file for editing.

.. note::

    The Edit feature was lost in version 0.11 but is back again after version 0.12.2 under a new guise. The new editor is unable to show a monospace font for a series of complex reasons involving how Unity works and how squad bundled the KSP game. The editor works, but will be in a proportional width font, which isn't ideal for editing code. The best way to edit code remains to use a text editor external to KSP, however for a fast peek at the code during play, this editor is useful.

Example::

    EDIT filename.       // edits filename.ks
    EDIT filename.ks.    // edits filename.ks
    EDIT "filename.ks".  // edits filename.ks
    EDIT "filename".     // edits filename.ks
    EDIT "filename.txt". // edits filename.txt


``LOG text TO filename.``
-------------------------

Logs the selected text to a file on the local volume. Can print strings, or the result of an expression.

Arguments
^^^^^^^^^

-  argument 1: Value you would like to log.
-  argument 2: Name of file to log into.

Example::

    LOG “Hello” to mylog.txt.    // logs to "mylog.txt".
    LOG 4+1 to "mylog" .         // logs to "mylog.ks" because .ks is the default extension.
    LOG “4 times 8 is: “ + (4*8) to mylog.   // logs to mylog.ks because .ks is the default extension.


``RENAME VOLUME Volume|volumeId|oldVolumeName TO name.``
--------------------------------------------------------

``RENAME FILE oldName TO newName.``
-----------------------------------

Renames a file or volume. Volumes can be referenced by
instances of :struct:`Volume`, their ID numbers or their names if they’ve been given one.

Arguments
^^^^^^^^^

-  argument 1: Volume/File Name you would like to change.
-  argument 2: New name for $1.

Example::

    RENAME VOLUME 1 TO AwesomeDisk
    RENAME FILE MyFile TO AutoLaunch.

.. _run_once:

``RUN [ONCE] <program>.``
-------------------------

Runs the specified file as a program, optionally passing information to the program in the form of a comma-separated list of arguments in parentheses.

If the optional ``ONCE`` keyword is used after the word ``RUN``, it means
the run will not actually occur if the program has already been run once
during the current program context.  This is intended mostly for loading library
program files that may have mainline code in them for initialization purposes
that you don't want to get run a second time just because you use the library
in two different subprograms.

``RUN ONCE`` means "Run unless it's already been run, in which case skip it."

Arguments
^^^^^^^^^

-  <program>: File to run.
-  comma-separated-args: a list of values to pass into the program.

Example::

    RUN AutoLaunch.ks.
    RUN AutoLaunch.ksm.
    RUN AutoLaunch.      // runs AutoLaunch.ksm if available, else runs AutoLaunch.ks.
    RUN AutoLaunch( 75000, true, "hello" ).
    RUN AutoLaunch.ks( 75000, true, "hello" ).
    RUN AutoLaunch.ksm( 75000, true, "hello" ).

    RUN ONCE myLibrary. // run myLibrary unless it's been run already.

The program that is reading the arguments sees them in the variables it
mentions in :ref:`DECLARE PARAMETER`.

Important exceptions to the usual filename rules for RUN
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

The RUN command does not allow the same sorts of generic open-ended
filenames that the other
file commands allow. This is very important.

RUN only works when the filename is a bareword filename. It cannot use expression filenames::

    RUN "ProgName"   // THIS WILL FAIL.  Run needs a bareword filename.
    SET ProgName to "MyProgram".
    RUN ProgName     // THIS WILL FAIL also.  It will attempt to run a file
                     // called "ProgName.ksm" or "ProgName.ks", when it sees this,
                     // rather than "MyProgram".

The reasons for the exception to how filenames work for the RUN command are
too complex to go into in large detail here. Here's the short version: While
the kOS system does defer the majority of the work of actually compiling
subprogram scripts until run-time, it still has to generate some header info
about them at compile time, and the filename has to be set in stone at that
time. Changing this would require a large re-write of some of the architecture
of the virtual machine.


``SWITCH TO Volume|volumeId|volumeName.``
-----------------------------------------

Switches to the specified volume. Volumes can be referenced by
instances of :struct:`Volume`, their ID numbers or their names if they’ve been given one. See LIST and RENAME. Understanding how
:ref:`volumes work <volumes>` is important to understanding this command.

Example::

    SWITCH TO 0.                        // Switch to volume 0.
    RENAME VOLUME 1 TO AwesomeDisk.     // Name volume 1 as AwesomeDisk.
    SWITCH TO AwesomeDisk.              // Switch to volume 1.
    PRINT VOLUME:NAME.                  // Prints "AwesomeDisk".

``WRITEJSON(OBJECT, FILENAME).``
--------------------------------

Serializes the given object to JSON format and saves it under the given filename on the current volume.

**Important:** only certain types of objects can be serialized. If a type is serializable then that fact
is explicitly mentioned in the type's documentation like so:

.. note::

  This type is serializable.


Usage example::

    SET L TO LEXICON().
    SET NESTED TO QUEUE().

    L:ADD("key1", "value1").
    L:ADD("key2", NESTED).

    NESTED:ADD("nestedvalue").

    WRITEJSON(l, "output.json").

``READJSON(FILENAME).``
-----------------------

Reads the contents of a file previously created using ``WRITEJSON`` and deserializes them. Example::

    SET L TO READJSON("output.json").
    PRINT L["key1"].


.. _boot:

Special handling of files starting with "boot" (example ``boot.ks``)
--------------------------------------------------------------------
**(experimental)**

For users requiring even more automation, the feature of custom boot scripts was introduced. If you have at least 1 file in your Archive volume starting with "boot" (for example "boot.ks", "boot2.ks" or even "boot_custom_script.ks"), you will be presented with the option to choose one of those files as a boot script for your kOS CPU.
 
.. image:: http://i.imgur.com/05kp7Sy.jpg

As soon as you vessel leaves VAB/SPH and is being initialised on the launchpad (e.g. its status is PRELAUNCH) the assigned script will be copied to CPU's local hard disk with the same name.  If kOS is configured to start on the archive, the file will not be copied locally automatically. This script will be run as soon as CPU boots, e.g. as soon as you bring your CPU in physics range or power on your CPU if it was turned off.  You may get or set the name of the boot file using the :ref:`core:bootfilename<core>` suffix.

.. warning::

    .. versionchanged:: 0.18

        **boot file name changed**

        Previously boot files were copied to the local hard disk as "boot.ks".  This behaviour was changed so that boot files could be handled consistently if kOS is configured to start on the Archive.  Some scripts may have terminated with a generic "delete boot." line to clear the boot script.  Going forward you should use the new core:bootfilename suffix when dealing the boot file.

Important things to consider:
	* kOS CPU hard disk space is limited, avoid using complex boot scripts or increase disk space using MM config.
	* Boot script runs immediately on initialisation, it should avoid interaction with parts/modules until physics fully load. It is best to wait for couple seconds or until certain trigger.
	
	
Possible uses for boot scripts:

	* Automatically activate sleeper/background scripts which will run on CPU until triggered by certain condition.
	* Create basic station-keeping scripts - you will only have to focus your probes once in a while and let the boot script do the orbit adjustment automatically.
	* Create multi-CPU vessels with certain cores dedicated to specific tasks, triggered by user input or external events (Robotic-heavy Vessels)
	* Anything else you can come up with
