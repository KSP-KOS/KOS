.. _gui_overview:

Making GUIs in kOS
==================

.. versionadded:: 1.1.0

Sometimes it is useful to create an in-game set of screen widgets to let
you interact with the kOS program with mouseclicks.  You can create
GUI widgets in kOS for this purpose, making them as complex or as
simple as you like.  The widgets will live inside windows that pop
up on screen as you invoke them.

Be aware that these widgets cannot be operated via the :ref:`telnet <telnet>`
feature.  You need to manipulate them with the mouse on-screen in the
game window.

The topic of how you actually make a GUI widget in kOS is on its
:ref:`own separate page in these documents. <gui>`.  The basic
object type you start with is the :struct:`GUI` structure.
