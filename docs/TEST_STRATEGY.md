# Strategy

System under test: bundled ASP.NET Core Minimal API. Tests exercise serialization, endpoint routing, body parsing, validation and store mutation over real loopback HTTP. No request mocking and no external service dependency.

Each test starts and stops an isolated server with a new singleton store. This removes inter-test ordering and cleanup dependence. Risk selection covers invalid input, silent mutations, resource lifecycle, conflicts and query handling. Rejected writes are checked with subsequent reads in focused cases.

Limitations: the same project contains the demo and tests; successful results are not independent production quality evidence. Concurrency/load, auth, databases and deployment topology remain outside scope. Distinguish restore/build/network fixture errors from contract assertion failures. Synthetic HTTP transcripts are attached only on failed tests.
