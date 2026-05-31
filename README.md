# MELL CLIMB RACING - Step-by-Step Development Roadmap

---

## Core Car Physics (The Foundation)
**Goal:** Get a simple box (placeholder car) driving over a basic flat terrain.

### Tasks
- [x] Create the Unity 2D project.
- [x] Set up a simple 2D car using `Rigidbody2D` and `WheelJoint2D`.
- [x] Write the basic `CarController` script (Acceleration, Braking).
- [x] Implement mid-air rotation (so the player can balance the car while jumping).

---

## Terrain & Environment
**Goal:** Create hills for the car to climb and interact with.

### Tasks
- [x] Introduce Unity's **SpriteShape**.
- [x] Configure the ground physics materials so the car tires actually grip the hills.
- [x] Set up a camera script that smoothly follows the car.

---

## Upgrades & Physics Logic
**Goal:** Make the car feel different when upgraded.

### Tasks
- [x] Wire the `CarController` script to accept variable stats:
  - **Engine:** Torque
  - **Tires:** Grip / Friction
  - **Suspension:** Spring stiffness
  - **Fuel:** Capacity
- [x] Test different preset values to ensure upgrades feel impactful.

---

## The Core Game Loop (Fuel, Pickups, & Death)
**Goal:** Implement the actual gameplay rules.

### Tasks
- [x] Create the **Fuel depletion** logic.
- [x] Create prefabs for **Coins** and **Fuel Cans**.
- [x] Write the collection scripts (triggers for picking up items).
- [x] Implement **Game Over** conditions:
  - Running out of fuel.
  - Driver's head hitting the ground (crash).
- [x] Set up a basic distance tracker.

---

## The "Double Mellstroy" Meme System (The USP)
**Goal:** Integrate the Mellstroy memes into the gameplay.

### Tasks
- [ ] **The Driver (1:1 Video/GIF):** Attach a UI/Sprite player to the car seat that stays upright. Connect it to the game state (*Idle, Speeding, Flipping*).
- [ ] **The Streamer (16:9 Corner Cam):** Create a UI canvas overlay.
- [ ] **The Event Manager:** Write a script that detects events (*Airtime, Crash, Low Fuel, Coin streak*) and tells the Streamer and Driver which meme video/sprite to play.

---

## UI, Garage & Menus
**Goal:** Build the menus to navigate the game and upgrade cars.

### Tasks
- [ ] Create the **HUD (Heads Up Display)** showing Fuel Bar, Distance, and Coins.
- [ ] Build the **Main Menu**.
- [ ] Build the **Garage UI** using a horizontal scrolling card layout.
- [ ] Implement the save system (`PlayerPrefs`) so coins, unlocked cars, and upgrades persist when the game is closed.

---

## Content Expansion
**Goal:** Add more of everything.

### Tasks
- [ ] Add new vehicles with different base stats and hitboxes.
- [ ] Create multiple maps using **SpriteShape** (*e.g., Desert, Moon, Forest*) with custom gravity or grip settings.
- [ ] Import all final **Nano Banana** graphics and replace placeholders.

---

## Audio & Final Polish
**Goal:** Make the game sound good and feel "Play Store ready."

### Tasks
- [ ] Add looping **Engine sounds** (pitch changes dynamically based on vehicle speed).
- [ ] Add UI click sounds, coin pickup sounds, and Mellstroy meme audio clips.
- [ ] Performance optimization (ensuring stable framerates on older mobile devices).
- [ ] Build and export the final **Android APK/AAB** file.
