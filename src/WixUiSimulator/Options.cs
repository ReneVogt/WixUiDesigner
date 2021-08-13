/*
 *
 * Published under MIT license as described in the LICENSE.md file.
 *
 */

using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Controls;
using Microsoft.VisualStudio.Shell;
using WixUiSimulator.Exceptions;
using WixUiSimulator.Logging;

#nullable enable

namespace WixUiSimulator
{
    public sealed class Options : DialogPage, INotifyPropertyChanged
    {
        double simulatorsize = DefaultSimulatorSize;
        public const DebugContext DefaultDebugContext = DebugContext.None;
        public const Dock DefaultSimulatorPosition = Dock.Top;
        public const double DefaultSimulatorSize = 0.4d;
        public const double DefaultUpdateInterval = 0.3d;

        DebugContext debugcontext = DefaultDebugContext;
        Dock simulatorposition = DefaultSimulatorPosition;
        double updateinterval = DefaultUpdateInterval;

        public event PropertyChangedEventHandler? PropertyChanged;

        [Category("Layout")]
        [DisplayName("Simulator position")]
        [Description("Select where the simulator should appear.")]
        [DefaultValue(DefaultSimulatorPosition)]
        public Dock SimulatorPosition
        {
            get => simulatorposition;
            set
            {
                if (value == simulatorposition)
                    return;
                simulatorposition = value;
                OnPropertyChanged();
            }
        }
        [Category("Layout")]
        [DisplayName("Simulator size")]
        [Description("The initial relative size of the simulator margin.")]
        [DefaultValue(DefaultSimulatorSize)]
        public double SimulatorSize
        {
            get => simulatorsize;
            set
            {
                if (value == simulatorsize)
                    return;
                if (value is not (> 0 and < 1)) throw Errors.InvalidSimulatorSize(value);
                simulatorsize = value;
                OnPropertyChanged();
            }
        }

        [Category("Behaviour")]
        [DisplayName("Update interval")]
        [Description("The seconds to wait after code changes before updating the simulator view.")]
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
{nameof(SimulatorPosition)}: {SimulatorPosition}
{nameof(SimulatorSize)}: {SimulatorSize}
{nameof(UpdateInterval)}: {UpdateInterval}";

        void OnPropertyChanged([CallerMemberName] string caller = "") => PropertyChanged?.Invoke(this, new(caller));
    }
}
