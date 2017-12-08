.. _exenode:

Execute Node Script
===================

Let's try to automate one of the most common tasks in orbital maneuvering - execution of the maneuver node. In this tutorial I'll try to show you how to write a script for somewhat precise maneuver node execution.

So to start our script we need to get the next available :ref:`maneuver node <maneuver node>`::

    set nd to nextnode.

Our next step is to calculate how much time our vessel needs to burn at full throttle to execute the node::

    //print out node's basic parameters - ETA and deltaV
    print "Node in: " + round(nd:eta) + ", DeltaV: " + round(nd:deltav:mag).

    //calculate ship's max acceleration
    set max_acc to ship:maxthrust/ship:mass.

    // Now we just need to divide deltav:mag by our ship's max acceleration
    // to get the estimated time of the burn.
    //
    // Please note, this is not exactly correct.  The real calculation
    // needs to take into account the fact that the mass will decrease
    // as you lose fuel during the burn.  In fact throwing the fuel out
    // the back of the engine very fast is the entire reason you're able
    // to thrust at all in space.  The proper calculation for this
    // can be found easily enough online by searching for the phrase
    //   "Tsiolkovsky rocket equation".
    // This example here will keep it simple for demonstration purposes,
    // but if you're going to build a serious node execution script, you
    // need to look into the Tsiolkovsky rocket equation to account for
    // the change in mass over time as you burn.
    //
    set burn_duration to nd:deltav:mag/max_acc.
    print "Crude Estimated burn duration: " + round(burn_duration) + "s".

So now we have our node's deltav vector, ETA to the node and we calculated our burn duration. All that is left for us to do is wait until we are close to node's ETA less half of our burn duration. But we want to write a universal script, and some of our current and/or future ships can be quite slow to turn, so let's give us some time, 60 seconds, to prepare for the maneuver burn::

    wait until nd:eta <= (burn_duration/2 + 60).

This wait can be tedious and you'll most likely end up warping some time, but we'll leave kOS automation of warping for a given period of time to our readers.

The wait has finished, and now we need to start turning our ship in the direction of the burn::

    set np to nd:deltav. //points to node, don't care about the roll direction.
    lock steering to np.

    //now we need to wait until the burn vector and ship's facing are aligned
    wait until vang(np, ship:facing:vector) < 0.25.

    //the ship is facing the right direction, let's wait for our burn time
    wait until nd:eta <= (burn_duration/2).

Now we are ready to burn. It is usually done in the `until` loop, checking main parameters of the burn every iteration until the burn is complete::

    //we only need to lock throttle once to a certain variable in the beginning of the loop, and adjust only the variable itself inside it
    set tset to 0.
    lock throttle to tset.

    set done to False.
    //initial deltav
    set dv0 to nd:deltav.
    until done
    {
        //recalculate current max_acceleration, as it changes while we burn through fuel
        set max_acc to ship:maxthrust/ship:mass.

        //throttle is 100% until there is less than 1 second of time left to burn
        //when there is less than 1 second - decrease the throttle linearly
        set tset to min(nd:deltav:mag/max_acc, 1).

        //here's the tricky part, we need to cut the throttle as soon as our nd:deltav and initial deltav start facing opposite directions
        //this check is done via checking the dot product of those 2 vectors
        if vdot(dv0, nd:deltav) < 0
        {
            print "End burn, remain dv " + round(nd:deltav:mag,1) + "m/s, vdot: " + round(vdot(dv0, nd:deltav),1).
            lock throttle to 0.
            break.
        }

        //we have very little left to burn, less then 0.1m/s
        if nd:deltav:mag < 0.1
        {
            print "Finalizing burn, remain dv " + round(nd:deltav:mag,1) + "m/s, vdot: " + round(vdot(dv0, nd:deltav),1).
            //we burn slowly until our node vector starts to drift significantly from initial vector
            //this usually means we are on point
            wait until vdot(dv0, nd:deltav) < 0.5.

            lock throttle to 0.
            print "End burn, remain dv " + round(nd:deltav:mag,1) + "m/s, vdot: " + round(vdot(dv0, nd:deltav),1).
            set done to True.
        }
    }
    unlock steering.
    unlock throttle.
    wait 1.

    //we no longer need the maneuver node
    remove nd.

    //set throttle to 0 just in case.
    SET SHIP:CONTROL:PILOTMAINTHROTTLE TO 0.

That is all, this short script can execute any maneuver node with 0.1 m/s dv precision or even better.
