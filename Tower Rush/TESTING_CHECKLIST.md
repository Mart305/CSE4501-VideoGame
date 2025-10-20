# Testing Checklist - New Features

Use this checklist to systematically test all implemented features before merging to main.

---

## Pre-Testing Setup

### Unity Inspector Setup Required

#### 1. AudioManager Setup (ManagerScene)
- [ ] Open `ManagerScene` in Unity
- [ ] Create GameObject named "AudioManager"
- [ ] Add `AudioManager.cs` component
- [ ] Assign audio clips (or use placeholders):
  - [ ] Fire Tower Shoot Sound
  - [ ] Ice Tower Shoot Sound
  - [ ] Ballista Tower Shoot Sound
  - [ ] Lightning Tower Shoot Sound
  - [ ] Void Tower Shoot Sound
  - [ ] Default Tower Shoot Sound (REQUIRED minimum)
- [ ] Set Music Volume: 0.5
- [ ] Set SFX Volume: 0.7
- [ ] Save scene

#### 2. TerrainDecorator Setup (First Scene)
- [ ] Open `Tower Rush Functionality.unity`
- [ ] Create GameObject named "TerrainDecorator"
- [ ] Add `TerrainDecorator.cs` component
- [ ] Assign Rock Prefabs:
  - [ ] `Assets/3D Enivronment Assets/Prefabs/DesertPrefabs/DesertRock_01.prefab`
  - [ ] `Assets/3D Enivronment Assets/Prefabs/DesertPrefabs/DesertRock_02.prefab`
  - [ ] `Assets/3D Enivronment Assets/Prefabs/DesertPrefabs/DesertRock_03.prefab`
- [ ] Assign Tree Prefabs:
  - [ ] `Assets/3D Enivronment Assets/Prefabs/DesertPrefabs/DesertTree.prefab`
  - [ ] `Assets/3D Enivronment Assets/Prefabs/GeneralPrefabs/Tree.prefab`
- [ ] Assign Bush Prefabs:
  - [ ] `Assets/TerrainSampleAssets/Prefabs/Bush_A.prefab`
  - [ ] `Assets/TerrainSampleAssets/Prefabs/Bush_B.prefab`
- [ ] Configure settings:
  - [ ] Total Objects To Spawn: 50
  - [ ] Min Spacing: 5.0
  - [ ] Randomize Rotation: ✓
  - [ ] Scale Variance: 0.2
  - [ ] Rock Spawn Weight: 0.5
  - [ ] Tree Spawn Weight: 0.3
  - [ ] Bush Spawn Weight: 0.2
  - [ ] Auto Detect Bounds: ✓
- [ ] Save scene

#### 3. FeatureValidator Setup (Optional but Recommended)
- [ ] Open `ManagerScene` or `Tower Rush Functionality.unity`
- [ ] Create GameObject named "FeatureValidator"
- [ ] Add `FeatureValidator.cs` component
- [ ] Enable "Validate On Start"
- [ ] Enable "Log Detailed Results"
- [ ] Save scene

---

## Feature Testing

### Test 1: Feature Validator (Automated)

#### 1.1 Run Validation
- [ ] Enter Play mode in Unity
- [ ] Check Console for validation results
- [ ] Verify: "ALL FEATURES VALIDATED SUCCESSFULLY!" or warnings/errors listed
- [ ] Fix any errors shown in console
- [ ] Re-run until all errors resolved

#### 1.2 Manual Validation Tests
- [ ] In Play mode, right-click FeatureValidator in Hierarchy
- [ ] Select "Test Audio System" from context menu
- [ ] Listen for audio playback
- [ ] Select "Test Terrain Refresh" from context menu
- [ ] Check Console for success messages

**Expected Results**:
- ✓ AudioManager found and configured
- ✓ WaveManager methods present
- ✓ BaseTower integration verified
- ✓ TerrainDecorator operational (if in gameplay scene)

---

### Test 2: Terrain Rendering Fix

#### 2.1 Scene Transition Test
- [ ] Start game from Main Menu
- [ ] Click "Start Game"
- [ ] Place at least one tower
- [ ] Play through waves 1-5
- [ ] Observe scene transition after wave 5

**Expected Results**:
- ✓ Terrain loads smoothly without flickering
- ✓ No missing terrain chunks
- ✓ No visual glitches or alternating terrain
- ✓ Console shows no errors
- ✓ New scene loads completely before old scene unloads

