# Aspire.Qdrant.Client.Tests

This project contains tests for the Aspire.Qdrant.Client project.

When running tests locally until TestContainers support is enabled, you will need to have a Qdrant instance running locally.

Run:

```bash
docker run -p 6333:6333 -p 6334:6334 qdrant/qdrant:v1.8.0
```

Then run the tests to enable the connected tests.
