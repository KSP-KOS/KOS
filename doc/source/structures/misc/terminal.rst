.. _terminal:

Terminal
========

The TERMINAL identifier refers to a special structure that lets you access
some of the information about the screen you are running on.

Structure
---------

.. structure:: Terminal

    .. list-table:: Members
        :header-rows: 1
        :widths: 2 1 1 4

	* - Suffix
	  - Type
	  - Get/Set
	  - Description

        * - :attr:`WIDTH`
	  - integer
	  - get and set
	  - Terminal width in characters

        * - :attr:`HEIGHT`
	  - integer
	  - get and set
	  - Terminal height in characters

.. attribute:: Terminal:WIDTH

    :access: Get/Set
    :type: integer.

    If you read the width it will return a number of character cells wide the terminal
    is.  If you set this value, it will cause the terminal to resize. 
    If there's multiple terminals connected to the same CPU part via telnet clients,
    then kOS will attempt to keep them all the same size, and one terminal being resized
    will resize them all.  (caveat: Some terminal types cannot be resized from the
    server side, and therefore this doesn't always work in both directions).

.. attribute:: Terminal:HEIGHT

    :access: Get/Set
    :type: integer.

    If you read the width it will return a number of character cells tall the terminal
    is.  If you set this value, it will cause the terminal to resize. 
    If there's multiple terminals connected to the same CPU part via telnet clients,
    then kOS will attempt to keep them all the same size, and one terminal being resized
    will resize them all.  (caveat: Some terminal types cannot be resized from the
    server side, and therefore this doesn't always work in both directions).

