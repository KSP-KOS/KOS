﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace kOS.Compilation
{
    public class ProgramBuilderInterpreter : ProgramBuilder
    {
        protected override void AddInitializationCode(CodePart linkedObject, CodePart part)
        {
            linkedObject.MainCode.AddRange(part.InitializationCode);
        }

        protected override void AddEndOfProgram(CodePart linkedObject, bool isMainProgram)
        {
            linkedObject.MainCode.Add(new OpcodeEOF());
        }
    }
}
