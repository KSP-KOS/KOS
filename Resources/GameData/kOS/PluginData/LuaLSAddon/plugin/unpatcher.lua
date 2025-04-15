local client = require 'client'

local patchFilePath = package.path:match("([^?]+)").."parser/compile.lua"
local originalFilePath = package.path:match("([^?]+)").."parser/original.compile.lua"

local originalFile, originalErr = io.open(originalFilePath, "r")
if not originalFile then
    client.showMessage("Error", "Unpatcher plugin: Failed to open original file at \""..originalFilePath.."\". \n"..originalErr)
    return
end
local originalFileText, originalFileReadErr = originalFile:read("a")
if not originalFileText then
    client.showMessage("Error", "Unpatcher plugin: Failed to read original file at \""..originalFilePath.."\". \n"..originalFileReadErr)
end
originalFile:close()

local patchFile, patchErr = io.open(patchFilePath, "w")
if not patchFile then
    client.showMessage("Error", "Unpatcher plugin: Failed to open patch file at \""..patchFilePath.."\". \n"..patchErr)
    return
end
local _, patchFileWriteErr = patchFile:write(originalFileText)
if patchFileWriteErr then
    client.showMessage("Error", "Unpatcher plugin: Failed to write to patch file at \""..patchFilePath.."\". \n"..patchFileWriteErr)
end
patchFile:close()

client.showMessage("Info", "Unpatcher plugin: Successfully unpatched lua language server. It is highly recommended to remove this plugin after use")
