# Threading Model

The renderer remains controlled by the main/render thread.

CPU-heavy tasks may run on worker threads.

```text
                       MAIN THREAD
                           |
              +------------+------------+
              |                         |
              v                         v
          World Update              Renderer
              |
              v
          Job Queue
              |
       +------+------+------+
       |      |      |      |
       v      v      v      v
     Job 1  Job 2  Job 3  Job N
       |      |      |      |
       +------+------+------+
              |
              v
        CPU-side Results
              |
              v
         Upload Queue
              |
              v
           Renderer
```

## Rules

Workers should avoid direct access to renderer-owned OpenGL resources unless explicitly supported.

Worker results need clear ownership and lifetime.

Threading should be introduced after profiling identifies CPU generation as a meaningful bottleneck.
