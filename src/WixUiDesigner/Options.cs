/*
 * (C) René Vogt
 *
 * Published under MIT license as described in the LICENSE.md file.
 *
 */

using System.ComponentModel;
using Microsoft.VisualStudio.Shell;
using WixUiDesigner.Logging;
using System.Runtime.CompilerServices;

#nullable enable

namespace WixUiDesigner
{
    public sealed class Options : DialogPage, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        DebugContext debugcontext;

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

        void OnPropertyChanged([CallerMemberName] string caller = "") => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(caller));
    }
}
