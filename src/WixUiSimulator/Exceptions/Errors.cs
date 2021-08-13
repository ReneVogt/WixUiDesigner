/*
 *
 * Published under MIT license as described in the LICENSE.md file.
 *
 */

using System;

#nullable enable

namespace WixUiSimulator.Exceptions
{
    static class Errors
    {
        public static WixException InvalidDialogSize() => new("The dialog node has invalid size attributes.");
        public static WixException ImageButtonNotSupported() => new("Image buttons are not yet supported.");
        public static WixException BitmapButtonNotSupported() => new("Bitmap buttons are not yet supported.");
        public static WixException IconButtonNotSupported() => new("Icon buttons are not yet supported.");
        public static ArgumentOutOfRangeException NonPositiveUpdateInterval(double actualValue) =>
            new(paramName: nameof(Options.UpdateInterval), actualValue: actualValue, "The update interval must be a positive value.");
        public static ArgumentOutOfRangeException InvalidSimulatorSize(double actualValue) =>
            new(paramName: nameof(Options.SimulatorSize), actualValue: actualValue, "The simulator size must be greater than zero and less than one.");
    }
}
