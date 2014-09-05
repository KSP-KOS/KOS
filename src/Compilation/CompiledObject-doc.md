CompiledObject doc
==================

The purpose of this file is to explain to other developers how the machine
language file format for kOS is designed to work.  It's too complex a topic
to fit into comments in the code.  (The reason this is not in the KOS_DOC
documents is that it is truly developer-level documentation not intended
for the end user.  Perhaps if others disagree it can be moved into
KOS-DOC, as it is written in Github-friendly MD format.)

Terminology
------------

* **Machine Language** (abrev. ML) : To distinguish it from the
fully-expanded List<Opcode> style of representing a list of
kRISC instructions in a more human-readable assembly-like format,
the file format described here will be called "Machine-Language".
It is meant to match to the kRISC program in an exact one-to-one way,
but everything has been packed down into the tightest form possible,
and any information that can be rebuilt upon loading the file is
missing from the format on purpose.  It is the job of the program
that loads the file to reconstitute all the "missing" redundant data.
* **kRISC** The name for @marianoapp's overhaul of kOS that created
a virtual computer running a virtual assembly code.  For the context
here, when this document mentions kRISC, it's referring to the
slightly more high-level way of representing the kRISC code, as 
C# Collections of the C# type kos.Compilation.Opcode.  For example,
if you are thinking in terms of List<Opcode>, then you're thinking
of what this document is calling *kRISC*

The purpose of CompiledObject.cs is to convert between the kRISC and
the Machine Langauge (ML) formats of storing the same thing.

At What Level of the script compilation?
----------------------------------------

The term *kRISC* is still not that precise because in the act of
compiling a textual kerboscript program into its kRISC representation,
it goes through several steps of transform along the way, all of
which could be thought of as *kRISC*.  For the sake of this document,
we are referring to the state the program is in *just prior* to
being appended into the ProgramContext.  In this state, the program
is stored as *List<CodePart>*, and still has a few symbolic
references that need to be reassigned when the program is actually
loaded into its final location in memory.

The purpose of the ML (*Machine Language*) format is to essentially
store a *List<CodePart>* in the most compact possible binary form.

Goals and Ideas - Compactness
-----------------------------

The first thing to mention about the format is that it is designed
for maximum compactness, NOT human readability, and NOT for simple
programming conveninece.  The goal is to make a format that stores
the compiled program with the least 'wastage' due to indenting
and comments and long variable names, and lets users run code
on their meager small local volumes with them crunched down into
the smallest possible form.

Getting rid of wastage from comments and spacing is simple. 
Because we're storing the compiled format, they're already gone.

But getting rid of wastage from long variable names is a bit more
complex.  Because the "CPU" runs the code in a namespace-aware way,
opcodes refer to values by their variable names (not by their 
offset from a stack position or a heap address), and therefore
variable names cannot be entirely removed from the compiled
file.  Instead the format tries to "mash down" all the names into
a form where each stringy operand is only spelled out once, and
then referred to by index position into a common operand heap
every time it's mentioned.

Future Maintenence for kOS Programmers - adding/editing Opcodes
===============================================================

In order to minimize the effort needed to keep the system up
to date as the kRISC opcodes get more sophisticated and the
kOS system gets smarter, the system uses a lot of reflection
techniques to "learn" what it needs to know to read/write 
the ML file.  This reduces the effort that other kOS 
developers have to do to keep the system working, but they do
need to be aware of a few important things, outlined here.

### Always test ML compiled code too:

Because of the potential for edits of the kRISC system to
break this code if things are not adhered to carefully, it 
should be a policy that any change that affects the language
syntax, or adds a new opcode, MUST REQUIRE running a test
in which code using the new feature is run from compiled ML
code before the feature is considered working and is allowed
to "pass".

### If you edit Opcode, or add a new Opcode, read this section:

**If you ever add or alter kRISC Opcodes, you need to
keep the following rules in mind or else the ML files can't
be read/written.**

*(Note in the future we may make assertions that check
all these rules when KSP is run and detect when there's a
problem.  Using reflection it should be possible to enforce
all these rules progamatically to ensure no mistakes.)*

All Opcodes need a default constructor
--------------------------------------

