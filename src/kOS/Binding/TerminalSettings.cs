using System;
using kOS.Safe.Binding;

namespace kOS.Binding
{
    [Binding]
    public class TerminalSettings : Binding
    {
        public override void AddTo(SharedObjects shared)
        {

            shared.BindingMgr.AddSetter("TERMINAL", (value) =>
            {
                bool open;
                if (Boolean.TryParse(value.ToString(), out open))
                {
                    if (open)
                        shared.Window.Open();
                    else
                        shared.Window.Close();

                }
            });
            shared.BindingMgr.AddGetter("TERMINAL", () => shared.Window.IsOpen());
            shared.BindingMgr.AddGetter("SESSIONTIME", () => shared.Cpu.SessionTime);
            shared.BindingMgr.AddGetter("VERSION", () => Core.VersionInfo);
            shared.BindingMgr.AddGetter("VOLUME:NAME", () => shared.VolumeMgr.CurrentVolume.Name);
        }
    }
}