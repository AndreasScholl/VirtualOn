using ModelBuilder;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VirtualOnData;

public class Importer : MonoBehaviour
{
    const int HWRAM = 0x06000000;
    const int LWRAM = 0x00200000;

    public MemoryManager _dataHW;
    public MemoryManager _dataLW;

    public string _virtualOnFolderPath = "V:/";
    public int _subDivide = 2;

    public bool _debugOutput = true;

    public Material _opaqueMaterial = null;
    public Material _transparentMaterial = null;

    void Start()
    {
        _dataHW = new MemoryManager(HWRAM);
        _dataLW = new MemoryManager(LWRAM);

        string[] filesToImport = {
            "APH1PMM1.BIN", "APHTD1P1.BIN",
            "BBB1PMM1.BIN", "BBBTD1P1.BIN",
            "BEL1PMM1.BIN", "BELTD1P1.BIN",
            "FEI1PMM1.BIN", "FEITD1P1.BIN",
            "JAG1PMM1.BIN", "JAGTD1P1.BIN",
            "KAS1PMM1.BIN", "KASTD1P1.BIN",
            "RAI1PMM1.BIN", "RAITD1P1.BIN",
            "RAW1PMM1.BIN", "RAITD1P1.BIN",
            "TEM1PMM1.BIN", "TEMTD1P1.BIN",
            "TEW1PMM1.BIN", "TEMTD1P1.BIN",
            "VIP1PMM1.BIN", "VIPTD1P1.BIN",
            "ZIG1PMM1.BIN", "ZGTTD1P1.BIN"
        };

        for (int index = 0; index < filesToImport.Length; index += 2)
        {
            GameObject importedObject = ImportVirtualOnFile(filesToImport[index], LWRAM, filesToImport[index + 1]);
            importedObject.transform.position = new Vector3(index * 2f, 0f, 0f);
        }
    }

    public MemoryManager GetMemoryManager(int address)
    {
        if (_dataLW.IsAddressValid(address))
        {
            return _dataLW;
        }
        else if (_dataHW.IsAddressValid(address))
        {
            return _dataHW;
        }

        return null;
    }

