.. _crewmember:

CrewMember
===========

Represents a single crew member of a vessel.

.. structure:: CrewMember

    .. list-table::
        :header-rows: 1
        :widths: 2 1 4

        * - Suffix
          - Type
          - Description

        * - :attr:`NAME`
          - :ref:`string <string>`
          - crew member's name
        * - :attr:`GENDER`
          - :ref:`string <string>`
          - "Male" or "Female"
        * - :attr:`EXPERIENCE`
          - :ref:`scalar <scalar>`
          - experience level (number of stars)
        * - :attr:`TRAIT`
          - :ref:`string <string>`
          - "Pilot", "Engineer" or "Scientist"
        * - :attr:`TOURIST`
          - :ref:`Boolean <boolean>`
          - true if this crew member is a tourist
        * - :attr:`PART`
          - :struct:`Part`
          - part in which the crew member is located


.. attribute:: CrewMember:NAME

    :type: :ref:`string <string>`
    :access: Get only

    crew member's name

.. attribute:: CrewMember:GENDER

    :type: :ref:`string <string>`
    :access: Get only

    "Male" or "Female"

.. attribute:: CrewMember:EXPERIENCE

    :type: :ref:`scalar <scalar>`
    :access: Get only

    experience level (number of stars)

.. attribute:: CrewMember:TRAIT

    :type: :ref:`string <string>`
    :access: Get only

    crew member's trait (specialization), for example "Pilot"

.. attribute:: CrewMember:TOURIST

    :type: :ref:`Boolean <boolean>`
    :access: Get only

    true if this crew member is a tourist

.. attribute:: CrewMember:PART

    :type: :struct:`Part`
    :access: Get only

    :struct:`Part` this crew member is located in
