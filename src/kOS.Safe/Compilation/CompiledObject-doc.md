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
fully-expanded List&lt;Opcode&gt; style of representing a list of
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
if you are thinking in terms of List&lt;Opcode&gt;, then you're thinking
of what this document is calling *kRISC*

The purpose of CompiledObject.cs is to convert between the kRISC and
the Machine Language (ML) formats of storing the same thing.

At What Level of the script compilation?
----------------------------------------

The term *kRISC* is still not that precise because in the act of
compiling a textual kerboscript program into its kRISC representation,
it goes through several steps of transform along the way, all of
which could be thought of as *kRISC*.  For the sake of this document,
we are referring to the state the program is in *just prior* to
being appended into the ProgramContext.  In this state, the program
is stored as *List&lt;CodePart&gt;*, and still has a few symbolic
references that need to be reassigned when the program is actually
loaded into its final location in memory.

The purpose of the ML (*Machine Language*) format is to essentially
store a *List&lt;CodePart&gt;* in the most compact possible binary form.

Goals and Ideas - Compactness
-----------------------------

The first thing to mention about the format is that it is designed
for maximum compactness, NOT human readability, and NOT for simple
programming convenience.  The goal is to make a format that stores
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

**IMPORTANT NOTE:**

As of kOS v1.0.2, the ML file is compressed using the SharpZipLib
reference that was already included with the project.  This further
reduces the size of the file without kOS having to create a new
and unique compression algorithm.  This change does not break
previously compiled programs however, as the unpack logic is able
to identify if the file is gzipped.

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
spec, and not by "normal" code.

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

**All [MLField] members must be *Properties* not *Fields*,
despite the name "MLField". **

You need not make sure your [MLField] members are public, as
the code that is loading/reading them is "cheating" by using
reflection that bypasses access protections.  This may seem
like bad design, but the alternative is to allow *everyone*
to write to your members in order to allow the loader to restore
them from the ML file, and that's even worse design.

### The MLField attribute needs 2 arguments, as follows:

[MLField(int order, bool needsRelocation)]

The *order* is just a number on which to sort, to guarantee that
the arguments get written in the ML file in the same order as
they'll be read back in.  It serves no purpose other than that
and you can pick any arbitrary number here you like as long as
each MLField of your Opcode gets a unique number.  The only
reason it's here is because Reflection operates via hashes that
don't guarantee that fields come out in any particular order,
not even necessarily the order that they appear in the source code.

The *needsRelocation* flag indicates whether or not this member
will be altered upon loading the file into memory by
prepending a string prefix to it.  It's meant for destination
labels and index labels that store the opcode's position in
a string like "@0001".  They will be automatically altered
so as to make them fit with the code in memory from other
programs without causing clashes with them.

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

TODO: Maybe create an assert that will detect when someone makes
an MLField that is not one of these types, and issue a Nag
message about it to remind them to make the aforemetioned change?

PopulateFromMLFields(List<object> fields)
------------------------------------------

If your Opcode has [MLField]'s then it also will need to override
this method:

```
    public override void PopulateFromMLFields(List<object> fields)

```

