
# Ideas

- [ ] pivot vs center
- [ ] local vs global
- [ ] multi object transforming
- [x] different "raycast" origin in VR - can be done with the bridge now
- [ ] show little intersection dots in VR
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
