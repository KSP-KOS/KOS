local M = {}

function M.init()
    vecdraw = M.vecdraw
    clearvecdraws = M.clearvecdraws
end

M.vecdraws = setmetatable({}, { __mode = "v" })
M.updatingvecdraws = setmetatable({}, { __mode = "v" })

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
                end, 0, M.updatecallbacks)
            end
            table.insert(M.updatingvecdraws, vd)
        end
    end,
    __gc = function(vd) vd.show = false end,
}

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