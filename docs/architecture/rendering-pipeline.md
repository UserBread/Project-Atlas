# Rendering Pipeline

```text
World
  ↓
Visible Chunks
  ↓
LOD Selection
  ↓
Frustum / Distance Culling
  ↓
Render Queue
  ↓
Batch / Sort
  ↓
OpenGL
  ↓
Framebuffer
```

## Responsibilities

### World
Determines active/available chunks.

### LOD
Determines representation detail.

### Culling
Removes work that does not need rendering.

### Render Queue
Collects visible renderable work.

### Batch / Sort
Attempts to reduce state changes and draw-call overhead.

### OpenGL
Executes GPU rendering commands.
