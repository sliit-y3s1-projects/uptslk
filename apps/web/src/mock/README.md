# Mock UI data

These exports are intentionally UI-only and contain no API calls. Pages import a named collection from `mock-data.ts`; when backend endpoints are ready, replace each collection with a feature repository that returns the same shape. Keep transport-specific sample records consistent across features (for example, a vehicle's maintenance state should also affect dispatch and incidents).
