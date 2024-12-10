-- Function that gets called by kOS each physics tick.
---@type function | nil
fixedupdate = nil

-- Function that gets called by kOS each frame.
---@type function | nil
update = nil

-- Function that gets called by kOS when `Ctrl+C` is pressed.
-- If `Ctrl+C` was pressed 3 times while the command code was deprived of instructions by the `fixedupdate` function,
-- both `fixedupdate` and `update` function get set to `nil` as a way to prevent the core from geting stuck.
-- To prevent it from happening this function must ensure the terminal is not deprived of instructions.
---@type function | nil
breakexecution = nil
