local M = {}

function M.init()
    wait = M.wait
    waituntil = M.waituntil
    vecdraw = M.vecdraw
    clearvecdraws = M.clearvecdraws

    ---@class CJson
    ---@field encode fun(table): string encodes a `table` into a json `string`
    ---@field decode fun(string): table decodes a json `string` into a `table`

    ---[openrestry/lua-cjson module](https://github.com/openresty/lua-cjson/tree/2.1.0.10rc1).
    ---Not all keys are annotated. Complete documentation is available at the link.
    ---@type CJson
    json = select(2, pcall(require, "cjson"))
end

---Suspends the execution for the specified amount of time.
---Any call to this function will suspend execution for at least one physics tick.
---This function is a simple abstraction made to achieve the same effect as the kerboscript `wait *number*.` command.
---One difference is it only suspends the execution of the coroutine it was called from, making it totally fine to use inside callbacks.
function M.wait(seconds)
    local waitEnd = time.seconds + seconds
    coroutine.yield()
    while time.seconds < waitEnd do coroutine.yield() end
end

---Suspends the execution until the `condition` function returns a true value.
---This function is a simple abstraction made to achieve the same effect as the kerboscript `wait until *condition*.` command.
---One difference is it only suspends the execution of the coroutine it was called from, making it totally fine to use inside callbacks.
---@param condition function
function M.waituntil(condition)
    while not condition() do coroutine.yield() end
end

M.vecdraws = setmetatable({}, { __mode = "v" })
M.updatingvecdraws = setmetatable({}, { __mode = "v" })

---A wrapper around kOS `CLEARVECDRAWS` function that also clears vecdraws created with the `vecdraw` function
function M.clearvecdraws()
    CLEARVECDRAWS()
    for _,vd in pairs(M.vecdraws) do
        vd.parameters.show = false
    end
    M.updatingvecdraws.keepCallback = false
    M.updatingvecdraws = setmetatable({}, { __mode = "v" })
end

local vecdrawmt = {
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

        for i,v in ipairs(M.updatingvecdraws) do
            if v == vd then
                vdUpdating = true
                if not vdShouldBeUpdating then
                    table.remove(M.updatingvecdraws, i)
                    if #M.updatingvecdraws == 0 then
                        M.updatingvecdraws.keepCallback = false
                    end
                end
                break
            end
        end

        if vdShouldBeUpdating and not vdUpdating then
            if #M.updatingvecdraws == 0 then
                M.updatingvecdraws.keepCallback = true
                addcallback(function()
                    for _, vd in ipairs(M.updatingvecdraws) do
                        if vd.isStartFunction then vd.structure.start = vd.parameters.start() end
                        if vd.isVectorFunction then vd.structure.vector = vd.parameters.vector() end
                        if vd.isColorFunction then vd.structure.color = vd.parameters.color() end
                    end
                    return M.updatingvecdraws.keepCallback
                end, 0, callbacks.updatecallbacks)
            end
            table.insert(M.updatingvecdraws, vd)
        end
    end,
    __gc = function(vd) vd.show = false end,
}

---@class VecdrawTable : Vecdraw
---@field structure Vecdraw
---@field parameters table

---Wrapper around kOS `VECDRAW` function that uses the callbacks library to automatically update the "start", "vector" and "color" values.
---Those 3 parameters can also accept functions, in which case their values will be changed each frame with the return value of the functions.
---This function returns a table representing a Vecdraw structure, and when this table gets garbage collected the vecdraw is removed.
---```
---vd = vecdraw(nil, mun.position) -- assign the return value to a variable to keep it from being collected
---vd.show = true
---vd = nil -- this will remove the vecdraw by garbage collection
---```
---@param start? Vector | function `Vector` in ship-raw reference frame where the `Vecdraw` will be drawn from.
---@param vector? Vector | function absolute `Vector` position where the `Vecdraw` should end.
---@param color? RGBA | function
---@param label? string 
---@param scale? number 
---@param show? boolean 
---@param width? number 
---@param pointy? boolean 
---@param wiping? boolean 
---@return VecdrawTable
---@nodiscard
function M.vecdraw(start, vector, color, label, scale, show, width, pointy, wiping)
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
    setmetatable(vd, vecdrawmt)
    table.insert(M.vecdraws, vd)
    if start then vd.start = start end
    if vector then vd.vector = vector end
    if color then vd.color = color end
    if show then vd.show = show end
    return vd
end

return M