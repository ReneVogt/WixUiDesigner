/*
 *
 * Published under MIT license as described in the LICENSE.md file.
 *
 */

#nullable enable

namespace WixUiDesigner.Exceptions
{
    static class Errors
    {
        public static WixException InvalidDialogSize() => new("The dialog node has invalid size attributes.");
        public static WixException ImageButtonNotSupported() => new("Image buttons are not yet supported.");
        public static WixException BitmapButtonNotSupported() => new("Bitmap buttons are not yet supported.");
        public static WixException IconButtonNotSupported() => new("Icon buttons are not yet supported.");
    }
}