    private GameObject ImportVirtualOnFile(string file, int fileMemoryOffset, string textureFile, bool riggedModel = true, string fxFile = "", int fxMemoryOffset = 0)
    {
        bool debug = _debugOutput;

        file = _virtualOnFolderPath + file;
        MemoryManager memory = GetMemoryManager(fileMemoryOffset);
        int size = memory.LoadFile(file, fileMemoryOffset);

        int paletteMemoryOffset = fileMemoryOffset + 0x020;
        int textureVdp1Offset = 0x30b00;

        List<int> modelPointers = GetModelPointers(fileMemoryOffset, size);

        float gridOffset = 16f;
        ModelData modelData = ImportVirtualOnModelFromMemory(modelPointers, textureFile, textureVdp1Offset, paletteMemoryOffset, gridOffset, 512, 1024, debug);

        GameObject obj = CreateObject(modelData, Path.GetFileNameWithoutExtension(file) + "_parts", _opaqueMaterial, _transparentMaterial);
        obj.transform.position = new Vector3(0f, 0f, 0f);
        obj.transform.eulerAngles = new Vector3(0f, 0f, 0f);

        PostprocessModel(obj, false, true);

        if (fxFile != "")
        {
            fxFile = _virtualOnFolderPath + fxFile;
            MemoryManager fxMemory = GetMemoryManager(fxMemoryOffset);
            int fxSize = fxMemory.LoadFile(fxFile, fxMemoryOffset);

            List<int> fxModelPointers = GetModelPointers(fxMemoryOffset, fxSize);

            ModelData fxModelData = ImportVirtualOnModelFromMemory(fxModelPointers, textureFile, textureVdp1Offset, paletteMemoryOffset, gridOffset, 512, 512, debug);

            GameObject fxObj = CreateObject(fxModelData, Path.GetFileNameWithoutExtension(file) + "_parts", _opaqueMaterial, _transparentMaterial);
            fxObj.transform.position = new Vector3(0f, 0f, 0f);
            fxObj.transform.eulerAngles = new Vector3(0f, 0f, 0f);

            PostprocessModel(fxObj, false, true);
        }

        if (riggedModel == false)
        {
            return null;
        }

        // read animations
        List<VirtualOnAnimation> lowerAnims = new List<VirtualOnAnimation>();
        List<VirtualOnAnimation> upperAnims = new List<VirtualOnAnimation>();

        int animationPointers = fileMemoryOffset + 0x30;
        bool animTableEnd = false;
        while (animTableEnd == false)
        {
            int animationPointer1 = memory.GetInt32(animationPointers);
            int animationPointer2 = memory.GetInt32(animationPointers + 4);

            if (animationPointer1 != 0 || animationPointer2 != 0)
            {
                VirtualOnAnimation animation;
                if (animationPointer1 != 0)
                {
                    animation = ReadVirtualOnAnimation(animationPointer1);
                    upperAnims.Add(animation);
                }

                if (animationPointer2 != 0)
                {
                    animation = ReadVirtualOnAnimation(animationPointer2);
                    lowerAnims.Add(animation);
                }

                animationPointers += 8;
            }
            else
            {
                animTableEnd = true;
            }
        }

        Transform root = obj.transform.GetChild(0);

        List<Transform> lowerPartTransforms = new List<Transform>();
        int lowerRigPointer = memory.GetInt32(fileMemoryOffset + 0x08);
        GetPartTransforms(root, lowerPartTransforms, lowerRigPointer, modelPointers);

        GameObject upperBodyRoot = new GameObject("upperbodylink");
        upperBodyRoot.transform.parent = root;
        lowerPartTransforms.Add(upperBodyRoot.transform);

        // apply position and rotation
        VirtualOnAnimFrame lowerFrame = lowerAnims[0].Frames[0];
        int transformIndex = 0;
        foreach (Transform transform in lowerPartTransforms)
        {
            if (transformIndex >= lowerFrame.Transforms.Count)
            {
                continue;
            }

            transform.localPosition = lowerFrame.Transforms[transformIndex].Position;
            transform.localRotation = lowerFrame.Transforms[transformIndex].Rotation;

            transformIndex++;
        }

        // upper rig and head
        List<Transform> upperPartTransforms = new List<Transform>();
        int upperRigPointer = memory.GetInt32(fileMemoryOffset + 0x04);
        if (upperRigPointer != 0)
        {
            int upperEndPointer = memory.GetInt32(fileMemoryOffset + 0x08);
            int rigPartCount = (upperEndPointer - upperRigPointer) / 8;
            GetPartTransforms(root, upperPartTransforms, upperRigPointer, modelPointers, rigPartCount);

            // link head to "node2" of upperPartTransforms
            List<Transform> headTransforms = new List<Transform>();
            int headRigPointer = memory.GetInt32(fileMemoryOffset);
            int headEndPointer = memory.GetInt32(fileMemoryOffset + 0x04);
            rigPartCount = (headEndPointer - headRigPointer) / 8;
            GetPartTransforms(root, headTransforms, headRigPointer, modelPointers, rigPartCount);
            foreach (Transform upperPart in upperPartTransforms)
            {
                if (upperPart.name == "node2")
                {
                    headTransforms[1].parent = upperPart;
                    headTransforms[1].localRotation = Quaternion.identity;
                    headTransforms[1].localPosition = Vector3.zero;
                    headTransforms[1].localScale = Vector3.one;
                }
            }

            // apply position and rotation
            VirtualOnAnimFrame upperFrame = upperAnims[0].Frames[0];
            transformIndex = 0;
            foreach (Transform transform in upperPartTransforms)
            {
                if (transformIndex >= upperFrame.Transforms.Count)
                {
                    continue;
                }

                transform.localPosition = upperFrame.Transforms[transformIndex].Position;
                transform.localRotation = upperFrame.Transforms[transformIndex].Rotation;

                transformIndex++;
            }
        }

        GameObject virtuaRoidObject = new GameObject(Path.GetFileNameWithoutExtension(file));
        virtuaRoidObject.transform.localScale = new Vector3(-0.1f, 0.1f, 0.1f);

        VirtualOnCharacter virtuaRoid = virtuaRoidObject.AddComponent<VirtualOnCharacter>();
        virtuaRoid.Init(lowerPartTransforms, upperPartTransforms, lowerAnims, upperAnims);

        return virtuaRoidObject;
    }

