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
        public static WixException InvalidDialogSize() => new WixException("The dialog node has invalid size attributes.");
    }
}
