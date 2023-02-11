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
          - :struct:`String`
          - Get only
          - The :struct:`String` status of the transfer (eg "Inactive", "Transferring", "Failed", "Finished")
        * - :attr:`MESSAGE`
          - :struct:`String`
          - Get only
          - A message about the current status
        * - :attr:`GOAL`
          - :struct:`Scalar`
          - Get only
          - This is how much of the resource will be transferred.
        * - :attr:`TRANSFERRED`
          - :struct:`Scalar`
          - Get only
          - This is how much of the resource has been transferred.
        * - :attr:`RESOURCE`
          - :struct:`String`
          - Get only
          - The name of the resource (eg oxidizer, liquidfuel)   
        * - :attr:`ACTIVE`
          - :struct:`Boolean`
          - Get / Set
          - Setting this value will either start, pause or restart a transfer. Default is false.
    
          
.. attribute:: RESOURCETRANSFER:STATUS

    :access: Get only
    :type: :struct:`String`

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
    :type: :struct:`String`

    This shows the detail related to :attr:`STATUS`
    
.. attribute:: RESOURCETRANSFER:GOAL

    :access: Get only
    :type: :struct:`Scalar`
    
    If you specified an amount to transfer in your transfer request, it will be shown here.
    If you did not, this will return the sentinel value -1.

.. attribute:: RESOURCETRANSFER:TRANSFERRED

    :access: Get only
    :type: :struct:`Scalar`

    Returns the amount of the specified resource that has been transferred by this resource transfer.
    
.. attribute:: RESOURCETRANSFER:RESOURCE

    :access: Get only
    :type: :struct:`String`

    The name of the resource that will be transferred. (eg, oxidizer, liquidfuel)
    
.. attribute:: RESOURCETRANSFER:ACTIVE

    :access: Get / Set
    :type: :struct:`Boolean`

    When getting, this suffix is simply a shortcut to tell you if :attr:`STATUS` is Transferring.
    Setting true will change the status of the transfer to Transferring, setting false will change status to inactive.
    