    private void GetPartTransforms(Transform root, List<Transform> partTransforms, int tablePointer, List<int> modelPointers, int fixedCount = -1)
    {
        MemoryManager memory = GetMemoryManager(tablePointer);

        bool tableEnd = false;
        while (tableEnd == false)
        {
            int value1 = memory.GetInt32(tablePointer);
            int partReferencePoimter = memory.GetInt32(tablePointer + 4);

            if (value1 == 0 && partReferencePoimter == 0)
            {
                tableEnd = true;
                continue;
            }

            int normalModel = 0;
            if (memory.IsAddressValid(partReferencePoimter))
            {
                normalModel = memory.GetInt32(partReferencePoimter);
                //int damageModel = memory.GetInt32(partReferencePoimter + 4);      // not used yet
            }

            int index = modelPointers.IndexOf(normalModel);
            if (index != -1)
            {
                partTransforms.Add(root.GetChild(index));
            }
            else
            {
                GameObject node = new GameObject("node" + (partReferencePoimter + 1));
                node.transform.parent = root;
                partTransforms.Add(node.transform);
            }

            tablePointer += 8;

            if (fixedCount > 0)
            {
                fixedCount--;

                if (fixedCount == 0)
                {
                    tableEnd = true;
                }
            }
        }
    }

    private VirtualOnAnimation ReadVirtualOnAnimation(int frameHeaderMemory)
    {
        MemoryManager memory = GetMemoryManager(frameHeaderMemory);

        int frameMemory = memory.GetInt32(frameHeaderMemory);
        int frameCount = memory.GetInt16(frameHeaderMemory + 4);
        int transformCount = memory.GetInt16(frameHeaderMemory + 6);

        //frameCount = 1;
        //Debug.Log(frameMemory.ToString("X6"));
        //Debug.Log("frm: " + frameCount.ToString("X4"));
        //Debug.Log("frmcalc: " + ((frameHeaderMemory - frameMemory) / 6).ToString("X4"));
        //Debug.Log(transformCount.ToString("X4"));

        VirtualOnAnimation virtualOnAnimation = new VirtualOnAnimation();

        byte[] transformRaw = new byte[8];
        Vector3 position = Vector3.zero;
        Vector3 angle = Vector3.zero;

        for (int frameIndex = 0; frameIndex < frameCount; frameIndex++)
        {
            VirtualOnAnimFrame frame = new VirtualOnAnimFrame();

            for (int transformIndex = 0; transformIndex < transformCount; transformIndex++)
            {
                for (int rawIndex = 0; rawIndex < transformRaw.Length; rawIndex++)
                {
                    transformRaw[rawIndex] = memory.GetByte(frameMemory);
                    frameMemory++;
                }

                VirtualOnTranform virtualOnTransform = new VirtualOnTranform();

                position.x = -GetFloatFromHiLoBytes(transformRaw[5], (byte)((transformRaw[0] & 0x0f) << 4));
                position.y = GetFloatFromHiLoBytes(transformRaw[6], (byte)(transformRaw[1] & 0xf0));
                position.z = GetFloatFromHiLoBytes(transformRaw[7], (byte)((transformRaw[1] & 0x0f) << 4));

                virtualOnTransform.Position = position;

                float angleX = GetFloatFromHiLoBytes(transformRaw[2], (byte)(transformRaw[0] & 0x80));
                float angleY = GetFloatFromHiLoBytes(transformRaw[3], (byte)((transformRaw[0] & 0x40) << 1));
                float angleZ = GetFloatFromHiLoBytes(transformRaw[4], (byte)((transformRaw[0] & 0x20) << 2));

                angleX = (angleX * 360.0f) / 256.0f;
                angleY = (angleY * 360.0f) / 256.0f;
                angleZ = (angleZ * 360.0f) / 256.0f;

                //Debug.Log("t: " + transformIndex + " xr " + angleX + " yr: " + angleY + " zr: " + angleZ);

                Quaternion rotation = Quaternion.identity;
                rotation *= Quaternion.Euler(0, 0, angleZ);
                rotation *= Quaternion.Euler(0, angleY, 0);
                rotation *= Quaternion.Euler(-angleX, 0, 0);

                virtualOnTransform.Rotation = rotation;

                frame.AddTransform(virtualOnTransform);
            }

            virtualOnAnimation.AddFrame(frame);
        }

        return virtualOnAnimation;
    }

