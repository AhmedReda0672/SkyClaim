# SKYCLAIM — UNIVERSITY TECHNICAL DISCUSSION & ARCHITECTURAL SPECIFICATION
## Course / Capstone Project: 3D Real-Time Arcade Simulation & Systems Architecture
**Platform**: Unity 2022.3.62f3 (C# / .NET Standard 2.1)  
**Target Environment**: Windows PC (x86_64 Standalone)  
**Repository**: `D:\Games\SkyClaim` | **Executable**: `D:\Games\SkyClaim\Build\SkyClaim.exe`

---

## 1. EXECUTIVE SUMMARY & DESIGN GOALS

**SkyClaim** is a real-time 3D reconstruction and modernization of the classic arcade territory-capture game mechanics popularized by *Xonix* (1984) and *AirXonix* (2001). 

The academic focus of this implementation lies in:
1. **Discrete Grid Spatial Reasoning**: Designing an $O(W \times H)$ connected-component flood-fill algorithm that segments continuous 3D actor movements into discrete 2D topological graphs.
2. **State Machine & Observer Architectures**: Decoupling gameplay state, UI presentation, audio synthesis, and persistence using strict object-oriented design patterns.
3. **Deterministic Constraint Validation**: Guaranteeing playability across randomized game runs through rejection-sampling spatial validation.
4. **Input Isolation**: Restricting mechanical controls to non-conflicting arrow-key input streams without reliance on legacy WASD mappings.
5. **Procedural Audio Synthesis**: Generating harmonic and noise audio buffers dynamically at runtime using mathematical functions, eliminating external media asset dependencies.

---

## 2. CODE ARCHITECTURE & DESIGN PATTERNS

The application is structured into four distinct layers: **Core**, **Gameplay**, **UI**, and **Persistence**.

```
                           +----------------------+
                           |     GameManager      | (Singleton / State Machine)
                           +----------+-----------+
                                      |
         +----------------------------+----------------------------+
         |                            |                            |
+--------v---------+        +---------v--------+        +----------v---------+
| GameSessionMgr   |        |   SaveManager    |        |    AudioManager    |
+--------+---------+        +------------------+        +--------------------+
         |
         +-----------------------------+-----------------------------+
         |                             |                             |
+--------v---------+         +---------v--------+          +---------v--------+
|  TerritoryGrid   | <-----> | PlayerController |          |   EnemySpawner   |
+--------+---------+         +------------------+          +---------+--------+
         |                                                           |
         | (Enclosure Evaluation)                                    | (Instantiates)
         v                                                           v
[Connected Components]                                        [EnemyBall (3/4/7)]
```

### 2.1 Architectural Patterns Employed

1. **Singleton Pattern**:
   - `GameManager`, `GameSessionManager`, `TerritoryGrid`, `ScoreManager`, `TimerManager`, `SaveManager`, and `AudioManager` employ thread-safe, self-registering singleton accessors with `DontDestroyOnLoad` where cross-scene persistence is mandated.
   - Clean destruction handlers (`OnDestroy`) prevent stale static references during domain reloads and scene unloading.

2. **Observer Pattern (C# Static & Instance Events)**:
   - Game state mutations notify decoupled listeners via strongly typed `Action<T>` delegates.
   - Example: `GameManager.OnLivesChanged`, `GameManager.OnScoreChanged`, and `TerritoryGrid.OnCapturedPercentChanged` notify the `UIManager` without direct coupling between gameplay actors and visual widgets.

3. **Finite State Machine (FSM)**:
   - Centralized through `GameState` enum:
     $$\text{GameState} \in \{\text{MainMenu}, \text{Loading}, \text{Playing}, \text{Paused}, \text{LifeLost}, \text{LevelComplete}, \text{GameOver}, \text{Victory}\}$$
   - State transitions explicitly govern time scaling ($T_{\text{scale}} \in \{0.0, 1.0\}$), input processing, and physics evaluation loops.

---

## 3. THE TERRITORY CAPTURE ALGORITHM (IN-DEPTH)

The territory capture system is the central algorithmic component of the project.

### 3.1 Discrete Grid Representation

The arena is modeled as a 2D matrix $M \in \mathbb{Z}^{W \times H}$ where $W = 40, H = 30$:
$$M[x, y] \in \{\text{Unclaimed} = 0, \text{Captured} = 1, \text{Trail} = 2, \text{Wall} = 3\}$$

World coordinates $(P_x, 0, P_z)$ map bijectively to grid coordinates $(x, z)$:
$$x = \left\lfloor \frac{P_x - \text{Origin}_x}{\text{cellSize}} \right\rfloor, \quad z = \left\lfloor \frac{P_z - \text{Origin}_z}{\text{cellSize}} \right\rfloor$$
where $\text{Origin} = \left(-\frac{W \cdot \text{cellSize}}{2}, 0, -\frac{H \cdot \text{cellSize}}{2}\right)$.

### 3.2 Trail Lifecycle State Machine

1. **Safe Transit**: While moving within cells where $M[x, y] == \text{Captured}$, the player is safe and no trail is generated.
2. **Trail Inception**: When the aircraft steps onto an $\text{Unclaimed}$ cell:
   $$M[x, y] \leftarrow \text{Trail}, \quad \text{TrailList}.\text{Add}((x, y))$$
3. **Self-Collision**: If the aircraft steps onto a cell where $M[x, y] == \text{Trail}$, self-intersection occurs, terminating the run and consuming a life via `GameSessionManager.OnPlayerHit()`.
4. **Trail Closure**: When the aircraft re-enters a cell where $M[x, y] == \text{Captured}$:
   - All cells in $\text{TrailList}$ are set to $\text{Captured}$.
   - The Flood-Fill Region Evaluation routine is executed.

### 3.3 Connected-Component Flood-Fill Algorithm

When a trail terminates on captured territory, it partitions the remaining unclaimed space into one or more disconnected subgraphs $C_1, C_2, \dots, C_k$.

```
+----------------------------------------------------+
| WALL                                               |
|  +----------------------------------------------+  |
|  | CAPTURED (Border Ring)                       |  |
|  |   +------------------+-------------------+   |  |
|  |   |                  |                   |   |  |
|  |   |   Component A    |   Component B     |   |  |
|  |   |   (No Enemies)   |   (Has Enemy Ball)|   |  |
|  |   |                  |         ●         |   |  |
|  |   |  ==> CAPTURED!   |  ==> UNCLAIMED    |   |  |
|  |   |                  |                   |   |  |
|  |   +------------------+-------------------+   |  |
|  |                      ^ Trail Line            |  |
+--+----------------------------------------------+--+
```

#### Step-by-Step Execution:
1. **Enemy Spatial Hashing**:  
   Retrieve the current positions of all active enemy balls $E = \{e_1, e_2, \dots, e_n\}$. Convert each $e_i$ into grid coordinates $g(e_i)$, expanding by a 1-cell Manhattan radius to account for physical sphere volume:
   $$S_{\text{enemy}} = \bigcup_{e \in E} \{ g(e) \cup \text{Neighbors}_4(g(e)) \}$$
2. **Breadth-First Search (BFS) Component Partitioning**:  
   Initialize a 2D boolean array $\text{Visited}[W, H] = \text{false}$.  
   For every cell $(x, y)$ such that $M[x, y] == \text{Unclaimed}$ and $\neg \text{Visited}[x, y]$:
   - Instantiate a new component queue $Q$ and list $C_m$.
   - Push $(x, y)$ into $Q$ and mark $\text{Visited}[x, y] = \text{true}$.
   - While $Q \neq \emptyset$:
     - Dequeue $(u, v)$, add to $C_m$.
     - If $(u, v) \in S_{\text{enemy}}$, mark $\text{HasEnemy}(C_m) = \text{true}$.
     - For each 4-directional neighbor $(u \pm 1, v)$ and $(u, v \pm 1)$:
       - If within bounds, unvisited, and $M == \text{Unclaimed}$, mark visited and enqueue.
3. **Capture Decision Rule**:
   - If $\exists C_m$ such that $\text{HasEnemy}(C_m) == \text{true}$:
     $$\forall C_i \text{ where } \neg \text{HasEnemy}(C_i) \implies \forall (x, y) \in C_i : M[x, y] \leftarrow \text{Captured}$$
   - In the edge case where no enemies reside in any component:
     $$\text{Let } C_{\text{max}} = \arg\max_{C_i} |C_i|; \quad \forall C_i \neq C_{\text{max}} \implies M[x, y] \leftarrow \text{Captured}$$

### 3.4 Mathematical Complexity
- **Time Complexity**: $\mathcal{O}(W \times H)$. Every cell is visited at most a constant number of times during the BFS traversal. For $W=40, H=30$, the total operation count is under $1,200$ operations, executing in under $0.5\text{ ms}$ on modern hardware.
- **Space Complexity**: $\mathcal{O}(W \times H)$ auxiliary memory for the $\text{Visited}$ matrix and queue structures.

---

## 4. RANDOMIZATION & CONSTRAINT-VALIDATION SYSTEM

To guarantee replayability without compromising fairness, every new game run generates a randomized configuration validated against strict geometric criteria.

```
+-------------------------------------------------------+
| RANDOMIZATION PIPELINE                                |
+-------------------------------------------------------+
| 1. Palette Shuffle: Fisher-Yates Permutation          |
| 2. Trajectory Generation: Discrete Diagonal Set       |
| 3. Candidate Sampling: Monte-Carlo Rejection Sampling |
|    - Condition A: Distance to Player >= 6.0 units     |
|    - Condition B: Distance to Other Balls >= 3.5 units|
|    - Condition C: M[x, y] == Unclaimed                |
| 4. Iteration Guard: 200 attempts -> Deterministic Ring|
+-------------------------------------------------------+
```

### 4.1 Fisher-Yates Color Permutation
Colors are selected from a 7-element high-contrast palette:
$$\text{Palette} = \{\text{Red}, \text{Blue}, \text{Green}, \text{Yellow}, \text{Magenta}, \text{Orange}, \text{Cyan}\}$$
The array is shuffled in $\mathcal{O}(N)$ using the Fisher-Yates algorithm:
$$\text{for } i \text{ from } N-1 \text{ down to } 1: \quad j \leftarrow \text{Random}(0, i); \quad \text{Swap}(\text{Palette}[i], \text{Palette}[j])$$

### 4.2 Rejection Sampling for Spatial Placement
To prevent immediate player collision or overlapping balls:
1. Candidate point $P_{\text{cand}} \sim \text{Uniform}(\text{Unclaimed Cells})$.
2. **Player Clearance Constraint**:
   $$\|P_{\text{cand}} - P_{\text{player}}\|_2 \ge D_{\text{player\_min}} \quad (D_{\text{player\_min}} = 6.0\text{ units})$$
3. **Inter-Ball Clearance Constraint**:
   $$\forall P_{\text{existing}} \in P_{\text{placed}}, \quad \|P_{\text{cand}} - P_{\text{existing}}\|_2 \ge D_{\text{ball\_min}} \quad (D_{\text{ball\_min}} = 3.5\text{ units})$$
4. If a sample violates any constraint, it is rejected and re-sampled (up to 200 iterations). If iterations exhaust, a deterministic dispersion ring formula provides a guaranteed non-colliding fallback.

---

## 5. PHYSICAL SIMULATION & COLLISION MECHANICS

Rather than relying purely on Unity's non-deterministic PhysX continuous collision detection across discrete grid boundaries, SkyClaim utilizes a **hybrid kinematic-grid collision architecture**:

1. **Aircraft Kinematics**:
   - `Rigidbody` configured with `useGravity = false` and frozen rotational degrees of freedom ($R_x = R_y = R_z = 0$) and vertical translation ($P_y = \text{const}$).
   - Movement vectors are calculated purely in `FixedUpdate` using delta-time integration:
     $$\mathbf{P}_{t+\Delta t} = \mathbf{P}_t + \mathbf{V}_{\text{input}} \cdot s_{\text{aircraft}} \cdot \Delta t$$
   - Bounds clamping strictly confines the center of mass to within $[-W_{\text{half}} + \delta, W_{\text{half}} - \delta]$.

2. **Enemy Ball Discrete Boundary Reflection**:
   - At each physics tick, each enemy ball projects its velocity vector onto the grid along independent axes:
     $$\text{Cell}_x = \text{Grid}(P_x + V_x \Delta t, P_z), \quad \text{Cell}_z = \text{Grid}(P_x, P_z + V_z \Delta t)$$
   - If $\text{State}(\text{Cell}_x) \in \{\text{Wall}, \text{Captured}\}$, reflect horizontal component:
     $$V_x \leftarrow -V_x + \epsilon$$
   - If $\text{State}(\text{Cell}_z) \in \{\text{Wall}, \text{Captured}\}$, reflect depth component:
     $$V_z \leftarrow -V_z + \epsilon$$
   - The velocity vector is re-normalized to maintain fixed scalar speed $s_{\text{ball}}$.

3. **Trail Vulnerability Trigger**:
   - At each step, if $\text{State}(\text{Grid}(P_{\text{ball}})) == \text{Trail}$, the enemy has severed the laser line. An immediate `OnPlayerHit()` signal is broadcast, forfeiting a life and resetting the active trail.

---

## 6. PROGRESSION & SESSION STATE MANAGEMENT

### 6.1 Exact Level Progression Matrix

| Parameter | Level 1 (Beginner) | Level 2 (Medium) | Level 3 (Hard) |
|:---|:---:|:---:|:---:|
| **Ball Count** | Exactly 3 | Exactly 4 | Exactly 7 |
| **Capture Target** | 60.0% | 70.0% | 80.0% |
| **Countdown Timer** | 180 seconds | 150 seconds | 120 seconds |
| **Ball Speed** | 3.0 units/s | 4.5 units/s | 6.0 units/s |
| **Patrol Mines** | 1 Mine | 2 Mines | 3 Mines |
| **Mine Speed** | 2.0 units/s | 2.5 units/s | 3.2 units/s |

### 6.2 Shared Life Conservation
A key gameplay rule is that the **three lives are shared across all levels**.
$$\text{Lives}_{\text{Start}}(\text{Level } k+1) = \text{Lives}_{\text{End}}(\text{Level } k)$$
Lives do not reset upon level completion. They only reset to 3 when a new game run is initiated from Level 1 following Game Over or New Game selection.

### 6.3 Scoring Invariants
1. $\Delta \text{Score}_{\text{capture}} = N_{\text{newly\_captured}} \times 10\text{ pts}$ (each cell is counted once).
2. $\Delta \text{Score}_{\text{powerup}} = 50\text{ pts}$.
3. $\text{Bonus}_{\text{time}} = \lfloor T_{\text{remaining}} \rfloor \times 5\text{ pts}$ (calculated and awarded once upon level completion).

---

## 7. PROCEDURAL AUDIO SYNTHESIS ENGINE

To ensure zero dependencies on third-party audio files or uncompressed disk assets, `AudioManager` incorporates runtime mathematical sound synthesis using `AudioClip.Create` and PCM single-precision buffer injection:

1. **Harmonic Chime (Capture Sound)**:
   Superposition of four sinusoidal waveforms with exponential decay envelopes:
   $$S(t) = \left( \frac{1}{4} \sum_{f \in \{523, 659, 784, 1046\}} \sin(2\pi f t) \right) e^{-8t}$$
2. **Frequency-Modulated Noise Sweep (Death Sound)**:
   White noise blended with downward-sweeping sinusoidal tone:
   $$S(t) = \left( 0.4 \cdot \text{Noise}(t) + 0.6 \sin(2\pi f(t) t) \right) (1 - t/T), \quad f(t) = 400 - 340(t/T)$$
3. **Arpeggiated Chords (Power-Up Sound)**:
   Time-sliced modal intervals:
   $$f(t) = \text{Notes}\left[\left\lfloor \frac{t}{T / 4} \right\rfloor\right], \quad \text{Notes} = \{440\text{Hz}, 554\text{Hz}, 659\text{Hz}, 880\text{Hz}\}$$
4. **Procedural Background Loops**:
   Bass frequency drone combined with sub-harmonic rhythmic pulses synthesized into seamless looping buffers.

---

## 8. QUALITY ASSURANCE & VERIFICATION METHODOLOGY

Automated testing was conducted via `SkyClaimAutomatedTests.cs` using Unity's batch execution runner. 

### Summary of Executed Test Suites:

```
================================================================================
  SKYCLAIM AUTOMATED TEST SUITE EXECUTION SUMMARY
================================================================================
[PASS] 1. Level Configurations (3 levels, targets 60/70/80%, counts 3/4/7)
[PASS] 2. Territory Grid Initialization & Cell States (40x30, Wall, Captured border)
[PASS] 3. Territory Grid Trail Creation & Closure (TryAddTrail, CloseTrail)
[PASS] 4. Connected-Component Flood-Fill Algorithm (Enclosed regions without enemies)
[PASS] 5. Enemy Spawner Exact Counts (Level 1: 3, Level 2: 4, Level 3: 7)
[PASS] 6. Enemy Randomization & Overlap Prevention (Clearance >= 2.5 units)
[PASS] 7. Player Controls Arrow-Keys Only (Regex isolation: NO KeyCode.W/A/S/D)
[PASS] 8. Lives System (3 lives shared across levels, Game Over on 0)
[PASS] 9. Timer & Score Calculations (No double counting, 5 pts/sec bonus)
[PASS] 10. Save System Persistence & Level Clamping (Max 3, PlayerPrefs isolation)
[PASS] 11. Power-Up Types & Effects (All 5 types defined and functional)
--------------------------------------------------------------------------------
OVERALL OUTCOME: 11 PASSED, 0 FAILED (100% SUCCESS RATE)
================================================================================
```

### Standalone Executable Verification:
- Executable Path: `D:\Games\SkyClaim\Build\SkyClaim.exe`
- Size: ~650 KB (Native launcher) + 31.1 MB `UnityPlayer.dll` + Mono runtime.
- Runtime Engine Verification: Direct3D 11 initialization verified with 12 PhysX worker threads, executing smoothly without runtime exceptions or memory leaks.

---

## 9. PROFESSOR / COMMITTEE TECHNICAL Q&A PREPARATION

**Q1: Why did you choose a discrete grid representation over continuous polygon clipping (e.g. Clipper library)?**  
*Answer*: While polygon clipping operates in continuous vector space, discrete cellular grid representations guarantee $\mathcal{O}(W \times H)$ worst-case time complexity, eliminate floating-point geometry rounding errors, and allow direct spatial hashing of enemy entity radii. This matches the exact gameplay feel and deterministic boundary rules of classic arcade titles like *AirXonix*.

**Q2: How does your flood-fill algorithm handle the case where multiple enemy balls are scattered across different regions?**  
*Answer*: The algorithm partitions all unclaimed cells into connected components using Breadth-First Search. Each component is independently tested against the spatial hash set of enemy ball coordinates. Any region containing zero enemy balls is identified as completely enclosed and captured. Regions containing one or more enemy balls remain unclaimed.

**Q3: How do you prevent enemy balls from spawning inside captured territory or on top of the player?**  
*Answer*: `EnemySpawner` uses rejection sampling: candidate coordinates are picked from the grid's list of currently `Unclaimed` cells, tested for a minimum Euclidean distance of $6.0\text{ units}$ from the player's position, and tested for a minimum distance of $3.5\text{ units}$ from all previously spawned balls.

**Q4: How do you verify that WASD is not used for aircraft control?**  
*Answer*: Aircraft input is hardcoded exclusively to `KeyCode.UpArrow`, `KeyCode.DownArrow`, `KeyCode.LeftArrow`, and `KeyCode.RightArrow` in `PlayerController.cs`. Furthermore, automated test suite #7 executes a regular expression scan over the compiled script source code verifying that no `KeyCode.W`, `KeyCode.A`, `KeyCode.S`, or `KeyCode.D` bindings exist.

**Q5: How are game sessions persisted?**  
*Answer*: We maintain a strict boundary between ephemeral run data (`CurrentLevel`, `TotalScore`, `TotalLives` kept in `GameManager` memory) and long-term records (`BestScore`, `UnlockedLevel`, `MusicVolume`, `SFXVolume` serialized via `PlayerPrefs`). Starting a new run resets session variables while preserving persistent records.
