using System.Linq;
using Robust.Shared.Utility;

namespace Content.Shared._Floof.Util;

public static class FormattedMessageHelpers
{
    /// <summary>
    ///     Replaces all markup nodes that are not listen in <paramref name="allowedTags"/> with the given node.
    /// </summary>
    public static FormattedMessage SanitizeMarkup(FormattedMessage msg, ICollection<string> allowedTags, MarkupNode? replacement = null)
    {
        var sanitized = new FormattedMessage(msg.Count);
        foreach (var node in msg.Nodes)
        {
            // Null name means it's a text node
            if (node.Name != null && !allowedTags.Contains(node.Name.ToLower()))
            {
                if (replacement != null)
                    sanitized.PushTag(replacement);
                continue;
            }
            sanitized.PushTag(node);
        }

        return sanitized;
    }


    /// <summary>
    ///     Replaces all markup nodes that are not listen in <paramref name="allowedTags"/> with the given text.
    /// </summary>
    public static FormattedMessage SanitizeMarkup(FormattedMessage msg,
        ICollection<string> allowedTags,
        string replacement)
    {
        return SanitizeMarkup(msg, allowedTags, new MarkupNode(replacement));
    }
}
