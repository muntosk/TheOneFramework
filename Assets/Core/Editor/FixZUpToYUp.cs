using UnityEditor;
using UnityEngine;

namespace TheOneFramework.EditorTools
{
    // Some imported GLB packs (Source-engine-derived exports in particular) keep their
    // original Z-up authoring axis instead of converting to glTF's Y-up convention, so
    // everything drops into Unity lying on its side. This wraps each selected object in
    // a parent with the -90 degree X correction, without touching the source asset.
    public static class FixZUpToYUp
    {
        [MenuItem("Tools/Fix Z-Up To Y-Up (Wrap Selected)")]
        private static void WrapSelected()
        {
            foreach (Transform t in Selection.transforms)
            {
                var wrapper = new GameObject(t.name + "_YUp");
                Undo.RegisterCreatedObjectUndo(wrapper, "Fix Z-Up To Y-Up");

                Undo.SetTransformParent(wrapper.transform, t.parent, "Fix Z-Up To Y-Up");
                wrapper.transform.localPosition = t.localPosition;
                wrapper.transform.localRotation = t.localRotation;
                wrapper.transform.localScale = t.localScale;

                Undo.SetTransformParent(t, wrapper.transform, "Fix Z-Up To Y-Up");
                Undo.RecordObject(t, "Fix Z-Up To Y-Up");
                t.localPosition = Vector3.zero;
                t.localRotation = Quaternion.Euler(-90f, 0f, 0f);
                t.localScale = Vector3.one;
            }
        }

        [MenuItem("Tools/Fix Z-Up To Y-Up (Wrap Selected)", true)]
        private static bool ValidateWrapSelected()
        {
            return Selection.transforms.Length > 0;
        }
    }
}
