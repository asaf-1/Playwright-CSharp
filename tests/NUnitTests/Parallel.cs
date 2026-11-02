// NUnit runs the tests 4 at a time, on every run: this PC and CI alike.
//
// Safe because nothing is shared between tests: each test gets its own instance
// of its class (InstancePerTestCase), its own browser context, and its own new
// customer. The demo accounts are only signed in to, never changed, and the
// login hold is per email, so a test's wrong passwords can't lock another out.
[assembly: Parallelizable(ParallelScope.All)]
[assembly: FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
[assembly: LevelOfParallelism(4)]
