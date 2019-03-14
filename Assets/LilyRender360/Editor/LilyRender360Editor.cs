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

[CustomEditor(typeof(LilyRender360))]
public class LilyRender360Editor : Editor
{
    bool showAdvanced = false;

    LilyRender360 cont;

    #region [Texts]
    GUIContent prefixLabel = new GUIContent(
        "Output Directory",
        "Absolute path, or path relative to the project root. If it does not end with a slash, the last part is used as filename prefix."
    );
    GUIContent nDigitsLabel = new GUIContent(
        "Digits",
        "Number of digits in the output file name. This limits the maximum number of frames."
    );
    GUIContent formatLabel = new GUIContent(
        "Format",
        "File format used for output. PNG files are 8 bit sRGB. EXR are 32 bit Linear."
    );
    GUIContent widthLabel = new GUIContent(
        "Width",
        "Width of the output frames. The height will always be half of it."
    );
    GUIContent heightLabel = new GUIContent(
        "Height",
        "Height of the output frames. If disabled, the height is auto-computed from the width and FoV parameters."
    );
    GUIContent framerateLabel = new GUIContent(
        "Framerate",
        "Number of frames per second in the target render. If the game cannot run at this frame rate, it will be slowed down, so that no frame is dropped."
    );
    GUIContent overwriteLabel = new GUIContent(
        "Overwrite Existing Files",
        "If the output file already exists, overwrite it or not. If not, the frame is not rendered and a warning is logged in the console."
    );
    GUIContent startFrameLabel = new GUIContent(
        "Start Frame",
        "Start rendering from this frame on."
    );
    GUIContent endFrameLabel = new GUIContent(
        "End Frame",
        "Stop the game once this frame has been rendered."
    );
    GUIContent horizontalFovLabel = new GUIContent(
        "Horizontal Field of View",
        "Range of angles covered by the render on the horizontal axis."
    );
    GUIContent verticalFovLabel = new GUIContent(
        "Vertical Field of View",
        "Range of angles covered by the render on the vertical axis."
    );
    GUIContent overlapLabel = new GUIContent(
        "Overlap",
        "Increase this to smooth the stitching. Render faces with a wider FoV, so that they overlap, to account for non local post-effects. This is expressed in percentage of the face size."
    );
    GUIContent orientationLabel = new GUIContent(
        "Stitching Orientation",
        "By default, the intermediate cube of renders is aligned to the camera axes. If this transform is specified, its local axes will be used instead. You may or may not want to parent this transform to the camera."
    );
    GUIContent showLinesLabel = new GUIContent(
        "Show Stitch Lines",
        "Use this for debug, to visualize where cube faces boundaries are."
    );
    GUIContent faceSizeLabel = new GUIContent(
        "Custom Cube Face Size",
        "Size in pixels of the intermediate textures rendered in each of the six directions. Auto-computed given output width and stitching overlap. You may want to snap this value to a power of two for more efficiency."
    );
    GUIContent doubleRenderLabel = new GUIContent(
        "Double render",
        "Render the cubemap twice, with a rotation of the stitching lines, then merge them to minimize stitch lines artifacts. Does not gain so much compared to increasing the overlap."
    );
    GUIContent smoothStitchingLabel = new GUIContent(
        "Smooth Stitching",
        "Merge the faces where they overlap, to minimize stitch lines artifacts. This must be used in conjunction with Overlap."
    );
    #endregion

    private string[] presetNames = { "4K", "2K", "1K" };
    private int[] presetValues = { 4096, 2048, 1024 };

    void OutputPanel()
    {
        cont.prefix = LilyGUI.DirnameField(prefixLabel, cont.prefix);
        cont.format = (LilyRender360.Format)EditorGUILayout.EnumPopup(formatLabel, cont.format);
        cont.nDigits = EditorGUILayout.IntSlider(nDigitsLabel, cont.nDigits, 1, 7);

        // Int field with presets for output width
        EditorGUILayout.BeginHorizontal();
        cont.width = EditorGUILayout.IntField(widthLabel, cont.width);
        GUILayout.Space(-5);
        int i = EditorGUILayout.Popup(-1, presetNames, GUILayout.MaxWidth(15));
        if (i > -1) cont.width = presetValues[i];
        EditorGUILayout.EndHorizontal();

        LilyGUI.OptionalIntField(heightLabel, ref cont.enableHeight, ref cont.height);
        
        cont.overwriteFile = GUILayout.Toggle(cont.overwriteFile, overwriteLabel);
    }

    void TimePanel()
    {
        cont.targetFramerate = EditorGUILayout.IntField(framerateLabel, cont.targetFramerate);
        cont.startFrame = EditorGUILayout.IntField(startFrameLabel, cont.startFrame);
        LilyGUI.OptionalIntField(endFrameLabel, ref cont.enableEndFrame, ref cont.endFrame);
    }

    void ProjectionPanel()
    {
        cont.horizontalFov = EditorGUILayout.Slider(horizontalFovLabel, cont.horizontalFov, 0, 360);
        cont.verticalFov = EditorGUILayout.Slider(verticalFovLabel, cont.verticalFov, 0, 180);
    }

    void StitchingPanel()
    {
        cont.overlap = LilyGUI.PercentageField(overlapLabel, cont.overlap);
        cont.stitchingOrientation = (Transform)EditorGUILayout.ObjectField(orientationLabel, cont.stitchingOrientation, typeof(Transform), true);
        cont.showStitchLines = GUILayout.Toggle(cont.showStitchLines, showLinesLabel);

        showAdvanced = LilyGUI.BeginFoldout(showAdvanced, "Advanced");
        if (showAdvanced)
        {
            LilyGUI.OptionalIntField(faceSizeLabel, ref cont.enableCubeFaceSize, ref cont.cubeFaceSize);

            cont.doubleRender = GUILayout.Toggle(cont.doubleRender, doubleRenderLabel);
            EditorGUI.BeginDisabledGroup(cont.overlap == 0);
            cont.smoothStitching = GUILayout.Toggle(cont.smoothStitching, smoothStitchingLabel);
            EditorGUI.EndDisabledGroup();
        }
        LilyGUI.EndFoldout();
    }

    void InfoPanel()
    {
        string msg = "";
        msg += "Output file: " + cont.AbsoluteFramePath(0) + "\n";
        msg += "Number of renders: " + (cont.doubleRender ? 12 : 6).ToString() + " x " + cont.cubeFaceSize.ToString() + "px\n";
        msg += "Render from frame " + cont.startFrame + " to frame " + cont.MaxFrame;
        EditorGUILayout.HelpBox(msg, MessageType.Info);
    }

    public override void OnInspectorGUI()
    {
        cont = (LilyRender360)target;
        cont.ChechParameters();
        
        LilyGUI.BeginPanel("Output");
        OutputPanel();
        LilyGUI.EndPanel();

        LilyGUI.BeginPanel("Time");
        TimePanel();
        LilyGUI.EndPanel();

        LilyGUI.BeginPanel("Projection");
        ProjectionPanel();
        LilyGUI.EndPanel();

        LilyGUI.BeginPanel("Stitching");
        StitchingPanel();
        LilyGUI.EndPanel();
        
        InfoPanel();
    }
}
