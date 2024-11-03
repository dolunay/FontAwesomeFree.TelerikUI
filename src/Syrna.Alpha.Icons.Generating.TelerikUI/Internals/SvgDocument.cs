using HtmlAgilityPack;

namespace Syrna.Alpha.Icons.Generating.TelerikUI.Internals;

internal class SvgDocument
{
    public SvgDocument(string svg)
    {
        Svg = svg;
        Document = new HtmlDocument();
        Document.OptionOutputOriginalCase = true;
        Document.GlobalAttributeValueQuote = AttributeValueQuote.SingleQuote;
        Document.LoadHtml(Svg);
        Document.UseAttributeOriginalName("svg");

        SvgNode = Document.DocumentNode.SelectSingleNode("//svg")
            ?? throw new InvalidOperationException("The provided SVG does not contain a root <svg> element.");
    }

    public HtmlDocument Document { get; }

    public string Svg { get; }

    public HtmlNode SvgNode { get; }

    public SvgColorType CalculateSvgColorType()
    {
        var nodes = SvgNode.DescendantsAndSelf().ToList();
        var fillColors = nodes.Where(x => x.Attributes.Contains("fill")).Select(x => x.Attributes["fill"].Value).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        fillColors.Remove("none");
        var strokeColors = nodes.Where(x => x.Attributes.Contains("stroke")).Select(x => x.Attributes["stroke"].Value).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        strokeColors.Remove("none");
        var colorColors = nodes.Where(x => x.Attributes.Contains("color")).Select(x => x.Attributes["color"].Value).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var allColors = fillColors.Concat(strokeColors).Concat(colorColors).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (allColors.Count > 1)
        {
            return SvgColorType.Multiple;
        }

        if (fillColors.Count + strokeColors.Count + colorColors.Count > 1)
        {
            return SvgColorType.SingleShared;
        }

        if (fillColors.Count == 1)
        {
            return SvgColorType.SingleFill;
        }

        if (strokeColors.Count == 1)
        {
            return SvgColorType.SingleStroke;
        }

        if (colorColors.Count == 1)
        {
            return SvgColorType.SingleColor;
        }

        return SvgColorType.None;
    }

    public void ConvertStylesToAttributes()
    {
        var nodes = SvgNode.DescendantsAndSelf().ToList();
        foreach (var node in nodes.Where(node => node.Attributes.Contains("style")))
        {
            var styleValue = node.Attributes["style"].Value;
            var parts = styleValue.Split([';'], StringSplitOptions.RemoveEmptyEntries);
            var attributes = new List<HtmlAttribute>();
            foreach (var part in parts)
            {
                var styleEntry = part.Split(':');
                if (styleEntry.Length > 1)
                {
                    attributes.Add(Document.CreateAttribute(styleEntry[0].Trim(), styleEntry[1].Trim()));
                }
            }

            node.Attributes.Remove("style");
            foreach (var attribute in attributes)
            {
                node.Attributes.Add(attribute);
            }
        }
    }

    public Dictionary<string, string> GetAttributes()
    {
        return SvgNode.Attributes.ToDictionary(x => x.Name, x => x.Value);
    }

    /// <summary>
    /// Determines if the SVG and its children contain multiple fill colors.
    /// </summary>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public bool HasMultipleFillColors()
    {
        var type = CalculateSvgColorType();
        return type == SvgColorType.Multiple;
    }

    public void RemoveComments()
    {
        var comments = SvgNode.Descendants().Where(x => x.NodeType == HtmlNodeType.Comment).ToList();
        foreach (var comment in comments)
        {
            comment.Remove();
        }
    }

    public void Scrub(bool skipColorScrub = false)
    {
        RemoveComments();
        ConvertStylesToAttributes();
        CalculateSvgColorType();
        if (!skipColorScrub)
        {
            UpdateColors();
        }
    }

    public void UpdateColors()
    {
        var colorType = CalculateSvgColorType();
        UpdateFillColors(colorType);

        if (colorType == SvgColorType.SingleStroke || colorType == SvgColorType.None)
        {
            if (SvgNode.Attributes.Contains("fill"))
            {
                if (!SvgNode.Attributes["fill"].Value.Equals("none", StringComparison.OrdinalIgnoreCase))
                {
                    SvgNode.Attributes["fill"].Value = "currentColor";
                }
            }
            else
            {
                SvgNode.Attributes.Add("fill", "currentColor");
            }
        }

        UpdateStrokeColors(colorType);
        UpdateColorColors(colorType);
    }

    private void UpdateColorColors(SvgColorType colorType)
    {
        if (colorType == SvgColorType.SingleColor || colorType == SvgColorType.SingleShared)
        {
            var nodes = Document.DocumentNode.DescendantsAndSelf();
            foreach (var node in nodes.Where(node => node.Attributes.Contains("color") && !node.Attributes["color"].Value.Equals("transparent", StringComparison.OrdinalIgnoreCase)))
            {
                node.Attributes["color"].Value = "currentColor";
            }
        }
    }

    private void UpdateFillColors(SvgColorType colorType)
    {
        if (colorType == SvgColorType.SingleFill || colorType == SvgColorType.SingleShared)
        {
            var nodes = Document.DocumentNode.DescendantsAndSelf();
            foreach (var node in nodes.Where(node => node.Attributes.Contains("fill") && !node.Attributes["fill"].Value.Equals("none", StringComparison.OrdinalIgnoreCase)))
            {
                node.Attributes["fill"].Value = "currentColor";
            }
        }
    }

    private void UpdateStrokeColors(SvgColorType colorType)
    {
        if (colorType == SvgColorType.SingleStroke || colorType == SvgColorType.SingleShared)
        {
            var nodes = Document.DocumentNode.DescendantsAndSelf();
            foreach (var node in nodes.Where(node => node.Attributes.Contains("stroke") && !node.Attributes["stroke"].Value.Equals("none", StringComparison.OrdinalIgnoreCase)))
            {
                node.Attributes["stroke"].Value = "currentColor";
            }
        }
    }
}