# Chunk Lifecycle

```text
UNLOADED
   ↓
LOADING
   ↓
GENERATING
   ↓
UPLOADING
   ↓
ACTIVE
   ↓
UNLOADING
   ↓
UNLOADED
```

## State Responsibilities

**UNLOADED** — no active generated representation.

**LOADING** — source data is being located/read.

**GENERATING** — CPU procedural generation is occurring.

**UPLOADING** — CPU geometry is transferred into GPU resources.

**ACTIVE** — chunk is available to the renderer.

**UNLOADING** — resources are released.

Failure paths should return to a safe state and be observable through logs/metrics.
