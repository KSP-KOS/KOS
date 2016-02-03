.. _volume:

Volume
======

Represents a :struct:`kOSProcessor` hard disk or the archive.


.. structure:: Volume

    .. list-table::
        :header-rows: 1
        :widths: 2 1 4

        * - Suffix
          - Type
          - Description

        * - :attr:`FREESPACE`
          - scalar
          - Free space left on the volume

        * - :attr:`CAPACITY`
          - scalar
          - Total space on the volume

        * - :attr:`NAME`
          - `String`
          - Volume name

        * - :attr:`RENAMEABLE`
          - scalar
          - True if the name can be changed

        * - :attr:`FILES`
          - :struct:`List`
          - List of all files on the volume

        * - :attr:`POWERREQUIREMENT`
          - scalar
          - Amount of power consumed when this volume is set as the current 
		
		* - :meth:`DISASSEMBLE(filename)`
		  - `String`
		  - Disassembles a compiled file into a textual format
		
		* - :meth:`ASSEMBLE(assembly,filename)`
		  - `String`
		  - Assembles a textual format into a compiled file

.. attribute:: Volume:FREESPACE

    :type: scalar
    :access: Get only

    Free space left on the volume

.. attribute:: Volume:CAPACITY

    :type: scalar
    :access: Get only

    Total space on the volume

.. attribute:: Volume:NAME

    :type: `String`
    :access: Get only

    Volume name. This name can be used instead of the volumeId with some :ref:`file and volume-related commands<files>`

.. attribute:: Volume:RENAMEABLE

    :type: boolean
    :access: Get only

    True if the name of this volume can be changed. Currently only the name of the archive can't be changed.


.. attribute:: Volume:FILES

    :type: :struct:`List` of :struct:`FileInfo`
    :access: Get only

    List of files on this volume

.. attribute:: Volume:POWERREQUIREMENT

    :type: scalar
    :access: Get only

    Amount of power consumed when this volume is set as the current volume

.. method:: Volume:DISASSEMBLE(filename)

    :parameter filename: (string) Filename of the compiled file

    Disassembles a compiled file into a textual format

.. method:: Volume:ASSEMBLE(assembly,filename)

	:parameter assembly: (string) Assembly representation
    :parameter filename: (string) Filename of the compiled file

    Assembles a textual format into a compiled file
