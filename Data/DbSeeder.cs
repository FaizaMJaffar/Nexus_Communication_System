using NexusCommunicationSystem.Models;

namespace NexusCommunicationSystem.Data;

// Specification data only (the plans and the bulk-discount slabs given in the project specification).
// No sample customers, employees or shops are created: the Admin adds the real ones.
// Only inserts when the table is empty, so the Admin's own data is never touched.
public static class DbSeeder
{
    // Adds the spec plans and discount slabs on the first run
    public static void Seed(ApplicationDbContext db)
    {
        if (!db.Plans.Any())
        {
            // All plans are postpaid (specification). Prices are in $ exactly as given there.
            Plan P(string type, string name, string speed, decimal price, int validityDays, string features, bool active = true) => new()
            {
                PlanName = name,
                PlanType = "Postpaid",
                ConnectionType = type,
                Speed = speed,
                Price = price,
                DataGB = 0,
                ValidityDays = validityDays,
                Description = features,
                IsActive = active
            };

            db.Plans.AddRange(
                // ---- Dial-Up ----
                P("Dial-Up", "Dial-Up Hourly 10 Hrs",  "10 Hrs",  50m,  30,  "10 hours of usage|Valid for 1 month"),
                P("Dial-Up", "Dial-Up Hourly 30 Hrs",  "30 Hrs",  130m, 90,  "30 hours of usage|Valid for 3 months"),
                P("Dial-Up", "Dial-Up Hourly 60 Hrs",  "60 Hrs",  260m, 180, "60 hours of usage|Valid for 6 months"),
                P("Dial-Up", "Dial-Up Unlimited 28 Kbps Monthly",   "28 Kbps", 75m,  30, "Unlimited usage at 28 Kbps|Billed monthly"),
                P("Dial-Up", "Dial-Up Unlimited 28 Kbps Quarterly", "28 Kbps", 150m, 90, "Unlimited usage at 28 Kbps|Billed quarterly"),
                P("Dial-Up", "Dial-Up Unlimited 56 Kbps Monthly",   "56 Kbps", 100m, 30, "Unlimited usage at 56 Kbps|Billed monthly"),
                P("Dial-Up", "Dial-Up Unlimited 56 Kbps Quarterly", "56 Kbps", 180m, 90, "Unlimited usage at 56 Kbps|Billed quarterly"),

                // ---- Broadband ----
                P("Broadband", "Broadband Hourly 30 Hrs", "30 Hrs", 175m, 30,  "30 hours of usage|Valid for 1 month"),
                P("Broadband", "Broadband Hourly 60 Hrs", "60 Hrs", 315m, 180, "60 hours of usage|Valid for 6 months"),
                P("Broadband", "Broadband Unlimited 64 Kbps Monthly",    "64 Kbps",  225m, 30, "Unlimited usage at 64 Kbps|Billed monthly"),
                P("Broadband", "Broadband Unlimited 64 Kbps Quarterly",  "64 Kbps",  400m, 90, "Unlimited usage at 64 Kbps|Billed quarterly"),
                P("Broadband", "Broadband Unlimited 128 Kbps Monthly",   "128 Kbps", 350m, 30, "Unlimited usage at 128 Kbps|Billed monthly"),
                P("Broadband", "Broadband Unlimited 128 Kbps Quarterly", "128 Kbps", 445m, 90, "Unlimited usage at 128 Kbps|Billed quarterly"),

                // ---- Landline (rental + call charges) ----
                P("Landline", "Local Unlimited (Yearly)", "Local calls", 75m, 365, "Yearly rental|Call charges: 55 cents per minute"),
                P("Landline", "Local Monthly",            "Local calls", 35m, 30,  "Monthly rental|Call charges: 75 cents per minute"),
                P("Landline", "STD Monthly",              "Local + STD", 125m, 30, "Monthly rental|Local: 70 cents/min|STD: $2.25/min|Messaging for mobiles: $1.00/min"),
                P("Landline", "STD Half-Yearly",          "Local + STD", 420m, 180, "Half-yearly rental|Local: 60 cents/min|STD: $2.00/min|Messaging for mobiles: $1.15/min"),
                // The specification leaves the price of this plan blank, so it is created inactive:
                // set the price in Admin > Plans and tick Active.
                P("Landline", "STD Yearly",               "Local + STD", 0m, 365,  "Yearly rental|Local: 60 cents/min|STD: $1.75/min|Messaging for mobiles: $1.25/min", false)
            );
        }

        // Bulk / corporate discount slabs from the project specification
        if (!db.Discounts.Any())
        {
            db.Discounts.AddRange(
                new Discount { Name = "Bulk 10-15 connections (25%)", Type = "Percentage", Value = 25, IsActive = true },
                new Discount { Name = "Bulk 15-25 connections (50%)", Type = "Percentage", Value = 50, IsActive = true },
                new Discount { Name = "Bulk 25-50 connections (75%)", Type = "Percentage", Value = 75, IsActive = true },
                new Discount { Name = "Bulk 50+ connections (100%)",  Type = "Percentage", Value = 100, IsActive = true }
            );
        }

        db.SaveChanges();
    }
}
