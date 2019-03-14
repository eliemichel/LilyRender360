/**
 * Copyright (c) 2019 Elie Michel
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to
 * deal in the Software without restriction, including without limitation the
 * rights to use, copy, modify, merge, publish, distribute, sublicense, and/or
 * sell copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS
 * IN THE SOFTWARE.
 *
 * This file is part of Lily Render 360, a unity tool for equirectangular
 * rendering, available at https://github.com/eliemichel/LilyRender360
 */

using System.IO;
using UnityEngine;
using UnityEditor;

/**
 * Extra GUILayout utility functions
 */
public class LilyGUI
{
    public static float PercentageField(GUIContent label, float value)
    {
        EditorGUILayout.BeginHorizontal();
        value = EditorGUILayout.Slider(label, value * 100, 0, 100) / 100;
        GUILayout.Space(-20);
        GUILayout.Label("%", GUILayout.MaxWidth(20));
        EditorGUILayout.EndHorizontal();
        return value;
    }

    public static void OptionalIntField(GUIContent label, ref bool enable, ref int value)
    {
        EditorGUILayout.BeginHorizontal();
        enable = GUILayout.Toggle(enable, label);
        EditorGUI.BeginDisabledGroup(!enable);
        value = EditorGUILayout.IntField("", value);
        EditorGUI.EndDisabledGroup();
        EditorGUILayout.EndHorizontal();
    }

    public static void BeginPanel(string label)
    {
        GUIStyle headerStyle = new GUIStyle();
        headerStyle.fontStyle = FontStyle.Bold;
        GUILayout.Label(label, headerStyle);
        BeginIndent(10);
    }

    public static void EndPanel(bool spaceAfter = true)
    {
        EndIndent();
        if (spaceAfter)
        {
            EditorGUILayout.Space();
        }
    }

    public static void BeginIndent(int space)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Space(space);
        GUILayout.BeginVertical();
    }

    public static void EndIndent()
    {
        GUILayout.EndVertical();
        GUILayout.EndHorizontal();
    }
    
    public static bool BeginFoldout(bool show, string label)
    {
        LilyGUI.BeginIndent(10);
        show = EditorGUILayout.Foldout(show, label);
        LilyGUI.BeginIndent(10);
        return show;
    }

    public static void EndFoldout()
    {
        LilyGUI.EndIndent();
        LilyGUI.EndIndent();
    }

    public static string DirnameField(GUIContent label, string value)
    {
        EditorGUILayout.BeginHorizontal();
        value = EditorGUILayout.TextField(label, value);
        GUIStyle buttonStyle = new GUIStyle(EditorStyles.miniButtonRight);
        buttonStyle.stretchWidth = false;
        buttonStyle.padding.bottom += 1;
        GUILayout.Space(-5);
        if (GUILayout.Button("...", buttonStyle))
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath).Replace(Path.DirectorySeparatorChar, '/') + "/"; // This should be a parameter
            string absPath = Path.IsPathRooted(value) ? value : projectRoot + value;
            string path = EditorUtility.OpenFolderPanel("Output directory", absPath, "") + "/";
            if (path != null)
            {
                if (path.StartsWith(projectRoot))
                {
                    path = path.Substring(projectRoot.Length);
                }
                value = path;
            }
        }
        EditorGUILayout.EndHorizontal();
        return value;
    }
}
