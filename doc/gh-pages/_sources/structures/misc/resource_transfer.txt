.. _resource_transfer:

Resource Transfer 
=================

Usage is covered elsewhere <../commands/resource_transfer.html>`__

Structure
---------

.. structure:: ResourceTransfer

    .. list-table:: Members
        :header-rows: 1
        :widths: 1 1 1 2

        * - Suffix
          - Type (units)
          - Access
          - Description

        * - :attr:`STATUS`
          - :ref:`string <string>`
          - Get only
          - The :ref:`string <string>` status of the transfer (eg "Inactive", "Transferring", "Failed", "Finished")
        * - :attr:`MESSAGE`
          - :ref:`string <string>`
          - Get only
          - A message about the current status
        * - :attr:`GOAL`
          - :ref:`scalar <scalar>`
          - Get only
          - This is how much of the resource will be transferred.
        * - :attr:`TRANSFERRED`
          - :ref:`scalar <scalar>`
          - Get only
          - This is how much of the resource has been transferred.
        * - :attr:`RESOURCE`
          - :ref:`string <string>`
          - Get only
          - The name of the resource (eg oxidizer, liquidfuel)   
        * - :attr:`ACTIVE`
          - :ref:`boolean <boolean>`
          - Get / Set
          - Setting this value will either start, pause or restart a transfer. Default is false.
    
          
.. attribute:: RESOURCETRANSFER:STATUS

    :access: Get only
    :type: :ref:`string <string>`

    This enumerated type shows the status of the transfer. the possible values are:
    
    * Inactive (default)
        - Transfer is stopped
    * Finished 
        - Transfer has reached its goal
    * Failed
        - There was an error in the transfer, see :attr:`MESSAGE` for details
    * Transferring
        - The transfer is in progress.
      
.. attribute:: RESOURCETRANSFER:MESSAGE

    :access: Get only
    :type: :ref:`string <string>`

    This shows the detail related to :attr:`STATUS`
    
.. attribute:: RESOURCETRANSFER:GOAL

    :access: Get only
    :type: :ref:`scalar <scalar>`
    
    If you specified an amount to transfer in your transfer request, it will be shown here.
    If you did not, this will return the sentinel value -1.

.. attribute:: RESOURCETRANSFER:TRANSFERRED

    :access: Get only
    :type: :ref:`scalar <scalar>`

    Returns the amount of the specified resource that has been transferred by this resource transfer.
    
.. attribute:: RESOURCETRANSFER:RESOURCE

    :access: Get only
    :type: :ref:`string <string>`

    The name of the resource that will be transferred. (eg, oxidizer, liquidfuel)
    
.. attribute:: RESOURCETRANSFER:ACTIVE

    :access: Get / Set
    :type: :ref:`boolean <boolean>`

    When getting, this suffix is simply a shortcut to tell you if :attr:`STATUS` is Transferring.
    Setting true will change the status of the transfer to Transferring, setting false will change status to inactive.
    