#### 2.2 Multiple Transition Test
- [ ] Continue playing through wave 10
- [ ] Observe transition to third scene
- [ ] Continue to wave 15
- [ ] Observe transition to fourth scene
- [ ] Continue to wave 20 (if applicable)

**Expected Results**:
- ✓ All transitions are smooth
- ✓ Consistent terrain rendering across all scenes
- ✓ No performance degradation over time

#### 2.3 Edge Case Tests
- [ ] Pause during scene transition
- [ ] Check terrain rendering after unpause
- [ ] Minimize/maximize Unity window during transition
- [ ] Alt-tab during transition

**Expected Results**:
- ✓ Terrain remains stable in all cases
- ✓ No crashes or errors

---

### Test 3: Tower Shooting Sounds

#### 3.1 Basic Audio Test
- [ ] Start game
- [ ] Place a Fire Tower
- [ ] Wait for enemies to spawn
- [ ] Listen for shooting sound when tower attacks

**Expected Results**:
- ✓ Sound plays when tower shoots
- ✓ Sound timing matches visual attack
- ✓ Volume is appropriate (not too loud/quiet)

#### 3.2 Different Tower Types
- [ ] Place Fire Tower → Listen for sound
- [ ] Place Ice Tower → Listen for sound
- [ ] Place Ballista Tower → Listen for sound
- [ ] Place Lightning Tower → Listen for sound
- [ ] Place Void Tower → Listen for sound

**Expected Results**:
- ✓ All towers produce shooting sounds
- ✓ Sounds play from correct positions
- ✓ If specific sounds assigned, different towers sound different
- ✓ If specific sounds not assigned, default sound plays

#### 3.3 Multiple Towers
- [ ] Place 10+ towers of various types
- [ ] Wait for enemies
- [ ] All towers attack simultaneously

**Expected Results**:
- ✓ Audio doesn't cut out (pooling works)
- ✓ No audio distortion or crackling
- ✓ Sounds from closer towers are louder (3D audio)
- ✓ Performance remains smooth

#### 3.4 3D Audio Test
- [ ] Place tower on left side of screen
- [ ] Move camera to right side
- [ ] Listen for sound panning

**Expected Results**:
- ✓ Sound appears to come from tower's position
- ✓ Sound panning matches visual position
- ✓ Sound volume decreases with distance

---

### Test 4: Terrain Decorations

#### 4.1 Initial Spawn Test
- [ ] Load first scene (Tower Rush Functionality.unity)
- [ ] Enter Play mode
- [ ] Wait for decorations to spawn (1-2 seconds)
- [ ] Check Console for spawn report

**Expected Results**:
- ✓ Console shows: "TerrainDecorator: Successfully spawned X decorations"
- ✓ Decorations visible on terrain
- ✓ Rocks, trees, and bushes present
- ✓ Objects distributed across terrain

#### 4.2 Visual Quality Test
- [ ] Observe decoration placement
- [ ] Check object spacing
- [ ] Check object rotation variety
- [ ] Check object scale variety

**Expected Results**:
- ✓ Objects don't overlap
- ✓ Minimum spacing maintained (approximately 5 units)
- ✓ Objects have varied rotations (natural look)
- ✓ Objects have varied scales (not all identical)
- ✓ Objects align with terrain (not floating/buried)

#### 4.3 Gameplay Impact Test
- [ ] Try to place towers near decorations
- [ ] Check if decorations block placement
- [ ] Check if enemies can navigate around decorations
- [ ] Check if decorations block important areas

