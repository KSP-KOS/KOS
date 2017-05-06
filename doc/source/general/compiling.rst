.. _compiling:

KerboScript Machine Code
========================

.. contents::
    :local:
    :depth: 1

Compiling to a KSM File
-----------------------

When you run your Kerboscript programs, behind the scenes they get compiled into a form in memory that runs much smoother but at the same time is quite hard for a Kerbal to read and understand. The actual computer hardware built by your friends at Compotronix incorporated actually run the program using these tiny instructions called "opcodes". In the early days of room-sized computers before we were able to get them down to the compact size of just a meter or so across, all programmers had to use this difficult system, referred to as "machine language". Ah, those were heady days, but they were hard.

The commands you actually write when you say something like ``SET X TO 1.0.`` are really a euphemism for these "machine language" opcodes under the surface.

When you try to run your script, the first thing that kOS does is transform your script into this ancient and arcane "machine language" form, storing it in its memory, and from then on it runs using that.

This process of transforming your script into Machine Language, or "ML" is called "Compiling".

The :key:`RUN` and :func:`RUNPATH` commands do this silently, without telling
you.  The :key:`COMPILE` command explicitly compiles the file and saves it for
future use.

.. _threaded_compile:

.. note::
    Compiling scripts takes time, and while the compiler is working it pauses
    execution much like the :ref:`wait command<wait_mainline_trigger>`.  As such
    compiling from mainline code will pause mainline code but allow triggers to
    continue to execute. Compiling from within a trigger will pause both the
    mainline code and all trigger code. Also be aware that the universe will
    continue to move during the compilation, so you should not assume that any
    values for mass, position, velocity, or similar physical properties will
    remain constant through compilation.

    .. versionchanged:: 1.1.0
        The universe continues to update during compilation.  Previous versions
        would freeze the universe while scripts were compiled, effectively
        making them instantaneous in the universe.

The Compile Keyword
~~~~~~~~~~~~~~~~~~~

.. keyword:: COMPILE sourcePath [TO destinationPath]

    :parameter sourcePath: Path information pointing to the source script file
    :parametertype sourcePath: :struct:`String` or bare word string
    :parameter destinationPath: Path information pointing to the destination
        compiled machine language file
    :parametertype destinationPath: *Optional* :struct:`String` or bare word string

    This command will compile the source script file, transforming it from raw
    text into machine language opcodes.  These opcodes are then encoded,
    compressed, and saved in the destination file.

Why Do I Care?
~~~~~~~~~~~~~~

.. warning::

    This is an experimental feature.

The reason it matters is this: Although once it's loaded into memory and running, these opcodes actually have a lot of baggage and take up a lot of space, when they're stored passively on the disk not doing anything, they can be smaller than your script programs are. For one thing, they don't care about your comments (only other Kerbals reading your script do), and they don't care about your indenting (only other Kerbals reading your script do).

So, given that the compiled "ML" codes are the only thing your program really needs to be run, why not just store THAT instead of storing the entire script, and then you can put the ML files on your remote probes instead of putting the larger script files on them.

And THAT is the purpose of the :key:`COMPILE` command.

It does some, but not all, of the compiling work that the RUN (or RUNPATH) command does, and then stores the results in a file that you can run instead of running the original script.

The output of the :key:`COMPILE` command is a file in what we call KSM format.

KSM stands for "KerboScript Machine code", and it has nearly the same information the program will have when it's loaded and running, minus a few extra steps about relocating it in memory.

How to Use KSM Files
--------------------

Let's say that you have 3 programs your probe needs, called:

-  myprog1.ks
-  myprog2.ks
-  myprog3.ks

And that myprog1 calls myprog2 and myprog3, and you normally would call the progam this way::

    SWITCH TO 1.
    COPYPATH( "0:/myprog1", "" ).
    COPYPATH( "0:/myprog2", "" ).
    COPYPATH( "0:/myprog3", "" ).
    RUNPATH("myprog1", 1, 2, "hello").

Then you can put just the compiled KSM versions of them on your vessel and run it this way::

    SWITCH TO ARCHIVE.

    COMPILE "myprog1.ks" to "myprog1.ksm".
    COPYPATH( "0:/myprog1.ksm", "1:/" ).

    COMPILE "myprog2". // If you leave the arguments off, it assumes you are going from .ks to .ksm
    COPYPATH( "0:/myprog2.ksm", "1:/" ).

    COMPILE "myprog3". // If you leave the arguments off, it assumes you are going from .ks to .ksm
    COPYPATH( "0:/myprog2.ksm", "1:/" ).

    SWITCH TO 1.
    RUNPATH("myprog1", 1, 2, "hello").

Default File Naming Conventions
-------------------------------

When you have both a .ks and a .ksm file, the RUN (or RUNPATH) command allows you to specify which one you meant explicitly, like so::

    RUNPATH("myprog1.ks").
    // or this alternate way to say it:
    RUN myprog1.ks.

    RUNPATH("myprog1.ksm").
    // or this alternate way to say it:
    RUM myprog1.ksm.

But if you just leave the file extension off, and do this::

    RUNPATH("myprog1").
    // or this alternate way to say it:
    RUN myprog1.

Then the :key:`RUN` command will first try to run a file called "myprog1.ksm" and if it cannot find such a file, then it will try to run one called "myprog1.ks".

In this way, if you decide to take the plunge and attempt the use of KSM files, you shouldn't have to change the way any of your scripts call each other, provided you just used versions of the filenames without mentioning the file extensions.

Downsides to Using KSM Files
----------------------------

1. Be aware that if you use this feature, you do lose the ability to have the line of code printed out for you when the kOS computer finds an error in your program. It will still tell you what line number the error happened on, but it cannot show you the line of code. Just the number.

2. Know that you cannot view the program inside the in-game editor anymore when you do this. A KSM file will not appear right in the editor. It requires a magic tool called a "hex editor" to properly see what's happening inside the file.

3. **The file isn't always smaller**. There's a threshold at which the KSM file is actually bigger than the source KS file. For large KS files, the KSM file will be smaller, but for short KS files, the KSM file will be bigger, because there's a small amount of overhead they have to store that is only efficient if the data was large enough.

More Reading and Fiddling with Your Bits
----------------------------------------

So, if you are intrigued by all this and want to see how it all *REALLY* works under the hood, Computronix has deciced to make `internal document MLfile-zx1/a <https://github.com/KSP-KOS/KOS/blob/develop/src/kOS.Safe/Compilation/CompiledObject-doc.md>`__ on the basic plan of the ML file system open for public viewing, if you are one of those rare Kerbals that enjoys fiddling with your bits. No, not THOSE kind of bits, the computery kind!
