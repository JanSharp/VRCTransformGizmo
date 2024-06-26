using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class TransformGizmoDevBridge : TransformGizmoBridge
    {
        public TransformGizmo transformGizmo;
        public Transform tracked;

        private VRCPlayerApi localPlayer;

        private void Start()
        {
            localPlayer = Networking.LocalPlayer;
            transformGizmo.SetTracked(tracked, this);
        }

        public override void GetHead(out Vector3 position, out Quaternion rotation)
        {
            VRCPlayerApi.TrackingData head = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
            position = head.position;
            rotation = head.rotation;
        }

        public override void GetRaycastOrigin(out Vector3 position, out Quaternion rotation)
        {
            VRCPlayerApi.TrackingData head = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
            position = head.position;
            rotation = head.rotation;
        }

        public override bool ActivateThisFrame()
        {
            return Input.GetMouseButtonDown(0);
        }

        public override bool DeactivateThisFrame()
        {
            return Input.GetMouseButtonUp(0);
        }

        public override bool SnappingThisFrame()
        {
            return Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
        }

        public override void OnPositionModified()
        {
        }

        public override void OnRotationModified()
        {
        }

        public override void OnScaleModified()
        {
        }
    }
}
