# Semantic Conventions for the ModernTelemetry system

## Build

```shell
docker run --rm \
    -v $(pwd)/Conventions:/output \
    -v $(pwd)/Model:/conventions \
    -v $(pwd)/templates:/templates \
    otel/weaver:latest \
    registry generate csharp \
    --registry=/conventions \
    --templates=/templates \
    /output/
```