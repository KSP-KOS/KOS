using System;
using System.Linq;
using kOS.Safe.Compilation.KS;

namespace kOS.Safe.Compilation.IR
{
    /// <summary>
    /// This class represents a trigger definition.
    /// </summary>
    /// <seealso cref="CodeElement" />
    /// <seealso cref="ILabeledComponent" />
    public class IRTrigger : CodeElement, ILabeledComponent
    {
        private readonly Trigger trigger;
        public string Identifier { get; }
        public string Label { get; private set; }
        /// <summary>
        /// Initializes a new instance of the <see cref="IRTrigger"/> class.
        /// </summary>
        /// <param name="trigger">The trigger object to convert.</param>
        /// <param name="codePart">The parent IRCodePart object.</param>
        public IRTrigger(Trigger trigger, IRCodePart codePart) : base(codePart, codePart.GetClosureScope(GetTriggerIdentifier(trigger)).Scope)
        {
            this.trigger = trigger;
            Label = GetTriggerIdentifier(trigger);
            Identifier = Label;
            Lower(trigger.Code);
        }
        public static string GetTriggerIdentifier(Trigger trigger)
            => trigger.Code.FirstOrDefault()?.Label;
        public void EmitCode(IREmitter emitter)
        {
            EmitCode(emitter, trigger.Code);
            Label = GetTriggerIdentifier(trigger);
        }
        public override string ToString()
            => $"IRTrigger: {Identifier}";
    }
}
