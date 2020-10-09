using Discord;

namespace ServerWakeBot {
    internal static class Extensions {
        public static bool HasStringPrefixLower(this IUserMessage msg, string str, ref int argPos) {
            var text = msg.Content.ToLowerInvariant();
            if (text.StartsWith(str)) {
                argPos = str.Length;
                return true;
            }
            return false;
        }
    }
}
