using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Exceptions;
using kOS.Safe.Execution;
using UnityEngine;

namespace kOS.Suffixed.Widget
{
    [kOS.Safe.Utilities.KOSNomenclature("Label")]
    public class Label : Widget
    {
        private GUIContent content { get; set; }
        private GUIContent content_visible { get; set; }

        public string Text { get { return content.text; } }

        private UserDelegate UserTextUpdater { get; set; }
        private TriggerInfo UserTextUpdateResult;

        public Label(Box parent, string text, WidgetStyle style) : base(parent, style)
        {
            RegisterInitializer(InitializeSuffixes);
            content = new GUIContent(text);
            content_visible = new GUIContent(text);
        }

        public Label(Box parent, string text) : this(parent, text, parent.FindStyle("label"))
        {
        }

        private void InitializeSuffixes()
        {
            AddSuffix("TEXT", new SetSuffix<StringValue>(() => content.text, value => SetText(value)));
            AddSuffix("IMAGE", new SetSuffix<StringValue>(() => "", value => SetContentImage(value)));
            AddSuffix("TEXTUPDATER", new SetSuffix<UserDelegate>(() => CallbackGetter(UserTextUpdater), value => UserTextUpdater = CallbackSetter(value)));
            AddSuffix("TOOLTIP", new SetSuffix<StringValue>(() => content.tooltip, value => { if (content.tooltip != value) { content.tooltip = value; Communicate(() => content_visible.tooltip = value); } }));
        }

        protected void SetText(string newValue)
        {
            if (content.text != newValue)
            {
                content.text = newValue;
                Communicate(() => content_visible.text = newValue);
            }
        }

        protected void SetInitialContentImage(Texture2D img)
        {
            content.image = img;
            content_visible.image = img;
        }

        protected void SetContentImage(string img)
        {
            Texture2D tex = GetTexture(img);
            content.image = tex;
            Communicate(() => content_visible.image = tex);
        }

        protected string StoredText()
        {
            return content.text;
        }

        protected string VisibleTooltip()
        {
            return content_visible.tooltip;
        }
        protected string VisibleText()
        {
            return content_visible.text;
        }

        protected void SetVisibleText(string t)
        {
            if (content_visible.text != t) {
                content_visible.text = t;
                Communicate(() => content.text = t);
            }
        }

        protected GUIContent VisibleContent()
        {
            return content_visible;
        }

        public override void DoGUI()
        {
            ScheduleTextUpdate();
            myId = GUIUtility.GetControlID(FocusType.Passive);
            GUI.SetNextControlName(myId.ToString());
            GUILayout.Label(content_visible, ReadOnlyStyle);
        }

        private void ScheduleTextUpdate()
        {
            if (UserTextUpdater == null)
                return;

            if (UserTextUpdateResult != null)
            {
                if (UserTextUpdateResult.CallbackFinished)
                {
                    SetText(UserTextUpdateResult.ReturnValue.ToString());
                    UserTextUpdateResult = (guiCaused ?
                        UserTextUpdater.TriggerOnFutureUpdate(InterruptPriority.CallbackOnce) :
                        UserTextUpdater.TriggerOnNextOpcode(InterruptPriority.NoChange ));
                }
                // Else just do nothing because a previous call is still pending its return result.
                // don't start up a second call while still waiting for the first one to finish.  (we
                // don't want to end up stacking up calls faster than they execute.)
            }
            else
            {
                UserTextUpdateResult = (guiCaused ?
                    UserTextUpdater.TriggerOnFutureUpdate(InterruptPriority.CallbackOnce) :
                    UserTextUpdater.TriggerOnNextOpcode(InterruptPriority.NoChange));
            }
        }

        public override string ToString()
        {
            return "LABEL(" + content.text.Ellipsis(10) + ")";
        }
    }
}