    private float GetFloatFromHiLoBytes(byte high, byte low)
    {
        sbyte integral = (sbyte)high;

        float value = integral + ((float)low / (float)0x100);

        return value;
    }

    private List<int> GetModelPointers(int memoryLocation, int size)
    {
        MemoryManager memory = GetMemoryManager(memoryLocation);

        List<int> modelPointers = new List<int>();

        int memoryEnd = memoryLocation + size;

        for (int offset = memoryLocation; offset < memoryEnd; offset += 4)
        {
            int numPoints = memory.GetInt32(offset);

            if (numPoints < 0x100 && numPoints >= 0x004)
            {
                int attribOffset = offset + 4 + (numPoints * 6);

                // align 4
                if ((attribOffset & 3) == 2)
                {
                    attribOffset += 2;
                }

                if (attribOffset >= memoryEnd)
                {
                    break;
                }

                int numPolygons = memory.GetInt32(attribOffset);

                if (numPolygons < 2048 && numPolygons >= 1)
                {
                    attribOffset += 4;

                    bool invalidPolygons = false;

                    for (int polygonIndex = 0; polygonIndex < numPolygons; polygonIndex++)
                    {
                        if (attribOffset >= memoryEnd)
                        {
                            break;
                        }

                        int idx1 = memory.GetByte(attribOffset + 4);
                        int idx2 = memory.GetByte(attribOffset + 5);
                        int idx3 = memory.GetByte(attribOffset + 6);
                        int idx4 = memory.GetByte(attribOffset + 7);

                        if (idx1 >= numPoints || idx2 >= numPoints || idx3 >= numPoints || idx4 >= numPoints)
                        {
                            invalidPolygons = true;
                            break;
                        }

                        if ((idx1 == idx2 && idx1 == idx3 && idx1 == idx4) ||
                            (idx1 == idx2 && idx1 == idx3) ||
                            (idx1 == idx2 && idx3 == idx4))
                        {
                            invalidPolygons = true;
                            break;
                        }


                        attribOffset += 8;
                    }

                    if (invalidPolygons == false)
                    {
                        //Debug.Log(modelPointers.Count + ": model pointer: " + offset.ToString("X6"));
                        modelPointers.Add(offset);
                    }
                }
            }
        }

        return modelPointers;
    }