Any time you create a new Opcode, be sure it can be
instanced using a default (no parameters) constructor.  This
is needed by the reflection code that builds the ML, as it
has to create a dummy instance of each Opcode to read some
of the overridden values (specifically .Name and .Code).

The default constructor does not need to be *public*.  If
you don't want it to be possible to construct your Opcode
without passing arguments to the constructor, you can make
your default constructor *protected*, so it can only be
used by the reflection code that builds the machine language
spec, and not by anyone else.

(Note that if your Opcode has no constructor at all, that
works too, as C# makes a default constructor for you when you
do that.  Many of the existing Opcodes have no explicitly
defined constructor and they work fine.)

All Opcodes need a globally unique value for the .Code property
---------------------------------------------------------------

Opcode.Code is a single byte value that encodes this Opcode in
the ML file.  All Opcodes need to ensure that their Opcode.Code
is a unique value not shared by any other Opcode.  To help
make this uniqueness easier to maintain, Opcode.Code is actually
a byte-sized enum.  To make a new Opcode.Code, edit the enum
*ByteCode* in *src/Compilation/Opcode.cs* to add your new
value, then assign your Opcode's .Code to a get-only property
that retrieves this value.

Use the [MLField] Attribute to mark what goes in the ML file
------------------------------------------------------------

Opcodes have a *lot* of fields that are derived on the fly and
don't really need to be saved in the ML file.  To mark a field
or property as being something that needs to be saved/loaded 
when writing out a ML file, give it the [MLField] attribute
and CompiledObject.cs will automatically use it.

You need not make sure your [MLField] fields are public, as
the code that is loading/reading them is "cheating" by using
reflection that bypasses access protections.  This may seem
like bad design, but the alternative is to allow *everyone*
to write to your fields in order to allow the loader to restore
them from the ML file, and that's even worse design.

Only a few object types can be [MLField]'s
------------------------------------------

The only types of objects that can be [MLField]'s are those
mentioned in the Table that is built in
*CompiledObject.InitTypeData()*.

If you are trying to apply [MLField] to a field or property
that is not one of the types mentioned in
CompiledObject.InitTypeData(), then you need to edit
CompiledObject.InitTypeData() to add your new type, and
also ensure that CompiledObject.WriteSomeBinaryPrimitive() 
and CompiledObject.ReadSomeBinaryPrimitive()
both know how to deal with that type.

In general, only primitives and strings can be used as
[MLField], as they are meant to be the values to Opcodes
in the low level computer code.

Publish and advertise backward-compatible-breakages
---------------------------------------------------

There may come a time when you realize you have to make some
change that would make it impossible to read previously written
ML files - a change that would require that users recompile
their source files into ML first before they can use them.
This is acceptable, but it does require that you tell the users
if you do make such a change, so they know they have to re-run
the compile step.

The Format
==========

"Magic" Number
--------------

byte 0|byte 1|byte 2|byte 4
======|======|======|======
'k'   |0x03  |'X'   |'E' 


The ML file always begins with a 4-byte "magic number" to identify
it as a kEXE type of file.  The magic number is this pattern:

* ASCII lowercase 'k',
* byte value 0x03,  // Not letter 'e', because something has to make it
clear to applications that this file doesn't have printable ascii in it,
as some will assume if they see a string of printable ascii at the start,
that it must be an ascii file.
* ASCII uppercase 'X',
* ASCII uppercase 'E'

Argument Section (%A)
---------------------

The Argument Section immediately follows the Magic Number.

The purpose of the argument seciton is to pack together all the 
existing arguments to Opcodes into one place, to avoid duplication
(i.e. integer zero is only stored once, not the 20 times it appears
in the script, and the string "throttle" is only stored once, not
the several times it appears in the script, and so on).

Note that the only arguments that are packed into the Argument
Section are those that appear as actual arguments to Opcodes in
the kRISC assembly language dumps.  "Arguments" which are in
fact communicated by pushing them onto the stack are not stored 
this way.  For example, if two numbers are being divided, like so:

    push $num1
    push 3.14159
    div

then the arguments *$num* and *3.14159* are arguments to the PUSH 
Opcodes, but they are not arguments to the Div Opcode.

### Argument header: 3 bytes.  "%An"

