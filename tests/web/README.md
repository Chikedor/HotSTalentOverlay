# Browser-client regression tests

Run the deterministic frontend tests with Node.js (no packages needed):

```powershell
node --test --test-isolation=none tests/web/status-polling.test.cjs
```

The legacy SSE integration test needs a running, isolated app instance. Do not use your live match instance for integration testing. Set its URL and run both suites:

```powershell
$env:OVERLAY_TEST_URL = 'http://127.0.0.1:3988'
node --test --test-isolation=none tests/web/status-polling.test.cjs tests/web/legacy-events.test.cjs
```

For browser verification, open four dashboard tabs in the same browser. Check that all talent images load in the dashboards and embedded previews, a fresh image URL loads immediately, reload completes, and status updates resume after restarting the isolated app.
