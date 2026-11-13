using System.Globalization;
using System.Text.RegularExpressions;

namespace LoopMeet.App.Tests.Features.Profile;

/// <summary>
/// FR-017 (feature 022): Delete account's terracotta, and the chip text, stay readable on their
/// backgrounds. Reads the shipped colours from Colors.xaml and applies the WCAG 2.x contrast ratio.
/// </summary>
public sealed class ProfileColorContrastTests
{
    [Theory]
    [InlineData("DestructiveTerracotta", "White")]
    [InlineData("DestructiveTerracottaDark", "Gray900")]
    [InlineData("AmberTintText", "AmberTint")]
    public void ProfileColors_MeetWcagAaForNormalText(string foreground, string background)
    {
        var colors = ReadColors();

        var ratio = ContrastRatio(colors[foreground], colors[background]);

        Assert.True(ratio >= 4.5, $"{foreground} on {background} is {ratio:F2}:1; FR-017 needs at least 4.5:1.");
    }

    private static Dictionary<string, string> ReadColors()
    {
        var xaml = File.ReadAllText(Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "../../../../..", "src/LoopMeet.App/Resources/Styles/Colors.xaml")));
        // The first definition wins: Colors.xaml keeps an older, commented-out palette below the live one.
        return Regex.Matches(xaml, @"<Color x:Key=""(\w+)"">#([0-9A-Fa-f]{6})</Color>")
            .GroupBy(match => match.Groups[1].Value)
            .ToDictionary(group => group.Key, group => group.First().Groups[2].Value);
    }

    private static double ContrastRatio(string foreground, string background)
    {
        var (first, second) = (Luminance(foreground), Luminance(background));
        return (Math.Max(first, second) + 0.05) / (Math.Min(first, second) + 0.05);
    }

    private static double Luminance(string hex)
    {
        double Channel(int offset)
        {
            var value = int.Parse(hex.AsSpan(offset, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture) / 255.0;
            return value <= 0.03928 ? value / 12.92 : Math.Pow((value + 0.055) / 1.055, 2.4);
        }

        return 0.2126 * Channel(0) + 0.7152 * Channel(2) + 0.0722 * Channel(4);
    }
}
