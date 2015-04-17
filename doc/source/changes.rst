.. _changes:

Changes from version to version
===============================

This is a slightly more verbose version of the new features
mentioned in the CHANGELOG, specifically for new features and for
users familiar with older versions of the documentation who want
only a quick update to the docs without reading the entire set
of documentation again from scratch.

.. contents::
    :local:
    :depth: 3

Changes in 0.17.0
-----------------

Physics Ticks not Update Ticks
::::::::::::::::::::::::::::::

The updates have been :ref:`moved to the physics update <physics tick>`
portion of Unity, instead of the animation frame rate updates.
This may affect your preferred CONFIG:IPU setting.  The new move
creates a much more uniform performance across all users, without
penalizing the users of faster computers anymore.  (Previously,
if your computer was faster, you'd be charged more electricity as
the updates came more often).

LIST constructor can now initialize lists
:::::::::::::::::::::::::::::::::::::::::

You can now do this::

    set mylist to list(2,6,1,6,21).

to initialize a :ref:`list of values <list>` from the start, so
you no longer have to have a long list of list:ADD commands to
populate it.
