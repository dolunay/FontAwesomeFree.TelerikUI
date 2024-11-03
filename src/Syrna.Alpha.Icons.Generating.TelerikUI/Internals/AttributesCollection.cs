using System.Collections.ObjectModel;
using System.Text;

namespace Syrna.Alpha.Icons.Generating.TelerikUI.Internals;

internal class AttributesCollection : List<Dictionary<string, string>>
{
    /// <summary>
    /// Gets the matching unique attributes or adds a new set of attributes.
    /// </summary>
    /// <param name="attributes">The attributes to match.</param>
    /// <returns>The index of the matching attributes.</returns>
    public int FindOrAdd(Dictionary<string, string> attributes)
    {
        var match = Find(x => x.Count == attributes.Count && x.Keys.All(k => attributes.ContainsKey(k) && attributes[k] == x[k]));
        if (match is null)
        {
            Add(attributes);
            match = attributes;
        }

        return IndexOf(match);
    }
    protected readonly ReadOnlyDictionary<string, string> a1 = new(new Dictionary<string, string>() { { "a", "1" }, { "b", "2" } });
    public string ToCSharp()
    {
        var builder = new StringBuilder();

        for (var i = 0; i < Count; i++)
        {
            builder.Append("private static readonly ReadOnlyDictionary<string, string> attributeSet");
            builder.Append(i);
            builder.Append(" = new(new Dictionary<string, string>() {");
            foreach (var attribute in this[i])
            {
                builder.Append("{\"");
                builder.Append(attribute.Key);
                builder.Append("\", \"");
                builder.Append(attribute.Value);
                builder.Append("\"}, ");
            }

            builder.AppendLine("});");
        }

        return builder.ToString();
    }
}