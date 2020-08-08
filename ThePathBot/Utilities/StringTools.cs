using System;
namespace ThePathBot.Utilities
{
    public class StringTools
    {
        public static string ReverseString(string toReverse)
        {
            var stringArray = toReverse.ToCharArray();
            Array.Reverse(stringArray);
            return new string(stringArray);
        }
    }
}
