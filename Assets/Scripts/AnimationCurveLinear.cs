using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationCurveLinear
{
    static public AnimationCurve Convert(AnimationCurve curve)
    {
        AnimationCurve outCurve = new AnimationCurve();

        for (int count_key = 0; count_key < curve.keys.Length; count_key++)
        {
            float intangent = 0f;
            float outtangent = 0f;
            bool intangent_set = false;
            bool outtangent_set = false;
            Vector2 point1;
            Vector2 point2;
            Vector2 deltapoint;
            Keyframe key = curve[count_key];

            if (count_key == 0)
            {
                intangent = 0f;
                intangent_set = true;
            }

            if (count_key == curve.keys.Length - 1)
            {
                outtangent = 0f;
                outtangent_set = true;
            }

            if (!intangent_set)
            {
                point1.x = curve.keys[count_key - 1].time;
                point1.y = curve.keys[count_key - 1].value;
                point2.x = curve.keys[count_key].time;
                point2.y = curve.keys[count_key].value;

                deltapoint = point2 - point1;

                intangent = deltapoint.y / deltapoint.x;
            }
            if (!outtangent_set)
            {
                point1.x = curve.keys[count_key].time;
                point1.y = curve.keys[count_key].value;
                point2.x = curve.keys[count_key + 1].time;
                point2.y = curve.keys[count_key + 1].value;

                deltapoint = point2 - point1;

                outtangent = deltapoint.y / deltapoint.x;
            }

            key.inTangent = intangent;
            key.outTangent = outtangent;
            outCurve.AddKey(key);
        }

        return outCurve;
    }
}
