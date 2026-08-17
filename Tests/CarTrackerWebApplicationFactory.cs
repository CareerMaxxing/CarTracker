using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CarCareTracker.Tests
{
    /// <summary>
    /// StaticHelper.DbName/UserConfigPath/etc. are resolved relative to the process's current
    /// directory (via LiteDBHelper's `new LiteDatabase("data/cartracker.db")`), not
    /// IWebHostEnvironment.ContentRootPath - see docs/execution/DEFERRED.md's Phase 7 findings.
    /// This factory switches the process CWD to a fresh temp directory before the host is built, so
    /// tests never touch the real developer's data/ directory. All tests must share ONE instance of
    /// this factory (see CarTrackerTestCollection) since Directory.SetCurrentDirectory is
    /// process-wide, not per-test - running two instances concurrently would race.
    ///
    /// WebApplicationFactory's own content-root auto-detection assumes a "solution/ProjectName/
    /// ProjectName.csproj" layout and guesses wrong for this repo's flat layout (CarCareTracker.csproj
    /// lives directly in the repo root, not a subfolder of it). `dotnet test` (via VSTest) also
    /// starts the process with its CWD already set to the test assembly's own bin output folder, not
    /// wherever `dotnet test` was invoked from - so neither auto-detection nor
    /// Directory.GetCurrentDirectory() can be trusted here. ConfigureWebHost instead walks up from
    /// AppContext.BaseDirectory looking for CarCareTracker.csproj by name, the one path-finding
    /// approach that doesn't depend on either assumption.
    /// </summary>
    public class CarTrackerWebApplicationFactory : WebApplicationFactory<Program>, IDisposable
    {
        public string TempDataRoot { get; }
        private readonly string _originalCurrentDirectory;
        private readonly string _appContentRoot;

        public CarTrackerWebApplicationFactory()
        {
            _originalCurrentDirectory = Directory.GetCurrentDirectory();
            _appContentRoot = FindAppContentRoot();
            TempDataRoot = Path.Combine(Path.GetTempPath(), "CarTrackerTests_" + Guid.NewGuid());
            Directory.CreateDirectory(Path.Combine(TempDataRoot, "data"));
            Directory.SetCurrentDirectory(TempDataRoot);
        }

        private static string FindAppContentRoot()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null && !File.Exists(Path.Combine(dir.FullName, "CarCareTracker.csproj")))
            {
                dir = dir.Parent;
            }
            if (dir == null)
            {
                throw new DirectoryNotFoundException(
                    $"Could not find CarCareTracker.csproj by walking up from {AppContext.BaseDirectory} - " +
                    "the test project must stay under the main repo for this discovery to work.");
            }
            return dir.FullName;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseContentRoot(_appContentRoot);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            Directory.SetCurrentDirectory(_originalCurrentDirectory);
            try
            {
                Directory.Delete(TempDataRoot, true);
            }
            catch
            {
                //best-effort: LiteDBHelper doesn't implement IDisposable, so its LiteDatabase file
                //handle isn't guaranteed closed by the time base.Dispose(disposing) returns above,
                //which can make this delete fail with the file still locked. Not worth changing
                //production DI lifecycle just for test cleanup - a stray empty-ish folder in the OS
                //temp directory (never real user data) is a low-cost trade-off here.
            }
        }
    }

    [CollectionDefinition("CarTracker")]
    public class CarTrackerTestCollection : ICollectionFixture<CarTrackerWebApplicationFactory>
    {
        //marker class only - xUnit wires up the shared fixture from the ICollectionFixture<T> generic
        //argument. Every test class must use [Collection("CarTracker")] to share this one instance
        //and stay fully serialized (see class remarks above for why parallel instances aren't safe).
    }
}
