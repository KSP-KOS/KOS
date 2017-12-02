
function a {
  print("a").
  return false.
}

function b {
  print("b").
  return true.
}

print(a() and b()).
print(a() or b()).
print(b() and a()).
print(b() or a()).