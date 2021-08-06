/*
 *
 * Published under MIT license as described in the LICENSE.md file.
 *
 */

using System.ComponentModel;
using Microsoft.VisualStudio.Shell;
using WixUiDesigner.Logging;
using System.Runtime.CompilerServices;
using System.Windows.Controls;
using WixUiDesigner.Exceptions;

#nullable enable

namespace WixUiDesigner
{
    public sealed class Options : DialogPage, INotifyPropertyChanged
    {
        double designersize = DefaultDesignerSize;
        public const DebugContext DefaultDebugContext = DebugContext.None;
        public const Dock DefaultDesignerPosition = Dock.Top;
        public const double DefaultDesignerSize = 0.4d;
        public const double DefaultUpdateInterval = 0.3d;

        DebugContext debugcontext = DefaultDebugContext;
        Dock designerposition = DefaultDesignerPosition;
        double updateinterval = DefaultUpdateInterval;

        public event PropertyChangedEventHandler? PropertyChanged;

        [Category("Layout")]
        [DisplayName("Designer position")]
        [Description("Select where the designer should appear.")]
        [DefaultValue(DefaultDesignerPosition)]
        public Dock DesignerPosition
        {
            get => designerposition;
            set
            {
                if (value == designerposition)
                    return;
                designerposition = value;
                OnPropertyChanged();
            }
        }
        [Category("Layout")]
        [DisplayName("Designer size")]
        [Description("The initial relative size of the designer margin.")]
        [DefaultValue(DefaultDesignerSize)]
        public double DesignerSize
        {
            get => designersize;
            set
            {
                if (value == designersize)
                    return;
                if (!(value > 0 && value < 1)) throw Errors.InvalidDesignerSize(value);
                designersize = value;
                OnPropertyChanged();
            }
        }

        [Category("Behaviour")]
        [DisplayName("Update interval")]
        [Description("The seconds to wait after code changes before updating the design view.")]
        [DefaultValue(DefaultUpdateInterval)]
        public double UpdateInterval
        {
            get => updateinterval;
            set
            {
                if (value == updateinterval)
                    return;
                if (value <= 0) throw Errors.NonPositiveUpdateInterval(value);
                updateinterval = value;
                OnPropertyChanged();
            }
        }

        [Category("Logging")]
        [DisplayName("Debug context")]
        [Description("Select the kind of debug messages to be logged.")]
        [DefaultValue(DefaultDebugContext)]
        public DebugContext DebugContext
        {
            get => debugcontext;
            set
            {
                if (value == debugcontext)
                    return;
                debugcontext = value;
                OnPropertyChanged();
            }
        }

        public override string ToString() => $@"{nameof(DebugContext)}: {DebugContext}
{nameof(DesignerPosition)}: {DesignerPosition}
{nameof(DesignerSize)}: {DesignerSize}
{nameof(UpdateInterval)}: {UpdateInterval}";

        void OnPropertyChanged([CallerMemberName] string caller = "") => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(caller));
    }
}
