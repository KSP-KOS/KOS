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
            updateHandler.AddObserver(this);
        }

        private void InitializeSuffixes()
        {
            AddSuffix("COLOR", new SetSuffix<RgbaColor>(() => color, value =>
            {
                stale = true;
                color = value;
            }));
            AddSuffix("ENABLED", new SetSuffix<bool>(() => enabled, value =>
            {
                stale = true;
                enabled = value;
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

        public void Update(double deltaTime)
        {
            if (stale)
            {
                
            }
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
        }

        private void HighlightPart(PartValue partValue)
        {
            if (partValue == null)
            {
                SafeHouse.Logger.Log("HIGHLIGHTVALUE: partValue is null");
            }
            var part = partValue.Part;
            part.highlightColor = color.Color();
            part.highlightType = Part.HighlightType.AlwaysOn;
            part.SetHighlight(enabled, false);
        }

        public void Dispose()
        {
            updateHandler.RemoveObserver(this);
        }
    }
}