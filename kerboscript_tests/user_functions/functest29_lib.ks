function func1 {
  return "Func1".
}

function getfunc2 {
  function sameName {  // <--------+--- These two innner functions have the same name
    return "Func2".    // <-----+--|--- But have different effects when called.
  }                    //       |  |
  return sameName@.    // <--+--|--|--- These should return the 2 different versions.
}                      //    |  |  |
                       //    |  |  |
function getfunc3 {    //    |  |  |
  function sameName {  // <--|--|--'
    return "Func3".    // <--|--'
  }                    //    |
  return sameName@.    // <--'
}                      //