byte 0           |byte 1                | byte 2
section delimiter|section type character| numArgIndexBytes
=================|======================|====================
'%'              |'A'                   | one of ['1' .. '8']


The argument section starts with the two-byte code formed by the
ASCII string "%A" to demark it's start. 

**numArgIndexBytes**

The first byte immediately after the "%A" is the *numArgIndexBytes*,
a one-digit number that indicates the minimum size of unsigned integer
needed to index into the rest of the argument section.  Note that it
is an ASCII digit, NOT a raw number.  (i.e. the number 2 is stored
here as 0x32, the character '2', rather than as 0x02.)  If
*numArgIndexBytes* is '1', it means the Argument Section is under
256 bytes long, such that you could index into any position within it
using a mere one-byte unsigned integer.  If *numArgIndexBytes* is '2',
it means the Argument Section is between 257 and 65536 bytes long, such
that you could index into any position within it using a 2-byte unsigned
integer, and so on.  Although the format supports any number up to 4
here, it is highly unlikely that anything higher than 2 will ever occur
as that would require an initial kerboscript source file at least 64kB
long, probably much longer in actuality.

The number *numArgIndexBytes* will be important, as it decides later
on how big each argument to the opcodes down in the code will be.
By putting all the argumnets up at the top and referring to them
indirectly, it will cause all the opcode's arguments to take up
the exact same length of bytes in the file, since all their arguments
will be index addresses pointing up into the Argument Section.

### Each argument

byte 0       | bytes 1..n, n varying by arg type
arg type ID  | The result of System.IO.BinaryWriter.Write()
=============|===============================================
from table in InitTypeData() | Binary coding of primitive type

Each actual argument in the Argument Section is encoded by first
using a byte that describes the type, followed by the result
of Binary writing the value out, in whatever the format used
by System.IO.BinaryWriter.Write() is for that type.  The length
of each argument in the Argument section varies depending on the
argument type, and the next argument starts right after the
previous one ended.

Very few types are supported by the system, as it only needs to
support the types used in ML arguments.  The list of the
types supported is this:

arg type ID | Type it represents | Length of bytes 1..n
============|====================|========================
0           | Any null.  Nulls technically don't have a type  | nonexistent.
1           | bool                                            | 1
2           | byte                                            | 1
3           | Int16                                           | 2
4           | Int32                                           | 4
5           | float                                           | 4
6           | double                                          | 8
7           | String                                          | varies by string length.  BinaryWriter writes the string length in UTF7 coding first, then writes the content of the string.  kOS encodes the string as UTF-8.

CodePart sections:
------------------

After the Argument Section, the CodePart sections are just appended one 
after the other.  It will be possible to identify where one stops
and the next begins because they always begin with a function 
section marked by '%F', as explained below.

(Remember, the ML file records a *List<CodePart>* structure.)

byte 0           |byte 1                |byte 2 and up
section delimiter|section type character|Opcode List
=================|======================|======================
'%'              |'F', 'I', or 'M'      |Encoded opcode list

Each CodePart section is composed of the following 3 subsections, in
this order:

* Function Opcode List (%F), which stores CodePart.FunctionsCode
* Initialization Opcode List (%I), which stores CodePart.InitializationCode
* Main Code Opcode List (%M), which stores CodePart.MainCode

The CodePart section has no header.  Instead you know you are
looking at a new CodePart section when you see the start of a new
Function Opcode List (%F)

### CodePart headers:

Each codepart begins with a two byte code starting with a percent
sign followed by a letter: "%F" for functions, "%I" for initializations,
and "%M" for main code.

Note that it is possible to see these strings elsewhere in the file
as well, as a pure coincidence. Only when the percent-sign '%' is 
located in the place where an Opcode's ByteCode would go does it
indicate the start of a new section.

Regardless of whether it's a %F, %I, or %M section, the format of what
follows is the same - the opcodes are listed as described below.

Encoded Opcode List
-------------------

The program code is stored as a list of the opcodes, but because
not all opcodes use up the same amount of ML file space, the only
way to read them all is to read through them sequentially in order.
In order to know where one Opcode starts, you have to know where
the previous one started, and how many arguments it needs.  (i.e.
opcode 1 might take up 1 byte, 2 might need 4 bytes, then maybe
3 needs 2 bytes, and so on.)

