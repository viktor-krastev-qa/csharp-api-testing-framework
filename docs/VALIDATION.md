# Validation status

- Linux build environment, .NET SDK 9.0.318: 39 passed, 0 failed, 0 skipped.
- Actual test requests traversed Kestrel over loopback HTTP.
- Build/restore completed; TRX converted to HTML and saved under examples/build/.
- Local Windows execution with .NET 9: 39 passed, 0 failed, 0 skipped.
- Actual Windows TRX and HTML reports are saved under examples/local/.
- GitHub Actions: pending first repository run.

The local API and tests were authored together; these results do not establish independent production quality. Update Windows/CI status only after observing actual runs.
