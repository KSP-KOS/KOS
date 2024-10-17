stateFinalizer = setmetatable({}, { __gc = function() -- Called when the state is disposed(on shutdown, reboot)
    toggleflybywire("steering", false)
    toggleflybywire("throttle", false)
    toggleflybywire("wheelSteering", false)
    toggleflybywire("wheelThrottle", false)
end})

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
            warn(error, 1)
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
            warn(error, 1)
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
            warn(error, 1)
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
            warn(error, 1)
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

vecDraws = setmetatable({}, { __mode = "v" })
updatingVecDraws = setmetatable({}, { __mode = "v" })

function clearVecDraws()
    CLEARVECDRAWS()
    for _,vd in pairs(vecDraws) do
        vd.parameters.show = false
    end
    updatingVecDraws.keepCallback = false
    updatingVecDraws = setmetatable({}, { __mode = "v" })
end

local vecDrawMetatable = {
    __index = function(vd, index)
        return vd.parameters[index] == nil and vd.structure[index] or vd.parameters[index]
    end,
    __newindex = function(vd, index, value)
        local parameters = vd.parameters
        if type(value) ~= "function" then vd.structure[index] = value end
        if parameters[index] == nil then return end
        parameters[index] = value

        if index == "start" then vd.isStartFunction = type(value) == "function"
        elseif index == "vector" then vd.isVectorFunction = type(value) == "function"
        elseif index == "color" then vd.isColorFunction = type(value) == "function" end

        local vdShouldBeUpdating = parameters.show and (vd.isStartFunction or vd.isVectorFunction or vd.isColorFunction)
        local vdUpdating = false

        for i,v in ipairs(updatingVecDraws) do
            if v == vd then
                vdUpdating = true
                if not vdShouldBeUpdating then
                    table.remove(updatingVecDraws, i)
                    if #updatingVecDraws == 0 then
                        updatingVecDraws.keepCallback = false
                    end
                end
                break
            end
        end

        if vdShouldBeUpdating and not vdUpdating then
            if #updatingVecDraws == 0 then
                updatingVecDraws.keepCallback = true
                addCallback(function()
                    for _, vd in ipairs(updatingVecDraws) do
                        if vd.isStartFunction then vd.structure.start = vd.parameters.start() end
                        if vd.isVectorFunction then vd.structure.vector = vd.parameters.vector() end
                        if vd.isColorFunction then vd.structure.color = vd.parameters.color() end
                    end
                    return updatingVecDraws.keepCallback
                end, 0, updateCallbacks)
            end
            table.insert(updatingVecDraws, vd)
        end
    end,
    __gc = function(vd) vd.show = false end,
}

function vecDraw(start, vector, color, label, scale, show, width, pointy, wiping)
    local vd = {
        structure = VECDRAW(v(0,0,0), v(0,0,0), white, label or "", scale or 1, show ~= nil and show, width or 0.2, pointy == nil or pointy, wiping == nil or wiping),
        isStartFunction = false,
        isVectorFunction = false,
        isColorFunction = false,
        parameters = {
            start = v(0,0,0),
            vector = v(0,0,0),
            color = white,
            show = false,
        }
    }
    setmetatable(vd, vecDrawMetatable)
    table.insert(vecDraws, vd)
    if start then vd.start = start end
    if vector then vd.vector = vector end
    if color then vd.color = color end
    if show then vd.show = show end
    return vd
end