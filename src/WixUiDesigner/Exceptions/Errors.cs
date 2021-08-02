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
    }
}
