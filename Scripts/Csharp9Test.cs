using System.Collections.Generic;

namespace DaftAppleGames.VehicleFrameworkBZ
{
    internal static class CSharp9Test
    {
        // Target-typed new: C# 9.
        private static readonly List<string> messages = new();

        internal static bool Check(object value)
        {
            // "not" and "or" patterns: C# 9.
            if (value is not string text)
            {
                return false;
            }

            messages.Add(text);
            return text.Length is 1 or 2;
        }
    }
}
