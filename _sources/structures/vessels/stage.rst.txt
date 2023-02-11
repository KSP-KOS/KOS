.. _stage:

Stage
=============

*Contents*

    - :ref:`Staging Example <stagingExample>`
    - :ref:`Staging Function <StageFunction>`
    - :ref:`Staging Structure <stageStructure>`

.. _StagingExample:

Staging Example
---------------


    A very simple auto-stager using :attr:`:READY <stage:ready>`
    ::

        LIST ENGINES IN elist.

        UNTIL false {
            PRINT "Stage: " + STAGE:NUMBER AT (0,0).
            FOR e IN elist {
                IF e:FLAMEOUT {
                    STAGE.
                    PRINT "STAGING!" AT (0,0).

                    UNTIL STAGE:READY {
                        WAIT 0.
                    }

                    LIST ENGINES IN elist.
                    CLEARSCREEN.
                    BREAK.
                }
            }
        }

.. _StageFunction:

Stage Function
--------------

.. global:: Stage

    :return: None

    Activates the next stage if the cpu vessel is the active vessel.  This will
    trigger engines, decouplers, and any other parts that would normally be
    triggered by manually staging.  The default equivalent key binding is the
    space bar.  As with other parameter-less functions, both ``STAGE.`` and
    ``STAGE().`` are acceptable ways to call the function.

    .. note::
        .. versionchanged:: 1.0.1

            The stage function will automatically pause execution until the next
            tick.  This is because some of the results of the staging event take
            effect immediately, while others do not update until the next time
            that physics are calculated.  Calling ``STAGE.`` is essentially
            equivalent to::

                STAGE.
                WAIT 0.

    .. warning::
        Calling the :global:`Stage` function on a vessel other than the active
        vessel will throw an exception.

.. _StageStructure:

Stage Structure
---------------

The "Stage" structure gives you some information about the current stage
of the vessel.

.. structure:: Stage

    .. list-table:: Members
        :header-rows: 1
        :widths: 1 1 1 2

        * - Suffix
          - Type (units)
          - Access
          - Description

        * - :attr:`READY`
          - :struct:`Boolean`
          - Get only
          - Is the craft ready to activate the next stage.
        * - :attr:`NUMBER`
          - :struct:`Scalar`
          - Get only
          - The current stage number for the craft
        * - :attr:`RESOURCES`
          - :struct:`List`
          - Get only
          - the :struct:`List` of :struct:`AggregateResource` in the current stage
        * - :attr:`RESOURCESLEX`
          - :struct:`Lexicon`
          - Get only
          - the :struct:`Lexicon` of name :struct:`String` keyed :struct:`AggregateResource` values in the current stage
        * - :attr:`NEXTDECOUPLER`
          - :struct:`Decoupler` or :struct:`String`
          - Get only
          - one of the nearest :struct:`Decoupler` parts that is going to be activated by staging (not necessarily in next stage). `None` if there is no decoupler.
        * - :attr:`NEXTSEPARATOR`
          - :struct:`Decoupler` or :struct:`String`
          - Get only
          - Alias name for :attr:`NEXTDECOUPLER`
        * - :attr:`DELTAV`
          - :struct:`DeltaV`
          - Get only
          - Gets delta-V information about the current stage.

.. attribute:: Stage:READY

    :access: Get only
    :type: :struct:`Boolean`

    Kerbal Space Program enforces a small delay between staging commands, this is to allow the last staging command to complete. This bool value will let you know if kOS can activate the next stage.

.. attribute:: Stage:NUMBER

    :access: Get only
    :type: :struct:`Scalar`

    Every craft has a current stage, and that stage is represented by a number, this is it!

.. attribute:: Stage:Resources

    :access: Get
    :type: :struct:`List`

    This is a collection of the available :struct:`AggregateResource` for the current stage.

.. attribute:: Stage:Resourceslex

    :access: Get
    :type: :struct:`Lexicon`

    This is a dictionary style collection of the available :struct:`Resource`
    for the current stage.  The :struct:`String` key in the lexicon will match
    the name suffix on the :struct:`AggregateResource`.  This suffix walks the parts
    list entirely on every call, so it is recommended that you cache the value
    if it will be reference repeatedly.

.. attribute:: Stage:NextDecoupler

    :access: Get
    :type: :struct:`Decoupler`

    One of the nearest :struct:`Decoupler` parts that is going to be activated by staging
    (not necessarily in next stage, if that stage does not contain any decoupler, separator,
    launch clamp or docking port with staging enabled). `None` if there is no decoupler.

    This is particularly helpful for advanced staging logic, e.g.:
    ::

        STAGE.
        IF stage:nextDecoupler:isType("LaunchClamp")
            STAGE.
        IF stage:nextDecoupler <> "None" {
            WHEN availableThrust = 0 or (
                stage:resourcesLex["LiquidFuel"]:amount = 0 and
                stage:resourcesLex["SolidFuel"]:amount = 0)
            THEN {
                STAGE.
                return stage:nextDecoupler <> "None".
            }
        }

.. attribute:: Stage:NextSeparator

    :access: Get
    :type: :struct:`Decoupler`

    Alias for :attr:`NEXTDECOUPLER<Stage:NEXTDECOUPLER>`

.. attribute:: Stage:DELTAV

    :type: :struct:`DeltaV`
    :access: Get only

    Returns delta-V information (see :struct:`DeltaV`) about the current stage.::

        // These two lines would do the same thing:
        SET DV TO STAGE:DELTAV.
        SET DV TO SHIP:STAGEDELTAV(SHIP:STAGRENUM).

