# Level 2 — Office Prop Catalog & Movement Kinematics (Phase 1)

## 1. Player Movement & Jump Numbers (from `RigidbodyFpsController.cs`)
- **Walk Speed:** `9.0 m/s`
- **Sprint Speed:** `15.0 m/s`
- **Jump Vertical Velocity (`jumpSpeed`):** `9.0 m/s`
- **Gravity:** `27.0 m/s^2`
- **Coyote Time:** `0.10 s` | **Jump Buffer:** `0.12 s`
- **Air Control:** `airAcceleration = 32 m/s^2`, `airControlSpeed = 5 m/s`, `airSpeedLimit = 27 m/s`
- **Wall-Run:** `minWallrunSpeed = 8 m/s`, `wallrunSpeed = 18 m/s`, `wallrunDuration = 1.25 s`, `wallrunGravity = 5 m/s^2`, `wallJumpUp = 8.5 m/s`, `wallJumpAway = 8.0 m/s`
- **Shotgun Blast Jump:** `blastForce = 18 m/s`, `blastMaxDistance = 4.0 m`, `blastHorizontalMultiplier = 0.6`, `blastDecay = 12 m/s^2`

### Computed Parkour Limits
- **Max Vertical Rise (Standard Jump):** `H_max = v_y^2 / (2 * g) = 9^2 / (2 * 27) = 1.50 m` (70% Phase 2 design cap = **`1.05 m`**)
- **Total Jump Flight Time (Level-to-Level):** `T = 2 * (9 / 27) = 0.667 s`
- **Max Horizontal Jump Gap While Sprinting:** `D_max = 15.0 * 0.667 = 10.00 m` ballistic (up to `11.50 m` with `0.10 s` coyote time; 60% Phase 2 design cap = **`6.00 m`**)
- **Max Single Wall-Run Distance:** `18.0 m/s * 1.25 s = 22.50 m`

## 2. Complete Office Prop Table (180 Models in `Assets/OFFICE/`)
> **Note on Colliders & Materials:** Raw FBX files ship with `addColliders: 0` (no built-in collider) and embedded FBX materials mapped to `Assets/OFFICE/textures/`. `OfficePropColliderAndMaterialFixer` automatically fits a flat, hole-free unit-scale `BoxCollider` (tilt `0 deg`) to every walkable/wall-run/cover prop and assigns URP materials preserving each prop's texture.

