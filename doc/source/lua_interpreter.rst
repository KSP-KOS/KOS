.. _lua_interpreter:

Lua Interpreter
===============

Lua interpreter is one of the options for the "interpreter" field on kOSProcessors.
You can enable it with the PAW menu activated by right clicking on the kOSProcessor part or with the ``core:setfield("interpreter", "lua").`` terminal command.
Lua interpreter uses all the same structures, functions and bound variables that are available in kOS.
This documentation will walk you through how to use it if you are already comfortable using kOS with kerboscript, documentation for people new to kOS may come in the future.
Basic knowledge of lua is required.

- `Main differences from kerboscript`_
- `Main lua changes`_
- `Development environment`_
- `Name changes to bound variables and functions`_
- `Default modules`_
- `Callbacks module`_
- `Misc module`_
- `Design patterns`_
- `Example scripts`_
- `System variables`_

Main differences from kerboscript
---------------------------------
For the most part you can directly apply your kerboscript knowledge when using the lua interpreter.
Just write the lua scripts using its syntax as you would kerboscript, but there are a few differences you need to be aware of.

- The difference between kerboscript ``RUN`` command and ``dofile`` and ``loadfile`` functions. The main difference being that the context is shared across the whole core.
- Triggers are implemented using the `callback system`_. While you can use them exactly like you would use kerboscript triggers, that is not the most efficient way.
- Some `name changes to bound variables and functions`_.

Main lua changes
----------------
- ``io``, ``os``, ``debug`` libraries are not loaded.
- Basic library functions accessing the file system are made to access the internal kOS file system.
- The environment table has ``__index`` and ``__newindex`` metamethods that act as a binding layer for kOS functions and bound variables.
- `Default modules`_ are loaded.
- There is a small syntactic sugar to define functions that return expressions. ``> *expression*`` gets interpreted as ``function() return *expression* end``.

Development environment
-----------------------
The best way to use the lua interpreter is with a text editor that assists you.
To get annotations for all structures, functions and bound variables with their descriptions,
as well as support for the added syntactic sugar see the README file at ``*KSP_Folder*/GameData/kOS/PluginData/LuaLSAddon/README.md``.

Name changes to bound variables and functions
---------------------------------------------
Some name changes were neccessary to resolve name conflicts:
    | ``STAGE`` bound variable → ``stageinfo``.
    | ``HEADING`` bound variable → ``shipheading``.
    | ``BODY`` function → ``getbody``.

kOS variables and functions that are accessable only by capital names:
    | ``STEERING``
    | ``THROTTLE``
    | ``WHEELSTEERING``
    | ``WHEELTHROTTLE``
    | ``VECDRAW``
    | ``CLEARVECDRAWS``

    This was done so there is no confusion between kOS variables and lua variables used by default modules.

Kerboscript command alternatives:
    | ``WHEN condition THEN *body*.`` → `when(condition, body) <#when-condition-body-priority-callbacks>`_
    | ``ON state *body*.`` → `on(state, body) <#on-state-body-priority-callbacks>`_
    | ``WAIT seconds.`` → `wait(seconds)`_
    | ``WAIT UNTIL condition.`` → `waituntil(condition)`_
    | ``PRINT item AT(column, row).`` → ``printat(item, column, row)``
    | ``STAGE.`` → ``stage()``
    | ``CLEARSCREEN.`` → ``clearscreen()``
    | ``ADD node.`` → ``add(node)``
    | ``REMOVE node.`` → ``remove(node)``
    | ``LOG text TO path.`` → ``logfile(text, path)``
    | ``SWITCH TO volumeId.`` → ``switch(volumeId)``
    | ``EDIT path.`` → ``edit(path)``
    | ``REBOOT.`` → ``reboot()``
    | ``SHUTDOWN.`` → ``shutdown()``
    | ``LIST listType IN variable.`` → ``variable = buildlist(listType)``
    | ``LIST listType.`` → ``printlist(listType)``
    | ``LIST.`` → ``printlist("files")``

    All these functions, except the first 4, are available in kerboscript but are documented as commands.

Default modules
---------------
You can look at the code for the default modules at the ``*KSP_Folder*/GameData/kOS/PluginData/LuaModules`` folder.
You can also create your own modules, place them in this folder and kOS will automatically require them during core booting.

Callbacks module
----------------

Ship control
------------
There are 4 variables that are used to access ship control: ``steering``, ``throttle``, ``wheelsteering``, ``wheelthrottle``.
If a function is assigned to those variables the return value of the function will be calculated at the start of each physics tick and used as the value to control the ship.

::

        steering = function() return prograde end -- kOS will continuously point the ship prograde

        steering => prograde -- equivalent to the previous command using the added syntactic sugar

        steering = prograde -- value will be updated once and stay, even if prograde changes in the future


Callback system
---------------
Callbacks is a list of lua `coroutines <https://www.lua.org/manual/5.4/manual.html#2.6>`_ that get called each physics tick(or each frame).
The main interface to the callback system is the ``addcallback`` function.

