#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;


/// <summary>
/// A debugger to analyse and organize large Player.log files
/// </summary>
/// <remarks>
/// Written and Owned by Jake Rose exclusively with rights to use distributed on a per-person basis
/// Distribution of this without permission I will sue your ass (jk but please Im proud of this dont do that thanks fam)
/// </remarks>

public class JakesDebugWindow : EditorWindow
{
    [Serializable]
    public struct ErrorStack
    {
        public Error m_Error;
        public int count;
    }
    [Serializable]
    public struct Error
    {
        public string m_Filename;
        public string[] m_StackInfo;
    }

    public List<ErrorStack> m_Errors = new List<ErrorStack>();

    public Vector2 m_ErrorScrollPosition = Vector2.zero;
    public Vector2 m_StackInfoScrollPosition = Vector2.zero;
    public int m_CurrentErrorIndex = -1;

    //Async shit
    private Task readingFileTask = null;
    private bool cancelTask = false;

    //Opens Menu
    [MenuItem("Window/Analysis/Jake's Log Debugger")]
    public static void ShowWindow()
    {
        EditorWindow.GetWindow<JakesDebugWindow>("Log Debugger");
    }

    //Drag and Drop from Unity Answers: 
    //https://answers.unity.com/questions/1548292/gui-editor-drag-and-drop-inspector.html
    private void OnGUI()
    {
        Rect myRect;
        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.border = new RectOffset(4, 4, 4, 4);
        buttonStyle.fontSize = 32;
        EditorGUILayout.BeginHorizontal();
        if (m_Errors.Count == 0)
        {
            myRect = GUILayoutUtility.GetRect(position.width - 20, 50);
        }
        else
        {
            myRect = GUILayoutUtility.GetRect((position.width / 2) - 10, 50);
            Rect resetRect = GUILayoutUtility.GetRect((position.width / 2) - 10, 50);
            GUI.backgroundColor = Color.Lerp(Color.red, Color.white, 0.4f);
            if (GUI.Button(resetRect, "Reset Data", buttonStyle))
            {
                ResetData();
            }
            GUI.backgroundColor = Color.white; //reset color when done
        }
        GUI.backgroundColor = Color.Lerp(Color.cyan, Color.white, 0.4f);
        if (GUI.Button(myRect, "Open .log File", buttonStyle))
        {
            string path = EditorUtility.OpenFilePanel("Load .log file", System.Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "*");
            if(path.Length != 0)
            {
                ResetData();
                if (readingFileTask != null && !readingFileTask.IsCompleted)
                {
                    cancelTask = true;
                    readingFileTask.Wait();
                }
                readingFileTask = ReadFilesAsync(new string[] { path });
            }
        }
        GUI.backgroundColor = Color.white; //reset color when done
        EditorGUILayout.EndHorizontal();
        

        //Drag and Drop stuff here
        if (myRect.Contains(Event.current.mousePosition))
        {
            if (Event.current.type == EventType.DragUpdated)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                Debug.Log("Drag Updated!");
                Event.current.Use();
            }
            else if (Event.current.type == EventType.DragPerform)
            {
                Debug.Log("Drag Perform!");
                Debug.Log(DragAndDrop.paths.Length + " debug files");
                ResetData();
                if(readingFileTask != null && !readingFileTask.IsCompleted)
                {
                    cancelTask = true;
                    readingFileTask.Wait();
                }
                readingFileTask = ReadFilesAsync(DragAndDrop.paths);
                Event.current.Use();
            }
        }
        
