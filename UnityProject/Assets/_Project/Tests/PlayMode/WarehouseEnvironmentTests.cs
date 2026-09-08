using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace EFTM.Tests.PlayMode
{
    public sealed class WarehouseEnvironmentTests
    {
        private Scene scene;
        private GameObject environment;

        [UnitySetUp]
        public IEnumerator LoadWarehouse()
        {
            yield return SceneManager.LoadSceneAsync("Assets/_Project/Scenes/CombatFoundationV1.unity", LoadSceneMode.Additive);
            scene = SceneManager.GetSceneByPath("Assets/_Project/Scenes/CombatFoundationV1.unity");
            environment = scene.GetRootGameObjects().Single(o => o.name == "WarehouseEnvironment");
            Physics.SyncTransforms();
        }

        [UnityTearDown]
        public IEnumerator UnloadWarehouse()
        {
            if (scene.IsValid() && scene.isLoaded) yield return SceneManager.UnloadSceneAsync(scene);
        }

        [Test]
        public void ImportedMeshesKeepMetreScaleAndMatchCriticalOccluders()
        {
            var renderers = environment.GetComponentsInChildren<MeshRenderer>();
            Assert.That(renderers.Length, Is.GreaterThan(10));
            Assert.That(renderers.All(r => r.sharedMaterials.All(m => m != null && m.shader.name == "Standard")), Is.True);
            var floor = renderers.Single(r => r.name == "Floor").bounds;
            Assert.That(floor.size.x, Is.EqualTo(4f).Within(.03f));
            Assert.That(floor.size.z, Is.EqualTo(24f).Within(.03f));
            foreach (var name in new[] { "NearCoverRight", "NearCoverLeft", "FarWall", "LeftWall", "RightWall" })
            {
                var visual = renderers.Single(r => r.name == name).bounds;
                var collision = environment.GetComponentsInChildren<BoxCollider>().Single(c => c.name == name).bounds;
                Assert.That(Vector3.Distance(visual.center,collision.center), Is.LessThan(.04f), name + " collider center");
                Assert.That(Vector3.Distance(visual.size,collision.size), Is.LessThan(.07f), name + " collider extent");
            }
        }

        [Test]
        public void OriginalPeekPathOpensRealSightlineWithoutReocclusion()
        {
            var poses = scene.GetRootGameObjects().Single(o => o.name == "CoverSideRig")
                .GetComponent<EFTM.Combat.Camera.CoverSideRig>().Get(EFTM.Combat.Foundation.CoverSide.Right);
            var hidden = poses.hiddenPose.position;
            var exposed = poses.exposedPose.position;
            Assert.That(Vector3.Distance(hidden,new Vector3(.73f,1.55f,-.85f)), Is.LessThan(.001f));
            Assert.That(Vector3.Distance(exposed,new Vector3(.22f,1.495f,-.65f)), Is.LessThan(.001f));
            var target = new Vector3(0f,1.5f,18f);
            Assert.That(Physics.Linecast(hidden,target,out var hit), Is.True);
            Assert.That(hit.collider.name, Is.EqualTo("NearCoverRight"));
            var hasOpened = false;
            for (var i = 0; i <= 20; i++)
            {
                var position = Vector3.Lerp(hidden,exposed,Mathf.SmoothStep(0,1,i/20f));
                var blocked = Physics.Linecast(position,target);
                if (hasOpened) Assert.That(blocked, Is.False, "Sightline closed again during outward Peek.");
                if (!blocked) hasOpened = true;
            }
            Assert.That(hasOpened, Is.True, "No view down the corridor at full Peek.");
        }
    }
}
