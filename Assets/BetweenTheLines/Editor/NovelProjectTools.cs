using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Yarn.Unity;

namespace Emotionalaw.Editor
{
    public static class NovelProjectTools
    {
        private const string ScenePath = "Assets/Scenes/BetweenTheLines.unity";
        private const string StoryPath = "Assets/BetweenTheLines/Story/BetweenTheLines.yarnproject";

        [MenuItem("Tools/Between the Lines/Author Practical Ending Guidance")]
        public static void AuthorPracticalEndingGuidance()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().path != ScenePath)
                throw new InvalidOperationException("Open BetweenTheLines.unity first.");

            var flow = UnityEngine.Object.FindFirstObjectByType<NovelFlow>();
            var sceneObjects = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var ending = sceneObjects.FirstOrDefault(item => item.name == "04 • Ending Screen");
            var results = ending != null ? ending.Find("Results") as RectTransform : null;
            if (flow == null || ending == null || results == null)
                throw new InvalidOperationException("Expected authored ending objects were not found.");

            Undo.RecordObject(results, "Make room for practical ending guidance");
            results.anchorMin = new Vector2(.08f, .2f);
            results.anchorMax = new Vector2(.47f, .58f);
            results.offsetMin = Vector2.zero;
            results.offsetMax = Vector2.zero;

            var guidanceTransform = ending.Find("Practical Next Steps") as RectTransform;
            if (guidanceTransform == null)
            {
                var guidance = new GameObject("Practical Next Steps", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
                Undo.RegisterCreatedObjectUndo(guidance, "Add practical ending guidance");
                guidance.transform.SetParent(ending, false);
                guidanceTransform = (RectTransform)guidance.transform;
            }
            guidanceTransform.anchorMin = new Vector2(.52f, .2f);
            guidanceTransform.anchorMax = new Vector2(.92f, .58f);
            guidanceTransform.offsetMin = Vector2.zero;
            guidanceTransform.offsetMax = Vector2.zero;

            var guidanceText = guidanceTransform.GetComponent<Text>();
            guidanceText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            guidanceText.fontSize = 21;
            guidanceText.fontStyle = FontStyle.Normal;
            guidanceText.alignment = TextAnchor.UpperLeft;
            guidanceText.color = new Color(.89f, .82f, .7f, 1f);
            guidanceText.raycastTarget = false;
            guidanceText.horizontalOverflow = HorizontalWrapMode.Wrap;
            guidanceText.verticalOverflow = VerticalWrapMode.Truncate;

            var flowProperties = new SerializedObject(flow);
            flowProperties.FindProperty("showEndingGuidance").boolValue = true;
            flowProperties.FindProperty("endingGuidance").objectReferenceValue = guidanceText;
            flowProperties.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(flow);
            EditorUtility.SetDirty(guidanceText);
            EditorSceneManager.MarkSceneDirty(flow.gameObject.scene);
            EditorSceneManager.SaveScene(flow.gameObject.scene);
            Debug.Log("BTL: Authored practical ending guidance in the Ending Screen hierarchy.");
        }

