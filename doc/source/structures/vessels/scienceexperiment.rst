.. _scienceexperimentmodule:

ScienceExperimentModule
=======================

The type of structures returned by kOS when querying a module that contains a science experiment.

Some of the science-related tasks are normally not available to kOS scripts. It is for
example possible to deploy a science experiment::

    SET P TO SHIP:PARTSNAMED("GooExperiment")[1].
    SET M TO P:GETMODULE("ModuleScienceExperiment").
    M:DOEVENT("observe mystery goo").

Hovewer, this results in a dialog being shown to the user. Only from that dialog it is possible
to reset the experiment or transmit the experiment results back to Kerbin.
:struct:`ScienceExperimentModule` structure introduces a few suffixes that allow the player
to perform all science-related tasks without any manual intervention::

    SET P TO SHIP:PARTSNAMED("GooExperiment")[0].
    SET M TO P:GETMODULE("ModuleScienceExperiment").
    M:DEPLOY.
    WAIT UNTIL M:HASDATA.
    M:TRANSMIT.

Please note the use of :code:`WAIT UNTIL M:HASDATA`.

This structure should work well with stock science experiments. Mods that introduce their own
science parts might not be compatible with it. One notable example is SCANsat. Even though
SCANsat parts look and behave very similarly to stock science experiments under the hood
they work very differently. Other mods can cause problems as well, please test them before use.

:ref:`DMagic Orbital Science <orbitalscience>` has dedicated support in kOS and should work
properly.

.. structure:: ScienceExperimentModule

    .. list-table::
        :header-rows: 1
        :widths: 2 1 4

        * - Suffix
          - Type
          - Description

        * - All suffixes of :struct:`PartModule`
          -
          - :struct:`ScienceExperimentModule` objects are a type of :struct:`PartModule`
        * - :meth:`DEPLOY()`
          -
          - Deploy and run the science experiment
        * - :meth:`RESET()`
          -
          - Reset this experiment if possible
        * - :meth:`TRANSMIT()`
          -
          - Transmit the scientific data back to Kerbin
        * - :meth:`DUMP()`
          -
          - Discard the data
        * - :attr:`INOPERABLE`
          - :struct:`Boolean`
          - Is this experiment inoperable
        * - :attr:`RERUNNABLE`
          - :struct:`Boolean`
          - Can this experiment be run multiple times
        * - :attr:`DEPLOYED`
          - :struct:`Boolean`
          - Is this experiment deployed
        * - :attr:`HASDATA`
          - :struct:`Boolean`
          - Does the experiment have scientific data
        * - :attr:`DATA`
          - :struct:`List` of :struct:`ScienceData`
          - List of scientific data obtained by this experiment

.. note::

    A :struct:`ScienceExperimentModule` is a type of :struct:`PartModule`, and therefore can use all the suffixes of :struct:`PartModule`.

.. method:: ScienceExperimentModule:DEPLOY()

    Call this method to deploy and run this science experiment. This method will fail if the experiment already contains scientific
    data or is inoperable.

.. method:: ScienceExperimentModule:RESET()

    Call this method to reset this experiment. This method will fail if the experiment is inoperable.

.. method:: ScienceExperimentModule:TRANSMIT()

    Call this method to transmit the results of the experiment back to Kerbin. This will render the experiment
    inoperable if it is not rerunnable. This method will fail if there is no data to send.

.. method:: ScienceExperimentModule:DUMP()

    Call this method to discard the data obtained as a result of running this experiment.

.. attribute:: ScienceExperimentModule:INOPERABLE

    :access: Get only
    :type: :struct:`Boolean`

    True if this experiment is no longer operable.

.. attribute:: ScienceExperimentModule:RERUNNABLE

    :access: Get only
    :type: :struct:`Boolean`

    True if this experiment can be run multiple times.

.. attribute:: ScienceExperimentModule:DEPLOYED

    :access: Get only
    :type: :struct:`Boolean`

    True if this experiment is deployed.

.. attribute:: ScienceExperimentModule:HASDATA

    :access: Get only
    :type: :struct:`Boolean`

    True if this experiment has scientific data stored.

.. attribute:: ScienceExperimentModule:DATA

    :access: Get only
    :type: :struct:`List` of :struct:`ScienceData`

    List of scientific data obtained by this experiment