**Expected Results**:
- ✓ Decorations don't prevent tower placement on valid ground
- ✓ Enemies navigate properly (decorations don't block AI)
- ✓ Spawn points are clear
- ✓ Player movement unobstructed

#### 4.4 Scene Hierarchy Test
- [ ] Pause game
- [ ] Open Hierarchy window
- [ ] Find "Terrain Decorations" parent object

**Expected Results**:
- ✓ "Terrain Decorations" GameObject exists
- ✓ All spawned decorations are children of this object
- ✓ Hierarchy is clean and organized

---

### Test 5: System Integration

#### 5.1 Full Gameplay Loop
- [ ] Start from Main Menu
- [ ] Start game
- [ ] Verify decorations in first scene
- [ ] Place 3-5 towers
- [ ] Verify shooting sounds work
- [ ] Play through wave 5
- [ ] Verify smooth scene transition
- [ ] Verify decorations in second scene
- [ ] Verify towers continue shooting with sound
- [ ] Play through wave 10
- [ ] Repeat for subsequent scenes

**Expected Results**:
- ✓ All systems work together seamlessly
- ✓ No conflicts between features
- ✓ No performance issues
- ✓ No console errors

#### 5.2 Existing Features Test
- [ ] Test tower placement system
- [ ] Test tower upgrade system
- [ ] Test enemy spawning
- [ ] Test currency system
- [ ] Test wave progression
- [ ] Test game over functionality

**Expected Results**:
- ✓ All existing features still work
- ✓ No breaking changes introduced
- ✓ New features don't interfere with old features

#### 5.3 Performance Test
- [ ] Monitor FPS during gameplay
- [ ] Check FPS during scene transitions
- [ ] Check FPS with many decorations
- [ ] Check FPS with many towers shooting

**Expected Results**:
- ✓ FPS remains stable (target: 60fps)
- ✓ No frame drops during scene transitions
- ✓ Audio pooling prevents performance issues
- ✓ Decorations don't significantly impact performance

---

### Test 6: Error Handling

#### 6.1 Missing AudioManager Test
- [ ] Temporarily disable AudioManager GameObject
- [ ] Enter Play mode
- [ ] Place tower and wait for shooting

**Expected Results**:
- ✓ Game doesn't crash
- ✓ Tower shoots without sound (graceful degradation)
- ✓ Console may show warning (optional)

#### 6.2 Missing Audio Clips Test
- [ ] In AudioManager, leave some clips unassigned
- [ ] Test towers with missing sounds

**Expected Results**:
- ✓ Game doesn't crash
- ✓ Default sound plays (if assigned)
- ✓ Or silent if default also missing (no error)

#### 6.3 Missing TerrainDecorator Test
- [ ] Load scene without TerrainDecorator
- [ ] Enter Play mode

**Expected Results**:
- ✓ Game works normally
- ✓ No decorations spawn (expected)
- ✓ No errors in console

#### 6.4 Scene Without Terrain Test
- [ ] Add TerrainDecorator to scene without Terrain
- [ ] Enter Play mode

**Expected Results**:
- ✓ Game doesn't crash
- ✓ TerrainDecorator logs warning
- ✓ No decorations spawn

---

## Console Monitoring

### Throughout All Tests, Monitor for:
- [ ] No red errors
- [ ] No yellow warnings (or only expected warnings)
- [ ] Successful initialization messages
- [ ] Proper spawn reports from TerrainDecorator

### Expected Console Messages:
```
✓ "TerrainDecorator: Successfully spawned X decorations in Y attempts"
✓ "FeatureValidator: ALL FEATURES VALIDATED SUCCESSFULLY!"
✓ No "NullReferenceException" errors
✓ No "MissingReferenceException" errors
✓ No audio listener conflicts
```

---

## Build Testing (Optional but Recommended)

### 7.1 Build Test
- [ ] Create development build
- [ ] Run build
- [ ] Test all features in build
- [ ] Verify audio works in build
- [ ] Verify decorations spawn in build
- [ ] Verify terrain transitions work in build

**Expected Results**:
- ✓ Build completes without errors
- ✓ All features work in standalone build
- ✓ Performance is acceptable

---

## Final Checklist

### Before Merging to Main:
- [ ] All tests passed
- [ ] No console errors during normal gameplay
- [ ] All features documented
- [ ] Code reviewed for quality
- [ ] Performance is acceptable
- [ ] No breaking changes to existing systems

### Documentation Complete:
- [ ] IMPLEMENTATION_GUIDE.md created
- [ ] FEATURE_SUMMARY.md created
- [ ] TESTING_CHECKLIST.md created (this file)
- [ ] Code comments are clear
- [ ] Setup instructions are accurate

### Ready for Commit:
- [ ] All modified files saved
- [ ] All new files added to git
- [ ] Commit message prepared
- [ ] Branch is up to date

---

## Known Issues Log

Document any issues found during testing:

| Issue | Severity | Status | Notes |
|-------|----------|--------|-------|
| Example: Audio clip X missing | Low | Open | Use placeholder for now |
|  |  |  |  |
|  |  |  |  |

---

## Test Results Summary

**Tester**: ___________________
**Date**: ___________________
**Unity Version**: ___________________
**Platform**: ___________________

### Results:
- [ ] All tests passed ✓
- [ ] Tests passed with minor issues ⚠
- [ ] Tests failed - requires fixes ✗

**Notes**:
_______________________________________________________
_______________________________________________________
_______________________________________________________

**Approved for merge**: [ ] Yes  [ ] No

---

**Last Updated**: October 17, 2025
**Branch**: fortuna2
