using NUnit.Framework;
using TwoUp.EditorTools;
using UnityEditor;
using UnityEngine;

namespace TwoUp.Tests.EditMode
{
    public class GetCoinsPanelBuilderTests
    {
        [Test]
        public void Add_BuildsPanelWithAllRefs()
        {
            var root = new GameObject("TestRoot");
            try
            {
                var controller = GetCoinsPanelBuilder.Add(root.transform);
                Assert.IsNotNull(controller, "GetCoinsPanelBuilder.Add should return a non-null controller");

                var so = new SerializedObject(controller);
                AssertRefWired(so, "headline");
                AssertRefWired(so, "balanceText");
                AssertRefWired(so, "watchAdButton");
                AssertRefWired(so, "watchAdLabel");
                AssertRefWired(so, "closeButton");

                Assert.IsFalse(controller.gameObject.activeSelf, "Panel_GetCoins should be inactive by default");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void AssertRefWired(SerializedObject so, string fieldName)
        {
            var prop = so.FindProperty(fieldName);
            Assert.IsNotNull(prop, $"No serialized field '{fieldName}'");
            Assert.IsNotNull(prop.objectReferenceValue, $"Field '{fieldName}' is not wired");
        }
    }
}
