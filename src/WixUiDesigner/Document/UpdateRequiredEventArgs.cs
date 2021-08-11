/*
 *
 * Published under MIT license as described in the LICENSE.md file.
 *
 */

using System;

#nullable enable

namespace WixUiDesigner.Document
{
    sealed class UpdateRequiredEventArgs : EventArgs
    {
        public bool DocumentChanged { get; }
        public bool SelectionChanged { get; }
        public bool WixChanged { get; }
        public EventArgs OriginalArguments { get; }

        UpdateRequiredEventArgs(bool documentChanged, bool selectionChanged, bool wixChanged, EventArgs originalArgs) =>
            (DocumentChanged, SelectionChanged, WixChanged, OriginalArguments) = (documentChanged, selectionChanged, wixChanged, originalArgs);

        public static UpdateRequiredEventArgs DocumentChangedArgs(EventArgs originalArgs) => new (true, false, false, originalArgs);
        public static UpdateRequiredEventArgs SelectionChangedArgs(EventArgs originalArgs) => new (false, true, false, originalArgs);
        public static UpdateRequiredEventArgs WixChangedArgs(EventArgs originalArgs) => new (false, false, true, originalArgs);
    }
}
