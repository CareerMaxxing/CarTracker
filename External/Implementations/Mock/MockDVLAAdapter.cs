using CarCareTracker.External.Interfaces;
using CarCareTracker.Models;

namespace CarCareTracker.External.Implementations.Mock
{
    /// <summary>
    /// Deterministic, clearly-labelled (IsMockData=true) fake DVLA data - no network calls, no
    /// credentials. See IDVLAAdapter.cs / CLAUDE.md's locked "Government data" decision.
    /// </summary>
    public class MockDVLAAdapter : IDVLAAdapter
    {
        public DVLAVehicleData GetVehicleData(string registrationNumber)
        {
            var normalized = (registrationNumber ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(normalized))
            {
                return new DVLAVehicleData { Found = false, IsMockData = true };
            }
            var profile = MockGovernmentDataGenerator.GetProfile(normalized);
            var rnd = MockGovernmentDataGenerator.SeededRandomFor(normalized + "|dvla");
            var registrationDate = new DateTime(profile.Year, rnd.Next(1, 13), rnd.Next(1, 28));

            //derive status from the generated date (relative to today) instead of picking both
            //independently, so the mock doesn't show e.g. "Taxed" alongside a tax due date from 2021.
            var today = DateTime.Now.Date;
            var motExpiryDate = today.AddDays(rnd.Next(-90, 275));
            bool isSorn = rnd.NextDouble() < 0.1;
            var taxDueDate = isSorn ? today.AddDays(-rnd.Next(1, 400)) : today.AddDays(rnd.Next(-30, 335));

            return new DVLAVehicleData
            {
                Found = true,
                RegistrationNumber = normalized.ToUpperInvariant(),
                TaxStatus = isSorn ? "SORN" : (taxDueDate >= today ? "Taxed" : "Untaxed"),
                TaxDueDate = taxDueDate.ToString("yyyy-MM-dd"),
                MotStatus = motExpiryDate >= today ? "Valid" : "Not valid",
                MotExpiryDate = motExpiryDate.ToString("yyyy-MM-dd"),
                Make = profile.Make,
                YearOfManufacture = profile.Year,
                EngineCapacity = rnd.Next(10, 30) * 100,
                Co2Emissions = rnd.Next(90, 250),
                FuelType = profile.FuelType,
                Colour = profile.Colour,
                MarkedForExport = false,
                DateOfLastV5CIssued = registrationDate.AddYears(rnd.Next(0, 5)).ToString("yyyy-MM-dd"),
                Wheelplan = "2 AXLE RIGID BODY",
                MonthOfFirstRegistration = registrationDate.ToString("yyyy-MM"),
                IsMockData = true
            };
        }
    }
}
