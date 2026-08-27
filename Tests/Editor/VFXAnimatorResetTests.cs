using Jeomseon.Unity.GameObjectPooling.Lifecycle;
using Jeomseon.Unity.VFX;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Jeomseon.Tests.Editor
{
    public sealed class VFXAnimatorResetTests
    {
        private const string TemporaryFolder = "Assets/__VFXAnimatorResetTests";
        private const string ClipPath = TemporaryFolder + "/Motion.anim";
        private const string ControllerPath = TemporaryFolder + "/Motion.controller";

        [TearDown]
        public void TearDown()
        {
            AssetDatabase.DeleteAsset(TemporaryFolder);
        }

        [Test]
        public void ReleaseToPool_ResetsEveryChildAnimator()
        {
            AssetDatabase.CreateFolder("Assets", "__VFXAnimatorResetTests");
            var clip = new AnimationClip { name = "Motion" };
            clip.SetCurve(
                string.Empty,
                typeof(Transform),
                "m_LocalPosition.x",
                AnimationCurve.Linear(0f, 0f, 1f, 5f));
            AssetDatabase.CreateAsset(clip, ClipPath);
            AnimatorController controller =
                AnimatorController.CreateAnimatorControllerAtPathWithClip(ControllerPath, clip);

            var root = new GameObject("VFX Animator Reset Test");
            try
            {
                VFXInstance instance = root.AddComponent<VFXInstance>();
                Animator first = CreateChildAnimator(root.transform, "First", controller);
                Animator second = CreateChildAnimator(root.transform, "Second", controller);
                MoveAnimatorToEnd(first);
                MoveAnimatorToEnd(second);
                Assert.That(first.transform.localPosition.x, Is.GreaterThan(3f));
                Assert.That(second.transform.localPosition.x, Is.GreaterThan(3f));

                ((IPoolReleaseHandler)instance).OnReleaseToPool();

                Assert.That(first.transform.localPosition.x, Is.EqualTo(0f).Within(0.001f));
                Assert.That(second.transform.localPosition.x, Is.EqualTo(0f).Within(0.001f));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static Animator CreateChildAnimator(
            Transform parent,
            string name,
            RuntimeAnimatorController controller)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent, false);
            Animator animator = child.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            return animator;
        }

        private static void MoveAnimatorToEnd(Animator animator)
        {
            animator.Play("Motion", 0, 0.8f);
            animator.Update(0f);
        }
    }
}
