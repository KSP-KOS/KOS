local M = {}

function M.init()
    M.breakcontrol()
    M.fixedupdatecallbacks = {}
    M.updatecallbacks = {}
    fixedupdate = M.fixedupdate
    update = M.update
    breakexecution = M.breakexecution
    addcallback = M.addcallback
    when = M.when
    on = M.on
end

function M.kill()
    fixedupdate = nil
    update = nil
    M.breakcontrol()
    M.fixedupdatecallbacks = {}
    M.updatecallbacks = {}
end

function M.fixedupdate()
    M.runcontrol()
    M.runcallbacks(M.fixedupdatecallbacks)
end

function M.update()
    M.runcallbacks(M.updatecallbacks)
end

function M.breakexecution()
    M.breakcontrol()
    M.fixedupdatecallbacks = {}
    M.updatecallbacks = {}
end

M.finalizer = setmetatable({}, { __gc = function() -- Called when the state is disposed(on shutdown, reboot)
    toggleflybywire("steering", false)
    toggleflybywire("throttle", false)
    toggleflybywire("wheelsteering", false)
    toggleflybywire("wheelthrottle", false)
end})

function M.breakcontrol()
    steering, throttle, wheelsteering, wheelthrottle = nil, nil, nil, nil
    M.controlled = {}
end

function M.runcontrol()
    if M.controlcoroutine then coroutine.resume(M.controlcoroutine) end
    M.controlcoroutine = coroutine.create(M.processcontrol)
    coroutine.resume(M.controlcoroutine)
end

function M.processcontrol()
    if rawget(_ENV, "steering") then
        M.controlled.steering = true
        local success, error = pcall(function() STEERING = type(steering) == "function" and steering() or steering end)
        if not success then
            warn(error, 1)
            steering = nil
        end
    elseif M.controlled.steering then
        M.controlled.steering = false
        toggleflybywire("steering", false)
    end
    if rawget(_ENV, "throttle") then
        M.controlled.throttle = true
        local success, error = pcall(function() THROTTLE = type(throttle) == "function" and throttle() or throttle end)
        if not success then
            warn(error, 1)
            throttle = nil
        end
    elseif M.controlled.throttle then
        M.controlled.throttle = false
        toggleflybywire("throttle", false)
    end
    if rawget(_ENV, "wheelsteering") then
        M.controlled.wheelthrottle = true
        local success, error = pcall(function() WHEELSTEERING = type(wheelsteering) == "function" and wheelsteering() or wheelsteering end)
        if not success then
            warn(error, 1)
            wheelsteering = nil
        end
    elseif M.controlled.wheelthrottle then
        M.controlled.wheelthrottle = false
        toggleflybywire("wheelsteering", false)
    end
    if rawget(_ENV, "wheelthrottle") then
        M.controlled.wheelthrottle = true
        local success, error = pcall(function() WHEELTHROTTLE = type(wheelthrottle) == "function" and wheelthrottle() or wheelthrottle end)
        if not success then
            warn(error, 1)
            wheelthrottle = nil
        end
    elseif M.controlled.wheelthrottle then
        M.controlled.wheelthrottle = false
        toggleflybywire("wheelthrottle", false)
    end
    M.controlcoroutine = nil
end

function M.runcallbacks(callbacks)
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

function M.addcallback(body, priority, callbacks)
    callbacks = callbacks or M.fixedupdatecallbacks
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
                    warn("error in callback:\n" .. newPriority)
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

function M.when(condition, body, priority, callbacks)
    return M.addcallback(function (callback)
        if condition() then
            return body()
        else
            return callback.priority
        end
    end, priority, callbacks)
end

function M.on(state, body, priority, callbacks)
    local previousState = state()
    return M.addcallback(function (callback)
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

return M