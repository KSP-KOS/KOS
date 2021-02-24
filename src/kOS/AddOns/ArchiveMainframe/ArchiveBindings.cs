using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using kOS.Safe.Binding;
using kOS.Suffixed;
using kOS.Suffixed.Part;
using kOS.Utilities;
using kOS.Module;
using kOS.Communication;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;

namespace kOS.AddOns.ArchiveMainframe
{
    [Binding("archive")]
    public class ArchiveMissionSettings : kOS.Binding.Binding
    {
        private Lexicon vessel;
        private Lexicon GetVessel(SharedObjects shared)
        {
            if (vessel != null)
                return vessel;
            vessel = new Lexicon();
            vessel.Add(new KeyValuePair<Structure, Structure>(new StringValue("NAME"), new StringValue("Archive")));
            vessel.Add(new KeyValuePair<Structure, Structure>(new StringValue("CONNECTION"), GetLocalConnection(shared)));
            if (Mainframe.instance != null)
            {
                vessel.Add(new KeyValuePair<Structure, Structure>(new StringValue("MESSAGES"), new MessageQueueStructure(Mainframe.instance.messageQueue, shared)));
            }
            return vessel;
        }
        private Lexicon connection;
        private Lexicon GetLocalConnection(SharedObjects shared)
        {
            if (connection != null)
                return connection;
            connection = new Lexicon();
            connection.Add(new KeyValuePair<Structure, Structure>(new StringValue("ISCONNECTED"), new BooleanValue(true)));
            connection.Add(new KeyValuePair<Structure, Structure>(new StringValue("DELAY"), ScalarValue.Create(0)));
            connection.Add(new KeyValuePair<Structure, Structure>(new StringValue("DESTINATION"), GetVessel(shared)));
            //TODO: add SendMessage()
            /*
            if (Mainframe.instance != null)
            {
                connection.Add(new KeyValuePair<Structure, Structure>(new StringValue("MESSAGES"), new MessageQueueStructure(Mainframe.instance.messageQueue, shared)));
            }
            */
            return connection;
        }


        public override void AddTo(SharedObjects shared)
        {
            shared.BindingMgr.AddGetter("SHIP", () => GetVessel(shared));
            shared.BindingMgr.AddGetter("CORE", () => {
                var lex = new Lexicon();
                lex.Add(new KeyValuePair<Structure, Structure>(new StringValue("VESSEL"), GetVessel(shared)));
                lex.Add(new KeyValuePair<Structure, Structure>(new StringValue("TAG"), new StringValue("Archive")));
                lex.Add(new KeyValuePair<Structure, Structure>(new StringValue("VOLUME"), shared.VolumeMgr.CurrentVolume));
                lex.Add(new KeyValuePair<Structure, Structure>(new StringValue("CURRENTVOLUME"), shared.VolumeMgr.CurrentVolume));
                lex.Add(new KeyValuePair<Structure, Structure>(new StringValue("HOMECONNECTION"), GetLocalConnection(shared)));
                if (Mainframe.instance != null)
                {
                    lex.Add(new KeyValuePair<Structure, Structure>(new StringValue("MESSAGES"), new MessageQueueStructure(Mainframe.instance.messageQueue, shared)));
                }
                return lex;
            });
            shared.BindingMgr.AddGetter("TIME", () => new TimeStamp(Planetarium.GetUniversalTime()));
        }
    }
}
