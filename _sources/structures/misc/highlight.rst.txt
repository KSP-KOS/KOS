.. _highlight:

Part Highlighting
==================

Being able to :ref:`color <color>` tint a :ref:`part <part>` or a collection of parts
can be a powerful visualization to show their placement and status. The part highlighting
structure is defined as follows:

.. function:: HIGHLIGHT(p,c)

    This global function creates a part highlight::

        SET foo TO HIGHLIGHT(p,c).

    where:

    ``p``
        A single :ref:`part <part>`, a list of parts or an :ref:`element <element>`
    ``c``
        A :ref:`color <color>`

.. structure:: HIGHLIGHT

    .. list-table:: Members
        :header-rows: 1
        :widths: 2 1 4

        * - Suffix
          - Type
          - Description

        * - :COLOR
          - :ref:`Color <color>`
          - the color that will be used by the highlight
        * - :ENABLED
          - :struct:`Boolean`
          - controls the visibility of the highlight
		
Example::
    
	list elements in elist.
	
	// Color the first element pink
	SET foo TO HIGHLIGHT( elist[0], HSV(350,0.25,1) ). 
	
	// Turn the highlight off
	SET foo:ENABLED TO FALSE
	
	
	// Turn the highlight back on
	SET foo:ENABLED TO TRUE
