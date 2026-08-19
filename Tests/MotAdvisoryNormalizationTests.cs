using CarCareTracker.Helper;

namespace CarCareTracker.Tests
{
    /// <summary>
    /// Phase 17 Increment 4: pure-function coverage for the advisory-text normalization that later
    /// increments rely on to dedupe/group recurring MOT advisories (e.g. the same tyre wear advisory
    /// flagged across multiple years) into one Planner item instead of one per test. See PHASE_17.md.
    /// No WebApplicationFactory needed - StaticHelper.NormalizeMotAdvisoryText/GetMotAdvisoryKey are
    /// pure, dependency-free functions.
    /// </summary>
    public class MotAdvisoryNormalizationTests
    {
        [Fact]
        public void NormalizeMotAdvisoryText_StripsTrailingReferenceCode()
        {
            var withCode = StaticHelper.NormalizeMotAdvisoryText("Nearside front tyre worn close to legal limit (5.2.3)");
            var withoutCode = StaticHelper.NormalizeMotAdvisoryText("Nearside front tyre worn close to legal limit");
            Assert.Equal(withoutCode, withCode);
        }

        [Fact]
        public void NormalizeMotAdvisoryText_IsCaseAndWhitespaceInsensitive()
        {
            var a = StaticHelper.NormalizeMotAdvisoryText("  Nearside FRONT Tyre  worn close to legal limit (5.2.3)  ");
            var b = StaticHelper.NormalizeMotAdvisoryText("nearside front tyre worn close to legal limit");
            Assert.Equal(b, a);
        }

        [Fact]
        public void NormalizeMotAdvisoryText_DifferentIssues_StayDifferent()
        {
            var tyre = StaticHelper.NormalizeMotAdvisoryText("Nearside front tyre worn close to legal limit (5.2.3)");
            var brake = StaticHelper.NormalizeMotAdvisoryText("Offside rear brake disc worn, pitted or scored (1.1.14)");
            Assert.NotEqual(tyre, brake);
        }

        [Fact]
        public void NormalizeMotAdvisoryText_BlankInput_ReturnsEmpty()
        {
            Assert.Equal(string.Empty, StaticHelper.NormalizeMotAdvisoryText(""));
            Assert.Equal(string.Empty, StaticHelper.NormalizeMotAdvisoryText("   "));
        }

        [Fact]
        public void GetMotAdvisoryKey_SameNormalizedTextSameVehicle_ProducesSameKey()
        {
            var key1 = StaticHelper.GetMotAdvisoryKey(1, "Nearside front tyre worn close to legal limit (5.2.3)");
            var key2 = StaticHelper.GetMotAdvisoryKey(1, "nearside front tyre worn close to legal limit (5.2.4)");
            Assert.Equal(key1, key2);
        }

        [Fact]
        public void GetMotAdvisoryKey_SameTextDifferentVehicle_ProducesDifferentKey()
        {
            var key1 = StaticHelper.GetMotAdvisoryKey(1, "Nearside front tyre worn close to legal limit");
            var key2 = StaticHelper.GetMotAdvisoryKey(2, "Nearside front tyre worn close to legal limit");
            Assert.NotEqual(key1, key2);
        }

        [Fact]
        public void NormalizeMotAdvisoryText_StripsSidePositionQualifiers()
        {
            var nearside = StaticHelper.NormalizeMotAdvisoryText("Nearside Front Tyre worn close to the legal limit (4.1.E.1)");
            var offside = StaticHelper.NormalizeMotAdvisoryText("Offside Front Tyre worn close to the legal limit (4.1.E.1)");
            Assert.Equal(nearside, offside);
        }

        [Fact]
        public void NormalizeMotAdvisoryText_BrakePipeHoseLineSynonyms_Collapse()
        {
            // the exact real-world case reported by the user: same physical part, three different words.
            var pipe = StaticHelper.NormalizeMotAdvisoryText("Offside Brake pipe excessively corroded (1.1.11 (c))");
            var line = StaticHelper.NormalizeMotAdvisoryText("Offside Brake line excessively corroded");
            var hose = StaticHelper.NormalizeMotAdvisoryText("Offside Brake hose excessively corroded");
            Assert.Equal(pipe, line);
            Assert.Equal(pipe, hose);
        }

        [Fact]
        public void NormalizeMotAdvisoryText_SynonymOnlyMerges_WhenRestOfTextAlsoMatches()
        {
            // same component (pipe/hose), but genuinely different defects - must NOT merge.
            var deteriorated = StaticHelper.NormalizeMotAdvisoryText("Brake hose slightly deteriorated (1.1.12 (b) (ii))");
            var corroded = StaticHelper.NormalizeMotAdvisoryText("Offside Rear Brake pipe excessively corroded flexi to flexi (1.1.11 (c))");
            Assert.NotEqual(deteriorated, corroded);
        }
    }
}