``addcallback(body, priority?, callbacks?)``
````````````````````````````````````````````
    body:
        Callback function body to get executed on the next physics tick(or the next frame, see the third parameter).
        If returns a number or ``true`` the callback doesn't get cleared.
        If returns a number this number will be used as the callback priority, see the second parameter.
    priority?:
        Callback priority. Callbacks with highest priorities get executed first. Default is 0.
    callbacks?:
        Callbacks table where to add the callback to.
        Options:

        - ``callbacks.fixedupdatecallbacks``: gets executed each physics tick(Default).
        - ``callbacks.updatecallbacks``: gets executed each frame.

``when(condition, body, priority?, callbacks?)``
````````````````````````````````````````````````
    | **condition:** The callback executes only if this function returns a true value
    | **body:** Same as in ``addcallback`` function
    | **priority?:** Same as in ``addcallback`` function
    | **callbacks?:** Same as in ``addcallback`` function

    "When" trigger implemented as a wrapper around the ``addcallback`` function.

``on(state, body, priority?, callbacks?)``
``````````````````````````````````````````
    | **state:** The callback executes only if this function returns a value that is not equal to the value it returned previously
    | **body:** Same as in ``addcallback`` function
    | **priority?:** Same as in ``addcallback`` function
    | **callbacks?:** Same as in ``addcallback`` function

    "On" trigger implemented as a wrapper around the ``addcallback`` function.

.. note::
    Callbacks are coroutines, and `wait <#wait-seconds>`_/`waituntil <#waituntil-condition>`_ functions are simple abstractions using the ``coroutine.yield`` function.
    Because of this using ``wait`` in a callback will not block any other code.
    You can think of it as a callback saying "Ok, I am done for now, I will let other code execute until its my turn again".
    This unlocks some helpful design patterns:

    Timed actions in callbacks without nesting::
        
        when(> alt.radar < 100, function()
            gear = true
            wait(7)
            print("Gear deployed.")
        end)

    The ``wait(7)`` being inside the callback body means the following code(or the terminal) is not blocked.

    Running programs without blocking the terminal::

        addcallback(> dofile("launch.lua"))

    As long as the running program is not using all available instructions the terminal won't be blocked.
    This also allows running multiple blocking functions/programs at the same time.
    In that case the programs would "pass" the execution between each other using the "wait" functions.

Misc module
------------

``wait(seconds)``
`````````````````
    Suspends the execution for the specified amount of time.
    Any call to this function will suspend execution for at least one physics tick.
    This function is a simple abstraction made to achieve the same effect as the kerboscript ``wait *number*.`` command.
    
    implementation::

        function wait(seconds)
            local waitEnd = time.seconds + seconds
            coroutine.yield()
            while time.seconds < waitEnd do coroutine.yield() end
        end

``waituntil(condition)``
````````````````````````
    **condition:** function

    Suspends the execution until the ``condition`` function returns a true value.
    This function is a simple abstraction made to achieve the same effect as the kerboscript ``wait until *condition*.`` command.

    implementation::

        function waituntil(condition)
            while not condition() do coroutine.yield() end
        end

``vecdraw(start?, vector?, color?, label?, scale?, show?, width?, pointy?, wiping?): VecdrawTable``
```````````````````````````````````````````````````````````````````````````````````````````````````
    Wrapper around kOS :func:`VECDRAW` function that uses the `Callback system`_ to automatically update the "start" and "vector".
    Those parameters can accept functions, in which case their values will be changed each frame with the return value of the functions.
    This function returns a table representing a Vecdraw structure, and when this table gets garbage collected the vecdraw is removed.

    ::

            vd = vecdraw(nil, mun.position) -- assign the return value to a variable to keep it from being collected
            vd.show = true
            vd = nil -- this will remove the vecdraw by garbage collection

``clearvecdraws()``
```````````````````
    A wrapper around kOS :func:`CLEARVECDRAWS` function that also clears vecdraws created with the ``vecdraw`` function

``json``
````````
    `openrestry/lua-cjson module <https://github.com/openresty/lua-cjson/tree/2.1.0.10rc1>`_ used for encoding and decoding json.

Design patterns
---------------

Interactive scripts
```````````````````
    You can run programs defining functions/callbacks etc. made to be interacted with from the terminal.
    For example you can have a program defining basic utility functions and then using them how you please from the terminal.
    Or create an interactive script for a specific craft that runs as its firmware from the bootfile.
    It was possible with kerboscript using telnet to paste programs into the terminal, but in lua the context between programs and the terminal is shared, making it much easier to do.

Example scripts
---------------
    You can take a look at some examples of using lua at `sug44/kOSLuaScripts <https://github.com/sug44/kOSLuaScripts>`_

System variables
----------------

    There are 3 lua variables that are used by kOS:
        - ``fixedupdate``. Function called at the start of each physics tick.
        - ``update``. Function called at the start of each frame.
        - ``breakexecution``. Function called when the terminal receives the Ctrl+C code. If Ctrl+C was pressed 3 times while the command code was deprived of instructions by the ``fixedupdate`` function it will be set to ``nil`` to prevent the core from geting stuck. To prevent it from happening this function must ensure the terminal is not deprived of instructions.

    Those variables are used by the default `Callbacks module`_.
