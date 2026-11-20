# SKYCLAIM — 3D AirXonix-Inspired Arcade Game

**SkyClaim** is a complete, polished, fully-playable 3D arcade territory-capture game inspired by the classic PC hit *AirXonix*. Developed in **Unity 2022.3.62f3** for Windows PC as a university graduation/capstone demonstration project.

---

## 1. GAME CONCEPT & OBJECTIVE

In **SkyClaim**, the player pilots a futuristic aircraft above a floating grid arena suspended in the clouds. 
The objective is to capture unclaimed territory by flying outside the safe zone, drawing a laser trail, and returning to safe territory to claim enclosed regions. While drawing, the player is vulnerable: enemy balls bounce across unclaimed territory, and if a ball intersects the player's active trail or touches the aircraft, a life is lost. Patrol mines roam captured territory to keep safe zones active and challenging.

The game is won by reaching the required captured territory percentage across three progressively challenging levels.

---

## 2. AIRCRAFT CONTROLS (STRICT REQUIREMENT)

Aircraft flight is strictly controlled using the **FOUR ARROW KEYS ONLY**:

| Key | Movement Action |
|:---:|:---|
| **UP ARROW** | Move Forward (+Z) |
| **DOWN ARROW** | Move Backward (-Z) |
| **LEFT ARROW** | Move Left (-X) |
| **RIGHT ARROW** | Move Right (+X) |

* **WASD keys are strictly disabled** for aircraft movement.
* **Escape** or **P**: Pause / Resume the game.
* **Mouse**: Interactive menu navigation and button selection.

---

## 3. EXACT THREE-LEVEL CONFIGURATION

SkyClaim features exactly three handcrafted levels with fixed enemy ball configurations:

| Level | Difficulty | Enemy Balls | Target Captured % | Time Limit | Enemy Speed | Patrol Mines |
|:---:|:---:|:---:|:---:|:---:|:---:|:---:|
| **Level 1** | Beginner | **3 balls** | **60%** | 180 seconds | 3.0 units/s | 1 Mine |
| **Level 2** | Medium | **4 balls** | **70%** | 150 seconds | 4.5 units/s | 2 Mines |
| **Level 3** | Hard | **7 balls** | **80%** | 120 seconds | 6.0 units/s | 3 Mines |

* The number of enemy balls is **strictly fixed** and never changes randomly.
* Randomization applies exclusively to enemy spawn coordinates, distinct color assignments, and initial 8-way bounce vectors.

---

## 4. TERRITORY DRAWING & CAPTURE SYSTEM

The arena consists of a discrete 40×30 grid of cells categorized by state:
- `Wall`: Outer impenetrable arena boundaries.
- `Captured`: Safe player territory (starting outer ring and captured sectors).
- `Unclaimed`: Open territory inhabited by bouncing enemy balls.
- `Trail`: Active path drawn by the aircraft while outside safe territory.

### The Capture Algorithm
1. When the player leaves captured territory, moving into unclaimed cells creates `Trail` cells.
2. Returning to a `Captured` cell closes the trail and initiates the capture sequence.
3. Trail cells immediately convert to `Captured`.
4. A connected-component search partitions all remaining unclaimed cells into discrete 4-connected regions.
5. The algorithm cross-references active enemy ball positions against each region:
   - **Regions without enemy balls** are fully enclosed and become **Captured**.
   - **Regions containing enemy balls** remain **Unclaimed**.
   - If no enemies exist (or all are enclosed), all regions except the largest are captured.
6. The actual captured cell count is updated, visual meshes refresh, and score is awarded.
7. **The captured percentage is strictly calculated from the real grid state** (`capturedCells / totalPlayableCells * 100`) — never simulated or artificially incremented.

---

## 5. ENEMY BALLS & HAZARDS

### Enemy Balls
- Rendered as vibrant 3D spheres with dynamic trailing particles.
- Reflect their velocity vector when striking arena boundaries or captured territory edges.
- Each ball receives an independently randomized vibrant color from a 7-color palette (Red, Blue, Green, Yellow, Purple, Orange, Cyan) and a randomized diagonal trajectory.
- Intersecting the active player trail or colliding directly with the aircraft immediately destroys the trail and costs the player a life.

### Patrol Mines
- Recognizable rotating hazard drones that patrol exclusively inside **Captured territory**.
- Bounce off unclaimed territory edges and outer walls.
- Touching a mine triggers a localized explosion and costs a life.

---

## 6. POWER-UPS & BONUSES

