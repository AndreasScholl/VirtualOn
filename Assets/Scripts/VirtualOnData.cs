using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VirtualOnData
{
    public class VirtualOnTranform
    {
        public Vector3 Position;
        public Quaternion Rotation;
    }

    public class VirtualOnAnimFrame
    {
        public List<VirtualOnTranform> Transforms = new List<VirtualOnTranform>();

        public void AddTransform(VirtualOnTranform transform)
        {
            Transforms.Add(transform);
        }
    }

    public class VirtualOnAnimation
    {
        public List<VirtualOnAnimFrame> Frames = new List<VirtualOnAnimFrame>();

        public void AddFrame(VirtualOnAnimFrame frame)
        {
            Frames.Add(frame);
        }
    }

    public class VirtualOnTextureInfo
    {
        public int Width;
        public int Height;
        public Texture2D Texture;

        public static int ParseAddress(int word)
        {
            return (word & 0xfffc) * 8;
        }

        public static int ParseWidth(int word)
        {
            return (word & 0x0f00) >> 5;
        }

        public static int ParseHeight(int word)
        {
            return word & 0x00ff;
        }
    }
}
