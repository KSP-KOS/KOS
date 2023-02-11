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
          - :struct:`String`
          - crew member's name
        * - :attr:`GENDER`
          - :struct:`String`
          - "Male" or "Female"
        * - :attr:`EXPERIENCE`
          - :struct:`Scalar`
          - experience level (number of stars)
        * - :attr:`TRAIT`
          - :struct:`String`
          - "Pilot", "Engineer" or "Scientist"
        * - :attr:`TOURIST`
          - :struct:`Boolean`
          - true if this crew member is a tourist
        * - :attr:`PART`
          - :struct:`Part`
          - part in which the crew member is located


.. attribute:: CrewMember:NAME

    :type: :struct:`String`
    :access: Get only

    crew member's name

.. attribute:: CrewMember:GENDER

    :type: :struct:`String`
    :access: Get only

    "Male" or "Female"

.. attribute:: CrewMember:EXPERIENCE

    :type: :struct:`Scalar`
    :access: Get only

    experience level (number of stars)

.. attribute:: CrewMember:TRAIT

    :type: :struct:`String`
    :access: Get only

    crew member's trait (specialization), for example "Pilot"

.. attribute:: CrewMember:TOURIST

    :type: :struct:`Boolean`
    :access: Get only

    true if this crew member is a tourist

.. attribute:: CrewMember:PART

    :type: :struct:`Part`
    :access: Get only

    :struct:`Part` this crew member is located in
