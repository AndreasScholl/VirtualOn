using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ModelBuilder
{
    public class ModelPart
    {
        public enum PolygonType
        {
            Opaque,
            Cutout,
            Transparent
        }

        public bool DidNotProvideNormals = false;

        public List<Vector3> Vertices;
        public List<Vector3> VerticesTransparent;
        public List<Vector3> Normals;
        public List<Vector3> NormalsTransparent;
        public List<Color> Colors;
        public List<Color> ColorsTransparent;
        public List<Vector2> Uvs;
        public List<Vector2> UvsTransparent;
        public List<int> Indices;
        public List<int> IndicesTransparent;

        public List<PolygonType> Types;

        public Vector3 Translation;
        public int Parent;
        public GameObject OpaqueObject;
        public GameObject TransparentObject;
        public GameObject Pivot;

        public void Init()
        {
            Vertices = new List<Vector3>();
            VerticesTransparent = new List<Vector3>();
            Normals = new List<Vector3>();
            NormalsTransparent = new List<Vector3>();
            Indices = new List<int>();
            IndicesTransparent = new List<int>();
            Colors = new List<Color>();
            ColorsTransparent = new List<Color>();
            Uvs = new List<Vector2>();
            UvsTransparent = new List<Vector2>();
            Types = new List<PolygonType>();

            Parent = -1;
            OpaqueObject = null;
            TransparentObject = null;
        }

        public int GetNumPolygons()
        {
            return Types.Count;
        }

        public Mesh CreateMesh()
        {
            Mesh mesh = new Mesh();

            if (Vertices.Count == 0)
            {
                return null;
            }

            //mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt16;
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(Vertices);
            mesh.SetColors(Colors);
            mesh.SetNormals(Normals);
            mesh.SetUVs(0, Uvs);
            mesh.SetIndices(Indices.ToArray(), MeshTopology.Triangles, 0);

            mesh.name = "mesh";

            return mesh;
        }

        public Mesh CreateTransparentMesh()
        {
            if (VerticesTransparent.Count == 0)
            {
                return null;
            }

            Mesh mesh = new Mesh();

            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt16;
            mesh.SetVertices(VerticesTransparent);
            mesh.SetColors(ColorsTransparent);
            mesh.SetNormals(NormalsTransparent);
            mesh.SetUVs(0, UvsTransparent);
            mesh.SetIndices(IndicesTransparent.ToArray(), MeshTopology.Triangles, 0);

            mesh.name = "mesh";

            //mesh.RecalculateNormals(90f);
            //NormalSolver.RecalculateNormals(mesh, 90f);

            return mesh;
        }

        public void AddPolygon(Vector3 a, Vector3 b, Vector3 c, Vector3 d,
                       bool halfTransparent, bool doubleSided, Color colorA, Color colorB, Color colorC, Color colorD,
                       Vector2 uvA, Vector2 uvB, Vector2 uvC, Vector2 uvD,
                       Vector3 nA, Vector3 nB, Vector3 nC, Vector3 nD, int sub = 1, bool vert = true)
        {
            Vector3 a1, b1, c1, d1;
            Vector3 uvA1, uvB1, uvC1, uvD1;
            Vector3 nA1, nB1, nC1, nD1;
            Color ca1, cb1, cc1, cd1;

            for (int s = 0; s < sub; s++)
            {
                float subSize = 1f / (float)sub;
                float lStart = (float)s * subSize;
                float lEnd = lStart + subSize;

                if (vert == true)
                {
                    a1 = Vector3.Lerp(a, b, lStart);
                    b1 = Vector3.Lerp(a, b, lEnd);
                    d1 = Vector3.Lerp(d, c, lStart);
                    c1 = Vector3.Lerp(d, c, lEnd);

                    uvA1 = Vector3.Lerp(uvA, uvB, lStart);
                    uvB1 = Vector3.Lerp(uvA, uvB, lEnd);
                    uvD1 = Vector3.Lerp(uvD, uvC, lStart);
                    uvC1 = Vector3.Lerp(uvD, uvC, lEnd);

                    nA1 = Vector3.Lerp(nA, nB, lStart);
                    nB1 = Vector3.Lerp(nA, nB, lEnd);
                    nD1 = Vector3.Lerp(nD, nC, lStart);
                    nC1 = Vector3.Lerp(nD, nC, lEnd);

                    ca1 = Color.Lerp(colorA, colorB, lStart);
                    cb1 = Color.Lerp(colorA, colorB, lEnd);
                    cd1 = Color.Lerp(colorD, colorC, lStart);
                    cc1 = Color.Lerp(colorD, colorC, lEnd);

                    AddPolygon(a1, b1, c1, d1, halfTransparent, doubleSided, ca1, cb1, cc1, cd1,
                                           uvA1, uvB1, uvC1, uvD1, nA1, nB1, nC1, nD1, sub, false);
                }
                else
                {
                    a1 = Vector3.Lerp(a, d, lStart);
                    b1 = Vector3.Lerp(b, c, lStart);
                    d1 = Vector3.Lerp(a, d, lEnd);
                    c1 = Vector3.Lerp(b, c, lEnd);

                    uvA1 = Vector3.Lerp(uvA, uvD, lStart);
                    uvB1 = Vector3.Lerp(uvB, uvC, lStart);
                    uvD1 = Vector3.Lerp(uvA, uvD, lEnd);
                    uvC1 = Vector3.Lerp(uvB, uvC, lEnd);

                    nA1 = Vector3.Lerp(nA, nD, lStart);
                    nB1 = Vector3.Lerp(nB, nC, lStart);
                    nD1 = Vector3.Lerp(nA, nD, lEnd);
                    nC1 = Vector3.Lerp(nB, nC, lEnd);

                    ca1 = Color.Lerp(colorA, colorD, lStart);
                    cb1 = Color.Lerp(colorB, colorC, lStart);
                    cd1 = Color.Lerp(colorA, colorD, lEnd);
                    cc1 = Color.Lerp(colorB, colorC, lEnd);

                    AddSubPolygon(a1, b1, c1, d1, halfTransparent, doubleSided, ca1, cb1, cc1, cd1,
                                           uvA1, uvB1, uvC1, uvD1, nA1, nB1, nC1, nD1);
                }

            }
        }

        public void AddSubPolygon(Vector3 a, Vector3 b, Vector3 c, Vector3 d,
                               bool halfTransparent, bool doubleSided, Color colorA, Color colorB, Color colorC, Color colorD,
                               Vector2 uvA, Vector2 uvB, Vector2 uvC, Vector2 uvD,
                               Vector3 nA, Vector3 nB, Vector3 nC, Vector3 nD)
        {
            List<Vector3> vertices;
            List<int> indices;
            List<Vector3> normals;
            List<Color> colors;
            List<Vector2> uvs;

            PolygonType polygonType = PolygonType.Opaque;

            // get lists depending on transparency mode
            if (halfTransparent == false)
            {
                vertices = Vertices;
                indices = Indices;
                normals = Normals;
                colors = Colors;
                uvs = Uvs;
            }
            else
            {
                vertices = VerticesTransparent;
                indices = IndicesTransparent;
                normals = NormalsTransparent;
                colors = ColorsTransparent;
                uvs = UvsTransparent;

                polygonType = PolygonType.Transparent;
            }
            Types.Add(polygonType);

            // add vertices, normals and indices
            //
            int idxA = vertices.Count;
            vertices.Add(a);
            normals.Add(nA);

            int idxB = vertices.Count;
            vertices.Add(b);
            normals.Add(nB);

            int idxC = vertices.Count;
            vertices.Add(c);
            normals.Add(nC);

            int idxD = vertices.Count;
            vertices.Add(d);
            normals.Add(nD);

            indices.Add(idxA);    // triangle 1
            indices.Add(idxB);
            indices.Add(idxC);

            indices.Add(idxA);    // triangle 2
            indices.Add(idxC);
            indices.Add(idxD);

            if (doubleSided == true)
            {
                indices.Add(idxC);    // reversed triangle 1
                indices.Add(idxB);
                indices.Add(idxA);

                indices.Add(idxD);    // reversed triangle 2
                indices.Add(idxC);
                indices.Add(idxA);
            }

            uvs.Add(uvA);
            uvs.Add(uvB);
            uvs.Add(uvC);
            uvs.Add(uvD);

            colors.Add(colorA);
            colors.Add(colorB);
            colors.Add(colorC);
            colors.Add(colorD);
        }

        public void AddTriangle(Vector3 a, Vector3 b, Vector3 c,
                               bool halfTransparent, bool doubleSided, Color colorA, Color colorB, Color colorC,
                               Vector2 uvA, Vector2 uvB, Vector2 uvC,
                               Vector3 nA, Vector3 nB, Vector3 nC)
        {
            List<Vector3> vertices;
            List<int> indices;
            List<Vector3> normals;
            List<Color> colors;
            List<Vector2> uvs;

            PolygonType polygonType = PolygonType.Opaque;

            // get lists depending on transparency mode
            if (halfTransparent == false)
            {
                vertices = Vertices;
                indices = Indices;
                normals = Normals;
                colors = Colors;
                uvs = Uvs;
            }
            else
            {
                vertices = VerticesTransparent;
                indices = IndicesTransparent;
                normals = NormalsTransparent;
                colors = ColorsTransparent;
                uvs = UvsTransparent;

                polygonType = PolygonType.Transparent;
            }
            Types.Add(polygonType);

            // add vertices, normals and indices
            //
            int idxA = vertices.Count;
            vertices.Add(a);
            normals.Add(nA);

            int idxB = vertices.Count;
            vertices.Add(b);
            normals.Add(nB);

            int idxC = vertices.Count;
            vertices.Add(c);
            normals.Add(nC);

            indices.Add(idxA);    // triangle 1
            indices.Add(idxB);
            indices.Add(idxC);

            if (doubleSided == true)
            {
                indices.Add(idxC);    // reversed triangle 1
                indices.Add(idxB);
                indices.Add(idxA);
            }

            uvs.Add(uvA);
            uvs.Add(uvB);
            uvs.Add(uvC);

            colors.Add(colorA);
            colors.Add(colorB);
            colors.Add(colorC);
        }

        public GameObject Instantiate(Color[] colors)
        {
            GameObject obj = null;

            List<Color> colorOpaque = new List<Color>();
            List<Color> colorTransparent = new List<Color>();

            int count = 0;
            foreach (Color color in colors)
            {
                if (Types[count / 4] == PolygonType.Opaque)
                {
                    colorOpaque.Add(color);
                }
                else
                {
                    colorTransparent.Add(color);
                    //colorTransparent.Add(Color.black);
                }

                count++;
            }

            obj = GameObject.Instantiate(OpaqueObject);

            // apply opaque colors
            if (colorOpaque.Count > 0)
            {
                MeshFilter filter = obj.GetComponent<MeshFilter>();
                Mesh mesh = CopyMesh(filter.mesh);
                mesh.colors = colorOpaque.ToArray();
                filter.mesh = mesh;
            }

            if (TransparentObject != null)
            {
                GameObject transpObj = GameObject.Instantiate(TransparentObject);
                transpObj.transform.parent = obj.transform;
                transpObj.transform.localPosition = Vector3.zero;
                transpObj.transform.localEulerAngles = Vector3.zero;

                MeshFilter filter = transpObj.GetComponent<MeshFilter>();
                Mesh mesh = CopyMesh(filter.mesh);
                mesh.colors = colorTransparent.ToArray();
                filter.mesh = mesh;
            }

            return obj;
        }

        private Mesh CopyMesh(Mesh source)
        {
            Mesh mesh = source;
            Mesh copy = new Mesh();
            copy.vertices = mesh.vertices;
            copy.triangles = mesh.triangles;
            copy.uv = mesh.uv;
            copy.normals = mesh.normals;
            copy.colors = mesh.colors;
            copy.tangents = mesh.tangents;

            return copy;
        }

        public void AddCollider()
        {
            if (OpaqueObject != null)
            {
                if (OpaqueObject.GetComponent<MeshFilter>() != null)
                {
                    MeshCollider collider = OpaqueObject.AddComponent<MeshCollider>();
                }
            }

            if (TransparentObject != null)
            {
                MeshCollider collider = TransparentObject.AddComponent<MeshCollider>();
            }
        }
    }

    public class ModelData
    {
        public struct AnimationParams
        {
            public int Type;
            public int Repeat;
            public int Speed;
            public int NextId;
            public bool Loop;
            public float FirstFrameTime;
        }

        public GameObject Root;
        public List<ModelPart> Parts;
        public ModelTexture ModelTexture;

        public Dictionary<string, List<Vector3>[]> Animations;
        public Dictionary<string, List<Vector3>> Translations;
        public Dictionary<string, AnimationParams> AnimationParameters;

        public Material OpaqueMaterial;
        public Material TransparentMaterial;

        public void Init()
        {
            Parts = new List<ModelPart>();

            Animations = new Dictionary<string, List<Vector3>[]>();
            Translations = new Dictionary<string, List<Vector3>>();
            AnimationParameters = new Dictionary<string, AnimationParams>();
        }

        public Transform GetPartParent(int index)
        {
            return Parts[index].OpaqueObject.transform.parent;
        }

        public Transform GetPart(int index)
        {
            return Parts[index].OpaqueObject.transform;
        }

        public Transform GetPartTransparent(int index)
        {
            return Parts[index].TransparentObject.transform;
        }
    }

    public class ModelTexture
    {
        int _width = 1024;
        int _height = 1024;

        int X = 0;
        int Y = 0;
        int RowHeight = 0;

        public Texture2D Texture;

        public List<Texture2D> AddedTextures;
        public List<Vector4> TextureAreas;

        public void Init(bool transparentBackground = false, int width = 2048, int height = 1024)
        {
            _width = width;
            _height = height;

            Texture = new Texture2D(_width, _height);
            Texture.filterMode = FilterMode.Point;

            Color fillColor = new Color(1.0f, 1.0f, 1.0f);

            if (transparentBackground == true)
            {
                fillColor.a = 0f;
            }

            for (int y = 0; y < _height; y++)
            {
                for (int x = 0; x < _width; x++)
                {
                    Texture.SetPixel(x, y, fillColor);
                }
            }
            Texture.Apply();

            AddedTextures = new List<Texture2D>();

            TextureAreas = new List<Vector4>();
        }

        public void AddTexture(Texture2D texture, bool transparent = true, bool halftransparent = true)
        {
            int addWidth = texture.width;
            int addHeight = texture.height;

            if ((X + addWidth) > _width)
            {
                X = 0;
                Y += RowHeight;
                RowHeight = 0;
            }

            AddedTextures.Add(texture);

            Vector4 area = new Vector4();
            area.x = X;
            area.y = Y;
            area.z = X + addWidth;
            area.w = Y + addHeight;
            TextureAreas.Add(area);

            for (int yc = 0; yc < addHeight; yc++)
            {
                for (int xc = 0; xc < addWidth; xc++)
                {
                    Color pixel = texture.GetPixel(xc, yc);

                    if (transparent == false)
                    {
                        pixel.a = 1f;   // remove cutout alpha
                    }

                    if (halftransparent == true)
                    {
                        pixel.a = 0.5f;
                    }

                    Texture.SetPixel(X + xc, Y + yc, pixel);
                }
            }

            X += addWidth;

            if (addHeight > RowHeight)
            {
                RowHeight = addHeight;
            }
        }

        public void ApplyTexture()
        {
            Texture.Apply();
        }

        public int AddTexture(Texture2D texture, bool transparent = true, bool halftransparent = true, byte[] paletteData = null, int paletteOffset = 0, int cutOutColor = 0, bool overWriteCutoutColor = false)
        {
            int addWidth = texture.width;
            int addHeight = texture.height;

            if ((X + addWidth) > _width)
            {
                X = 0;
                Y += RowHeight;
                RowHeight = 0;
            }

            AddedTextures.Add(texture);

            Vector4 area = new Vector4();
            area.x = X;
            area.y = Y;
            area.z = X + addWidth;
            area.w = Y + addHeight;
            TextureAreas.Add(area);

            for (int yc = 0; yc < addHeight; yc++)
            {
                for (int xc = 0; xc < addWidth; xc++)
                {
                    Color pixel = texture.GetPixel(xc, yc);

                    if (paletteData != null)
                    {
                        // get color index from pixel
                        int colorIndex = (int)(pixel.r * 16f);

                        if (colorIndex < 16)
                        {
                            // get color from palette
                            int colorValue = ByteArray.GetInt16(paletteData, paletteOffset + (colorIndex * 2));
                            Color colorRgb = ColorConversion.ConvertColor(colorValue);

                            pixel = colorRgb;

                            if (transparent == true)
                            {
                                if (colorIndex == cutOutColor)
                                {
                                    if (overWriteCutoutColor == true)
                                    {
                                        pixel = Color.black;
                                    }

                                    pixel.a = 0f;
                                }
                            }
                        }
                        else
                        {
                            pixel = Color.magenta;  // error color
                        }
                    }

                    if (transparent == false)
                    {
                        pixel.a = 1f;
                    }

                    if (halftransparent == true)
                    {
                        pixel.a = 0.5f;
                    }

                    Texture.SetPixel(X + xc, Y + yc, pixel);
                }
            }

            X += addWidth;

            if (addHeight > RowHeight)
            {
                RowHeight = addHeight;
            }

            //Texture.Apply();

            return TextureAreas.Count - 1;  // return area index
        }

        public void AddTextureForSheet(Texture2D texture, int gridWidth, int gridHeight, bool transparent = true, bool halftransparent = true, bool alphaFromBrightness = false)
        {
            int textureWidth = texture.width;
            int textureHeight = texture.height;

            if ((X + gridWidth) > _width)
            {
                X = 0;
                Y -= gridHeight;
            }

            AddedTextures.Add(texture);

            Vector4 area = new Vector4();
            area.x = X;
            area.y = Y;
            area.z = X + gridWidth;
            area.w = Y + gridHeight;
            TextureAreas.Add(area);

            int xCenterOffset = (gridWidth - textureWidth) / 2;
            int yCenterOffset = (gridHeight - textureHeight) / 2;

            for (int yc = 0; yc < textureHeight; yc++)
            {
                for (int xc = 0; xc < textureWidth; xc++)
                {
                    Color pixel = texture.GetPixel(xc, yc);

                    if (transparent == false)
                    {
                        pixel.a = 1f;
                    }

                    if (halftransparent == true)
                    {
                        pixel.a = 0.25f;
                    }

                    if (alphaFromBrightness == true)
                    {
                        if (pixel.a > 0f)
                        {
                            float h, s, v;
                            Color.RGBToHSV(pixel, out h, out s, out v);
                            pixel.a = v;
                        }
                    }

                    Texture.SetPixel(X + xc + xCenterOffset, Y + yc + yCenterOffset, pixel);
                }
            }

            X += gridWidth;

            Texture.Apply();
        }

        public bool ContainsTexture(Texture2D texture)
        {
            if (AddedTextures.Contains(texture))
            {
                return true;
            }

            return false;
        }

        public void AddUv(List<Vector2> uvList, Texture2D texture, bool hflip, bool vflip, int textureIndex = -1)
        {
            if (textureIndex == -1)
            {
                textureIndex = AddedTextures.IndexOf(texture);
            }

            Vector4 area = TextureAreas[textureIndex];

            float halfU = 0.5f / (float)_width;
            float halfV = 0.5f / (float)_height;

            float fullU = 1f / (float)_width;
            float fullV = 1f / (float)_height;

            Vector2 uvA = new Vector2((fullU * area.x) + halfU, (fullV * area.y) + halfV);
            Vector2 uvB = new Vector2((fullU * area.z) - halfU, (fullV * area.y) + halfV);
            Vector2 uvC = new Vector2((fullU * area.z) - halfU, (fullV * area.w) - halfV);
            Vector2 uvD = new Vector2((fullU * area.x) + halfU, (fullV * area.w) - halfV);

            Vector2 uv1, uv2, uv3, uv4;

            if (vflip == false)
            {
                if (hflip == false)
                {
                    uv1 = uvA;
                    uv2 = uvB;
                    uv3 = uvC;
                    uv4 = uvD;
                }
                else
                {
                    uv1 = uvB;
                    uv2 = uvA;
                    uv3 = uvD;
                    uv4 = uvC;
                }
            }
            else
            {
                if (hflip == false)
                {
                    uv1 = uvD;
                    uv2 = uvC;
                    uv3 = uvB;
                    uv4 = uvA;
                }
                else
                {
                    uv1 = uvD;
                    uv2 = uvC;
                    uv3 = uvB;
                    uv4 = uvA;
                }
            }

            uvList.Add(uv1); // a
            uvList.Add(uv2); // b
            uvList.Add(uv3); // c
            uvList.Add(uv4); // d
        }

        public void AddUv(Texture2D texture, bool hflip, bool vflip, bool rotate, out Vector2 uv1, out Vector2 uv2, out Vector2 uv3, out Vector2 uv4, int textureIndex = -1)
        {
            if (textureIndex == -1)
            {
                textureIndex = AddedTextures.IndexOf(texture);
            }

            Vector4 area = TextureAreas[textureIndex];

            float halfU = 0.5f / (float)_width;
            float halfV = 0.5f / (float)_height;

            bool unfiltered = true;
            if (unfiltered == true)
            {
                halfU = 0f;
                halfV = 0f;
            }

            float fullU = 1f / (float)_width;
            float fullV = 1f / (float)_height;

            Vector2 uvA = new Vector2((fullU * area.x) + halfU, (fullV * area.y) + halfV);
            Vector2 uvB = new Vector2((fullU * area.z) - halfU, (fullV * area.y) + halfV);
            Vector2 uvC = new Vector2((fullU * area.z) - halfU, (fullV * area.w) - halfV);
            Vector2 uvD = new Vector2((fullU * area.x) + halfU, (fullV * area.w) - halfV);

            if (rotate == true)
            {
                uv1 = uvB;
                uv2 = uvC;
                uv3 = uvD;
                uv4 = uvA;

                uvA = uv1;
                uvB = uv2;
                uvC = uv3;
                uvD = uv4;
            }

            if (vflip == false)
            {
                if (hflip == false)
                {
                    uv1 = uvA;
                    uv2 = uvB;
                    uv3 = uvC;
                    uv4 = uvD;
                }
                else
                {
                    uv1 = uvB;
                    uv2 = uvA;
                    uv3 = uvD;
                    uv4 = uvC;
                }
            }
            else
            {
                if (hflip == false)
                {
                    uv1 = uvD;
                    uv2 = uvC;
                    uv3 = uvB;
                    uv4 = uvA;
                }
                else
                {
                    uv1 = uvC;
                    uv2 = uvD;
                    uv3 = uvA;
                    uv4 = uvB;
                }
            }
        }

        public void SetY(int y)
        {
            Y = y;
        }
    }
}