This is going to be called when creating the Opcode from the ML file.
After your Opcode's default constructor is called (which must exist, as
previously explained), then its PopulateFromMLFields() will be called
with all the values to the MLFields passed in.  You must use those
values to fill in the appropriate fields of your Opcode so it will be
ready to run.  The *fields* list is **guaranteed** to come out in the
same sort order as you gave in your constructors to the [MLField()]
properties.  (i.e. if your opcode has 3 MLFields, that were declared
thusly:

```
    [MLField(100,false)]
    public int x {get;set;} // All [MLField] members must be *Properties* not *Fields*
    [MLField(200,false)]
    public string str {get;set;} // All [MLField] members must be *Properties* not *Fields*
    [MLField(50,false)]
    public double d {get;set;} // All [MLField] members must be *Properties* not *Fields*

```

Then the List<object> fields will be passed in in this order:

* fields[0] is d   (it has sort order 50)
* fields[1] is x   (it has sort order 100)
* fields[2] is str (it has sort order 200)

It's the sort order, NOT the order in which they appear in the
class definition, that decides the ordering you'll see them
in.

An example of a PopulateFromMLFields that you might use in the above
example Opcode might be something like this:

```
      public override void PopulateFromMLFields(List<object> fields)
      {
	  d = (double)(fields[0]);
	  x = (int)(fields[1]);
	  str = (string)(fields[2]);
      }

```
Note that it is safe to write code that just assumes this is what you
will be given, without performing checks on the length of
the fields list and so on.  This is because the ML file was written
out using reflection to read how many MLFields you have and what order
to write them in, and PopulateFromMLFields is written to send them back
to you in the same order they were written.  The only way the fields
can come out differently is if the ML file itself is corrupted, which
will be caught because it will cause all sorts of other errors too in
that case.

Publish and advertise backward-compatible-breakages
---------------------------------------------------

There may come a time when you realize you have to make some
change that would make it impossible to read previously written
ML files - a change that would require that users recompile
their source files into ML first before they can use them.
This is acceptable, but it does require that you tell the users
if you do make such a change, so they know they have to re-run
the compile step.

Examples of changes that would cause a break in backward
compatibility with old compiled ML files:

* Adding, Deleting, re-ordering, or altering the semantic meaning of [MLFields] on an existing Opcode
* Editing the ByteCode enum in such a way as to alter the numerical value that goes with an existing Opcode (i.e. to avoid this, only append to the end of the list when making new Opcodes).
* Changing Opcode.SourceLine to a bigger integer (it's currently a short, and is encoded in the ML file that way).

Examples of changes that will NOT cause a break in backward
compatibility with old compiled ML files:

* Appending a new Opcode to the list of opcodes, and giving it a new Bytecode number.

The Format
==========

*Overview*:

As of kOS v1.0.2, the ML file is automatically stored using gzip compression.
A file that is stored using gzip compression may be identified using the opening
file header, which will begin with 4 bytes:

* decimal: [31, 139, 8, 0]
* hexadecimal: [0x1f, 0x8b, 0x08, 0x00]

The unpack logic checks for this information at the beginning of the file, and
will decompress the content if necessary.  Files that do not include the gzip
header information will not be decompressed.  A file that does not open with
either the gzip header or the ML format described below will throw an error.

The ML file format is a binary file with sections ordered as follows.
Each section is explained in detail below:

* magic number
* Argument Section
* Repeat the following once for each CodePart in the List&lt;CodePart&gt; for the program:
*   * Function Section
*   * Initializations Section
*   * Main COde Section
* Debug Line Number Section


"Magic" Number
--------------

    byte 0|byte 1|byte 2|byte 4
    ======|======|======|======
    'k'   |0x03  |'X'   |'E'


The ML file always begins with a 4-byte "magic number" to identify
it as a kEXE type of file.  The magic number is this pattern:

* ASCII lowercase 'k',
* byte value 0x03,  // Not letter 'e', because something has to make it
clear to applications that this file doesn't have printable ASCII in it,
as some will assume if they see a string of printable ASCII at the start,
that it must be an ASCII file.
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
                                            | 1 = 1-byte addressing.
					    | 2 = 2-byte addressing (i.e. a ushort).
					    | 3 = 3-byte addressing.
					    | 4 = 4-byte addressing (i.e. a uint).
					    |   .. etc..


The argument section starts with the two-byte code formed by the
ASCII string "%A" to demark it's start.

**numArgIndexBytes**

The first byte immediately after the "%A" is the *numArgIndexBytes*,
a one-byte number that indicates the minimum size of unsigned integer
needed to index into this argument section itself.
If *numArgIndexBytes* is '1', it means the Argument Section is under
256 bytes long, such that you could index into any position within it
using a mere one-byte unsigned integer.  If *numArgIndexBytes* is '2',
it means the Argument Section is between 257 and 65536 bytes long, such
that you could index into any position within it using a 2-byte unsigned
integer, and so on.  Although the format supports any number up to 4
here, it is highly unlikely that anything higher than 2 will ever occur
as that would require an initial kerboscript source file at least 64kB
long, probably much longer in actuality.

The addressing index into the Argument section starts counting with
zero being the "%" of the "%A" of the header.  (So the first
actual argument, after the 3-byte header, is going to have address index
0x03).

The number *numArgIndexBytes* will be important, as it decides later
on how big each argument to the opcodes down in the code will be.
By putting all the argumnets up at the top and referring to them
indirectly, it will cause all the opcode's arguments to take up
the exact same length of bytes in the file, since all their arguments
will be index addresses pointing up into the Argument Section.

### Each argument

    byte 0                       | bytes 1..n, n varying by arg type
    arg type ID                  | The result of System.IO.BinaryWriter.Write()
    =============================|=============================================
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

    arg type ID | Type it represents                              | Length of bytes 1..n
    ============|=================================================|========================
    0           | Any null.  Nulls technically don't have a type  | nonexistent.
    1           | bool                                            | 1
    2           | byte                                            | 1
    3           | Int16                                           | 2
    4           | Int32                                           | 4
    5           | float                                           | 4
    6           | double                                          | 8
    7           | String                                          | varies by string length.
	                                                            BinaryWriter writes the
								    string length in UTF7 coding
								    first, then writes the
								    content of the string.
								    kOS encodes the string
								    as UTF-8.
    8           | The magic mark pushed on the stack to help count args | nonexistent - like nulls it has no data.

CodePart sections:
------------------

After the Argument Section, the CodePart sections are just appended one
after the other.  It will be possible to identify where one stops
and the next begins because they always begin with a function
section marked by '%F', as explained below.

(Remember, the ML file records a *List&lt;CodePart&gt;* structure.)

    byte 0           |byte 1                |byte 2 and up
    section delimiter|section type character|Opcode List
    =================|======================|======================
    '%'              |'F', 'I', or 'M'      |Encoded opcode list

Each CodePart section is composed of the following 3 subsections, in
this order:

* **Function Opcode List** (%F), which stores CodePart.FunctionsCode
* **Initialization Opcode List** (%I), which stores CodePart.InitializationCode
* **Main Code Opcode List** (%M), which stores CodePart.MainCode

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

For an Opcode with no [MLField] properties:

    Opcode.Code      |
    byte enum value  |
    =================|
    1 byte for opcode|

For an Opcode with 1 [MLField] property:

    Opcode.Code      |Index into argument section|
    byte enum value  |length = numArgIndexBytes  |
    =================|===========================|
    1 byte for opcode|??                         |

For an Opcode with 2 [MLField] properties:

    Opcode.Code      |Index into argument section|Index into argument section|
    byte enum value  |length = numArgIndexBytes  |length = numArgIndexBytes  |
    =================|===========================|===========================|
    1 byte for opcode|??                         |??                         |

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
where the header of the address section began (the "%" of the
"%A").

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

Special Opcodes for ML file
---------------------------

There are special opcodes that exist ONLY in the ML file.

These special Opocodes are printed into the ML file when it's created,
and they get removed when the ML file is loaded back into memory.  They
exist to store data sparsely in the file in a way that would be verbose
and use up a lot of memory if they were stored in a more natural way.

### OpcodePushRelocateLater

**The problem it's meant to solve:**

Sometimes the operand of an *OpcodePush* is an opcode label address that
needs to be relocated later upon loading the program into memory, much
like what happens with the opcodes derived from *OpcodeBranch*.  This
happens mainly when calling functions (such as the functions created by
the LOCK command) where the way they work is by first pushing the label
of the function's starting opcode, and then executing OpcodeCall, which
reads the label to jump to from the top of the stack.

When these types of Push opcodes are loaded into memory, the branch
label is recalculated, and assigned into the Push's argument.  To store
this properly in the ML file so it would work on realoading would have
meant storing two operands for each OpcodePush - its normal Argument
and its DestinationLabel.  But this is massively inefficient because
they are never BOTH needed at the same time.  They could be both stored
in one argument if there was a flag indicating whether or not this is
an OpcodePush that requires such a reassignment of the label or not.

**The way it solves it:**

To create that "flag", the special opcode *OpcodePushRelocateLater* was
invented.  An *OpcodePushRelocateLater* is identical to an normal
*OpcodePush* except that its argument is meant to be relocated upon
loading the ML file.  By the time the program is actually running,
all the *OpcodePushRelocateLater* opcodes should have been replaced
by normal *OpcodePush*'es.  In fact, to enforce this and detect if
it's not being adhered to, the Execute() method of
*OpcodePushRelocateLater* throws an exception if it's ever called.

### OpcodeLabelReset

**The problem it's meant to solve:**

The Compiler builds opcodes out of the program text with string
labels on them so they can be referred to as the targets of branches
and function calls without tracking their exact position in the
program list.  As the opcodes get moved or inserted into different
sections, their ordering slightly changes, so the index position would
not be good enough to remember the destinations correctly.

But *MOST* of the opcodes' labels *do* in fact count up sequentially
by consecutive numbers embedded in the labels.  *Most* of the time
if one Opcode has a Label of "@0100", then the next one will have an
Opcode of "@0101" and the next one will be "@0102" and then "@0103"
and so on.

The numbers tend to "jump" strangely only when the compiler shifts
its attention to a different section of the code for a bit then switch
back.  For example, when compiling a LOCK statement, it switches
to the function section, compiles the expression there, then switches
back to the mainline code, causing the labels to jump in a way that's
not consecutive because it was counting up while it was doing that.
(i.e. you get labels sequenced like @0251,@0252,@0253,@0267,@0268,
with the jump between @0253 and @0267 because opcodes @0254 through
@0266 were inserted into the functions section instead of into the
main code.)

The problem is that to store the Opcode.Label for *every single*
Opcocde in the ML file would be massively inefficient when most
of the time they are just consecutive and can be just incremented
as the ML unpacker reads through the ML file.

So we want to *only* store the exception cases, not the normal
cases where opcode labels are consecutively counting.

**The way it solves it:**

The ML unpacker assumes the next opcode's Label is equal to the
previous opcode's Label plus one.  Because the Label is really
a string not a number, the definition of "plus one" for this
is as follows:  get the integer value of the trailing digits
at the end of the Label (i.e. 123 for the label @0_123), add 1
to that, and then reformat it into a string using the same padded
number of digits as it had in the original string.

Whenever this will NOT work, and the next label differs from
(previous label +1) in any way, then an *OpcodeLabelReset*
is inserted into the ML file to mark the change in the labels.
The ML unpacker will read this OpcodeLabelReset's argument
as the new label to apply to the next opcode, after which the
sequential counting would begin again unless there's yet
another OpcodeLabelReset to override it.

Example:  You have code like this:

~~~
    Label           Opcode

    myfunction      push 1
    @0033           push $x
    @0034           push $y
    @0035           add
    @0036           sub
    @0037           push @0129
    @0038           call
    @0080           push hello
    @0081           push $playername
    @0082... etc...
~~~

This would get stored in the ML file like so in this order:

* OpcodeLabelReset  myfunction
* push 1     // ML unpacker assigns its label to preceeding label reset: "myfunction"
* OpcodeLabelReset  @0033
* push $x    // ML unpacker assigns its label to preceeding label reset: "@0033"
* push $y    // ML unpacker assumes it's labeled @0033 + 1 = "@0034"
* add        // ML unpacker assumes it's labeled @0034 + 1 = "@0035"
* sub        // ML unpacker assumes it's labeled @0035 + 1 = "@0036"
* pushrelocatelater @0129  // ML unpacker assumes it's labeled @0036 + 1 = "@0037"
* call // ML unpacker assumes it's labeled @0037 + 1 = "@0038"
* OpcodeLabelReset  @0080
* push hello   // ML unpacker assignts its label to preceeding label reset: "@0038"
* push $playername  // ML unpacker assumes it's labeled @0080 + 1 = @0081
* ... etc...


Debug Line Number Section
=========================


>**_TODO_** Do we want to allow this section to be optional, where a user can leave it off entirely with a command flag, and thus produce more compact, but harder to debug, program files?

In order to be able to print line numbers for runtime errors,
it's important to be able to map from an opcode back to the
source line it came from.  Storing a compiled ML file loses
that information.  It's present in the Opcode class as

    short Opcode.SourceLine;
    short Opcode.SourceColume;

But storing that per Opcode would bloat the size of the ML file
by quite a bit.

The means of holding this information must adhere to the
following conditions:

* 1: Don't bother storing the column number.  Go ahead and accept
the loss of that information.
* 2: Because often times (but not always),a contiguous range of
Opcodes comes from a single line number, try to store a mapping
between line numbers and ranges of opcodes rather than a one to
one mapping of line number to opcode.
* 3: Because of the Reverse Polish Notation style of expression
evaluation the stack processor creates, opcodes don't always come
out in the same order as the line numbers do, so the scheme must
be able to account for discontinuous range chunks coming from the
same source line.

The format used to store this information is as follows:

### Debug Line Numbers Section Header

The Debug Line Number Section begins with the delimiter '%D',
followed by a single byte describing the width of bytes needed
to store indeces into the *Encoded Opcode List*, which
much like the number of bytes to store indeces into the Argument
List, depends on the length of that chunk of the ML file.

    byte 0           |byte 1                |byte 2
    section delimiter|section type character|Encoded Opcode List index size
    =================|======================|=====================================
    '%'              |'D'                   |a small number stored in 1 byte:
                                            | 1 = 1-byte addressing.
					    | 2 = 2-byte addressing (i.e. a ushort).
					    | 3 = 3-byte addressing.
					    | 4 = 4-byte addressing (i.e. a uint).
					    |   .. etc..

The addressing index into the Debug Line Numbers section starts
counting with zero being the "%" of the "%F" of the first Function
Section of the First CodePart.  (So the first actual opcode, after
that 3-byte header, is going to have address index 0x03).

For the purpose of this address indexing, the entire list of CodeParts
is considered one long contiguous section.  (In other words, the
numbering doesn't start over again at zero when the next CodePart
starts a new "%F" header - it keeps counting up.)

### Debug Line Numbers

The purpose of the section is to store a mapping that looks conceptually
like this example:

    Source Line   |   Code section index ranges from that line:
    ==============|===============================================
           1      |  [0x02..0x1a]
           3      |  [0x1b..0x1b],[0x1d,0x20],[0x25,0x28]
           4      |  [0x1c..0x1c],
           7      |  [0x21..0x24]

The way you'd read the above table is this:

* Source Line 1 produced the ML code that is stored from
byte 0x02 through byte 0x1a inclusive.
* Source Line 3 produced the ML code that s stored in a
discontinous set of ranges:
*   * The ML code at the single byte 0x1b, and
*   * The ML code at byte 0x1d through byte 0x20 inclusive, and
*   * The ML code at byte 0x25 through byte 0x28 inclusive.
* Source Line 4 produced the ML code that is stored at
the single byte 0x1c
* Source Line 7 produced the ML code that is stored at
bytes 0x21 through 0x24 inclusive.

You can infer in this example that source code lines 2,5, and 6
probably had no code on them at all, being comments or blank space.

*(This is the structure held by the class DebugLineMap in
CompiledObject.cs)*

To encode this into the ML file, the format is:

    2 byte ushort    |  1 byte num ranges  |  N bytes | N bytes ...
    =================|=====================|==========|======== ...
        line number  |   how many ranges   | start    | stop    ... (repeat start/stop for each range given)

With the start,stop repeated x numbers of times where x is the number
given for "how many ranges".  (This does make it impossible
to store more than 255 discontinuous ranges for one line, but
that would be a *really* weird compile result.)

The "N" in "N bytes" refers to however many bytes was given in the
header as the Encoded Opcode List Index Size immediately after the
"%D" of the section start.  Very short programs might be possible
to encode with just 1 byte addressing.  Typical ones will require 2
byte addressing.  Very very rarely it might require 3.  It will probably
never require 4 or more.

These start and stop ranges are encoded in big-endian order manually,
rather than by using the ordering that comes out of BinaryWriter.
The reason for this manual encoding is that BinaryWriter doesn't know
how to make arbitrary numerical types like 3-byte or 5-byte numbers.

An example bringing it all together
===================================

This is a small example of a tiny program that shows it all.  A
very small example was picked because it's possible to see it all
at once in one hexdump:

Original Program as ASCII source:
---------------------------------

    // This is testcode
    declare parameter p1,p2.
    run testcode3(5).
    set a to p1 + p2 + b.

The result as lists of CodePart sections:
-----------------------------------------

This results in a List&lt;CodePart&gt; of 2 CodePart sections:

CodePart Number 0 looks like this:

CodePart 0's .FunctionsCode is a List&lt;Opcode&gt; like this:

    File                 Line:Col IP   opcode operand
    ----                 ----:--- ---- ---------------------
    archive/testcode2       3:1   0000 push $program-testcode3*
    archive/testcode2       3:1   0001 push 0
    archive/testcode2       3:1   0002 eq
    archive/testcode2       3:1   0003 br.false 0
    archive/testcode2       3:1   0004 push $program-testcode3*
    archive/testcode2       3:1   0005 push testcode3
    archive/testcode2       3:1   0006 push null
    archive/testcode2       3:1   0007 call load()
    archive/testcode2       3:1   0008 store
    archive/testcode2       3:1   0009 call $program-testcode3*
    archive/testcode2       3:1   0010 return

CodePart 0's .InitializationsCode is a List&lt;Opcode&gt; like this:

    File                 Line:Col IP   opcode operand
    ----                 ----:--- ---- ---------------------
    archive/testcode2       3:1   0000 push $program-testcode3*
    archive/testcode2       3:1   0001 push 0
    archive/testcode2       3:1   0002 store

CodePart 0's .MainCode is an empty List&lt;Opcode&gt; like this:

    File                 Line:Col IP   opcode operand
    ----                 ----:--- ---- ---------------------

CodePart Number 1 looks like this:

CodePart 1's .FunctionCode is an empty Listx&lt;Opcode&gt; like this:

    File                 Line:Col IP   opcode operand
    ----                 ----:--- ---- ---------------------

CodePart 1's .InitializationsCode is an empty List&lt;Opcode*gt; like this:

    File                 Line:Col IP   opcode operand
    ----                 ----:--- ---- ---------------------

CodePart 1's .MainCode is a List*lt;Opcode&gt; like this:

    File                 Line:Col IP   opcode operand
    ----                 ----:--- ---- ---------------------
    archive/testcode2       2:22  0000 push p2
    archive/testcode2       2:22  0001 swap
    archive/testcode2       2:22  0002 store
    archive/testcode2       2:19  0003 push p1
    archive/testcode2       2:19  0004 swap
    archive/testcode2       2:19  0005 store
    archive/testcode2       3:15  0006 push 5
    archive/testcode2       3:15  0007 call  
    archive/testcode2       4:5   0008 push $a
    archive/testcode2       4:10  0009 push $p1
    archive/testcode2       4:15  0010 push $p2
    archive/testcode2       4:13  0011 add
    archive/testcode2       4:20  0012 push $b
    archive/testcode2       4:18  0013 add
    archive/testcode2       4:5   0014 store

The result as an ML file:
-------------------------

The binary ML file has a hexdump that looks like this:
------------------------------------------------------

    0000000: 6b03 5845 2541 0107 1324 7072 6f67 7261  k.XE%A...$progra
    0000010: 6d2d 7465 7374 636f 6465 332a 0400 0000  m-testcode3*....
    0000020: 0007 0974 6573 7463 6f64 6533 0007 066c  ...testcode3...l
    0000030: 6f61 6428 2907 0270 3207 0270 3104 0500  oad()..p2..p1...
    0000040: 0000 0702 2461 0703 2470 3107 0324 7032  ....$a..$p1..$p2
    0000050: 0702 2462 2546 4e03 4e18 453a 184e 034e  ..$b%FN.N.E:.N.N
    0000060: 1d4e 284c 2934 4c03 4d25 494e 034e 1834  .N(L)4L.M%IN.N.4
    0000070: 254d 2546 2549 254d 4e31 5134 4e35 5134  %M%F%I%MN1Q4N5Q4
    0000080: 4e39 4c28 4e3e 4e42 4e47 3c4e 4c3c 3425  N9L(N>NBNG<NL<4%
    0000090: 4401 0300 0300 1215 192a 2d02 0001 2229  D........*-...")
    00000a0: 0400 012e 38                             ....8  

Note that this is actually a bit larger than the original source file,
but that's only because the original file was so small.  More normal
programs with larger source files will tend to be smaller in their
ML form because of the repeated use of the same identifier names,
the repeated uses of the same numbers like 0 and 1, which the ML
format crunches down.

### Breaking the example down:

It starts with the "magic number":

0000000: **6b03 5845** 2541 0107 1324 7072 6f67 7261  **k.XE**%A...$progra

Then begins the Argument Section Header, "%A" followed by the byte 0x01:

0000000: 6b03 5845 **2541 01**07 1324 7072 6f67 7261  k.XE**%A.**..$progra

The byte 0x01 tells us that the entire Argument section fit in under 256
bytes, such that it can be addressed by just one byte.  A more typical
larger program will probably have 0x02 here.

**Argument at location 0x03 in Argument Section:**

    0000000:                  07 1324 7072 6f67 7261         ..$progra
    0000010: 6d2d 7465 7374 636f 6465 332a            m-testcode3*    

Type code is 0x07, string, followed by the BinaryWriter's encoding
of a string, which starts with a one-byte string length, 0x13,
and then the UTF-8 encoding of the Unicode string (so it looks like
ASCII unless there's some extended chars).

**Argument at location 0x18 in Argument Section:**

    0000010:                               0400 0000              ....
    0000020: 00                                       .               

Type code 0x04, Int32, value = 0

**Argument at location 0x1c in Argument Section:**

    0000020:   07 0974 6573 7463 6f64 6533            ...testcode3    

Type code 0x07, string, stringlength 0x09, value = "testcode3"

Etc, for the rest of the Argument section

Doing something similar for the entire rest of the Argument Section
yields this result:

    index | Type  | value
    ======|=======|=============================================
    0x03  |string | "$program-testcode3*"
    0x18  |Int32  | 0
    0x1d  |string | "testcode3"
    0x28  |null   | (no bytes spent storing value for null.  Typecode is enough.)
    0x29  |string | "load()"
    0x31  |string | "p2"
    0x35  |string | "p1"
    0x39  |Int32  | 5
    0x3e  |string | "$a"
    0x42  |string | "$p1"
    0x47  |string | "$p2"
    0x4e  |string | "$b"

Again, remember that the indeces here start counting zero at 4 bytes in, where
the "%" of the "%A" section starts, not at the start of the whole file
(so index 0x02 is at global file position 0x06).

### First Function Section: "%F"

Opcode 0:

    0000050:                4e03                            N.        

0x4e = ByteCode enum for PUSH.
0x03 = index into Argument Section for "$program-testcode3".

Therefore this is the "push $program-testcode3" operation.

Opcode 1:

    0000050:                     4e18                         N.      

0x4e = ByteCode enum for PUSH.
0x18 = index into Argument Section for (Int32)0

Therefore this is the "push 0" operation.

Opcode 2:

    0000050:                          45                        E     

0x45 = ByteCode enum for EQ. (EQ takes no arguments)

Opcode 3:

    0000050:                            3a 18                    :.   

0x3a = ByteCode enum for BRANCHFALSE.
0x18 = index into Argument Section for (Int32)0

Therefore this is the "br.false 0" operation.

That's the general idea.  Working through the example, you can build up
the entire program.

### Debug Line Number Section

Tacked onto the end of the file is the Debug Line Number
section, starting with this header:

    0000080:                                      25                 %
    0000090: 4401                                     D.              

This says: "Start of debug line num section.  Addresses only take 1 byte."
(A more typical larger source file will usually start this section with
0x25,0x44,0x02 rather than 0x25,0x44,0x01, to indicate 2-byte indexes have
been used in this section).

    0000090: 4401 0300 0300 1215 192a 2d02 0001 2229  D........*-...")
    00000a0: 0400 012e 380d 0a                        ....8..

Because these come from a dictionary hash, note that they don't
necessarily come out in order.  Note how line 3 is mentioned first:

Data for Source Line 3:

    0000090:      0300 0300 1215 192a 2d                .......*-     

"Source line 3 (0x3000) consists of 3 (0x03) ranges of the ML code:
They are ranges [0x00..0x12], [0x15..0x19], and [0x2a..0x2d]."

So if you start counting all the code from the first "%F" function
section in the ML file, these are the ranges of locations of code that
came from source line 3.  (In the Opcode list dump up above, note how many
opcodes came from location "3:1".  They cover a large range.)

Data for Source Line 2:

    0000090:                            02 0001 2229             ...")

"Source line 2 (0x0200) consists of 1 (0x01) range of the ML code:
from [0x22 to 0x29]".

Data for Source Line 4:

    00000a0: 0400 012e 38                             ....8  

"Source line 4 (0x0400) consists of 1 (0x01) range of the ML code:
from [0x2e to 0x38]".

Note that because Source line 1 is a comment, there ended up being no
data for it in the Debug Line Number Section.

Other Supporting changes to other parts of kOS
==============================================

To make this work, a few changes to other things in kOS were needed.

Labels prefixed with "@NNN_" instead of with "KL_",
--------------------------------------------------

### How it used to work:

In the program in memory (the list of Opcodes) the opcode labels
used to be formatted like so:

* KL_0001 for the first instruction
* KL_0002 for the next instruction
* KL_0003 for the next instruction

and so on.

then if a new program was added to the runtime memory (for example
if program1 runs program2, then both program1 and program2 exist
in the list of opcodes at the same time, with program2 appeneded
after program1), then the second program simply left off where the
first one ended.  So if program1 ended on label KL_0150, then
program2 would start with KL_0151.

### How it now works:

The first program loaded into memory uses a prefix of "@" instead
of "KL_".  This change had nothing to do with this compiler
feature, but was just a problem I noticed when looking at the code.
Technically "KL_0001" is a perfectly valid kOS identifier, and thus
could be the name of a function call and thus cause ambiguity in
the program.  Using "@" means that it's a character that can't exist
in a kOS identifier and thus cannot possibly clash with any labels
in user-land.

Now the second program loaded into memory uses a prefix of "@NNN_" for its
labels, in which the NNN refers to the highest instruction that already
existed in the memory before being loaded.  In the previous example
where program1 ended on KL_0150 and program2 starts with KL0151, the
new way it will work is for program1 to end with @0150, and program2
to begin with @150_0001, then @150_0002, then @150_0003, and so on.

### Justification for the change:

Previoiusly, because programs were never compiled until they were being
run right then, the system was aware during compilation of how much of
the memory was already being used before this program would be added to
it.  The built in Compile() function was taking advantage of this
information to know how to label things.  The reason it knew to start
labelling at KL_0151 when compiling program2 was because it knew it was
being loaded into memory just after whatever was there right now.

But now when it's being compiled for use *later*, it doesn't know
that yet at the time of compiling and it needs to be relocatable later.
Therefore it labels everything starting at 1 *as if* it was the first
program being labeled, and then this needs to be relocated when it gets
loaded into memory.

The reason for using a prefix instead of numerically adding is that once
the labels have been created by compilation, I didn't want to rely on
the fact that they were numerically increasing.  It may not be true
forever in the future - since the label is a string in the Opcode,
it can be *any* string, in principle, depending on future design
changes.  This prefix technique allows the code to be "relocatable" by
a string operation that is not dependant on the string contianing a
number.
