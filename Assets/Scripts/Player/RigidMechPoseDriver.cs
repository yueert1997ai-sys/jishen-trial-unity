using System;
using UnityEngine;

// Retargets the existing animation skeleton onto rigid armor pivots. Never edits mesh vertices.
// Called by RiggedMechAnimator after the source animation/recoil and before weapon/socket updates.
[DisallowMultipleComponent]
public sealed class RigidMechPoseDriver : MonoBehaviour
{
    [Serializable]
    public sealed class Segment
    {
        public Transform source, target;
        public Quaternion sourceRest, targetRest, calibration = Quaternion.identity;
        public Vector3 localPosition;
    }
    [Serializable]
    public sealed class Follower
    {
        public Transform target, from, to;
        public Quaternion rest;
        [Range(0, 1)] public float weight;
    }
    [Serializable]
    public sealed class Contact
    {
        public Transform part;
        public Vector3 localPoint;
    }

    public Transform assemblyRoot, sourceHips, pelvis, cannon;
    public Segment[] segments = Array.Empty<Segment>();
    public Follower[] followers = Array.Empty<Follower>();
    public Contact[] soleContacts = Array.Empty<Contact>();
    public Vector3 assemblyRestPosition, sourceHipsRest;
    public Quaternion cannonRest;
    public GameObject beam;
    public Transform[] thrusters = Array.Empty<Transform>();

    public Transform Resolve(Transform source)
    {
        foreach (var segment in segments) if (segment.source == source) return segment.target;
        return source;
    }

    public void ApplyPose(bool grounded = true)
    {
        assemblyRoot.localPosition = assemblyRestPosition;
        Quaternion world = transform.rotation;
        Quaternion inverseWorld = Quaternion.Inverse(world);
        foreach (var segment in segments)
        {
            segment.target.localPosition = segment.localPosition;
            Quaternion delta = inverseWorld * segment.source.rotation * Quaternion.Inverse(segment.sourceRest);
            segment.target.rotation = world * delta * segment.calibration * segment.targetRest;
        }
        // Preserve the new limb lengths. Only the pelvis receives source vertical translation.
        float lift = transform.InverseTransformPoint(sourceHips.position).y - sourceHipsRest.y;
        pelvis.position += transform.up * lift;
        foreach (var follower in followers)
            follower.target.rotation = Quaternion.Slerp(follower.from.rotation, follower.to.rotation, follower.weight) * follower.rest;
        cannon.localRotation = cannonRest;
        if (grounded) GroundFeet();
    }

    public void GroundFeet()
    {
        if (soleContacts.Length > 0)
        {
            float minimum = float.PositiveInfinity;
            foreach (var contact in soleContacts)
                minimum = Mathf.Min(minimum, transform.InverseTransformPoint(contact.part.TransformPoint(contact.localPoint)).y);
            assemblyRoot.position += transform.up * (.025f - minimum);
        }
    }

    public void SetBeamActive(bool active)
    {
        if (beam != null) beam.SetActive(active);
    }
}
