using System;
using System.IO;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace EFTM.Editor
{
    /// <summary>CLI entry for existing Unity Test Framework; survives PlayMode domain reload.</summary>
    public static class CombatValidationRunner
    {
        private const string OutputKey = "EFTM.CombatValidation.Output";
        private static TestRunnerApi api;
        private static Results callbacks;

        [InitializeOnLoadMethod]
        private static void Reconnect()
        {
            if (!string.IsNullOrEmpty(SessionState.GetString(OutputKey, ""))) Connect();
        }

        public static void RunPlayMode()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play before tests.");
            SessionState.SetString(OutputKey, Path.GetFullPath("../codex-chat-images/008-playmode.xml"));
            Connect();
            // Keep execution outside the CLI dispatch stack, and let TestRunner own scene/reload lifecycle.
            EditorApplication.update -= StartPlayTests;
            EditorApplication.update += StartPlayTests;
        }

        private static void StartPlayTests()
        {
            EditorApplication.update -= StartPlayTests;
            api.Execute(new ExecutionSettings(new Filter { testMode = TestMode.PlayMode,
                assemblyNames = new[] { "EFTM.Tests.PlayMode" } }));
        }

        private static void Connect()
        {
            if (api != null) return;
            api = ScriptableObject.CreateInstance<TestRunnerApi>();
            callbacks = new Results(); api.RegisterCallbacks(callbacks);
        }

        private sealed class Results : ICallbacks
        {
            public void RunStarted(ITestAdaptor tests) { }
            public void TestStarted(ITestAdaptor test) { }
            public void TestFinished(ITestResultAdaptor result) { }
            public void RunFinished(ITestResultAdaptor result)
            {
                var path = SessionState.GetString(OutputKey, "");
                if (string.IsNullOrEmpty(path)) return;
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                // Reflection avoids adding NUnit as a dependency to the production Editor assembly.
                var node = result.GetType().GetMethod("ToXml").Invoke(result, null);
                File.WriteAllText(path, (string)node.GetType().GetProperty("OuterXml").GetValue(node));
                SessionState.EraseString(OutputKey);
                Debug.Log($"[EFTM] PlayMode validation: {result.PassCount} passed, {result.FailCount} failed; {path}");
                api.UnregisterCallbacks(callbacks);
                UnityEngine.Object.DestroyImmediate(api); api = null; callbacks = null;
            }
        }
    }
}
