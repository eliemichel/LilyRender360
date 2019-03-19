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

using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/**
 * TODO: When using limited fov, detect which cubemap faces are not useful.
 * It is a bit tricky because of the stitching transform object.
 */
public class LilyRender360 : MonoBehaviour
{
    #region [Public Enumerations]

    public enum Format
    {
        PNG,
        EXR,
    };

    #endregion

    #region [Public Parameters]

    // Output options
    public int targetFramerate = 30;
    public Format format;
    public string prefix = "Recordings/";
    public int nDigits = 4;
    public bool overwriteFile = false;

    public int width = 1024;
    public bool enableHeight = false;
    public int height = 1024;
    public int startFrame = 0;
    public bool enableEndFrame = false;
    public int endFrame = -1;

    // Projection options
    public float horizontalFov = 360;
    public float verticalFov = 180;

    // Stitching options
    public float overlap = 0.5f;
    public Transform stitchingOrientation; // can be null
    public bool showStitchLines; // only taken into account at start

    // Advanced options
    public bool enableCubeFaceSize = false; // Use custom face size (otherwise auto-computed)
    public int cubeFaceSize = 512; // Cube face size used if enableCubeFaceSize is enabled
    public bool doubleRender = false; // twice as heavy, only taken into account at start
    public bool smoothStitching = true; // requires a non null margin, only taken into account at start

    #endregion

    #region [Private Enumerations]

    public enum CubeFace
    {
        PX, NX, PY, NY, PZ, NZ,
    };

    #endregion

    #region [Private Parameters]

    private Camera _cam;
    private Texture2D _tex; // Texture used to transfer render back from VRAM
    private int _frame = 0;

    private Material _equirectMat;
    private RenderTexture[] _faces;
    private RenderTexture _equirect;

    #endregion

    #region [Public API]

    public string AbsolutePrefix { get {
        string projectRoot = Path.GetDirectoryName(Application.dataPath).Replace(Path.DirectorySeparatorChar, '/') + "/";
        return Path.IsPathRooted(prefix) ? prefix : projectRoot + prefix;
    }}

    public int MaxFrame { get {
        return (int)Mathf.Min(enableEndFrame && endFrame > -1 ? endFrame : Mathf.Infinity, Mathf.Pow(10, nDigits) - 1);
    }}

    public string AbsoluteFramePath(int frame) {
        return string.Format("{0}{1:D" + nDigits.ToString() + "}.{2}", AbsolutePrefix, frame, format == Format.EXR ? "exr" : "png");
    }

    // width equivalent if the output were 360 deg wide
    public float FullWidth { get {
        float w = width * 360 / horizontalFov;
        return Mathf.Min(w, 8 * width); // security
    } }

    public float SuggestedHeight { get {
            float fullHeight = FullWidth / 2;
            return fullHeight * verticalFov / 180;
    } }

    // Called at start, update, and also from the custom inspector
    public void ChechParameters()
    {
        if (!enableHeight)
        {
            height = (int)SuggestedHeight;
        }

        if (!enableCubeFaceSize)
        {
            cubeFaceSize = (int)(FullWidth / 4 * (1 + overlap * 2));
        }
    }

    #endregion

    #region [Internal routines]

    void InitCubemap()
    {
        int nFaces = doubleRender ? 12 : 6;
        _faces = new RenderTexture[nFaces];
        for (int i = 0; i < nFaces; ++i)
        {
            _faces[i] = new RenderTexture(cubeFaceSize, cubeFaceSize, 24, format == Format.PNG ? RenderTextureFormat.ARGB32 : RenderTextureFormat.ARGBFloat);
        }

        _equirect = new RenderTexture(width, height, 24, format == Format.PNG ? RenderTextureFormat.ARGB32 : RenderTextureFormat.ARGBFloat);
    }

    RenderTexture Face(CubeFace face, int cube = 0)
    {
        return _faces[(int)face + cube * 6];
    }

    void RenderCubemap(int cube = 0)
    {
        // Save camera settings
        float currentFov = _cam.fieldOfView;
        Quaternion cameraOrientation = _cam.transform.rotation;
        RenderTexture currentTex = _cam.targetTexture;
        
        if (stitchingOrientation != null)
        {
            _cam.transform.rotation = stitchingOrientation.rotation;
        }

        if (cube == 1)
        {
            _cam.transform.Rotate(-45, 45, 0);
        }
        
        _cam.fieldOfView = 2 * Mathf.Atan(1 + overlap) / Mathf.PI * 180;
        
        // Render front (+X)
        _cam.targetTexture = Face(CubeFace.PX, cube);
        _cam.transform.Rotate(0, 90, 0);
        _cam.Render();
        
        // Render right (-Z)
        _cam.targetTexture = Face(CubeFace.NZ, cube);
        _cam.transform.Rotate(0, 90, 0);
        _cam.Render();

        // Render back (-X)
        _cam.targetTexture = Face(CubeFace.NX, cube);
        _cam.transform.Rotate(0, 90, 0);
        _cam.Render();

        // Render left (+Z)
        _cam.targetTexture = Face(CubeFace.PZ, cube);
        _cam.transform.Rotate(0, 90, 0);
        _cam.Render();

        // Render up (+Y)
        _cam.targetTexture = Face(CubeFace.PY, cube);
        _cam.transform.Rotate(90, 0, 0);
        _cam.Render();

        // Render down (-Y)
        _cam.targetTexture = Face(CubeFace.NY, cube);
        _cam.transform.Rotate(180, 0, 0);
        _cam.Render();

        // Restore camera settings
        _cam.fieldOfView = currentFov;
        _cam.transform.rotation = cameraOrientation;
        _cam.targetTexture = currentTex;
    }

