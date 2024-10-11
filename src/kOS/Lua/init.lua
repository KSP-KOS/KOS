function _onFixedUpdate()
    runProcessControl()
    runCallbacks(fixedUpdateCallbacks)
end

function _onUpdate()
    runCallbacks(updateCallbacks)
end

function setUpdateCallbacks()
    onFixedUpdate = _onFixedUpdate
    onUpdate = _onUpdate
end
setUpdateCallbacks()

function _onBreakExecution()
    breakControl()
    fixedUpdateCallbacks = {}
    callbacks = fixedUpdateCallbacks
    updateCallbacks = {}
end
onBreakExecution = _onBreakExecution

function breakControl()
    steering, throttle, wheelSteering, wheelThrottle = nil, nil, nil, nil
    steeringControlled, throttleControlled, wheelSteeringControlled, wheelThrottleControlled = nil, nil, nil, nil
end

function runProcessControl()
    if controlCoroutine then coroutine.resume(controlCoroutine) end
    controlCoroutine = coroutine.create(processControl)
    coroutine.resume(controlCoroutine)
end

function processControl()
    if rawget(_ENV, "steering") then
        steeringControlled = true
        local success, error = pcall(function() STEERING = type(steering) == "function" and steering() or steering end)
        if not success then
            warn(error)
            steering = nil
        end
    elseif steeringControlled then
        steeringControlled = false
        toggleflybywire("steering", false)
    end
    if rawget(_ENV, "throttle") then
        throttleControlled = true
        local success, error = pcall(function() THROTTLE = type(throttle) == "function" and throttle() or throttle end)
        if not success then
            warn(error)
            throttle = nil
        end
    elseif throttleControlled then
        throttleControlled = false
        toggleflybywire("throttle", false)
    end
    if rawget(_ENV, "wheelSteering") then
        wheelSteeringControlled = true
        local success, error = pcall(function() WHEELSTEERING = type(wheelSteering) == "function" and wheelSteering() or wheelSteering end)
        if not success then
            warn(error)
            wheelSteering = nil
        end
    elseif wheelSteeringControlled then
        wheelSteeringControlled = false
        toggleflybywire("wheelSteering", false)
    end
    if rawget(_ENV, "wheelThrottle") then
        wheelThrottleControlled = true
        local success, error = pcall(function() WHEELTHROTTLE = type(wheelThrottle) == "function" and wheelThrottle() or wheelThrottle end)
        if not success then
            warn(error)
            wheelThrottle = nil
        end
    elseif wheelThrottleControlled then
        wheelThrottleControlled = false
        toggleflybywire("wheelThrottle", false)
    end
    controlCoroutine = nil
end

fixedUpdateCallbacks = {}
updateCallbacks = {}

function runCallbacks(callbacks)
    if callbacks.continuation then
        coroutine.resume(callbacks.continuation)
    end
    if callbacks.unsorted then
        callbacks.continuation = coroutine.create(function()
            table.sort(callbacks, function(a, b) return a.priority < b.priority end)
            callbacks.unsorted = false
            callbacks.continuation = nil
        end)
        coroutine.resume(callbacks.continuation)
    end
    for i=#callbacks,1,-1 do
        local callback = callbacks[i]
        if callback.coroutine then
            coroutine.resume(callback.coroutine, callback)
        else
            if callback.body then
                callback.coroutine = coroutine.create(callback.body)
                coroutine.resume(callback.coroutine, callback)
            else
                table.remove(callbacks, i)
            end
        end
    end
end

function addCallback(body, priority, callbacks)
    callbacks = callbacks or fixedUpdateCallbacks
    local callback = {
        body = function(callback)
            local success, newPriority = pcall(body, callback)
            if not(newPriority == true or newPriority == callback.priority) then
                if success then
                    callback.priority = tonumber(newPriority)
                    if callback.priority then
                        callbacks.unsorted = true
                    else
                        callback.body = nil
                    end
                else
                    warn(newPriority)
                    callback.body = nil
                end
            end
            callback.coroutine = nil
        end,
        priority = priority or 0
    }
    table.insert(callbacks, callback)
    callbacks.unsorted = true
    return callback
end

function when(condition, body, priority, callbacks)
    return addCallback(function (callback)
        if condition() then
            return body()
        else
            return callback.priority
        end
    end, priority, callbacks)
end

function on(state, body, priority, callbacks)
    local previousState = state()
    return addCallback(function (callback)
        local currentState = state()
        if currentState ~= previousState then
            local newPriority = body()
            previousState = currentState
            return newPriority
        else
            return callback.priority
        end
    end, priority, callbacks)
end
