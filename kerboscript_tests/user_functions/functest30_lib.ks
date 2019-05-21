global function global1 { return "global1". }
function global2 { return "global2". }
local function local1 { return "local1". }

function outer {
  function local2 { return "local2". } // local by default
  global function global3 { return "global3". } // explicit global keyword despite being nested
  function local1 { return local2(). } // Note this local1 should mask the outer local1

  return local1@. // via this delegate, it actually will call local2().
}
