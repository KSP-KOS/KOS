.. _stage:

Stage
=============

*Contents*

    - :global:`EXAMPLE`
    - :global:`READY`
    - :global:`RESOURCES`
    - :struct:`Stage`

A planned velocity change along an orbit. These are the nodes that you can set in the KSP user interface. Setting one through kOS will make it appear on the in-game map view, and creating one manually on the in-game map view will cause it to be visible to kOS.

Access
--------

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
	