    public ModelData ImportVirtualOnModelFromMemory(List<int> xpData,
                                                        string textureFile = "TEMTD1P1.BIN",
                                                        int textureOffset = 0x30b00,
                                                        int paletteOffset = 0x200020,
                                                        float gridOffset = 24f,
                                                        int width = 256,
                                                        int height = 256,
                                                        bool debugOutput = false
                                                        )
    {
        byte[] textureBytes;
        string texturePath = "Textures/" + Path.GetFileNameWithoutExtension(textureFile) + "/";
        Directory.CreateDirectory(texturePath);

        ModelData model = new ModelData();
        model.Init();

        ModelTexture modelTexture = new ModelTexture();
        modelTexture.Init(false, width, height);
        model.ModelTexture = modelTexture;

        // pre-parse texture dictionary
        Dictionary<int, VirtualOnTextureInfo> textureDictionary = new Dictionary<int, VirtualOnTextureInfo>();

        for (int count = 0; count < xpData.Count; count++)
        {
            int offsetXPData = xpData[count];

            if (debugOutput)
            {
                Debug.Log("Obj: " + count + " Ptr: " + offsetXPData.ToString("X6"));
            }

            MemoryManager memory = GetMemoryManager(offsetXPData);

            int numPoints = memory.GetInt32(offsetXPData);
            int offsetPoints = offsetXPData + 4;
            offsetPoints += numPoints * 6;  // skip points

            // align 4
            if ((offsetPoints & 3) == 2)
            {
                offsetPoints += 2;
            }

            int numPolygons = memory.GetInt32(offsetPoints);
            int offsetPolygons = offsetPoints + 4;

            for (int countPolygons = 0; countPolygons < numPolygons; countPolygons++)
            {
                int word1 = memory.GetInt16(offsetPolygons);
                int word2 = memory.GetInt16(offsetPolygons + 2);

                int address = VirtualOnTextureInfo.ParseAddress(word1);

                if (textureDictionary.ContainsKey(address) == false)
                {
                    VirtualOnTextureInfo textureInfo = new VirtualOnTextureInfo();
                    textureInfo.Width = VirtualOnTextureInfo.ParseWidth(word2);
                    textureInfo.Height = VirtualOnTextureInfo.ParseHeight(word2);

                    textureDictionary[address] = textureInfo;

                    if (debugOutput)
                    {
                        Debug.Log("TexInfo: " + address.ToString("X6") + " w: " + textureInfo.Width + " h: " + textureInfo.Height);
                    }
                }

                int flags = (word1 & 0x0003) | (word2 & 0xf000);    // NOTE: flags not used for now

                offsetPolygons += 8;
            }
        }

        // get textures
        //
        MemoryManager paletteMemory = GetMemoryManager(paletteOffset);
        int paletteMemoryPointer = paletteMemory.GetInt32(paletteOffset);

        byte[] textureData = File.ReadAllBytes(_virtualOnFolderPath + textureFile);

        foreach (KeyValuePair<int, VirtualOnTextureInfo> pair in textureDictionary)
        {
            Texture2D texture = new Texture2D(pair.Value.Width, pair.Value.Height);

            int textureDataOffset = pair.Key - textureOffset;

            if (textureDataOffset < 0 || textureDataOffset >= textureData.Length)
            {
                Debug.Log("Invalid texture offset: " + textureDataOffset.ToString("X6"));
            }
            else
            {
                for (int y = 0; y < pair.Value.Height; y++)
                {
                    for (int x = 0; x < pair.Value.Width; x++)
                    {
                        int colorValue = paletteMemory.GetInt16(paletteMemoryPointer + (textureData[textureDataOffset] * 2));
                        Color colorRgb = ColorConversion.ConvertColor(colorValue);

                        texture.SetPixel(x, y, colorRgb);

                        textureDataOffset++;
                    }
                }

                texture.Apply();
            }

            pair.Value.Texture = texture;

            textureBytes = texture.EncodeToPNG();
            File.WriteAllBytes(texturePath + Path.GetFileNameWithoutExtension(textureFile) + "_" + pair.Key.ToString("X6") + ".png", textureBytes);
        }

        for (int count = 0; count < xpData.Count; count++)
        {
            if (debugOutput)
            {
                Debug.Log("--------------------");
                Debug.Log("OBJNR: " + count);
            }

            try
            {
                ModelPart part = new ModelPart();
                part.Init();
                model.Parts.Add(part);  // add part to part list

                int offsetXPData = xpData[count];
                MemoryManager memory = GetMemoryManager(offsetXPData);

                int numPoints = memory.GetInt32(offsetXPData);
                int offsetPoints = offsetXPData + 4;

                if (debugOutput)
                {
                    Debug.Log("  points: " + numPoints + " offs: " + offsetPoints.ToString("X6"));
                }

                List<Vector3> points = new List<Vector3>();

                // read POINTS
                //
                for (int countPoints = 0; countPoints < numPoints; countPoints++)
                {
                    float x, y, z;

                    x = memory.GetFloat16(offsetPoints);
                    y = memory.GetFloat16(offsetPoints + 2);
                    z = memory.GetFloat16(offsetPoints + 4);

                    offsetPoints += 6;

                    Vector3 point = new Vector3(x, y, z);
                    points.Add(point);

                    //Debug.Log(countPoints + ": " + x + " | " + y + " | " + z);
                }

                // align 4
                if ((offsetPoints & 3) == 2)
                {
                    offsetPoints += 2;
                }

                int numPolygons = memory.GetInt32(offsetPoints);
                int offsetPolygons = offsetPoints + 4;

                if (debugOutput)
                {
                    Debug.Log("  polys:  " + numPolygons + " offs: " + offsetPolygons.ToString("X6"));
                }

                // read POLYGONS
                //
                for (int countPolygons = 0; countPolygons < numPolygons; countPolygons++)
                {
                    int word1 = memory.GetInt16(offsetPolygons);
                    int word2 = memory.GetInt16(offsetPolygons + 2);

                    int address = VirtualOnTextureInfo.ParseAddress(word1);

                    VirtualOnTextureInfo textureInfo = textureDictionary[address];

                    bool cutOut = false;
                    bool halftransparent = false;
                    bool doubleSided = true;

                    bool hflip = false;
                    bool vflip = false;

                    int a, b, c, d;
                    Vector3 faceNormal = Vector3.one;

                    offsetPolygons += 4;

                    a = memory.GetByte(offsetPolygons);
                    b = memory.GetByte(offsetPolygons + 1);
                    c = memory.GetByte(offsetPolygons + 2);
                    d = memory.GetByte(offsetPolygons + 3);
                    offsetPolygons += 4;

                    if (debugOutput)
                    {
                        Debug.Log("Polygon: " + countPolygons + " a: " + a + " b: " + b + " c: " + c + " d: " + d);
                    }

                    // color
                    Color rgbColor = Color.white;
                    Color colorA = Color.white;
                    Color colorB = Color.white;
                    Color colorC = Color.white;
                    Color colorD = Color.white;

                    Vector2 uvA, uvB, uvC, uvD;
                    uvA = Vector2.zero;
                    uvB = Vector2.zero;
                    uvC = Vector2.zero;
                    uvD = Vector2.zero;

                    Texture2D texture = textureInfo.Texture;

                    if (modelTexture.ContainsTexture(texture) == false)
                    {
                        modelTexture.AddTexture(texture, cutOut, halftransparent);
                    }

                    // add texture uv
                    bool rotate = false;
                    modelTexture.AddUv(texture, hflip, vflip, rotate, out uvA, out uvB, out uvC, out uvD);

                    Vector3 vA, vB, vC, vD;
                    Vector3 nA, nB, nC, nD;
                    nA = faceNormal;
                    nB = faceNormal;
                    nC = faceNormal;
                    nD = faceNormal;

                    if (a < points.Count && b < points.Count && c < points.Count && d < points.Count)
                    {
                        vA = points[a];
                        vB = points[b];
                        vC = points[c];
                        vD = points[d];

                        part.AddPolygon(vA, vB, vC, vD,
                                        halftransparent, false,
                                        colorA, colorB, colorC, colorD,
                                        uvA, uvB, uvC, uvD,
                                        nA, nB, nC, nD, _subDivide);

                        if (doubleSided)
                        {
                            part.AddPolygon(vA, vD, vC, vB,
                                            halftransparent, false,
                                            colorA, colorD, colorC, colorB,
                                            uvA, uvD, uvC, uvB,
                                            -nA, -nD, -nC, -nB, _subDivide);
                        }
                    }
                }

                // get translation
                //
                float transX = 0f, transY = 0f, transZ = 0f;
                transX = (count / 5) * gridOffset;
                transZ = (count % 5) * gridOffset;
                transY = 0f;

                part.Translation = new Vector3(transX, transY, transZ);
            }
            catch (Exception e)
            {
                Debug.Log("Exception: " + e.Message + " on OBJ: " + count);
            }
        }

        model.ModelTexture.ApplyTexture();

        textureBytes = model.ModelTexture.Texture.EncodeToPNG();
        File.WriteAllBytes(texturePath + "/ModelTexture.png", textureBytes);

        return model;
    }