Floating 3D power-up orbs spawn periodically in unclaimed territory:
1. **Extra Life (Crimson)**: Grants +1 life (clamped to a maximum of 5).
2. **Extra Time (Cyan)**: Adds +30 seconds to the level timer.
3. **Enemy Slow (Gold)**: Reduces all enemy ball speeds by 55% for 6 seconds.
4. **Speed Boost (Emerald)**: Increases aircraft speed from 5.0 to 9.0 units/s for 6 seconds.
5. **Score Bonus (Purple)**: Awards +250 bonus points immediately.

---

## 7. LIVES, TIMER, SCORE & PROGRESSION

- **Shared Lives**: The player begins a game run with **3 lives**, which persist across all three levels. Completing Level 1 with 2 lives remaining means continuing into Level 2 with 2 lives.
- **Timer**: A countdown timer runs for each level. If time runs out, a life is lost.
- **Scoring**:
  - +10 points per newly captured grid cell.
  - +50 points per collected power-up.
  - +5 points per second remaining as a Level Completion Bonus.
- **Game Over**: When lives reach 0, Game Over is triggered. Starting a New Game resets to Level 1 with 3 lives and fresh randomized ball placements, while preserving persistent best scores.
- **Victory**: Achieving 80% territory capture on Level 3 triggers the Final Victory celebration screen with overall game statistics.

---

## 8. CODE ARCHITECTURE

The codebase is organized modularly under `Assets/Scripts/`:
- `Core/GameManager.cs`: Master singleton managing game states (`MainMenu`, `Playing`, `Paused`, `LifeLost`, `LevelComplete`, `GameOver`, `Victory`), level configs, and run stats.
- `Core/GameSessionManager.cs`: Coordinates level initialization, enemy/mine/power-up spawning, win/loss evaluation, and player respawning.
- `Core/ScoreManager.cs`: Handles point calculation and time bonuses without double-counting.
- `Core/TimerManager.cs`: Countdown timer with pause/resume and time extension support.
- `Core/SceneBootstrapper.cs`: Guarantees persistent manager availability across all scene loads.
- `Core/GameStateListener.cs`: Listens to state transitions to manage HUD and background music.
- `Gameplay/TerritoryGrid.cs`: Central 40×30 grid system, coordinate transforms, trail state machine, and connected-component flood-fill capture algorithm.
- `Gameplay/PlayerController.cs`: Aircraft movement strictly bound to arrow keys, propeller rotation, and boundary clamping.
- `Gameplay/EnemyBall.cs`: Bouncing physics, territory boundary reflection, trail collision detection, and slow effect routines.
- `Gameplay/EnemySpawner.cs`: Enforces exact enemy counts (3, 4, 7), color shuffling, and 150+ iteration overlap-prevention spawn validation.
- `Gameplay/MineController.cs` & `MineSpawner.cs`: Patrol mine movement within captured regions.
- `Gameplay/PowerUp.cs` & `PowerUpManager.cs`: Power-up collection, animations, and periodic spawn scheduler.
- `Audio/AudioManager.cs`: Audio management with procedural waveform synthesis (beeps, chimes, arpeggios, noise sweeps, fanfare, and ambient electronic music tracks).
- `Save/SaveManager.cs`: Persistent storage via PlayerPrefs for best score, unlocked levels (1–3), and audio volume preferences.
- `UI/UIManager.cs`: In-game HUD, progress slider, power-up banners, and overlay menus (Pause, Level Complete, Game Over, Victory).
- `UI/MainMenuController.cs`: Main menu, level selection with unlock gates, and audio sliders.
- `UI/CameraController.cs`: Elevated top-down isometric camera framing the arena.

---

## 9. HOW TO RUN THE GAME

### Running the Standalone Windows Build
The final standalone Windows 64-bit build is located at:
```
D:\Games\SkyClaim\Build\SkyClaim.exe
```
Simply double-click `SkyClaim.exe` to launch the game.

### Opening in Unity Editor
1. Launch Unity Hub.
2. Add project from disk: `D:\Games\SkyClaim`.
3. Open with Unity **2022.3.62f3**.
4. Load `Assets/Scenes/MainMenu.unity` and click **Play**.

---

## 10. VERIFICATION & AUTOMATED TESTING

An automated test suite is located in `Assets/Editor/SkyClaimAutomatedTests.cs`.
All **11 test suites** execute automatically via Unity batch mode:
```
D:\unity\Editor\Unity.exe -batchmode -nographics -quit -projectPath "D:\Games\SkyClaim" -executeMethod SkyClaimAutomatedTests.RunAllTests -logFile "D:\Games\SkyClaim\test_run.log"
```
**Results: 11 Passed, 0 Failed.** See `D:\Games\SkyClaim\TestResults.txt`.
