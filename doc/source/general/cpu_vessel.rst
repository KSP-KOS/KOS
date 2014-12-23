.. _cpu vessel:

CPU Vessel (SHIP)
=================

.. note::

    When kOS documentation refers to the "CPU vessel", it has the following definition:

    - The "CPU Vessel" is whichever vessel happens to currently contain the CPU in which the executing code is running.

It's important to distinguish this from "active vessel", which is a KSP term referring to whichever vessel the camera is centered on, and therefore the vessel that will receive the keyboard controls for ``W`` ``A`` ``S`` ``D`` and so on.

The two terms can differ when you are in a situation where there are two vessels near each other, both of them within full physics range (i.e. 2.5 km), such as would happen during a docking operation. In such a situation it is possible for kOS programs to be running on one, both, or neither of the two vessels. The vessel on which a program is executing is not necessarily the vessel the KSP game is currently considering the "active" one.

.. note::

    The built-in variable called ``SHIP`` is always set to the current CPU vessel. Whenever you see the documentation refer to CPU vessel, you can think of that as being "the ``SHIP`` variable".

For all places where a kOS program needs to do something with *this vessel*, for the sake of centering |SHIP-RAW| coordinates, for the sake of deciding which ship is having maneuver nodes added to it and for the sake of deciding which vessel is being controlled by the autopilot. The vessel it is referring to is itself the **CPU vessel** and not necessarily what KSP thinks of as the "active vessel".

.. |SHIP-RAW| replace:: :ref:`SHIP-RAW <ship-raw>`
