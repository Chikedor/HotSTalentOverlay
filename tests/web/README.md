# Browser-client regression tests

Run the deterministic frontend tests with Node.js (no packages needed):

```powershell
node --test --test-isolation=none tests/web/*.test.cjs
```

The legacy SSE and read-only preview API integration tests need a running, isolated app instance with a populated talent catalog. Do not use your live match instance for integration testing. Set its URL and run all suites:

```powershell
$env:OVERLAY_TEST_URL = 'http://127.0.0.1:3988'
$env:OVERLAY_TEST_VERSION = '1.4.6'
node --test --test-isolation=none tests/web/*.test.cjs
```

For browser verification, open four dashboard tabs in the same browser. Check that all talent images load in the dashboards and embedded previews, a fresh image URL loads immediately, reload completes, and status updates resume after restarting the isolated app.

Lifecycle tests execute the production overlay script with minimal browser primitives. They cover live preview without fake talents, match-end fade, no animation, independent duration, interrupted transitions, exit preview restoration, style edits during preview, and status updates without restarting the exit countdown. In the isolated dashboard also check all four exit directions, disabled controls for None, both languages, and hidden empty slots after clearing a demo match.

Typography checks: enable talent names and verify long names such as “Flechas de absorción” wrap without clipping. Compare each label's `scrollWidth <= clientWidth` and `scrollHeight <= clientHeight` in the real browser, including small icons, all six fonts, large text, normal/bold, line spacing and no shadow. Icon positions and canvas dimensions must remain unchanged. Verify the saved typography after reloading and toggling names off/on. The installed-version integration test checks local status without contacting GitHub; `OVERLAY_TEST_VERSION` optionally checks the exact packaged version.
