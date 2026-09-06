using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon.Common;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class TransformGizmoDevBridge : TransformGizmoBridge
    {
        public TransformGizmo transformGizmo;
        public Transform tracked;
        private float inputLookVertical;

        private VRCPlayerApi localPlayer;
        private bool isInVR;

        private void Start()
        {
            localPlayer = Networking.LocalPlayer;
            isInVR = localPlayer.IsUserInVR();
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
            if (isInVR)
            {
                VRCPlayerApi.TrackingData hand = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand);
                position = hand.position;
                rotation = hand.rotation * transformGizmo.handDirectionOffsetForVR;
            }
            else
            {
                VRCPlayerApi.TrackingData head = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
                position = head.position;
                rotation = head.rotation;
            }
        }

        public override void InputUse(bool value, UdonInputEventArgs args)
        {
            if (!isInVR || args.handType != HandType.RIGHT)
                return;
            if (value)
                transformGizmo.Activate();
            else
                transformGizmo.Deactivate();
        }

        public override void InputLookVertical(float value, UdonInputEventArgs args)
        {
            inputLookVertical = value;
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
            return isInVR
                ? inputLookVertical > 0.4f
                : (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl));
        }

        public override bool ShowVisualRaycastThisFrame()
        {
            return isInVR;
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
