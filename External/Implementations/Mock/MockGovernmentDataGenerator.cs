namespace CarCareTracker.External.Implementations.Mock
{
    /// <summary>
    /// Shared deterministic fake-vehicle-profile generator so MockDVLAAdapter and MockDVSAAdapter
    /// return mutually-consistent Make/Model/Year/FuelType/Colour for the same registration number,
    /// without either adapter depending on the other or on the caller's real vehicle record (the
    /// real DVLA/DVSA APIs are looked up by registration number alone, so a real adapter wouldn't
    /// have access to our stored Year/Make/Model either).
    /// </summary>
    internal static class MockGovernmentDataGenerator
    {
        private static readonly (string Make, string Model)[] VehicleProfiles =
        {
            ("FORD", "FOCUS"),
            ("VAUXHALL", "CORSA"),
            ("VOLKSWAGEN", "GOLF"),
            ("BMW", "3 SERIES"),
            ("AUDI", "A3"),
            ("TOYOTA", "YARIS"),
            ("NISSAN", "QASHQAI"),
            ("PEUGEOT", "208"),
            ("MERCEDES-BENZ", "C CLASS"),
            ("HONDA", "CIVIC")
        };
        private static readonly string[] Colours = { "BLUE", "BLACK", "WHITE", "SILVER", "RED", "GREY", "GREEN" };
        private static readonly string[] FuelTypes = { "PETROL", "DIESEL", "HYBRID ELECTRIC", "ELECTRICITY" };

        /// <summary>Deterministic Random seeded from the (case/whitespace-normalized) input string.</summary>
        public static Random SeededRandomFor(string seedSource)
        {
            var normalized = (seedSource ?? string.Empty).Trim().ToUpperInvariant();
            int seed = normalized.Aggregate(17, (acc, c) => unchecked(acc * 31 + c));
            return new Random(seed);
        }

        public static (string Make, string Model, int Year, string FuelType, string Colour) GetProfile(string registrationNumber)
        {
            var rnd = SeededRandomFor(registrationNumber);
            var profile = VehicleProfiles[rnd.Next(VehicleProfiles.Length)];
            int year = rnd.Next(2005, 2026);
            string fuelType = FuelTypes[rnd.Next(FuelTypes.Length)];
            string colour = Colours[rnd.Next(Colours.Length)];
            return (profile.Make, profile.Model, year, fuelType, colour);
        }
    }
}