        [MenuItem("Tools/Between the Lines/Author HUD Options and Notes Badge")]
        public static void AuthorHudOptionsAndBadge()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().path != ScenePath)
                throw new InvalidOperationException("Open BetweenTheLines.unity first.");

            var flow = UnityEngine.Object.FindFirstObjectByType<NovelFlow>();
            var sceneObjects = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            GameObject FindAuthored(string name) => sceneObjects.FirstOrDefault(item => item.name == name)?.gameObject;
            var chapter = FindAuthored("Chapter");
            var clueCounter = FindAuthored("Clue Count");
            var notes = FindAuthored("Notebook");
            if (flow == null || chapter == null || clueCounter == null || notes == null)
                throw new InvalidOperationException("Expected authored HUD objects were not found.");

            var indicator = notes.GetComponent<NotesClueIndicator>();
            if (indicator == null) indicator = Undo.AddComponent<NotesClueIndicator>(notes);

            var badgeTransform = notes.transform.Find("Unread Clue Badge") as RectTransform;
            if (badgeTransform == null)
            {
                var badge = new GameObject("Unread Clue Badge", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
                Undo.RegisterCreatedObjectUndo(badge, "Add authored unread clue badge");
                badge.transform.SetParent(notes.transform, false);
                badgeTransform = (RectTransform)badge.transform;
                badgeTransform.anchorMin = new Vector2(.88f, .72f);
                badgeTransform.anchorMax = new Vector2(.98f, 1.02f);
                badgeTransform.offsetMin = Vector2.zero;
                badgeTransform.offsetMax = Vector2.zero;
                badgeTransform.pivot = new Vector2(.5f, .5f);
            }

            var legacyImage = badgeTransform.GetComponent<Image>();
            if (legacyImage != null) Undo.DestroyObjectImmediate(legacyImage);
            var badgeText = badgeTransform.GetComponent<Text>();
            if (badgeText == null) badgeText = Undo.AddComponent<Text>(badgeTransform.gameObject);
            badgeText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            badgeText.text = "●";
            badgeText.fontSize = 32;
            badgeText.fontStyle = FontStyle.Bold;
            badgeText.alignment = TextAnchor.MiddleCenter;
            badgeText.color = new Color(.92f, .13f, .16f, 1f);
            badgeText.raycastTarget = false;

            var indicatorProperties = new SerializedObject(indicator);
            indicatorProperties.FindProperty("unreadBadge").objectReferenceValue = badgeTransform.gameObject;
            indicatorProperties.FindProperty("animatedTarget").objectReferenceValue = notes.transform;
            indicatorProperties.FindProperty("notesGraphic").objectReferenceValue = notes.GetComponent<Image>();
            indicatorProperties.ApplyModifiedPropertiesWithoutUndo();

            var flowProperties = new SerializedObject(flow);
            flowProperties.FindProperty("showClueCounter").boolValue = true;
            flowProperties.FindProperty("showChapterLabel").boolValue = true;
            flowProperties.FindProperty("clueCounter").objectReferenceValue = clueCounter;
            flowProperties.FindProperty("chapterLabel").objectReferenceValue = chapter;
            flowProperties.FindProperty("notesClueIndicator").objectReferenceValue = indicator;
            flowProperties.ApplyModifiedPropertiesWithoutUndo();

            badgeTransform.gameObject.SetActive(false);
            EditorUtility.SetDirty(flow);
            EditorUtility.SetDirty(indicator);
            EditorSceneManager.MarkSceneDirty(flow.gameObject.scene);
            EditorSceneManager.SaveScene(flow.gameObject.scene);
            Debug.Log("BTL: Authored Inspector HUD toggles and unread-clue badge. Existing layout preserved.");
        }

        // Repairs serialized import references only. Never creates UI or overwrites layout.
        [MenuItem("Tools/Between the Lines/Connect Imported References")]
        public static void ConnectReferences()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().path != ScenePath)
                throw new InvalidOperationException("Open BetweenTheLines.unity first.");
            var project = AssetDatabase.LoadAssetAtPath<YarnProject>(StoryPath);
            if (project == null || project.Program == null) throw new InvalidOperationException("Yarn Project did not compile.");
            var runner = UnityEngine.Object.FindFirstObjectByType<DialogueRunner>();
            Undo.RecordObject(runner, "Connect imported Yarn project");
            runner.SetProject(project);
            EditorUtility.SetDirty(runner);
            foreach (var character in UnityEngine.Object.FindObjectsByType<CharacterPortrait>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var name = character.transform.Find("Name");
                if (name == null) throw new InvalidOperationException("Missing authored Name child under " + character.name);
                var characterProperties = new SerializedObject(character);
                characterProperties.FindProperty("nameLabel").objectReferenceValue = name.gameObject;
                characterProperties.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(character);
            }
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            int repairedFonts = 0;
            foreach (var label in UnityEngine.Object.FindObjectsByType<Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (label.font != null) continue;
                Undo.RecordObject(label, "Connect built-in font");
                label.font = font;
                EditorUtility.SetDirty(label);
                repairedFonts++;
            }
            EditorSceneManager.MarkSceneDirty(runner.gameObject.scene);
            EditorSceneManager.SaveScene(runner.gameObject.scene);
            Debug.Log($"BTL: Connected Yarn project ({project.NodeNames.Length} nodes); repaired {repairedFonts} fonts. Scene saved. No UI was generated.");
            ValidateReferences();
        }

        [MenuItem("Tools/Between the Lines/Validate Scene References")]
        public static void ValidateReferences()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (scene.path != ScenePath) throw new InvalidOperationException("Open BetweenTheLines.unity first.");
            int objects = 0;
            foreach (var root in scene.GetRootGameObjects())
            foreach (var transform in root.GetComponentsInChildren<Transform>(true))
            {
                objects++;
                if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject) != 0)
                    throw new InvalidOperationException("Missing script on " + transform.name);
                foreach (var component in transform.GetComponents<MonoBehaviour>())
                {
                    if (!(component is NovelFlow) && !(component is NovelPresenter) && !(component is CharacterPortrait)) continue;
                    var so = new SerializedObject(component);
                    var property = so.GetIterator();
                    while (property.NextVisible(true))
                        if (property.propertyType == SerializedPropertyType.ObjectReference && property.objectReferenceValue == null)
                            throw new InvalidOperationException("Unassigned reference: " + component.GetType().Name + "." + property.propertyPath);
                }
            }
            foreach (var label in UnityEngine.Object.FindObjectsByType<Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (label.font == null) throw new InvalidOperationException("Missing font: " + label.name);
            var runner = UnityEngine.Object.FindFirstObjectByType<DialogueRunner>();
            if (runner.YarnProject == null || runner.YarnProject.Program == null) throw new InvalidOperationException("Missing compiled Yarn project.");
            Debug.Log($"BTL: Scene references PASS. {objects} authored GameObjects; all gameplay references and fonts assigned.");
        }

        internal static int Select(string node, int scenario)
        {
            if (scenario == 1 && node == "fear1") return 1;
            if (scenario == 2 && node == "sadness2") return 1;
            if (scenario == 3 && node == "angerIdentification") return 1;
            if (scenario == 4 && node == "anger2") return 1;
            if (scenario == 5 && node == "finalDeduction") return 0;
            if (scenario == 6 && new[] { "fear1", "sadness1", "anger1" }.Contains(node)) return 1;
            if (node == "fearIdentification") return 1;
            if (node == "sadnessIdentification" || node == "finalDeduction") return 2;
            return 0;
        }

        internal static readonly string[] CaseNames = {
            "Perfect run", "Fail first Fear / skip F2", "Pass S1 / fail S2", "All clues / wrong Anger",
            "Five clues / correct final", "Wrong final", "All first clues missed"
        };
        internal static readonly int[] ExpectedCounts = { 6, 4, 5, 6, 5, 6, 0 };

        [MenuItem("Tools/Between the Lines/Run Story Acceptance Tests")]
        public static void TestStory()
        {
            var project = AssetDatabase.LoadAssetAtPath<YarnProject>(StoryPath);
            if (project == null || project.Program == null) throw new InvalidOperationException("Yarn must compile first.");
            var results = new List<string>();
            for (int scenario = 0; scenario < CaseNames.Length; scenario++)
            {
                var store = new Yarn.MemoryVariableStore();
                var dialogue = new Yarn.Dialogue(store);
                {
                    dialogue.SetProgram(project.Program);
                    string node = "";
                    string ending = "";
                    var visited = new HashSet<string>();
                    dialogue.NodeStartHandler = name => { node = name; visited.Add(name); };
                    dialogue.LineHandler = line => { };
                    dialogue.CommandHandler = command => {
                        if (command.Text.StartsWith("ending ")) ending = command.Text.Contains("good") ? "good" : "bad";
                    };
                    dialogue.OptionsHandler = options => {
                        if (options.Options.Length != 3) throw new InvalidOperationException("Option count at " + node);
                        dialogue.SetSelectedOption(options.Options[Select(node, scenario)].ID);
                    };
                    dialogue.SetNode("opening");
                    int steps = 0;
                    do { dialogue.Continue(); if (++steps > 1000) throw new InvalidOperationException("Story loop"); }
                    while (dialogue.IsActive);
                    int count = 0;
                    foreach (string key in new[] { "$fear1", "$fear2", "$sadness1", "$sadness2", "$anger1", "$anger2" })
                        if (store.TryGetValue(key, out bool value) && value) count++;
                    if (count != ExpectedCounts[scenario] || ending != (scenario == 0 ? "good" : "bad"))
                        throw new InvalidOperationException(CaseNames[scenario] + $": unexpected {ending}, {count} clues");
                    if (scenario == 1 && visited.Contains("fear2")) throw new InvalidOperationException("F2 gating failed");
                    if (scenario == 6 && new[] { "fear2", "sadness2", "anger2" }.Any(visited.Contains))
                        throw new InvalidOperationException("First clue gating failed");
                    results.Add("PASS: " + CaseNames[scenario] + $" ({count}/6, {ending})");
                }
            }
            Directory.CreateDirectory("Docs/Validation");
            File.WriteAllLines("Docs/Validation/StoryAcceptance.txt", results);
            Debug.Log("BTL: 7/7 Yarn VM acceptance tests PASS.\n" + string.Join("\n", results));
        }
    }
}
