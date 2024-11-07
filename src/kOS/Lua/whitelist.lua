local registry, setUpvalue = ...

local env = {
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
    wait = wait,
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
env._G = env

local whitelistedRegistry = {
    registry[1], -- main thread
    env,
    _LOADED = {
        _G = env._G,
        package = env.package,
        coroutine = env.coroutine,
        math = env.math,
        string = env.string,
        table = env.table,
        utf8 = env.utf8,
    },
    _PRELOAD = {},
    _CLIBS = setmetatable({}, getmetatable(registry._CLIBS)),
}

env.package.loaded = whitelistedRegistry._LOADED
env.package.preload = whitelistedRegistry._PRELOAD

setUpvalue(env.require, 1, env.package)
for _,searcher in ipairs(env.package.searchers) do
    setUpvalue(searcher, 1, env.package)
end

local visitedTables = {}
local function deepCleanTable(tab)
    visitedTables[tab] = true
    for k,v in env.pairs(tab) do
        if env._type(v) == "table" and not visitedTables[v] then
            deepCleanTable(v)
        end
        tab[k] = nil
    end
end

deepCleanTable(registry)

for k,v in env.pairs(whitelistedRegistry) do
    registry[k] = v
end