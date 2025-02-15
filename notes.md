
# Ideas

- [ ] pivot vs center
- [ ] local vs global
- [ ] multi object transforming
- [x] different "raycast" origin in VR - can be done with the bridge now
- [ ] show little intersection dots in VR
- [ ] show raycast line in VR
- [x] better snapping handling - handled through the bridge
- [x] have some kind of "bridge" for all input data
  - [x] head position and rotation
  - [x] raycast origin position and rotation
  - [x] activate (like on mouse down)
  - [x] deactivate (like on mouse up)
  - [x] snapping (like GetKey(control))
- [x] events for changes made by the gizmo (maybe also going through the "bridge")
- [ ] snapping indicators for scaling, using another custom shader
- [ ] snapping indicators for moving, using shader
- [ ] abort current action. Like when the object is being moved, right click and it jumps back to where it was before the user started moving it and the state goes back to waiting
- [ ] integration into some kind of undo system
- [ ] the ability to limit what kind of transformations are allowed
- [ ] it's using head instead of hand in VR for some reason?
- [x] all of the changed events which are supposed to only get raised when the value difference since the last time it got raised just keep getting raised every frame
- [ ] why is rotating using world rotation while moving and scaling are using local?
- [ ] change raycast to match what the custom interacts and pickups system is doing, which is angling it downwards 45 degrees. This matches VRChat's interact and UI raycast rotation
