function onFixedUpdate(dt)
    runCallbacks()
end

function onBreakExecution()
    breakCallbacks()
end

function breakCallbacks()
    callbacksList = {}
    callbacksAddQueueLock = nil
    callbacksAddQueueRoot = nil
    callbacksAddQueueTail = nil
    callbacksContinuation = nil
    callbacksUnsorted = nil
end
breakCallbacks()

function runCallbacks()
    if callbacksContinuation then
        coroutine.resume(callbacksContinuation)
    end
    if callbacksAddQueueRoot and not callbacksAddQueueLock then
        callbacksContinuation = coroutine.create(function()
            callbacksList.next, callbacksAddQueueTail.next =
            callbacksAddQueueRoot, callbacksList.next
            callbacksAddQueueRoot = nil
            sortCallbacks()
        end)
        coroutine.resume(callbacksContinuation)
    end
    if callbacksUnsorted then
        callbacksContinuation = coroutine.create(sortCallbacks)
        coroutine.resume(callbacksContinuation)
    end
    local callback = callbacksList.next
    local previousCallback = callbacksList
    while callback do
        if callback.coroutine then
            coroutine.resume(callback.coroutine, callback)
            previousCallback = callback
        else
            if callback.body then
                callback.coroutine = coroutine.create(callback.body)
                coroutine.resume(callback.coroutine, callback)
                previousCallback = callback
            else
                previousCallback.next = callback.next
            end
        end
        callback = callback.next
    end
end

function callbacksSplit(head)
    local fast, slow = head, head
    while fast and fast.next do
        fast = fast.next.next
        if fast then slow = slow.next end
    end
    local second = slow.next
    slow.next = nil
    return second
end
function callbacksMerge(first, second)
    if not first then return second end
    if not second then return first end
    if first.priority > second.priority then
        first.next = callbacksMerge(first.next, second)
        return first
    else
        second.next = callbacksMerge(first, second.next)
        return second
    end
end
function callbacksMergeSort(head)
    if not head or not head.next then return head end
    local second = callbacksSplit(head)
    head = callbacksMergeSort(head)
    second = callbacksMergeSort(second)
    return callbacksMerge(head, second)
end
function sortCallbacks()
    callbacksList.next = callbacksMergeSort(callbacksList.next)
    callbacksUnsorted = false
    callbacksContinuation = nil
end

function addCallback(body, priority)
    local function callbackBody(callback)
        local success, newPriority = pcall(body, callback)
        if not(newPriority == true or newPriority == callback.priority) then
            if success then
                callback.priority = tonumber(newPriority)
                if callback.priority then
                    callbacksUnsorted = true
                else
                    callback.body = nil
                end
            else
                warn(newPriority)
                callback.body = nil
            end
        end
        callback.coroutine = nil
    end
    callbacksAddQueueLock = true
    callbacksAddQueueRoot = {
        next = callbacksAddQueueRoot,
        body = callbackBody,
        priority = priority or 0
    }
    if not callbacksAddQueueRoot.next then
        callbacksAddQueueTail = callbacksAddQueueRoot
    end
    callbacksAddQueueLock = false
end

function when(condition, body, priority)
    addCallback(function (callback)
        if condition() then
            return body()
        else
            return callback.priority
        end
    end, priority)
end

function on(state, body, priority)
    local previousState = state()
    addCallback(function (callback)
        local currentState = state()
        if currentState ~= previousState then
            local newPriority = body()
            previousState = currentState
            return newPriority
        else
            return callback.priority
        end
    end, priority)
end