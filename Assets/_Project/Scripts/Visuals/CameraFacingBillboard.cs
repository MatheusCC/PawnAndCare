using UnityEngine;

namespace PawsAndCare.Visuals
{
    /// <summary>
    /// Keeps this object screen-aligned (always facing the camera) so flat visuals like
    /// world-space progress bars stay readable as the isometric camera rotates. Works for
    /// both a 3D quad and a World Space Canvas — both are just transforms to orient.
    ///
    /// Attach to the object that should face the camera (e.g. the progress bar root). When
    /// that object is toggled inactive, LateUpdate stops running — so an idle station's bar
    /// costs nothing.
    /// </summary>
    public class CameraFacingBillboard : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Optional. Camera to face. Falls back to Camera.main if left empty.")]
        private UnityEngine.Camera targetCamera = null;

        private Transform cameraTransform;

        private void Awake()
        {
            // Inspector reference preferred; fall back to the tagged main camera once at boot
            // so prefab-spawned visuals (which can't reference a scene camera) still work.
            if (targetCamera == null)
            {
                targetCamera = UnityEngine.Camera.main;
            }

            if (targetCamera != null)
            {
                cameraTransform = targetCamera.transform;
            }
            else
            {
                Debug.LogError("[CameraFacingBillboard] No camera assigned and no Camera.main found — billboard will not orient.", this);
            }
        }

        // LateUpdate (not Update): run after the camera's own movement/rotation for this frame
        // so the billboard matches the final camera orientation with no one-frame lag.
        private void LateUpdate()
        {
            if (cameraTransform != null)
            {
                // Screen-align: copy the camera's rotation so the visual sits parallel to the
                // view plane. If the bar renders back-to-front, flip it 180° on Y in the prefab.
                transform.rotation = cameraTransform.rotation;
            }
        }
    }
}