    public GameObject CreateObject(ModelData modelData, string name, Material opaqueMaterial, Material transparentMaterial)
    {
        GameObject parent = new GameObject(name);
        GameObject root;

        root = new GameObject("root");
        modelData.Root = root;
        root.transform.parent = parent.transform;
        root.transform.localPosition = Vector3.zero;
        root.transform.localEulerAngles = Vector3.zero;

        modelData.OpaqueMaterial = new Material(opaqueMaterial);
        modelData.OpaqueMaterial.mainTexture = modelData.ModelTexture.Texture;

        modelData.TransparentMaterial = new Material(transparentMaterial);
        modelData.TransparentMaterial.mainTexture = modelData.ModelTexture.Texture;

        GameObject partObject;

        int parts = modelData.Parts.Count;

        for (int partIndex = 0; partIndex < parts; partIndex++)
        {
            ModelPart part = modelData.Parts[partIndex];

            Mesh mesh = part.CreateMesh();

            //if (mesh != null)
            //{
            //    if (part.DidNotProvideNormals)
            //    {
            //        mesh.RecalculateNormals(60f);
            //    }
            //}

            partObject = new GameObject("part" + partIndex);
            partObject.SetActive(true);

            part.OpaqueObject = partObject;
            if (part.Parent == -1)
            {
                partObject.transform.parent = root.transform;
            }
            else
            {
                partObject.transform.parent = modelData.Parts[part.Parent].OpaqueObject.transform;
            }
            partObject.transform.localPosition = part.Translation;
            partObject.transform.localScale = new Vector3(1f, 1f, 1f);

            if (mesh != null)
            {
                MeshFilter filter = partObject.AddComponent<MeshFilter>();
                filter.mesh = mesh;

                MeshRenderer renderer = partObject.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = modelData.OpaqueMaterial;
            }

            // transparent
            mesh = part.CreateTransparentMesh();

            if (mesh != null)
            {
                //if (part.DidNotProvideNormals)
                //{
                //    mesh.RecalculateNormals(60f);
                //    //Debug.Log(" CALC NORMALS FOR PART: " + partIndex);
                //}

                partObject = new GameObject("part_trans_" + partIndex);
                partObject.SetActive(true);

                part.TransparentObject = partObject;

                if (part.Parent == -1)
                {
                    partObject.transform.parent = root.transform;
                }
                else
                {
                    partObject.transform.parent = modelData.Parts[part.Parent].OpaqueObject.transform;
                }
                partObject.transform.localPosition = part.Translation;
                partObject.transform.localScale = new Vector3(1f, 1f, 1f);

                MeshFilter filter = partObject.AddComponent<MeshFilter>();
                filter.mesh = mesh;

                MeshRenderer renderer = partObject.AddComponent<MeshRenderer>();

                renderer.sharedMaterial = modelData.TransparentMaterial;
            }

            if (part.OpaqueObject || part.TransparentObject)
            {
                // pivoting
                GameObject partPivot = new GameObject("pivot" + partIndex);

                if (part.OpaqueObject)
                {
                    partPivot.transform.position = part.OpaqueObject.transform.position;
                    partPivot.transform.parent = part.OpaqueObject.transform.parent;
                }
                else
                {
                    partPivot.transform.position = part.TransparentObject.transform.position;
                    partPivot.transform.parent = part.TransparentObject.transform.parent;
                }

                partPivot.transform.localEulerAngles = Vector3.zero;

                if (part.OpaqueObject)
                {
                    part.OpaqueObject.transform.parent = partPivot.transform;
                }

                if (part.TransparentObject)
                {
                    part.TransparentObject.transform.parent = partPivot.transform;
                }

                part.Pivot = partPivot;
            }
        }

        parent.transform.eulerAngles = new Vector3(180f, 0f, 0f);
        parent.transform.localScale = new Vector3(-0.1f, 0.1f, 0.1f);
        return parent;
    }

