using System.Globalization;

namespace Core.Data;

/// <summary>The bank keeps money in cents; the pages show it as US dollars.</summary>
public static class Money
{
    private static readonly CultureInfo UnitedStates = CultureInfo.GetCultureInfo("en-US");

    /// <summary>15746900 → "$157,469.00", the way the pages show it.</summary>
    public static string Format(long cents) => (cents / 100m).ToString("C2", UnitedStates);

    /// <summary>25000 → "250.00", the way an amount is typed into a form.</summary>
    public static string ToInput(long cents) => (cents / 100m).ToString("0.00", CultureInfo.InvariantCulture);
}
