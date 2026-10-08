using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TheWheeler.Tests
{
    public class ToolkitTests
    {
        [Test]
        public void AssembliesDoNotDependOnTheGame()
        {
            foreach (var assembly in new[] { typeof(UIRegistry).Assembly, typeof(AnimationEventEditor).Assembly })
            {
                Assert.That(assembly.GetName().Name, Does.StartWith("TheWheeler."));
                Assert.That(assembly.GetReferencedAssemblies().Any(a => a.Name.StartsWith("Assembly-CSharp")), Is.False);
            }
        }

        [Test]
        public void DescendantSearchFindsNestedChildAndReturnsNullForMissingName()
        {
            var root = new GameObject("Root");
            try
            {
                var child = new GameObject("Child");
                child.transform.SetParent(root.transform);
                var leaf = new GameObject("Leaf");
                leaf.transform.SetParent(child.transform);
                Assert.That(root.transform.FindChildByNameDeeper("Leaf"), Is.EqualTo(leaf.transform));
                Assert.That(root.transform.FindChildByNameDeeper("Missing"), Is.Null);
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void SafeInvokeSkipsDestroyedUnityObject()
        {
            var target = new GameObject("Destroyed");
            Object.DestroyImmediate(target);
            var called = false;
            target.SafeInvoke(_ => { called = true; });
            Assert.That(called, Is.False);
            Assert.That(target.SafeInvoke(_ => 12, 42), Is.EqualTo(42));
        }

        [Test]
        public void JsonListUsesCallerSuppliedDataTypes()
        {
            var asset = new TextAsset("{\"items\":[{\"value\":3},{\"value\":7}]}");
            try
            {
                var results = new List<int>();
                JsonLoader.LoadJsonList<TestRoot, TestData, int>(asset, r => r.items, d => d.value * 2, results.Add);
                CollectionAssert.AreEqual(new[] { 6, 14 }, results);
                CollectionAssert.AreEqual(new[] { 1, 2, 3 }, JsonLoader.ParseIntList("1, 2,3"));
            }
            finally { Object.DestroyImmediate(asset); }
        }

        [Serializable] public class TestRoot { public List<TestData> items; }
        [Serializable] public class TestData { public int value; }

        [Test]
        public void UiSlotsReuseInstancesAndKeepTemplateHidden()
        {
            var root = new GameObject("List");
            try
            {
                var template = new GameObject("Template", typeof(RectTransform));
                template.transform.SetParent(root.transform);
                var first = new List<RectTransform>();
                UIListDrawer.DrawListToTarget<RectTransform, int>(root.transform, template, new List<int> { 1, 2 },
                    (slot, data, index) => first.Add(slot));
                Assert.That(template.activeSelf, Is.False);
                Assert.That(first.Count, Is.EqualTo(2));
                RectTransform reused = null;
                UIListDrawer.DrawListToTarget<RectTransform, int>(root.transform, template, new List<int> { 3 },
                    (slot, data, index) => reused = slot);
                Assert.That(reused, Is.SameAs(first[0]));
                Assert.That(first[1].gameObject.activeSelf, Is.False);
                Assert.That(root.transform.childCount, Is.EqualTo(3));
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void NestedPauseRequiresMatchingResumeRequests()
        {
            float previous = Time.timeScale;
            try
            {
                PauseManager.Reset();
                PauseManager.RequestPause();
                PauseManager.RequestPause();
                PauseManager.RequestResume();
                Assert.That(Time.timeScale, Is.Zero);
                PauseManager.RequestResume();
                Assert.That(Time.timeScale, Is.EqualTo(1));
                PauseManager.RequestResume();
                Assert.That(Time.timeScale, Is.EqualTo(1));
            }
            finally { PauseManager.Reset(); Time.timeScale = previous; }
        }

        [Test]
        public void ObjectsWithoutTeamAreNeitherFriendsNorEnemies()
        {
            var a = new GameObject("A");
            var b = new GameObject("B");
            try
            {
                Assert.That(TeamUtility.GetTeamId(a).IsValid, Is.False);
                Assert.That(TeamUtility.IsSameTeam(a, b), Is.False);
                Assert.That(TeamUtility.IsEnemy(a, b), Is.False);
            }
            finally { Object.DestroyImmediate(a); Object.DestroyImmediate(b); }
        }

        [Test]
        public void AnimationEditorPreservesCustomFunctionWhenEditingEvent()
        {
            string path = "Assets/TheWheelerTest-" + Guid.NewGuid().ToString("N") + ".anim";
            var window = ScriptableObject.CreateInstance<AnimationEventEditor>();
            try
            {
                var clip = new AnimationClip();
                AssetDatabase.CreateAsset(clip, path);
                var ev = new AnimationEvent { functionName = "OnCustomFootstep", time = 0.25f, intParameter = 7 };
                AnimationUtility.SetAnimationEvents(clip, new[] { ev });
                var type = typeof(AnimationEventEditor);
                const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
                type.GetField("targetClip", flags).SetValue(window, clip);
                type.GetMethod("SelectEventForEdit", flags).Invoke(window, new object[] { 0, ev });
                type.GetMethod("ApplyEventChanges", flags).Invoke(window, null);
                var saved = AnimationUtility.GetAnimationEvents(clip);
                Assert.That(saved.Single().functionName, Is.EqualTo("OnCustomFootstep"));
                Assert.That(saved.Single().intParameter, Is.EqualTo(7));
                type.GetField("eventFunctionName", flags).SetValue(window, "OnCustomAttack");
                type.GetMethod("AddEvent", flags).Invoke(window, null);
                Assert.That(AnimationUtility.GetAnimationEvents(clip).Any(e => e.functionName == "OnCustomAttack"), Is.True);
            }
            finally { Object.DestroyImmediate(window); AssetDatabase.DeleteAsset(path); }
        }

        [Test]
        public void EnvironmentEditorsCanBeCreatedWithoutGameAssets()
        {
            var palette = ScriptableObject.CreateInstance<SO_EnvironmentPalette>();
            var placer = ScriptableObject.CreateInstance<PropPlacerTool>();
            var blender = ScriptableObject.CreateInstance<MapBlenderTool>();
            try
            {
                placer.palette = palette;
                Assert.That(placer.palette, Is.SameAs(palette));
                Assert.That(blender.resolution, Is.GreaterThan(0));
            }
            finally
            {
                Object.DestroyImmediate(placer);
                Object.DestroyImmediate(blender);
                Object.DestroyImmediate(palette);
            }
        }
    }
}