    private Mesh CreateMesh(ModelPart part)
    {
        List<Vector3> vertices = part.Vertices;
        List<Vector3> normals = part.Normals;
        List<int> indices = part.Indices;
        List<Color> colors = part.Colors;
        List<Vector2> uvs = part.Uvs;

        Mesh mesh = new Mesh();

        if (indices.Count <= 65535)
        {
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt16;
        }
        else
        {
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        }

        mesh.SetVertices(vertices);
        mesh.SetColors(colors);
        mesh.SetNormals(normals);
        mesh.SetUVs(0, uvs);

        mesh.SetIndices(indices.ToArray(), MeshTopology.Triangles, 0);

        mesh.name = "mesh";

        return mesh;
    }

    private void PostprocessModel(GameObject obj, bool shadowOff = true, bool calculateNormals = true, float smoothAngle = 45f)
    {
        if (calculateNormals)
        {
            MeshFilter[] filters = obj.GetComponentsInChildren<MeshFilter>();
            foreach (MeshFilter filter in filters)
            {
                //filter.mesh.RecalculateNormals(smoothAngle);
                filter.mesh.RecalculateNormals();
            }
        }

        if (shadowOff)
        {
            MeshRenderer[] renderers = obj.GetComponentsInChildren<MeshRenderer>();
            foreach (MeshRenderer renderer in renderers)
            {
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }
    }
}
