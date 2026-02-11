# 🎰 Super Spinner – Unity Technical Assignment

A polished casino-style spinner game built in **Unity 6 (6000.0.61f1)**, implementing reactive networking, smooth animations, advanced VFX, and production-ready architecture.

The project follows clean code principles with UniRx for async flow, DOTween for high-quality animations, and custom Shader Graph effects.

---

## ✨ Key Features

### 🎯 Core Gameplay
- Fetches spinner values from remote API  
- Random spin result via network request  
- Infinite reel illusion using triple-buffer layout  
- Tap-to-spin interaction with full state management  
- Error handling with retry & timeout logic

### 🧠 Architecture
- **UniRx** reactive networking  
- Clean separation:
  - `SpinnerBootstrap` – initialization  
  - `SpinnerFlow` – game logic  
  - `SpinnerView` – UI & reel rendering  
  - Services layer for API  
- Disposable management & memory safety

### 🎨 Visual & Animation System

#### Reel & UI
- Smooth infinite reel scrolling  
- Center highlight scaling & alpha focus  
- Idle ambience animations  
- Adaptive layouts

#### Spin Experience
- Multi-phase spin:
  - Fast spin  
  - Slow-motion finish  
  - Micro-settle  
- Audio ticks synced with reel center  
- Pointer animations with particles

#### Win Presentation
- Tiered win system:
  - Small Win  
  - Big Win  
  - Mega Win  
- Count-up amount animation  
- Glow & flash impact  
- Coin burst particles  
- Tap-to-continue flow

#### Additional VFX (Assignment Boost 🚀)
- Custom **Shader Graph Shockwave**
  - Animated radial ring  
  - Additive glow  
  - Controlled via material properties  
  - Active during Spin & Win screens  
- Pointer particles & trails  
- Idle sparkles & ambience  
- Outer ring rotation

---

## 🛠 Tech Stack

- **Unity 6 – URP**
- **UniRx** – Reactive async
- **DOTween** – Sequenced animations
- **TextMeshPro**
- **Shader Graph**
- Custom VFX & Particles
- Git version control

---

## 📁 Project Structure
SuperSpinner
├── Core
│ ├── SpinnerBootstrap.cs
│ └── SpinnerFlow.cs
│
├── UI
│ ├── SpinnerView.cs
│ ├── SpinnerUiRefs.cs
│ └── SpinnerErrorUi.cs
│
├── Networking
│ └── SpinnerApiService.cs
│
├── VFX
│ ├── Shockwave Shader Graph
│ ├── Pointer Particles
│ └── Win Effects
│
└── Audio
└── SpinnerAudio.cs


---

## 🎮 How It Works

1. App loads spinner values from API  
2. User taps **Tap to Spin**  
3. Flow:
   - Zoom in  
   - Fast spin  
   - Slow motion finish  
   - Result lock  
4. Win presentation:
   - Count-up  
   - Glow + flash  
   - Particles  
   - Tap to continue → next round

---

## ⚙ Setup

1. Open in **Unity 6000.0.61f1**  
2. Install packages:
   - DOTween  
   - UniRx  
3. Open scene: `Scenes/Main.unity`  
4. Press Play 🎉

---

## 📌 Highlights for Evaluation

- Clean reactive architecture  
- Professional feel & juice  
- Extra shader/VFX beyond requirements  
- Robust error handling  
- Modular & scalable codebase

---

## 👤 Author

**Antonis**  
Unity Developer  
Technical Assignment – AbZorba Games

---

> Built with passion for game feel & polish 💛

