//The following is a modified version of: https://gist.github.com/LotteMakesStuff/8534e01043826754344a570a4cf21002

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.LowLevel;

public class PlayerLoopWindow : EditorWindow
{
    [MenuItem("Window/Player Loop Visualizer")]
    static void Init()
    {
        // Get existing open window or if none, make a new one:
        PlayerLoopWindow window = (PlayerLoopWindow)GetWindow(typeof(PlayerLoopWindow));
        window.titleContent = new GUIContent("PlayerLoop");
        window.Show();
    }

    private void OnEnable()
    {
        autoRepaintOnSceneChange = true;
    }

    private static PlayerLoopSystem currentPlayerLoop = new PlayerLoopSystem();
    Vector2 scroll;

    void OnGUI()
    {
        GUILayout.Label("Player Loop Visualizer", EditorStyles.boldLabel);

        // Check to see if we need to initialize the PlayerLoopSystem. 
        if (currentPlayerLoop.subSystemList == null)
        {
            // Grab current loop
            currentPlayerLoop = PlayerLoop.GetCurrentPlayerLoop();
        }

        // Draw the entier list out in a scrollable area (it gets really big!)
        scroll = EditorGUILayout.BeginScrollView(scroll, GUIStyle.none, GUI.skin.verticalScrollbar);
        foreach (var loopSystem in currentPlayerLoop.subSystemList)
        {
            DrawSubsystemList(loopSystem, 0);
        }
        EditorGUILayout.EndScrollView();
    }

    private Stack<string> pathStack = new Stack<string>();
    private Stack<PlayerLoopSystem> systemStack = new Stack<PlayerLoopSystem>();
    void DrawSubsystemList(PlayerLoopSystem system, int increment = 1)
    {
        // here were using a stack to generate a path name for the PlayerLoopSystem were currently trying to draw
        // e.g Update.ScriptRunBehaviourUpdate. Unity uses these path names when storing profiler data on a step
        // that means  we can use these path names to retrieve profiler samples!
        if (pathStack.Count == 0)
        {
            // if this is a root object, add its name to the stack
            pathStack.Push(system.type.Name);
        }
        else
        {
            // otherwise add its name to its parents name...
            pathStack.Push(pathStack.Peek() + "." + system.type.Name);
        }

        using (new EditorGUI.IndentLevelScope(increment))
        {
            // if this System has Subsystems, draw a foldout
            bool header = system.subSystemList != null;
            if (header)
            {
                var name = system.type.Name; var fullName = system.type.FullName;
                // check fold

                EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
                bool fold = EditorGUILayout.Foldout(GetFoldout(fullName), name, true); // use the GetFoldout helper method to see if its open or closed
                EditorGUILayout.EndHorizontal();

                if (fold)
                {
                    // if the fold is open, draw all the Subsytems~
                    foreach (var loopSystem in system.subSystemList)
                    {
                        // store the current system Useful if we need to know the parent of a system later
                        systemStack.Push(system);
                        DrawSubsystemList(loopSystem);
                        systemStack.Pop();
                    }
                }

                SetFoldout(fullName, fold);
            }
            else
            {
                // at the moment, all the defaut 'native' Systems update via a updateFunction (essentally, a pointer into the unmanaged C++ side of the engine.
                // So we can tell if a system is a custom one because it has a value in updateDelegate instead. So if this is a custom system, make note of that
                // so we can change how its drawn later
                bool custom = system.updateDelegate != null;
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space((float)EditorGUI.indentLevel * 18f); // indent the entry nicley. We have to do this manually cos the flexible space at the end conflicts with GUI.Indent

                string sourceText = custom ? "Custom" : "Native";
                string labelText = $"{system.type.Name} ({sourceText})";

                GUILayout.Label(labelText); // draw the name out....

                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
                //EditorGUILayout.LabelField(new GUIContent(/*"custom"*/));//, EditorGUIUtility.IconContent("cs Script Icon"));
            }
        }

        pathStack.Pop();
    }

    public bool GetFoldout(string key)
    {
        return EditorPrefs.GetBool("PlayerLoopWin_Foldout_" + key, false);
    }

    public void SetFoldout(string key, bool value)
    {
        EditorPrefs.SetBool("PlayerLoopWin_Foldout_" + key, value);
    }
}