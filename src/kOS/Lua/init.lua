function tablelength(t)
  local count = 0
  for _ in pairs(t) do count = count + 1 end
  return count
end

function print(...)
    local printSum = ""
    for i, v in pairs({...}) do
        printSum = printSum .. tostring(v) .. (i ~= tablelength({...}) and ", " or "")
    end
    Shared.Window:Print(printSum)
end

function dump(obj, depth)
	if depth==nil then depth=0 end
	if depth==0 then print(obj, type(obj)) end
	if type(obj)=="table" or getmetatable(obj) and getmetatable(obj).__pairs then
		for k,v in pairs(obj) do
			print(string.rep("  ",depth)..tostring(k).."("..type(k)..")", tostring(v).."("..type(v)..")")
			dump(v, depth+1)
		end
	end
	if getmetatable(obj) then
		print(string.rep("  ",depth).."metatable:")
		for k,v in pairs(getmetatable(obj)) do
			print(string.rep("  ",depth)..tostring(k).."("..type(k)..")", tostring(v).."("..type(v)..")")
			dump(v, depth+1)
		end
	end
end
