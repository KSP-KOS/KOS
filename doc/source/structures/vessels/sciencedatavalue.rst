.. _sciencedata:

ScienceData
===========

Represents results of a :struct:`science experiment <ScienceExperimentModule>`.

.. structure:: ScienceData

    .. list-table::
        :header-rows: 1
        :widths: 2 1 4

        * - Suffix
          - Type
          - Description

        * - :attr:`TITLE`
          - :struct:`String`
          - Experiment title
        * - :attr:`SCIENCEVALUE`
          - :struct:`Scalar`
          - Amount of science that would be gained by returning this data to KSC
        * - :attr:`TRANSMITVALUE`
          - :struct:`Scalar`
          - Amount of science that would be gained by transmitting this data to KSC
        * - :attr:`DATAAMOUNT`
          - :struct:`Scalar`
          - Amount of data

.. attribute:: ScienceData:TITLE

    :access: Get only
    :type: :struct:`String`

    Experiment title

.. attribute:: ScienceData:SCIENCEVALUE

    :access: Get only
    :type: :struct:`Scalar`

    Amount of science that would be gained by returning this data to KSC

.. attribute:: ScienceData:TRANSMITVALUE

    :access: Get only
    :type: :struct:`Scalar`

    Amount of science that would be gained by transmitting this data to KSC

.. attribute:: ScienceData:DATAAMOUNT

    :access: Get only
    :type: :struct:`Scalar`

    Amount of data
