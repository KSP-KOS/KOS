using System;
using System.Linq;
using kOS.Safe;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Utilities;
using kOS.Suffixed;
using kOS.Suffixed.Part;

namespace kOS.Function
{
    public class HighlightStructure : Structure, IUpdateObserver, IDisposable
    {
        private enum HighlightType
        {
            Part,
            Parts,
            Element
        }
        private readonly UpdateHandler updateHandler;
        private bool enabled;
        private readonly object toHighlight;
        private RgbaColor color;
        private HighlightType highlightType;
        private bool stale;

        public HighlightStructure(UpdateHandler updateHandler, object toHighlight, RgbaColor color)
        {
            this.updateHandler = updateHandler;
            this.toHighlight = toHighlight;
            this.color = color;
            DetermineType();
            InitializeSuffixes();
            stale = true;
            enabled = true;
            updateHandler.AddObserver(this);
        }

        private void InitializeSuffixes()
        {
            AddSuffix("COLOR", new SetSuffix<RgbaColor>(() => color, value =>
            {
                color = value;
                stale = true;
            }));
            AddSuffix("ENABLED", new SetSuffix<bool>(() => enabled, value =>
            {
                enabled = value;
                stale = true;
            }));
        }

        private void DetermineType()
        {
            if (toHighlight is PartValue)
            {
                highlightType = HighlightType.Part;
            }
            else if (toHighlight is ListValue)
            {
                highlightType = HighlightType.Parts;
            }
            else if (toHighlight is ElementValue)
            {
                highlightType = HighlightType.Element;
            }
        }

        public void KOSUpdate(double deltaTime)
        {
            if (!stale) return;

            switch (highlightType)
            {
                case HighlightType.Part:
                {
                    var part = toHighlight as PartValue;
                    HighlightPart(part);
                    break;
                }
                case HighlightType.Parts:
                {
                    var list = toHighlight as ListValue;
                    foreach (var part in list.OfType<PartValue>())
                    {
                        HighlightPart(part);
                    }
                    break;
                }
                case HighlightType.Element:
                {
                    var element = toHighlight as ElementValue;
                    foreach (var part in element.Parts)
                    {
                        HighlightPart(part as PartValue);
                    }
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }
            stale = false;
        }

        private void HighlightPart(PartValue partValue)
        {
            var part = partValue.Part;
            part.highlightColor = color.Color;
            part.highlightType = Part.HighlightType.AlwaysOn;
            part.SetHighlight(enabled, false);
        }

        public void Dispose()
        {
            updateHandler.RemoveObserver(this);
        }

        public override string ToString()
        {
            return string.Format("HIGHLIGHT( Item: {0} Color: {1} Enabled: {2}", toHighlight, color, enabled);
        }
    }
}