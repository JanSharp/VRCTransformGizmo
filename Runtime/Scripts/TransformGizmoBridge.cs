using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace JanSharp
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public abstract class TransformGizmoBridge : UdonSharpBehaviour
    {
        public abstract void GetHead(out Vector3 position, out Quaternion rotation);
        public abstract void GetRaycastOrigin(out Vector3 position, out Quaternion rotation); // TODO: use
        public abstract bool ActivateThisFrame();
        public abstract bool DeactivateThisFrame();
        public abstract bool SnappingThisFrame();

        public abstract void OnPositionModified();
        public abstract void OnRotationModified();
        public abstract void OnScaleModified();
    }
}