The Opcode.Code is stored in the low 7 bits of a single byte.  The high
bit is reserved for flagging whether or not the ML file will store the
line number information for the Opcode.  If the high bit is on, then the
line number will be present.  If the high bit is off, then it won't be.

If the line number is not stored, then that means when loading the code,
the loader will assume the opcode came from the same line number as
the previous opcode in the list.  Because often opcodes occur in
contiguous runs from the same line, this saves quite a few bytes.

The Opcode only stores the source line number data when it differs from
the line number of the previous opcode.

For an Opcode with no [MLField] properties, and source line information:

Opcode.ByteCode     |source line (Int16)
byte enum value     |2 bytes.
====================|===================
(0x?? bit_or 0x80)  | 0x????

For an Opcode with no [MLField] properties, and lacking source line information:

Opcode.ByteCode      ||
byte enum value      ||
=====================||
(0x?? bit_and 0x7f)  ||

(If the opcode does not store line number information, then the high bit
of the opcode is 0.  If the high bit is 1, then there will be a line number)

For an Opcode with 1 [MLField] property, with source line information:

Opcode.ByteCode     |source line (Int16)|Index into argument section|
byte enum value     |2 bytes.           |length = numArgIndexBytes  |
====================|===================|===========================|
(0x?? bit_or 0x80)  | 0x????            |??                         |

For an Opcode with 1 [MLField] property, without source line information:

Opcode.ByteCode      |Index into argument section|
byte enum value      |length = numArgIndexBytes  |
=====================|===========================|
(0x?? bit_and 0x7f)  |??                         |

For an Opcode with 2 [MLField] properties, with source line information:

Opcode.ByteCode     |source line (Int16)|Index into argument section|Index into argument section|
byte enum value     |2 bytes.           |length = numArgIndexBytes  |length = numArgIndexBytes  |
====================|===================|===========================|===========================|
(0x?? bit_or 0x80)  | 0x????            |??                         |??                         |

For an Opcode with 2 [MLField] properties, without source line information:

Opcode.ByteCode      |Index into argument section|Index into argument section|
byte enum value      |length = numArgIndexBytes  |length = numArgIndexBytes  |
=====================|===========================|===========================|
(0x?? bit_and 0x7f)  |??                         |??                         |

And so on.... for any 'N' of [MLField] properties.

### ByteCode

All opcodes start with a single byte that identifies the type of
Opcode it is.  Each derived subclass of Opcode uses a different
ByteCode.  The mapping of ByteCode to Opcode can be seen in the 
definition of the ByteCOde enum in *src/Compilation/Opcode.cs*.

### Arguments

After the Bytecode comes a list of the arguments for the Opcode.
All the fields of the Opcode which have an [MLField()] property
on them will be listed.

The order in which the arguments are listed follows the sort order
mentioned in the constructor for the MLField property.  (In other
wwords, a field with attribute [MLField(100)] would come before one
with attribute [MLField(200)].)

To pack the space down tightly, the actual arguments themselves are
not listed.  Instead each argument is represented by an index into
the Argument Section above (the section marked with '%A'), where
the actual argument is found.  The number of bytes required to
store that index, and thus the length of each argument index in the
packed code, is the value of numArgIndexBytes, which was recorded
above in the Argument Section.  It may be smaller for some ML files
than for others.

The index position is counted starting relative to the byte
immediately following the 'A' in the "%A" of the Argument section.
The index points not to the argument itself, but to the type byte
that immediately preceeds it.

When loading the ML file, the value of the [MLField] field or
property on the Opcode object will be copied from the value
in the Arguments Section of the file.

### Finding the next Opcode

The next encoded Opcode starts in the ML file as soon as the
current one finishes.  To know how many argument indeces need
to be read before getting to the next Opcode, you count the
total number of fields or properties that have [MLField]
Attributes in the Opcode's class definition (including its
base classes)

### Finding the end of the list of Opcodes

You know the list of encoded Opcodes is over when you see an
Opcode who's Code is set to Compilation.ByteCode.DELIMITER,
which is the '%' character that starts the next section, or
when you reach EOF.

An example bringing it all together
===================================

TODO
