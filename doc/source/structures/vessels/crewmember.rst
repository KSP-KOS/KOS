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
          - string
          - crew member's name
        * - :attr:`GENDER`
          - string
          - "Male" or "Female"
        * - :attr:`EXPERIENCE`
          - scalar
          - experience level
        * - :attr:`TRAIT`
          - string
          - "Pilot", "Engineer" or "Scientist"
        * - :attr:`TOURIST`
          - Boolean
          - true if this crew member is a tourist
        * - :attr:`PART`
          - :struct:`Part`
          - part in which the crew member is located


.. attribute:: CrewMember:NAME

    :type: string
    :access: Get only

    crew member's name

.. attribute:: CrewMember:GENDER

    :type: string
    :access: Get only

    "Male" or "Female"

.. attribute:: CrewMember:EXPERIENCE

    :type: scalar
    :access: Get only

    experience level

.. attribute:: CrewMember:TRAIT

    :type: string
    :access: Get only

    crew member's trait (specialization), for example "Pilot"

.. attribute:: CrewMember:TOURIST

    :type: Boolean
    :access: Get only

    true if this crew member is a tourist

.. attribute:: CrewMember:PART

    :type: :struct:`Part`
    :access: Get only

    :struct:`Part` this crew member is located in
