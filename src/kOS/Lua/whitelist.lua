local registry, setUpvalue = ...

local _ENV = {
    _VERSION = _VERSION,
    assert = assert,
    collectgarbage = collectgarbage,
    dofile = dofile,
    error = error,
    getmetatable = getmetatable,
    ipairs = ipairs,
    load = load,
    loadfile = loadfile,
    next = next,
    pairs = pairs,
    pcall = pcall,
    print = print,
    rawequal = rawequal,
    rawget = rawget,
    rawlen = rawlen,
    rawset = rawset,
    select = select,
    setmetatable = setmetatable,
    tonumber = tonumber,
    tostring = tostring,
    type = type,
    warn = warn,
    xpcall = xpcall,
    _type = _type,
    require = require,
    package = {
        config = package.config,
        path = package.path,
        searchers = {
            package.searchers[1],
            package.searchers[2],
            package.searchers[3],
            package.searchers[4],
        },
    },
    coroutine = {
        close = coroutine.close,
        create = coroutine.create,
        isyieldable = coroutine.isyieldable,
        resume = coroutine.resume,
        running = coroutine.running,
        status = coroutine.status,
        wrap = coroutine.wrap,
        yield = coroutine.yield,
    },
    math = {
        abs = math.abs,
        acos = math.acos,
        asin = math.asin,
        atan = math.atan,
        ceil = math.ceil,
        cos = math.cos,
        deg = math.deg,
        exp = math.exp,
        floor = math.floor,
        fmod = math.fmod,
        huge = math.huge,
        log = math.log,
        max = math.max,
        maxinteger = math.maxinteger,
        min = math.min,
        mininteger = math.mininteger,
        modf = math.modf,
        pi = math.pi,
        rad = math.rad,
        random = math.random,
        randomseed = math.randomseed,
        sin = math.sin,
        sqrt = math.sqrt,
        tan = math.tan,
        tointeger = math.tointeger,
        type = math.type,
        ult = math.ult,
    },
    string = {
        byte = string.byte,
        char = string.char,
        dump = string.dump,
        find = string.find,
        format = string.format,
        gmatch = string.gmatch,
        gsub = string.gsub,
        len = string.len,
        lower = string.lower,
        match = string.match,
        pack = string.pack,
        packsize = string.packsize,
        rep = string.rep,
        reverse = string.reverse,
        sub = string.sub,
        unpack = string.unpack,
        upper = string.upper,
    },
    table = {
        concat = table.concat,
        insert = table.insert,
        move = table.move,
        pack = table.pack,
        remove = table.remove,
        sort = table.sort,
        unpack = table.unpack,
    },
    utf8 = {
        char = utf8.char,
        charpattern = utf8.charpattern,
        codepoint = utf8.codepoint,
        codes = utf8.codes,
        len = utf8.len,
        offset = utf8.offset,
    },
}

local whitelistedRegistry = {
    registry[1], -- main thread
    _ENV,
    _LOADED = {
        _G = _ENV,
        package = package,
        coroutine = coroutine,
        math = math,
        string = string,
        table = table,
        utf8 = utf8,
    },
    _PRELOAD = {},
    _CLIBS = setmetatable({}, getmetatable(registry._CLIBS)),
}

local visitedTables = {}
local function deepCleanTable(tab)
    visitedTables[tab] = true
    for k,v in pairs(tab) do
        if _type(v) == "table" and not visitedTables[v] then
            deepCleanTable(v)
        end
        tab[k] = nil
    end
end
deepCleanTable(registry)

for k,v in pairs(whitelistedRegistry) do
    registry[k] = v
end

_G = _ENV
package.loaded = whitelistedRegistry._LOADED
package.preload = whitelistedRegistry._PRELOAD

setUpvalue(require, 1, package)
for _,searcher in ipairs(package.searchers) do
    setUpvalue(searcher, 1, package)
end

getmetatable("").__index = string