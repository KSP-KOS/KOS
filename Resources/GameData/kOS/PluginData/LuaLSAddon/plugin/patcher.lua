local patchMessage = [[
Patcher plugin will patch lua language server to add the arrow syntax support.
You may may also see this message after lua language server was updated and needs to get patched again.
If you do not wish to see this message remove the plugin from your lua language server settings.
To uninstall the patch replace the "patcher.lua" plugin with the "unpatcher.lua" plugin or reinstall lua language server.
The arrow syntax won't be enabled unless you have '"Lua.runtime.special": { "_G": "_G" }' in your settings("runtime.special" in case you use .luarc.json file).
For the syntax support to apply lua language server needs a restart.
If the patch fails, the added syntax doesn't work or there are errors popping up after the patch then most likely the installed version of lua language server is not supported.
The latest version of lua language server that is guaranteed to work is 3.13.2.
]]

local parseArrowFn = [[

--patch_id:0
local function parseArrow()
    local funcLeft  = getPosition(Tokens[Index], 'left')
    local funcRight = getPosition(Tokens[Index], 'right')
    local func = {
        type    = 'function',
        start   = funcLeft,
        finish  = funcRight,
        bstart  = funcRight,
        keyword = {
            [1] = funcLeft,
            [2] = funcRight,
        },
    }
    Index = Index + 2
    skipSpace(true)
    local LastLocalCount = LocalCount
    LocalCount = 0
    pushChunk(func)
    func.args = {
        type = 'funcargs',
        start = funcLeft,
        finish = funcRight,
        parent = func,
    }
    local returnExp = parseExp()
    popChunk()
    func.bfinish = getPosition(Tokens[Index], 'left')
    if returnExp then
        returnExp.parent = func
        func[1] = {
            returnExp,
            type = 'return',
            parent = func,
            finish = returnExp.finish,
            start = func.finish
        }
        func.bfinish = func[1].finish
        func.hasReturn = true
        func.returns = { func[1] }
        func.keyword[3] = returnExp.finish-1
        func.keyword[4] = returnExp.finish
        func.finish = returnExp.finish
    else
        func.finish = lastRightPosition()
        missExp()
    end
    LocalCount = LastLocalCount
    return func
end
]]

local parseTokenCheck = [[

    if State.options.special._G == "_G" and token == '>' then
        return parseArrow()
    end
]]

local client = require 'client'

local function insert(str1, str2, index) return str1:sub(1, index-1)..str2..str1:sub(index, -1) end

local function patcherError(err) client.showMessage("Error", "Patcher plugin: "..err) end
local function openFileFailedError(path, err) patcherError("Failed to open the file at \""..path.."\": \n"..err) end
local function readFileFailedError(path, err) patcherError("Failed to read the file at \""..path.."\": \n"..err) end
local function writeFileFailedError(path, err) patcherError("Failed to write to the file at \""..path.."\": \n"..err) end
local function patchFailedError() patcherError("Failed to apply the patch. See the patch confirmation message") end

local patchFilePath = package.path:match("([^?]+)").."parser/compile.lua"
local originalFilePath = package.path:match("([^?]+)").."parser/original.compile.lua"

-- get contents of the patch file
local patchFile, patchFileOpenErr = io.open(patchFilePath, "r")
if not patchFile then
    openFileFailedError(patchFilePath, patchFileOpenErr)
    return
end
local patchFileText, patchFileReadErr = patchFile:read("a")
if patchFileReadErr then
    readFileFailedError(patchFilePath, patchFileReadErr)
    patchFile:close()
    return
end
patchFile:close()

-- check if the file is already patched
local filePatched = patchFileText:match("--patch_id:(%d+)")
if filePatched then return end

-- ask the user if they want to patch
local result = client.awaitRequestMessage("Warning", patchMessage, { "Ok", "Don't patch" })
if result ~= "Ok" then return end

-- save original file before patching
local originalFile, originalFileOpenErr = io.open(originalFilePath, "w")
if not originalFile then
    openFileFailedError(originalFilePath, (originalFileOpenErr or ""))
    return
end
local _, originalFileWriteErr = originalFile:write(patchFileText)
if originalFileWriteErr then
    writeFileFailedError(originalFilePath, originalFileWriteErr)
    return
end
originalFile:close()

-- patch the text
local parseArrowFnInsertPos = patchFileText:match("()\nlocal function parseFunction")
if not parseArrowFnInsertPos then
    patchFailedError()
    return
end
patchFileText = insert(patchFileText, parseArrowFn, parseArrowFnInsertPos)
local tokenCheckInsertPos = patchFileText:match("return parseFunction%(%)\n    end\n()")
if not tokenCheckInsertPos then
    patchFailedError()
    return
end
patchFileText = insert(patchFileText, parseTokenCheck, tokenCheckInsertPos)

-- save patched text to the patch file
local patchFile, patchFileOpenErr = io.open(patchFilePath, "w")
if not patchFile then
    openFileFailedError(patchFilePath, patchFileOpenErr)
    return
end
local _, patchFileWriteErr = patchFile:write(patchFileText)
if patchFileWriteErr then
    writeFileFailedError(patchFilePath, patchFileWriteErr)
    patchFile:close()
    return
end
patchFile:close()