    void ConvertToEquirect()
    {
        Matrix4x4 orientMatrix = Matrix4x4.identity;
        if (stitchingOrientation != null)
        {
            Matrix4x4 tr = _cam.transform.worldToLocalMatrix * stitchingOrientation.localToWorldMatrix;
            Vector3 euler = tr.rotation.eulerAngles;
            Quaternion q = Quaternion.identity;
            q *= Quaternion.AngleAxis(euler.z, Vector3.forward);
            q *= Quaternion.AngleAxis(euler.x, Vector3.right);
            q *= Quaternion.AngleAxis(-euler.y, Vector3.up);
            orientMatrix = Matrix4x4.Rotate(q);
        }
        
        _equirectMat.SetTexture("_FaceTexPX", Face(CubeFace.PX, 0));
        _equirectMat.SetTexture("_FaceTexNX", Face(CubeFace.NX, 0));
        _equirectMat.SetTexture("_FaceTexPY", Face(CubeFace.PY, 0));
        _equirectMat.SetTexture("_FaceTexNY", Face(CubeFace.NY, 0));
        _equirectMat.SetTexture("_FaceTexPZ", Face(CubeFace.PZ, 0));
        _equirectMat.SetTexture("_FaceTexNZ", Face(CubeFace.NZ, 0));

        _equirectMat.SetMatrix("_OrientMatrix", orientMatrix);
        
        _equirectMat.SetFloat("_Beta", 1 / (1 + overlap));

        _equirectMat.SetFloat("_HorizontalFov", horizontalFov * Mathf.Deg2Rad);
        _equirectMat.SetFloat("_VerticalFov", verticalFov * Mathf.Deg2Rad);

        if (doubleRender)
        {
            _equirectMat.SetTexture("_FaceTexPX2", Face(CubeFace.PX, 1));
            _equirectMat.SetTexture("_FaceTexNX2", Face(CubeFace.NX, 1));
            _equirectMat.SetTexture("_FaceTexPY2", Face(CubeFace.PY, 1));
            _equirectMat.SetTexture("_FaceTexNY2", Face(CubeFace.NY, 1));
            _equirectMat.SetTexture("_FaceTexPZ2", Face(CubeFace.PZ, 1));
            _equirectMat.SetTexture("_FaceTexNZ2", Face(CubeFace.NZ, 1));

            Matrix4x4 rot = Matrix4x4.Rotate(Quaternion.Euler(45, 45, 0)).inverse;
            _equirectMat.SetMatrix("_OrientMatrix2", rot * orientMatrix);
        }
        
        Graphics.Blit(null, _equirect, _equirectMat);
    }

    #endregion

    #region [MonoBehavior]

    void Start()
    {
        ChechParameters();
        InitCubemap();
        _cam = GetComponent<Camera>();
        _tex = new Texture2D(_equirect.width, _equirect.height, format == Format.EXR ? TextureFormat.RGBAFloat : TextureFormat.RGB24, false);
        Time.maximumDeltaTime = (1.0f / targetFramerate);
        Time.captureFramerate = targetFramerate;
        
        _equirectMat = new Material(Shader.Find("Hidden/LilyRender/Equirectangular"));
        _equirectMat.EnableKeyword("ORIENT_CUBE");
        if (showStitchLines)
        {
            _equirectMat.EnableKeyword("SHOW_STITCH_LINES");
        }
        if (doubleRender)
        {
            _equirectMat.EnableKeyword("TWO_CUBES");
        }
        if (smoothStitching && overlap > 0)
        {
            _equirectMat.EnableKeyword("SMOOTH_STITCHING");
        }

        Directory.CreateDirectory(Path.GetDirectoryName(AbsoluteFramePath(0)));
    }
    
    void LateUpdate()
    {
        ChechParameters();

        if (_frame >= startFrame)
        {
            string filename = AbsoluteFramePath(_frame);
            if (File.Exists(filename) && !overwriteFile)
            {
                Debug.LogWarning("File '" + filename + "' already exists. Skipping frame (check 'Override' to force overriding existing files).");
            }
            else
            {
                RenderCubemap(0);
                if (doubleRender)
                {
                    RenderCubemap(1);
                }
                ConvertToEquirect();

                RenderTexture.active = _equirect;
                _tex.ReadPixels(new Rect(0, 0, _equirect.width, _equirect.height), 0, 0);
                byte[] data = format == Format.EXR ? _tex.EncodeToEXR(Texture2D.EXRFlags.CompressZIP) : _tex.EncodeToPNG();
                RenderTexture.active = null;
                File.WriteAllBytes(filename, data);
            }
        }

        _frame++;

        if (_frame > MaxFrame)
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }

    #endregion
}
