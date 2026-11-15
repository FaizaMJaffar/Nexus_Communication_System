#nullable enable
namespace NexusCommunicationSystem.Models
{
    // The money rules of the specification, kept in one place so every Accounts screen calculates the same way.
    public static class BillingRules
    {
        // Service tax on the WHOLE bill (charges + security deposit - discount)
        public const decimal ServiceTaxPercent = 12.24m;

        // Bulk / corporate discount by the number of connections a customer holds:
        //   10-15 -> 25 %,  16-25 -> 50 %,  26-50 -> 75 %,  more than 50 -> 100 %
        public static decimal BulkDiscountPercent(int connections)
        {
            if (connections > 50) return 100m;
            if (connections > 25) return 75m;
            if (connections > 15) return 50m;
            if (connections >= 10) return 25m;
            return 0m;
        }

        public static decimal Round(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);

        // Recalculates discount, service tax and total of a bill from its SubTotal and SecurityDeposit.
        // The discount is taken on charges + security deposit (specification: advance and deposit).
        public static void Recalculate(Bill bill, Discount? discount)
        {
            decimal basis = bill.SubTotal + bill.SecurityDeposit;

            decimal discountAmount = 0m;
            if (discount != null)
            {
                discountAmount = discount.Type == "Percentage"
                    ? basis * discount.Value / 100m
                    : discount.Value;
                if (discountAmount > basis) discountAmount = basis;
            }

            bill.DiscountAmount = Round(discountAmount);
            decimal taxable = basis - bill.DiscountAmount;
            bill.ServiceTax = Round(taxable * ServiceTaxPercent / 100m);
            bill.TotalAmount = taxable + bill.ServiceTax;
        }
    }
}
