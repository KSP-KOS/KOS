.. _stage:

Stage
=============

*Contents*

    - :global:`EXAMPLE`
    - :struct:`Stage`

You access the current stage for the vessel the kOS core is attached to with the STAGE: command.

.. global::EXAMPLE	
	
	
    A very simple auto-stager using :READY
	
	LIST ENGINES IN elist.

	UNTIL false {
	    PRINT "Stage: " + STAGE:NUMBER AT (0,0).
		FOR e IN elist {
			IF e:FLAMEOUT {
				STAGE.
				PRINT "STAGING!" AT (0,0).
				
				UNTIL STAGE:READY {	} 
				
				LIST ENGINES IN elist.
				CLEARSCREEN.
				BREAK.    
			}
		}
	}

.. global::NUMBER

	Every craft has a current stage, and that stage is represented by a number, this is it!
	
.. global::RESOURCES
    
	
	
Structure
---------

.. structure:: Stage

    .. list-table:: Members
        :header-rows: 1
        :widths: 1 1 1 2

        * - Suffix
          - Type (units)
          - Access
          - Description

        * - :attr:`READY`
          - bool
          - Get only
          - Is the craft ready to activate the next stage.
        * - :attr:`NUMBER`
          - scalar
          - Get only
          - The current stage number for the craft
        * - :attr:`RESOURCES`
          - :struct:`List`
          - Get only
          - the :struct:`List` of :struct:`Resource` in the current stage
        * - :attr:`RESOURCESLEX`
          - :struct:`List`
          - Get only
          - the :struct:`Lexicon` of name :struct:`String` keyed :struct:`Resource` values in the current stage

.. attribute:: Stage:READY

    :access: Get only
    :type: bool

	Kerbal Space Program enforces a small delay between staging commands, this is to allow the last staging command to complete. This bool value will let you know if kOS can activate the next stage.

.. attribute:: Stage:NUMBER

    :access: Get only
    :type: scalar
	
    Every craft has a current stage, and that stage is represented by a number, this is it!

.. attribute:: Stage:Resources

    :access: Get
    :type: :struct:`List`

    This is a collection of the available :struct:`Resource` for the current stage.

.. attribute:: Stage:Resourceslex

    :access: Get
    :type: :struct:`Lexicon`

    This is a dictionary style collection of the available :struct:`Resource`
    for the current stage.  The :struct:`String` key in the lexicon will match
    the name suffix on the :struct:`Resource`.  This suffix walks the parts
    list entirely on every call, so it is recommended that you cache the value
    if it will be reference repeatedly.