| # | Folder | Prop Name | Size (X x Y x Z m) | Raw FBX Collider | Flat Usable Top (Top Height) | Material OK in URP | Classification |
|---:|---|---|---|---|---|---|---|
| 1 | `book_things` | `book_case.fbx` | 2 x 2.5 x 0.49 | No (fitted via utility) | Vertical Wall (2.50 m H) | Yes (URP Lit / Toon) | **WALL_RUN** |
| 2 | `book_things` | `book_case_small.fbx` | 1 x 2.5 x 0.49 | No (fitted via utility) | Yes / Cover (2.50 m) | Yes (URP Lit / Toon) | **COVER** |
| 3 | `book_things` | `book_case_with_back.fbx` | 2.03 x 2.5 x 0.5 | No (fitted via utility) | Vertical Wall (2.50 m H) | Yes (URP Lit / Toon) | **WALL_RUN** |
| 4 | `book_things` | `book_case_with_back_Small.fbx` | 1.01 x 2.5 x 0.5 | No (fitted via utility) | Yes / Cover (2.50 m) | Yes (URP Lit / Toon) | **COVER** |
| 5 | `book_things` | `books.fbx` | 0.75 x 0.36 x 0.23 | No (fitted via utility) | No (Decor) | Yes (URP Lit / Toon) | **DECOR_FLOATING** |
| 6 | `book_things` | `books_alt.fbx` | 0.39 x 0.36 x 0.23 | No (fitted via utility) | No (Decor) | Yes (URP Lit / Toon) | **DECOR_FLOATING** |
| 7 | `book_things` | `books_alt2.fbx` | 0.53 x 0.4 x 0.23 | No (fitted via utility) | No (Decor) | Yes (URP Lit / Toon) | **DECOR_FLOATING** |
| 8 | `book_things` | `file_binder.fbx` | 0.09 x 0.42 x 0.3 | No (fitted via utility) | No (Decor) | Yes (URP Lit / Toon) | **DECOR_FLOATING** |
| 9 | `book_things` | `file_binder_alt.fbx` | 0.09 x 0.42 x 0.3 | No (fitted via utility) | No (Decor) | Yes (URP Lit / Toon) | **DECOR_FLOATING** |
| 10 | `book_things` | `file_binder_alt2.fbx` | 0.09 x 0.42 x 0.3 | No (fitted via utility) | No (Decor) | Yes (URP Lit / Toon) | **DECOR_FLOATING** |
| 11 | `book_things` | `file_binder_alt3.fbx` | 0.09 x 0.42 x 0.3 | No (fitted via utility) | No (Decor) | Yes (URP Lit / Toon) | **DECOR_FLOATING** |
| 12 | `book_things` | `file_binder_alt4.fbx` | 0.09 x 0.42 x 0.3 | No (fitted via utility) | No (Decor) | Yes (URP Lit / Toon) | **DECOR_FLOATING** |
| 13 | `book_things` | `file_binder_alt5.fbx` | 0.09 x 0.42 x 0.3 | No (fitted via utility) | No (Decor) | Yes (URP Lit / Toon) | **DECOR_FLOATING** |
| 14 | `carpet_floor` | `floor 2x1.fbx` | 1 x 0 x 2 | No (fitted via utility) | Yes (0.10 m (fitted slab), 0 deg tilt) | Yes (URP Lit / Toon) | **MEDIUM_PLATFORM** |
| 15 | `carpet_floor` | `floor_1x1.fbx` | 1 x 0 x 1 | No (fitted via utility) | Yes (0.10 m (fitted slab), 0 deg tilt) | Yes (URP Lit / Toon) | **MEDIUM_PLATFORM** |
| 16 | `carpet_floor` | `floor_2x2.fbx` | 2 x 0 x 2 | No (fitted via utility) | Yes (0.10 m (fitted slab), 0 deg tilt) | Yes (URP Lit / Toon) | **LARGE_WALKABLE** |
| 17 | `carpet_floor` | `floor_3x3.fbx` | 3 x 0 x 3 | No (fitted via utility) | Yes (0.10 m (fitted slab), 0 deg tilt) | Yes (URP Lit / Toon) | **BOSS_ARENA_CANDIDATE** |
| 18 | `carpet_floor` | `floor_corner.fbx` | 3 x 0 x 3 | No (fitted via utility) | Yes (0.10 m (fitted slab), 0 deg tilt) | Yes (URP Lit / Toon) | **BOSS_ARENA_CANDIDATE** |
| 19 | `carpet_floor` | `floor_corner_half.fbx` | 2 x 0 x 2 | No (fitted via utility) | Yes (0.10 m (fitted slab), 0 deg tilt) | Yes (URP Lit / Toon) | **MEDIUM_PLATFORM** |
| 20 | `carpet_floor` | `floor_corner_round.fbx` | 3 x 0 x 3 | No (fitted via utility) | Yes (0.10 m (fitted slab), 0 deg tilt) | Yes (URP Lit / Toon) | **BOSS_ARENA_CANDIDATE** |
| 21 | `carpet_floor` | `floor_corner_round_half.fbx` | 2 x 0 x 2 | No (fitted via utility) | Yes (0.10 m (fitted slab), 0 deg tilt) | Yes (URP Lit / Toon) | **MEDIUM_PLATFORM** |
| 22 | `chairs` | `conference_chair.fbx` | 0.69 x 0.93 x 0.64 | No (fitted via utility) | No (Decor) | Yes (URP Lit / Toon) | **DECOR_FLOATING** |
| 23 | `chairs` | `conference_chair_black.fbx` | 0.69 x 0.93 x 0.64 | No (fitted via utility) | No (Decor) | Yes (URP Lit / Toon) | **DECOR_FLOATING** |
| 24 | `chairs` | `conference_chair_blue.fbx` | 0.69 x 0.93 x 0.64 | No (fitted via utility) | No (Decor) | Yes (URP Lit / Toon) | **DECOR_FLOATING** |
| 25 | `chairs` | `conference_chair_grey.fbx` | 0.69 x 0.93 x 0.64 | No (fitted via utility) | No (Decor) | Yes (URP Lit / Toon) | **DECOR_FLOATING** |
| 26 | `chairs` | `office_chair_alt.fbx` | 0.76 x 1.04 x 0.71 | No (fitted via utility) | No (Decor) | Yes (URP Lit / Toon) | **DECOR_FLOATING** |
| 27 | `chairs` | `office_chair_black.fbx` | 0.87 x 1.03 x 0.81 | No (fitted via utility) | No (Decor) | Yes (URP Lit / Toon) | **DECOR_FLOATING** |
| 28 | `chairs` | `office_chair_black_alt.fbx` | 0.76 x 1.04 x 0.71 | No (fitted via utility) | No (Decor) | Yes (URP Lit / Toon) | **DECOR_FLOATING** |
| 29 | `chairs` | `office_chair_blue.fbx` | 0.87 x 1.03 x 0.81 | No (fitted via utility) | No (Decor) | Yes (URP Lit / Toon) | **DECOR_FLOATING** |
| 30 | `chairs` | `office_chair_blue_alt.fbx` | 0.76 x 1.04 x 0.71 | No (fitted via utility) | No (Decor) | Yes (URP Lit / Toon) | **DECOR_FLOATING** |
| 31 | `chairs` | `office_chair_fancy.fbx` | 0.9 x 1.07 x 0.87 | No (fitted via utility) | No (Decor) | Yes (URP Lit / Toon) | **DECOR_FLOATING** |
| 32 | `chairs` | `office_chair_grey.fbx` | 0.87 x 1.03 x 0.81 | No (fitted via utility) | No (Decor) | Yes (URP Lit / Toon) | **DECOR_FLOATING** |
| 33 | `chairs` | `office_chair_grey_alt.fbx` | 0.76 x 1.04 x 0.71 | No (fitted via utility) | No (Decor) | Yes (URP Lit / Toon) | **DECOR_FLOATING** |
| 34 | `chairs` | `office_chair_orange.fbx` | 0.87 x 1.03 x 0.81 | No (fitted via utility) | No (Decor) | Yes (URP Lit / Toon) | **DECOR_FLOATING** |
| 35 | `computers` | `computer_keyboard.fbx` | 0.68 x 0.04 x 0.22 | No (fitted via utility) | No (Decor) | Yes (URP Lit / Toon) | **DECOR_FLOATING** |
| 36 | `computers` | `computer_monitor.fbx` | 0.6 x 0.57 x 0.53 | No (fitted via utility) | No (Decor) | Yes (URP Lit / Toon) | **DECOR_FLOATING** |
| 37 | `computers` | `computer_mouse.fbx` | 0.07 x 0.04 x 0.11 | No (fitted via utility) | No (Decor) | Yes (URP Lit / Toon) | **DECOR_FLOATING** |
| 38 | `computers` | `computer_tower.fbx` | 0.26 x 0.61 x 0.61 | No (fitted via utility) | No (Decor) | Yes (URP Lit / Toon) | **DECOR_FLOATING** |
| 39 | `computers` | `computer_tower_alt.fbx` | 0.26 x 0.61 x 0.61 | No (fitted via utility) | No (Decor) | Yes (URP Lit / Toon) | **DECOR_FLOATING** |
| 40 | `computers` | `desktop_brick.fbx` | 0.89 x 0.48 x 0.81 | No (fitted via utility) | Yes (0.48 m, 0 deg tilt) | Yes (URP Lit / Toon) | **SMALL_STEPPING_STONE** |
| 41 | `computers` | `horizontal_desktop.fbx` | 0.68 x 0.2 x 0.58 | No (fitted via utility) | No (Decor) | Yes (URP Lit / Toon) | **DECOR_FLOATING** |
| 42 | `computers` | `TV.fbx` | 0.83 x 0.91 x 0.63 | No (fitted via utility) | No (Decor) | Yes (URP Lit / Toon) | **DECOR_FLOATING** |
| 43 | `couches` | `armchair_blue.fbx` | 1.4 x 1.19 x 1.36 | No (fitted via utility) | Yes (1.19 m, 0 deg tilt) | Yes (URP Lit / Toon) | **SMALL_STEPPING_STONE** |
| 44 | `couches` | `armchair_green.fbx` | 1.4 x 1.19 x 1.36 | No (fitted via utility) | Yes (1.19 m, 0 deg tilt) | Yes (URP Lit / Toon) | **SMALL_STEPPING_STONE** |
| 45 | `couches` | `armchair_pattern.fbx` | 1.4 x 1.19 x 1.36 | No (fitted via utility) | Yes (1.19 m, 0 deg tilt) | Yes (URP Lit / Toon) | **SMALL_STEPPING_STONE** |
| 46 | `couches` | `armchair_red.fbx` | 1.4 x 1.19 x 1.36 | No (fitted via utility) | Yes (1.19 m, 0 deg tilt) | Yes (URP Lit / Toon) | **SMALL_STEPPING_STONE** |
| 47 | `couches` | `beanbag_blue.fbx` | 2.52 x 1.3 x 2.51 | No (fitted via utility) | Yes (1.30 m, 0 deg tilt) | Yes (URP Lit / Toon) | **LARGE_WALKABLE** |
| 48 | `couches` | `beanbag_green.fbx` | 2.52 x 1.3 x 2.51 | No (fitted via utility) | Yes (1.30 m, 0 deg tilt) | Yes (URP Lit / Toon) | **LARGE_WALKABLE** |
| 49 | `couches` | `beanbag_red.fbx` | 2.52 x 1.3 x 2.51 | No (fitted via utility) | Yes (1.30 m, 0 deg tilt) | Yes (URP Lit / Toon) | **LARGE_WALKABLE** |
| 50 | `couches` | `couch_blue.fbx` | 2.24 x 1.19 x 1.36 | No (fitted via utility) | Yes (1.19 m, 0 deg tilt) | Yes (URP Lit / Toon) | **MEDIUM_PLATFORM** |
| 51 | `couches` | `couch_green.fbx` | 2.24 x 1.19 x 1.36 | No (fitted via utility) | Yes (1.19 m, 0 deg tilt) | Yes (URP Lit / Toon) | **MEDIUM_PLATFORM** |
| 52 | `couches` | `couch_large_blue.fbx` | 3.16 x 1.19 x 1.39 | No (fitted via utility) | Yes (1.19 m, 0 deg tilt) | Yes (URP Lit / Toon) | **LARGE_WALKABLE** |
| 53 | `couches` | `couch_large_green.fbx` | 3.16 x 1.19 x 1.39 | No (fitted via utility) | Yes (1.19 m, 0 deg tilt) | Yes (URP Lit / Toon) | **LARGE_WALKABLE** |
| 54 | `couches` | `couch_large_pattern.fbx` | 3.16 x 1.19 x 1.39 | No (fitted via utility) | Yes (1.19 m, 0 deg tilt) | Yes (URP Lit / Toon) | **LARGE_WALKABLE** |
| 55 | `couches` | `couch_large_red.fbx` | 3.16 x 1.19 x 1.39 | No (fitted via utility) | Yes (1.19 m, 0 deg tilt) | Yes (URP Lit / Toon) | **LARGE_WALKABLE** |
| 56 | `couches` | `couch_pattern.fbx` | 2.24 x 1.19 x 1.36 | No (fitted via utility) | Yes (1.19 m, 0 deg tilt) | Yes (URP Lit / Toon) | **MEDIUM_PLATFORM** |
| 57 | `couches` | `couch_red.fbx` | 2.24 x 1.19 x 1.36 | No (fitted via utility) | Yes (1.19 m, 0 deg tilt) | Yes (URP Lit / Toon) | **MEDIUM_PLATFORM** |
| 58 | `desks` | `conference_desk.fbx` | 4.97 x 0.88 x 2.57 | No (fitted via utility) | Yes (0.88 m, 0 deg tilt) | Yes (URP Lit / Toon) | **BOSS_ARENA_CANDIDATE** |
| 59 | `desks` | `conference_desk_alt.fbx` | 4.97 x 0.88 x 2.57 | No (fitted via utility) | Yes (0.88 m, 0 deg tilt) | Yes (URP Lit / Toon) | **BOSS_ARENA_CANDIDATE** |
| 60 | `desks` | `conference_desk_squared.fbx` | 4.19 x 0.88 x 2.03 | No (fitted via utility) | Yes (0.88 m, 0 deg tilt) | Yes (URP Lit / Toon) | **LARGE_WALKABLE** |
| 61 | `desks` | `conference_desk_squared_alt.fbx` | 4.17 x 0.88 x 2.02 | No (fitted via utility) | Yes (0.88 m, 0 deg tilt) | Yes (URP Lit / Toon) | **LARGE_WALKABLE** |
| 62 | `desks` | `desk1.fbx` | 2.21 x 0.89 x 1.09 | No (fitted via utility) | Yes (0.89 m, 0 deg tilt) | Yes (URP Lit / Toon) | **MEDIUM_PLATFORM** |
| 63 | `desks` | `desk1_alt.fbx` | 2.29 x 0.89 x 1.28 | No (fitted via utility) | Yes (0.89 m, 0 deg tilt) | Yes (URP Lit / Toon) | **MEDIUM_PLATFORM** |
| 64 | `desks` | `desk1_alt_weathered.fbx` | 2.21 x 0.89 x 1.1 | No (fitted via utility) | Yes (0.89 m, 0 deg tilt) | Yes (URP Lit / Toon) | **MEDIUM_PLATFORM** |
| 65 | `desks` | `desk1_alt2.fbx` | 2.29 x 0.89 x 1.28 | No (fitted via utility) | Yes (0.89 m, 0 deg tilt) | Yes (URP Lit / Toon) | **MEDIUM_PLATFORM** |
| 66 | `desks` | `desk1_alt2_weathered.fbx` | 2.29 x 0.89 x 1.28 | No (fitted via utility) | Yes (0.89 m, 0 deg tilt) | Yes (URP Lit / Toon) | **MEDIUM_PLATFORM** |
| 67 | `desks` | `desk1_alt3.fbx` | 2.29 x 0.89 x 1.28 | No (fitted via utility) | Yes (0.89 m, 0 deg tilt) | Yes (URP Lit / Toon) | **MEDIUM_PLATFORM** |
| 68 | `desks` | `desk1_alt3_weathered.fbx` | 2.29 x 0.89 x 1.28 | No (fitted via utility) | Yes (0.89 m, 0 deg tilt) | Yes (URP Lit / Toon) | **MEDIUM_PLATFORM** |
| 69 | `desks` | `desk1_alt4.fbx` | 2.29 x 0.89 x 1.28 | No (fitted via utility) | Yes (0.89 m, 0 deg tilt) | Yes (URP Lit / Toon) | **MEDIUM_PLATFORM** |
| 70 | `desks` | `desk1_alt4_weathered.fbx` | 2.29 x 0.89 x 1.28 | No (fitted via utility) | Yes (0.89 m, 0 deg tilt) | Yes (URP Lit / Toon) | **MEDIUM_PLATFORM** |
| 71 | `desks` | `desk1_corner.fbx` | 2.39 x 0.89 x 2.29 | No (fitted via utility) | Yes (0.89 m, 0 deg tilt) | Yes (URP Lit / Toon) | **LARGE_WALKABLE** |
| 72 | `desks` | `desk1_corner_weathered.fbx` | 2.2 x 0.89 x 2.2 | No (fitted via utility) | Yes (0.89 m, 0 deg tilt) | Yes (URP Lit / Toon) | **LARGE_WALKABLE** |
| 73 | `desks` | `desk1_weathered.fbx` | 2.21 x 0.89 x 1.09 | No (fitted via utility) | Yes (0.89 m, 0 deg tilt) | Yes (URP Lit / Toon) | **MEDIUM_PLATFORM** |
| 74 | `desks` | `desk2.fbx` | 2.21 x 0.89 x 1.09 | No (fitted via utility) | Yes (0.89 m, 0 deg tilt) | Yes (URP Lit / Toon) | **MEDIUM_PLATFORM** |
| 75 | `desks` | `desk2_alt.fbx` | 2.29 x 0.89 x 1.28 | No (fitted via utility) | Yes (0.89 m, 0 deg tilt) | Yes (URP Lit / Toon) | **MEDIUM_PLATFORM** |
| 76 | `desks` | `desk2_alt_weathered.fbx` | 2.29 x 0.89 x 1.28 | No (fitted via utility) | Yes (0.89 m, 0 deg tilt) | Yes (URP Lit / Toon) | **MEDIUM_PLATFORM** |
| 77 | `desks` | `desk2_alt2.fbx` | 2.29 x 0.89 x 1.28 | No (fitted via utility) | Yes (0.89 m, 0 deg tilt) | Yes (URP Lit / Toon) | **MEDIUM_PLATFORM** |
| 78 | `desks` | `desk2_alt2_weathered.fbx` | 2.29 x 0.89 x 1.28 | No (fitted via utility) | Yes (0.89 m, 0 deg tilt) | Yes (URP Lit / Toon) | **MEDIUM_PLATFORM** |
| 79 | `desks` | `desk2_alt3.fbx` | 2.29 x 0.89 x 1.28 | No (fitted via utility) | Yes (0.89 m, 0 deg tilt) | Yes (URP Lit / Toon) | **MEDIUM_PLATFORM** |
| 80 | `desks` | `desk2_alt3_weathered.fbx` | 2.29 x 0.89 x 1.28 | No (fitted via utility) | Yes (0.89 m, 0 deg tilt) | Yes (URP Lit / Toon) | **MEDIUM_PLATFORM** |
| 81 | `desks` | `desk2_alt4.fbx` | 2.29 x 0.89 x 1.28 | No (fitted via utility) | Yes (0.89 m, 0 deg tilt) | Yes (URP Lit / Toon) | **MEDIUM_PLATFORM** |
| 82 | `desks` | `desk2_alt4_weathered.fbx` | 2.29 x 0.89 x 1.28 | No (fitted via utility) | Yes (0.89 m, 0 deg tilt) | Yes (URP Lit / Toon) | **MEDIUM_PLATFORM** |
| 83 | `desks` | `desk2_corner.fbx` | 2.21 x 0.89 x 2.21 | No (fitted via utility) | Yes (0.89 m, 0 deg tilt) | Yes (URP Lit / Toon) | **LARGE_WALKABLE** |
| 84 | `desks` | `desk2_corner_weathered.fbx` | 2.2 x 0.89 x 2.2 | No (fitted via utility) | Yes (0.89 m, 0 deg tilt) | Yes (URP Lit / Toon) | **LARGE_WALKABLE** |
| 85 | `desks` | `desk2_weathered.fbx` | 2.21 x 0.89 x 1.09 | No (fitted via utility) | Yes (0.89 m, 0 deg tilt) | Yes (URP Lit / Toon) | **MEDIUM_PLATFORM** |
| 86 | `dividers` | `divider_black.fbx` | 3.06 x 2.5 x 0.52 | No (fitted via utility) | Vertical Wall (2.50 m H) | Yes (URP Lit / Toon) | **WALL_RUN** |
| 87 | `dividers` | `divider_black_blue.fbx` | 3.06 x 2.5 x 0.52 | No (fitted via utility) | Vertical Wall (2.50 m H) | Yes (URP Lit / Toon) | **WALL_RUN** |
| 88 | `dividers` | `divider_black_blue_half.fbx` | 2.07 x 2.5 x 0.52 | No (fitted via utility) | Vertical Wall (2.50 m H) | Yes (URP Lit / Toon) | **WALL_RUN** |
| 89 | `dividers` | `divider_black_blue_low.fbx` | 3.06 x 1.5 x 0.52 | No (fitted via utility) | Yes / Cover (1.50 m) | Yes (URP Lit / Toon) | **COVER** |
| 90 | `dividers` | `divider_black_blue_low_half.fbx` | 2.07 x 1.5 x 0.52 | No (fitted via utility) | Yes / Cover (1.50 m) | Yes (URP Lit / Toon) | **COVER** |
| 91 | `dividers` | `divider_black_half.fbx` | 2.07 x 2.5 x 0.52 | No (fitted via utility) | Vertical Wall (2.50 m H) | Yes (URP Lit / Toon) | **WALL_RUN** |
| 92 | `dividers` | `divider_black_low.fbx` | 3.06 x 1.5 x 0.52 | No (fitted via utility) | Yes / Cover (1.50 m) | Yes (URP Lit / Toon) | **COVER** |
| 93 | `dividers` | `divider_black_low_half.fbx` | 2.07 x 1.5 x 0.52 | No (fitted via utility) | Yes / Cover (1.50 m) | Yes (URP Lit / Toon) | **COVER** |
| 94 | `dividers` | `divider_black_red.fbx` | 3.06 x 2.5 x 0.52 | No (fitted via utility) | Vertical Wall (2.50 m H) | Yes (URP Lit / Toon) | **WALL_RUN** |
| 95 | `dividers` | `divider_black_red_half.fbx` | 2.07 x 2.5 x 0.52 | No (fitted via utility) | Vertical Wall (2.50 m H) | Yes (URP Lit / Toon) | **WALL_RUN** |
| 96 | `dividers` | `divider_black_red_low.fbx` | 3.06 x 1.5 x 0.52 | No (fitted via utility) | Yes / Cover (1.50 m) | Yes (URP Lit / Toon) | **COVER** |
| 97 | `dividers` | `divider_black_red_low_half.fbx` | 2.07 x 1.5 x 0.52 | No (fitted via utility) | Yes / Cover (1.50 m) | Yes (URP Lit / Toon) | **COVER** |
| 98 | `dividers` | `divider_full_c.fbx` | 2.99 x 1.5 x 1.59 | No (fitted via utility) | Yes (1.50 m, 0 deg tilt) | Yes (URP Lit / Toon) | **MEDIUM_PLATFORM** |
| 99 | `dividers` | `divider_full_c_blue.fbx` | 2.99 x 1.5 x 1.59 | No (fitted via utility) | Yes (1.50 m, 0 deg tilt) | Yes (URP Lit / Toon) | **MEDIUM_PLATFORM** |
| 100 | `dividers` | `divider_full_c_desk.fbx` | 2.99 x 1.5 x 1.59 | No (fitted via utility) | Yes (1.50 m, 0 deg tilt) | Yes (URP Lit / Toon) | **MEDIUM_PLATFORM** |
| 101 | `dividers` | `divider_full_c_desk_blue.fbx` | 2.99 x 1.5 x 1.59 | No (fitted via utility) | Yes (1.50 m, 0 deg tilt) | Yes (URP Lit / Toon) | **MEDIUM_PLATFORM** |
| 102 | `dividers` | `divider_full_c_desk_red.fbx` | 2.99 x 1.5 x 1.59 | No (fitted via utility) | Yes (1.50 m, 0 deg tilt) | Yes (URP Lit / Toon) | **MEDIUM_PLATFORM** |
| 103 | `dividers` | `divider_full_c_red.fbx` | 2.99 x 1.5 x 1.59 | No (fitted via utility) | Yes (1.50 m, 0 deg tilt) | Yes (URP Lit / Toon) | **MEDIUM_PLATFORM** |
| 104 | `dividers` | `divider_full_h.fbx` | 2.99 x 1.5 x 3 | No (fitted via utility) | Yes (1.50 m, 0 deg tilt) | Yes (URP Lit / Toon) | **LARGE_WALKABLE** |
| 105 | `dividers` | `divider_full_h_blue.fbx` | 2.99 x 1.5 x 3 | No (fitted via utility) | Yes (1.50 m, 0 deg tilt) | Yes (URP Lit / Toon) | **LARGE_WALKABLE** |
| 106 | `dividers` | `divider_full_h_desk.fbx` | 2.99 x 1.5 x 3 | No (fitted via utility) | Yes (1.50 m, 0 deg tilt) | Yes (URP Lit / Toon) | **LARGE_WALKABLE** |
| 107 | `dividers` | `divider_full_h_lue_desk.fbx` | 2.99 x 1.5 x 3 | No (fitted via utility) | Yes (1.50 m, 0 deg tilt) | Yes (URP Lit / Toon) | **LARGE_WALKABLE** |
| 108 | `dividers` | `divider_full_h_red.fbx` | 2.99 x 1.5 x 3 | No (fitted via utility) | Yes (1.50 m, 0 deg tilt) | Yes (URP Lit / Toon) | **LARGE_WALKABLE** |
| 109 | `dividers` | `divider_full_h_red_desk.fbx` | 2.99 x 1.5 x 3 | No (fitted via utility) | Yes (1.50 m, 0 deg tilt) | Yes (URP Lit / Toon) | **LARGE_WALKABLE** |
| 110 | `dividers` | `divider_veneer.fbx` | 3.06 x 2.5 x 0.52 | No (fitted via utility) | Vertical Wall (2.50 m H) | Yes (URP Lit / Toon) | **WALL_RUN** |
| 111 | `dividers` | `divider_veneer_blue.fbx` | 3.06 x 2.5 x 0.52 | No (fitted via utility) | Vertical Wall (2.50 m H) | Yes (URP Lit / Toon) | **WALL_RUN** |
| 112 | `dividers` | `divider_veneer_blue_half.fbx` | 2.07 x 2.5 x 0.52 | No (fitted via utility) | Vertical Wall (2.50 m H) | Yes (URP Lit / Toon) | **WALL_RUN** |
| 113 | `dividers` | `divider_veneer_blue_low.fbx` | 3.06 x 1.5 x 0.52 | No (fitted via utility) | Yes / Cover (1.50 m) | Yes (URP Lit / Toon) | **COVER** |
| 114 | `dividers` | `divider_veneer_blue_low_half.fbx` | 2.07 x 1.5 x 0.52 | No (fitted via utility) | Yes / Cover (1.50 m) | Yes (URP Lit / Toon) | **COVER** |
| 115 | `dividers` | `divider_veneer_half.fbx` | 2.07 x 2.5 x 0.52 | No (fitted via utility) | Vertical Wall (2.50 m H) | Yes (URP Lit / Toon) | **WALL_RUN** |
| 116 | `dividers` | `divider_veneer_low.fbx` | 3.06 x 1.5 x 0.52 | No (fitted via utility) | Yes / Cover (1.50 m) | Yes (URP Lit / Toon) | **COVER** |
| 117 | `dividers` | `divider_veneer_low_half.fbx` | 2.07 x 1.5 x 0.52 | No (fitted via utility) | Yes / Cover (1.50 m) | Yes (URP Lit / Toon) | **COVER** |
| 118 | `dividers` | `divider_veneer_red.fbx` | 3.06 x 2.5 x 0.52 | No (fitted via utility) | Vertical Wall (2.50 m H) | Yes (URP Lit / Toon) | **WALL_RUN** |
| 119 | `dividers` | `divider_veneer_red_half.fbx` | 2.07 x 2.5 x 0.52 | No (fitted via utility) | Vertical Wall (2.50 m H) | Yes (URP Lit / Toon) | **WALL_RUN** |
| 120 | `dividers` | `divider_veneer_red_low.fbx` | 3.06 x 1.5 x 0.52 | No (fitted via utility) | Yes / Cover (1.50 m) | Yes (URP Lit / Toon) | **COVER** |
| 121 | `dividers` | `divider_veneer_red_low_half.fbx` | 2.07 x 1.5 x 0.52 | No (fitted via utility) | Yes / Cover (1.50 m) | Yes (URP Lit / Toon) | **COVER** |
| 122 | `dividers` | `divider_wood.fbx` | 3.06 x 2.5 x 0.52 | No (fitted via utility) | Vertical Wall (2.50 m H) | Yes (URP Lit / Toon) | **WALL_RUN** |
| 123 | `dividers` | `divider_wood_blue.fbx` | 3.06 x 2.5 x 0.52 | No (fitted via utility) | Vertical Wall (2.50 m H) | Yes (URP Lit / Toon) | **WALL_RUN** |
| 124 | `dividers` | `divider_wood_blue_half.fbx` | 2.07 x 2.5 x 0.52 | No (fitted via utility) | Vertical Wall (2.50 m H) | Yes (URP Lit / Toon) | **WALL_RUN** |
| 125 | `dividers` | `divider_wood_blue_low.fbx` | 3.06 x 1.5 x 0.52 | No (fitted via utility) | Yes / Cover (1.50 m) | Yes (URP Lit / Toon) | **COVER** |
| 126 | `dividers` | `divider_wood_blue_low_half.fbx` | 2.07 x 1.5 x 0.52 | No (fitted via utility) | Yes / Cover (1.50 m) | Yes (URP Lit / Toon) | **COVER** |
| 127 | `dividers` | `divider_wood_half.fbx` | 2.07 x 2.5 x 0.52 | No (fitted via utility) | Vertical Wall (2.50 m H) | Yes (URP Lit / Toon) | **WALL_RUN** |
| 128 | `dividers` | `divider_wood_low.fbx` | 3.06 x 1.5 x 0.52 | No (fitted via utility) | Yes / Cover (1.50 m) | Yes (URP Lit / Toon) | **COVER** |
| 129 | `dividers` | `divider_wood_low_half.fbx` | 2.07 x 1.5 x 0.52 | No (fitted via utility) | Yes / Cover (1.50 m) | Yes (URP Lit / Toon) | **COVER** |
| 130 | `dividers` | `divider_wood_red.fbx` | 3.06 x 2.5 x 0.52 | No (fitted via utility) | Vertical Wall (2.50 m H) | Yes (URP Lit / Toon) | **WALL_RUN** |
| 131 | `dividers` | `divider_wood_red_half.fbx` | 2.07 x 2.5 x 0.52 | No (fitted via utility) | Vertical Wall (2.50 m H) | Yes (URP Lit / Toon) | **WALL_RUN** |
| 132 | `dividers` | `divider_wood_red_low.fbx` | 3.06 x 1.5 x 0.52 | No (fitted via utility) | Yes / Cover (1.50 m) | Yes (URP Lit / Toon) | **COVER** |
| 133 | `dividers` | `divider_wood_red_low_half.fbx` | 2.07 x 1.5 x 0.52 | No (fitted via utility) | Yes / Cover (1.50 m) | Yes (URP Lit / Toon) | **COVER** |
| 134 | `extras` | `circular_desklamp.fbx` | 0.2 x 0.38 x 0.36 | No (fitted via utility) | No (Decor) | Yes (URP Lit / Toon) | **DECOR_FLOATING** |
| 135 | `extras` | `circular_desklamp_alt.fbx` | 0.2 x 0.38 x 0.36 | No (fitted via utility) | No (Decor) | Yes (URP Lit / Toon) | **DECOR_FLOATING** |
| 136 | `extras` | `coffee_machine.fbx` | 1.25 x 2.12 x 1.28 | No (fitted via utility) | Yes / Cover (2.12 m) | Yes (URP Lit / Toon) | **COVER** |
| 137 | `extras` | `desk_phone.fbx` | 0.33 x 0.09 x 0.33 | No (fitted via utility) | No (Decor) | Yes (URP Lit / Toon) | **DECOR_FLOATING** |
| 138 | `extras` | `desk_phone_with_cable.fbx` | 0.39 x 0.1 x 0.4 | No (fitted via utility) | No (Decor) | Yes (URP Lit / Toon) | **DECOR_FLOATING** |
| 139 | `extras` | `glass.fbx` | 0.11 x 0.17 x 0.11 | No (fitted via utility) | No (Decor) | Yes (URP Lit / Toon) | **DECOR_FLOATING** |
| 140 | `extras` | `glass2.fbx` | 0.11 x 0.17 x 0.11 | No (fitted via utility) | No (Decor) | Yes (URP Lit / Toon) | **DECOR_FLOATING** |
| 141 | `extras` | `mug.fbx` | 0.14 x 0.12 x 0.14 | No (fitted via utility) | No (Decor) | Yes (URP Lit / Toon) | **DECOR_FLOATING** |
| 142 | `extras` | `mug_alt.fbx` | 0.14 x 0.12 x 0.14 | No (fitted via utility) | No (Decor) | Yes (URP Lit / Toon) | **DECOR_FLOATING** |
| 143 | `extras` | `mug_alt2.fbx` | 0.14 x 0.12 x 0.14 | No (fitted via utility) | No (Decor) | Yes (URP Lit / Toon) | **DECOR_FLOATING** |
| 144 | `extras` | `mug_alt3.fbx` | 0.14 x 0.12 x 0.14 | No (fitted via utility) | No (Decor) | Yes (URP Lit / Toon) | **DECOR_FLOATING** |
| 145 | `extras` | `square_desklamp.fbx` | 0.2 x 0.35 x 0.32 | No (fitted via utility) | No (Decor) | Yes (URP Lit / Toon) | **DECOR_FLOATING** |
| 146 | `extras` | `square_desklamp_alt.fbx` | 0.2 x 0.35 x 0.32 | No (fitted via utility) | No (Decor) | Yes (URP Lit / Toon) | **DECOR_FLOATING** |
| 147 | `extras` | `wall_clock.fbx` | 0.6 x 0.6 x 0.03 | No (fitted via utility) | No (Decor) | Yes (URP Lit / Toon) | **DECOR_FLOATING** |
| 148 | `extras` | `water_cooler.fbx` | 0.38 x 1.4 x 0.45 | No (fitted via utility) | No (Decor) | Yes (URP Lit / Toon) | **DECOR_FLOATING** |
| 149 | `extras` | `water_cooler_bottle.fbx` | 0.35 x 0.51 x 0.4 | No (fitted via utility) | No (Decor) | Yes (URP Lit / Toon) | **DECOR_FLOATING** |
| 150 | `extras` | `water_cooler_bottle_attached.fbx` | 0.57 x 1.85 x 0.55 | No (fitted via utility) | Yes / Cover (1.85 m) | Yes (URP Lit / Toon) | **COVER** |
| 151 | `extras` | `whiteboard.fbx` | 2.15 x 2.57 x 0.86 | No (fitted via utility) | Yes / Cover (2.57 m) | Yes (URP Lit / Toon) | **COVER** |
| 152 | `file_cabinets` | `file_cabinet_large.fbx` | 1.44 x 0.71 x 0.7 | No (fitted via utility) | Yes (0.71 m, 0 deg tilt) | Yes (URP Lit / Toon) | **SMALL_STEPPING_STONE** |
| 153 | `file_cabinets` | `file_cabinet_large_open.fbx` | 1.45 x 0.71 x 0.7 | No (fitted via utility) | Yes (0.71 m, 0 deg tilt) | Yes (URP Lit / Toon) | **SMALL_STEPPING_STONE** |
| 154 | `file_cabinets` | `file_cabinet_large_shelf.fbx` | 1.43 x 0.34 x 0.69 | No (fitted via utility) | Yes (0.34 m, 0 deg tilt) | Yes (URP Lit / Toon) | **SMALL_STEPPING_STONE** |
| 155 | `file_cabinets` | `file_cabinet_large_shelf_fancy.fbx` | 1.43 x 0.34 x 0.69 | No (fitted via utility) | Yes (0.34 m, 0 deg tilt) | Yes (URP Lit / Toon) | **SMALL_STEPPING_STONE** |
| 156 | `file_cabinets` | `file_cabinet_larger.fbx` | 1.44 x 1.05 x 0.7 | No (fitted via utility) | Yes (1.05 m, 0 deg tilt) | Yes (URP Lit / Toon) | **SMALL_STEPPING_STONE** |
| 157 | `file_cabinets` | `file_cabinet_larger_open.fbx` | 1.45 x 1.05 x 0.7 | No (fitted via utility) | Yes (1.05 m, 0 deg tilt) | Yes (URP Lit / Toon) | **SMALL_STEPPING_STONE** |
| 158 | `file_cabinets` | `file_cabinet_largest.fbx` | 1.44 x 1.41 x 0.7 | No (fitted via utility) | Yes (1.41 m, 0 deg tilt) | Yes (URP Lit / Toon) | **SMALL_STEPPING_STONE** |
| 159 | `file_cabinets` | `file_cabinet_largest_open.fbx` | 1.45 x 1.41 x 0.7 | No (fitted via utility) | Yes (1.41 m, 0 deg tilt) | Yes (URP Lit / Toon) | **SMALL_STEPPING_STONE** |
| 160 | `file_cabinets` | `file_cabinet_small.fbx` | 0.48 x 1.41 x 0.52 | No (fitted via utility) | Yes (1.41 m, 0 deg tilt) | Yes (URP Lit / Toon) | **SMALL_STEPPING_STONE** |
| 161 | `file_cabinets` | `file_cabinet_small_open.fbx` | 0.48 x 1.41 x 0.52 | No (fitted via utility) | Yes (1.41 m, 0 deg tilt) | Yes (URP Lit / Toon) | **SMALL_STEPPING_STONE** |
| 162 | `file_cabinets` | `file_cabinet_small_shelf.fbx` | 0.46 x 0.34 x 0.52 | No (fitted via utility) | Yes (0.34 m, 0 deg tilt) | Yes (URP Lit / Toon) | **SMALL_STEPPING_STONE** |
| 163 | `file_cabinets` | `file_cabinet_small_shelf_fancy.fbx` | 0.46 x 0.34 x 0.52 | No (fitted via utility) | Yes (0.34 m, 0 deg tilt) | Yes (URP Lit / Toon) | **SMALL_STEPPING_STONE** |
| 164 | `file_cabinets` | `file_cabinet_small_with_shelfs.fbx` | 0.48 x 1.4 x 0.77 | No (fitted via utility) | Yes (1.40 m, 0 deg tilt) | Yes (URP Lit / Toon) | **SMALL_STEPPING_STONE** |
| 165 | `file_cabinets` | `file_cabinet_smaller.fbx` | 0.48 x 1.05 x 0.52 | No (fitted via utility) | Yes (1.05 m, 0 deg tilt) | Yes (URP Lit / Toon) | **SMALL_STEPPING_STONE** |
| 166 | `file_cabinets` | `file_cabinet_smaller_open.fbx` | 0.48 x 1.05 x 0.52 | No (fitted via utility) | Yes (1.05 m, 0 deg tilt) | Yes (URP Lit / Toon) | **SMALL_STEPPING_STONE** |
| 167 | `file_cabinets` | `file_cabinet_smaller_with_shelfs.fbx` | 0.48 x 1.05 x 0.68 | No (fitted via utility) | Yes (1.05 m, 0 deg tilt) | Yes (URP Lit / Toon) | **SMALL_STEPPING_STONE** |
| 168 | `file_cabinets` | `file_cabinet_smallest.fbx` | 0.48 x 0.71 x 0.52 | No (fitted via utility) | Yes (0.71 m, 0 deg tilt) | Yes (URP Lit / Toon) | **SMALL_STEPPING_STONE** |
| 169 | `file_cabinets` | `file_cabinet_smallest_open.fbx` | 0.48 x 0.71 x 0.52 | No (fitted via utility) | Yes (0.71 m, 0 deg tilt) | Yes (URP Lit / Toon) | **SMALL_STEPPING_STONE** |
| 170 | `file_cabinets` | `file_cabinet_smallest_with_shelfs.fbx` | 0.48 x 0.71 x 0.66 | No (fitted via utility) | Yes (0.71 m, 0 deg tilt) | Yes (URP Lit / Toon) | **SMALL_STEPPING_STONE** |
| 171 | `stationary` | `elastic_ball.fbx` | 1.87 x 1.87 x 1.28 | No (fitted via utility) | No (Decor) | Yes (URP Lit / Toon) | **DECOR_FLOATING** |
| 172 | `stationary` | `paper_single_sheet.fbx` | 1.42 x 0.44 x 1.42 | No (fitted via utility) | Yes (0.44 m, 0 deg tilt) | Yes (URP Lit / Toon) | **SMALL_STEPPING_STONE** |
| 173 | `stationary` | `paper_stack.fbx` | 1.42 x 2.97 x 1.42 | No (fitted via utility) | Yes (2.97 m, 0 deg tilt) | Yes (URP Lit / Toon) | **SMALL_STEPPING_STONE** |
| 174 | `stationary` | `paper_stack_large.fbx` | 1.42 x 11.81 x 1.42 | No (fitted via utility) | Vertical Wall (11.81 m H) | Yes (URP Lit / Toon) | **WALL_RUN** |
| 175 | `stationary` | `pen.fbx` | 0.02 x 0.24 x 0.02 | No (fitted via utility) | No (Decor) | Yes (URP Lit / Toon) | **DECOR_FLOATING** |
| 176 | `stationary` | `pen2.fbx` | 0.02 x 0.24 x 0.02 | No (fitted via utility) | No (Decor) | Yes (URP Lit / Toon) | **DECOR_FLOATING** |
| 177 | `stationary` | `pen3.fbx` | 0.02 x 0.23 x 0.02 | No (fitted via utility) | No (Decor) | Yes (URP Lit / Toon) | **DECOR_FLOATING** |
| 178 | `stationary` | `pen4.fbx` | 0.02 x 0.24 x 0.02 | No (fitted via utility) | No (Decor) | Yes (URP Lit / Toon) | **DECOR_FLOATING** |
| 179 | `stationary` | `pen5.fbx` | 0.02 x 0.24 x 0.02 | No (fitted via utility) | No (Decor) | Yes (URP Lit / Toon) | **DECOR_FLOATING** |
| 180 | `stationary` | `pen6.fbx` | 0.02 x 0.23 x 0.02 | No (fitted via utility) | No (Decor) | Yes (URP Lit / Toon) | **DECOR_FLOATING** |

## 3. Summary Counts per Class
- **BOSS_ARENA_CANDIDATE**: 5
- **LARGE_WALKABLE**: 20
- **MEDIUM_PLATFORM**: 34
- **SMALL_STEPPING_STONE**: 26
- **WALL_RUN**: 21
- **COVER**: 23
- **DECOR_FLOATING**: 51
