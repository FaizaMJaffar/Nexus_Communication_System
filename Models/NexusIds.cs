namespace NexusCommunicationSystem.Models;

// Builds the two codes defined in the specification.
//   Order id   : 11 characters = type letter + 10-digit serial          e.g. D0000000001
//   Account id : 16 characters = type letter + 3-digit city + 12-digit serial   e.g. B101000000000001
// Type letter: D = Dial-Up, B = Broadband, T = telephone (landline) only.
// The serial runs separately for every type letter.
public static class NexusIds
{
    public static string TypeCode(string connectionType) => connectionType switch
    {
        "Dial-Up" => "D",
        "Landline" => "T",
        _ => "B"
    };

    public static string CityCode(string city) =>
        city != null && PlanCatalog.CityCodes.TryGetValue(city, out var code) ? code : "999";

    // existing = every order number that starts with typeCode
    public static string NextOrderNumber(IEnumerable<string> existing, string typeCode)
    {
        long last = 0;
        foreach (var n in existing)
        {
            if (n != null && n.Length == 11 && n[0] == typeCode[0]
                && long.TryParse(n.Substring(1), out var serial) && serial > last)
            {
                last = serial;
            }
        }
        return typeCode + (last + 1).ToString("D10");
    }

    // existing = every account id that starts with typeCode
    public static string NextAccountNumber(IEnumerable<string> existing, string typeCode, string cityCode)
    {
        long last = 0;
        foreach (var n in existing)
        {
            if (n != null && n.Length == 16 && n[0] == typeCode[0]
                && long.TryParse(n.Substring(4), out var serial) && serial > last)
            {
                last = serial;
            }
        }
        return typeCode + cityCode + (last + 1).ToString("D12");
    }
}
