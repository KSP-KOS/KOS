.. _orbitalscience:

DMagic Orbital Science
======================

DMagic Orbital Science is a modification for Squad’s "Kerbal Space Program" (KSP) which adds extra science experiments to the game. Those experiments under the hood work differently
than stock ones and require dedicated support (see :ref:`ScienceExperimentModule <scienceexperimentmodule>`).

- Download: http://spacedock.info/mod/128/DMagic%20Orbital%20Science or https://github.com/DMagic1/Orbital-Science/releases
- Sources: https://github.com/DMagic1/Orbital-Science

Most of the time Orbital Science experiments should work exactly like stock ones,
they inherit all suffixes from :ref:`ScienceExperimentModule <scienceexperimentmodule>`::

  SET P TO SHIP:PARTSTAGGED("")[0].
  SET M TO P:GETMODULE("dmmodulescienceanimate").

  PRINT M:RERUNNABLE.
  PRINT M:INOPERABLE.
  M:DEPLOY.
  WAIT UNTIL M:HASDATA.
  M:TRANSMIT.

All Orbital Science experiments do get an extra :code:`TOGGLE` suffix that activates and
deactivates them::


  SET P TO SHIP:PARTSTAGGED("collector")[0].
  SET M TO P:GETMODULE("dmsolarcollector").

  M:TOGGLE.

`Submersible Oceanography and Bathymetry` has two extra suffixes that turn the experiment's
lights on and off::

  SET P TO SHIP:PARTSTAGGED("bathymetry")[0].
  SET M TO P:GETMODULE("dmbathymetry").

  M:LIGHTSON.
  WAIT 3.
  M:LIGHTSOFF.
