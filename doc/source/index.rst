.. _index:

Welcome to the **kOS** Documentation Website!
=============================================

**kOS** is a scriptable autopilot modification for **Kerbal Space Program**. It allows you write small programs that automate specific tasks.

.. toctree::
    :includehidden:
    :titlesonly:
    :maxdepth: 5

    Home <self>
    Tutorials <tutorials>
    General <general>
    Language <language>
    Mathematics <math>
    Commands <commands>
    Structures <structures>
    Addons <addons>
    Contribute <contribute>
    About <about>

Introduction to **kOS** and **KerboScript**
===========================================

**KerboScript** is the language used to program the CPU device attached to your vessel and **kOS** is the operating system that interprets the code you write. The program can be as simple as printing the current altitude of the vessel and as complicated as a six-axis autopilot controller taking your vessel from the launchpad to Duna and back! With **kOS**, the sky is *not* the limit.

This mod *is* compatible with `RemoteTech`_, you just have to make sure you copy the program onto the local CPU before it goes out of range of KSC.

Installation
------------

Like other mods, simply merge the contents of the zip file into your
Kerbal Space Program folder.

KerboScript
-----------

**KerboScript** is a programming language that is derived from the language of planet Kerbin, which *sounds* like gibberish to non-native speakers but for some reason is *written* exactly like English. As a result, **KerboScript** is very English-like in its syntax. For example, it uses periods as statement terminators.

The language is designed to be easily accessible to novice programmers, therefore it is case-insensitive, and types are cast automatically whenever possible.

A typical command in **KerboScript** might look like this:

::

    PRINT "Hello World".

Indices and tables
==================

* :ref:`genindex`
* :ref:`modindex`
* :ref:`search`

.. _RemoteTech: https://kerbalstuff.com/mod/134/RemoteTech
