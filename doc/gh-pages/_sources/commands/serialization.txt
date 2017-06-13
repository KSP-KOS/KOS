.. _serialization:

Serialization
=============

kOS has the ability to transform certain data structures into a format that can be stored in a file or in memory and later
reconstruct those objects without any data loss. In computer science this is usually called *serialization*.

Take for example a slightly complicated data structure - a list that contains other lists. In kOS it would be created like so: `LIST(LIST(1,2,3), LIST(4,5))`.
This is a complex structure that isn't easy for a kOS developer to store in a file. This is where kOS's serialization comes in.

There's no need for you to understand how it works internally. This pages exist primarily to explain two most important things about serialization:

1. **Only certain types of objects can be serialized.** If a type is serializable then that fact is explicitly mentioned in the type's documentation with a note like this one:

.. note::

  This type is serializable.

All collection types (:struct:`List`, :struct:`Lexicon` etc.) are serializable. They can contain other serializable types or primitives (numbers, string, booleans)
and still be serializable.

2. Currently there are 2 functionalities within kOS that use serialization. The first are :ref:`WRITEJSON AND READJSON <writejson>` commands.
They allow to transform data structures into JSON objects and store them in a file. The other functionality is :ref:`communication`.
It serializes messages currently stored on message queues to ConfigNode (KSP data format) and adds them to KSP save files.

It is **important** to remember that any data that you supply to :ref:`WRITEJSON` and :meth:`Connection:SENDMESSAGE` must be serializable.
