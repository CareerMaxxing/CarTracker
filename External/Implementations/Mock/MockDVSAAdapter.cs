using CarCareTracker.External.Interfaces;
using CarCareTracker.Models;

namespace CarCareTracker.External.Implementations.Mock
{
    /// <summary>
    /// Deterministic, clearly-labelled (IsMockData=true) fake DVSA MOT history - no network calls,
    /// no credentials. See IDVSAAdapter.cs / CLAUDE.md's locked "Government data" decision.
    /// </summary>
    public class MockDVSAAdapter : IDVSAAdapter
    {
        private static readonly string[] AdvisoryPhrases =
        {
            "Front tyre worn close to legal limit",
            "Nearside headlamp aim slightly high",
            "Offside rear brake disc worn, pitted or scored, but not seriously weakened",
            "Exhaust has a minor leak",
            "Windscreen wiper blade worn or damaged"
        };

        public DVSAMotHistory GetMotHistory(string registrationNumber)
        {
            var normalized = (registrationNumber ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(normalized))
            {
                return new DVSAMotHistory { Found = false, IsMockData = true };
            }
            var profile = MockGovernmentDataGenerator.GetProfile(normalized);
            var rnd = MockGovernmentDataGenerator.SeededRandomFor(normalized + "|dvsa");

            var firstUsedDate = new DateTime(profile.Year, rnd.Next(1, 13), rnd.Next(1, 28));
            int testCount = rnd.Next(1, 5);
            var tests = new List<DVSAMotTest>();
            var testDate = firstUsedDate.AddYears(3);
            int odometer = rnd.Next(2000, 12000);
            for (int i = 0; i < testCount; i++)
            {
                bool passed = rnd.NextDouble() > 0.15;
                var comments = new List<DVSAMotComment>();
                if (rnd.NextDouble() > 0.4)
                {
                    comments.Add(new DVSAMotComment { Text = AdvisoryPhrases[rnd.Next(AdvisoryPhrases.Length)], Type = "ADVISORY" });
                }
                if (!passed)
                {
                    comments.Add(new DVSAMotComment { Text = "Nearside front brake pad(s) worn below 1.5mm", Type = "MAJOR" });
                }
                tests.Add(new DVSAMotTest
                {
                    CompletedDate = testDate.ToString("yyyy-MM-dd"),
                    TestResult = passed ? "PASSED" : "FAILED",
                    ExpiryDate = passed ? testDate.AddYears(1).ToString("yyyy-MM-dd") : string.Empty,
                    OdometerValue = odometer.ToString(),
                    OdometerUnit = "mi",
                    MotTestNumber = rnd.Next(100000000, 999999999).ToString(),
                    RfrAndComments = comments
                });
                testDate = testDate.AddYears(1);
                odometer += rnd.Next(2000, 12000);
            }

            return new DVSAMotHistory
            {
                Found = true,
                Registration = normalized.ToUpperInvariant(),
                Make = profile.Make,
                Model = profile.Model,
                FirstUsedDate = firstUsedDate.ToString("yyyy-MM-dd"),
                FuelType = profile.FuelType,
                PrimaryColour = profile.Colour,
                MotTests = tests,
                IsMockData = true
            };
        }
    }
}
