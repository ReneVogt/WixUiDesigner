/*
 * (C) René Vogt
 *
 * Published under MIT license as described in the LICENSE.md file.
 *
 */

using System;
using System.ComponentModel;
using Microsoft.VisualStudio.Shell;
using WixUiDesigner.Logging;
using System.Runtime.CompilerServices;
using System.Windows.Controls;

#nullable enable

namespace WixUiDesigner
{
    public sealed class Options : DialogPage, INotifyPropertyChanged
    {
        public const int DefaultDesignerSize = 200;

        int designersize = DefaultDesignerSize;
        Dock designerposition = Dock.Top;
        public event PropertyChangedEventHandler? PropertyChanged;
        DebugContext debugcontext = DebugContext.None;

        [Category("Logging")]
        [DisplayName("Debug context")]
        [Description("Select the kind of debug messages to be logged.")]
        [DefaultValue(DebugContext.None)]
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
        [Category("Layout")]
        [DisplayName("Designer position")]
        [Description("Select where the designer should appear.")]
        [DefaultValue(Dock.Top)]
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
        [Description("The height or width of the designer.")]
        [DefaultValue(DefaultDesignerSize)]
        public int DesignerSize
        {
            get => designersize;
            set
            {
                if (value == designersize)
                    return;
                if (value < 0)
                    throw new ArgumentOutOfRangeException(paramName: nameof(DesignerSize), value, "The size must be a non-negative value.");
                designersize = value;
                OnPropertyChanged();
            }
        }

        public override string ToString() => $@"{nameof(DebugContext)}: {DebugContext}
{nameof(DesignerPosition)}: {DesignerPosition}
{nameof(DesignerSize)}: {DesignerSize}";

        void OnPropertyChanged([CallerMemberName] string caller = "") => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(caller));
    }
}
