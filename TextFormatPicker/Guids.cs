// Guids.cs
// MUST match guids.h
using System;

namespace MattManela.TextFormatPicker
{
    static class GuidList
    {
        public const string guidTextFormatPickerPkgString = "e05115da-78b5-4bb3-accb-6b424cf525ad";
        public const string guidTextFormatPickerCmdSetString = "59afd2ef-8aa5-49ab-a4af-d30e1d087443";

        public static readonly Guid guidTextFormatPickerCmdSet = new Guid(guidTextFormatPickerCmdSetString);
    };
}