        //What to draw when done
        if (readingFileTask == null)
        {
            EditorGUILayout.BeginHorizontal();
            //add filter buttons
            EditorGUILayout.EndHorizontal();
            m_ErrorScrollPosition = EditorGUILayout.BeginScrollView(m_ErrorScrollPosition, false, true, GUILayout.Height((position.height / 2) - myRect.height), GUILayout.Width(position.width));
            for (int i = 0; i < m_Errors.Count; i++)
            {
                GUIStyle errorButtonStyle = new GUIStyle(GUI.skin.button);
                if (m_Errors[i].m_Error.m_StackInfo.Length > 4)
                {
                    if (m_Errors[i].m_Error.m_StackInfo[4].Contains("LogError(Object)"))
                    {
                        GUI.backgroundColor = Color.Lerp(Color.red, Color.white, 0.4f);
                    }
                    else if (m_Errors[i].m_Error.m_StackInfo[4].Contains("LogWarning(Object)"))
                    {
                        GUI.backgroundColor = Color.Lerp(Color.yellow, Color.white, 0.4f);
                    }
                    else if (m_Errors[i].m_Error.m_StackInfo[4].Contains("Log(Object)"))
                    {
                        GUI.backgroundColor = Color.Lerp(Color.green, Color.white, 0.4f);
                    }
                }
                if(i == m_CurrentErrorIndex)
                {
                    GUI.backgroundColor = Color.Lerp(GUI.backgroundColor, Color.black, 0.4f);
                }
                
                
                if (GUILayout.Button("[" + m_Errors[i].count + "] " + m_Errors[i].m_Error.m_StackInfo[0], errorButtonStyle, GUILayout.Width(position.width - 20), GUILayout.Height(40)))
                {
                    m_CurrentErrorIndex = i;
                }
                GUI.backgroundColor = Color.white;
            }
            EditorGUILayout.EndScrollView();

            m_StackInfoScrollPosition = EditorGUILayout.BeginScrollView(m_StackInfoScrollPosition, GUILayout.Height((position.height - (myRect.height + 50)) / 2), GUILayout.Width(position.width));
            if (m_CurrentErrorIndex >= 0 && m_CurrentErrorIndex < m_Errors.Count)
            {
                for (int i = 0; i < m_Errors[m_CurrentErrorIndex].m_Error.m_StackInfo.Length; i++)
                {
                    string labelText = m_Errors[m_CurrentErrorIndex].m_Error.m_StackInfo[i];

                    Vector2 size = GUI.skin.label.CalcSize(new GUIContent(labelText));
                    GUILayout.Label(labelText, GUILayout.Width(size.x), GUILayout.Height(size.y));

                    /* todo possibly detect files and open them in text editor like in Unity
                    try
                    {
                        string typeText = labelText.Substring(0, labelText.IndexOf(':')-1);
                        throw new NotImplementedException();
                    }
                    catch(Exception e)
                    {
                        Vector2 size = GUI.skin.label.CalcSize(new GUIContent(labelText));
                        GUILayout.Label(labelText, GUILayout.Width(size.x), GUILayout.Height(size.y));
                    }
                    */
                }
            }
            EditorGUILayout.EndScrollView();
        }
        else //if still running the readLog Task
        {
            EditorGUILayout.BeginHorizontal();
            Rect progressRect = GUILayoutUtility.GetRect(position.width / 2, 50);
            EditorGUI.ProgressBar(progressRect, (float)m_CurrentLine / m_TotalLines, m_CurrentLine.ToString() + " / " + m_TotalLines.ToString());
            //EditorGUILayout.LabelField("Loading..." + m_CurrentLine.ToString() + "/" + m_TotalLines.ToString(), GUILayout.Width(position.width / 2));
            if(GUILayout.Button("Cancel", GUILayout.Width(position.width / 2), GUILayout.Height(50)))
            {
                if (readingFileTask != null && !readingFileTask.IsCompleted)
                {
                    cancelTask = true;
                    readingFileTask.Wait();
                }
            }
            EditorGUILayout.EndHorizontal();
            if(readingFileTask.IsCompleted)
            {
                readingFileTask = null;
            }
            Repaint();
        }
        
    }

    public void ResetData()
    {
        m_Errors.Clear();
    }

    public void ReadFiles(string[] filePaths)
    {
        for(int i = 0; i < filePaths.Length; i++)
        {
            ReadFile(filePaths[i]);
        }    
    }

    public Task ReadFilesAsync(string[] filePaths)
    {
        return Task.Run(() => ReadFiles(filePaths));
    }

    public void AddError(Error e)
    {
        for (int i = 0; i < m_Errors.Count; i++)
        {
            bool isUnique = true;
            if (m_Errors[i].m_Error.m_StackInfo.Length == e.m_StackInfo.Length)
            {
                bool stacksEqual = true;
                for (int j = 0; j < e.m_StackInfo.Length; j++)
                {
                    if (m_Errors[i].m_Error.m_StackInfo[j] != e.m_StackInfo[j])
                    {
                        stacksEqual = false;
                        break;
                    }
                }
                isUnique = !stacksEqual;
            }
            else
            {
                isUnique = true;
            }
            if (!isUnique)
            {
                ErrorStack es = m_Errors[i];
                es.count = es.count + 1;
                m_Errors[i] = es;
                return;
            }
        }
        //if here then we are unique
        ErrorStack errorStack = new ErrorStack();
        errorStack.count = 1;
        errorStack.m_Error = e;
        m_Errors.Add(errorStack);

    }

    int m_CurrentLine = 0;
    int m_TotalLines = 0;
    public void ReadFile(string filePath)
    {
        m_CurrentLine = 0;
        List<Error> errors = new List<Error>();
        string[] lines = File.ReadAllLines(filePath);
        m_TotalLines = lines.Length;
        for( ; m_CurrentLine < m_TotalLines; m_CurrentLine++)
        {
            if(cancelTask)
            {
                return;
            }
            const string newErrorCommonString = "Filename: ";
            if(lines[m_CurrentLine].Contains(newErrorCommonString))
            {
                Error e = new Error();
                e.m_Filename = lines[m_CurrentLine].Substring(newErrorCommonString.Length, lines[m_CurrentLine].Length - newErrorCommonString.Length -1); //wow, didnt know you could do this but hopefully more readable
                List<string> stackInfo = new List<string>();
                m_CurrentLine++;
                while(!lines[m_CurrentLine].Contains(newErrorCommonString))
                {
                    if (lines[m_CurrentLine] != "")
                    {
                        stackInfo.Add(lines[m_CurrentLine]);
                    }
                    m_CurrentLine++;
                }
                e.m_StackInfo = stackInfo.ToArray();
                AddError(e);
                //read line by line 
            }
        }
    }
}
#endif