using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using VirtualOnData;

public class VirtualOnCharacter : MonoBehaviour
{
    private Animation _lowerAnim = null;
    private Animation _upperAnim = null;

    private int _currentAnimIndex = -1;
    private int _animIndex;

    Transform _upperBodyLink;

    int[] _animTable = { 0x00, 0x03, 0x05, 0x07, 0x08, 0x09, 0x0c, 0x10, 0x11, 0x14, 0x15,
                         0x19, 0x1c, 0x1d, 0x1f, 0x22, 0x23, 0x25, 0x26, 0x2a, 0x2b, 0x32,
                         0x35, 0x3d, 0x3f, 0x40, 0x41, 0x42, 0x44, 0x45, 0x46, 0x48, 0x49,
                         0x4a, 0x53, 0x5b, 0x66, 0x6c, 0x6f, 0x74, 0x76, 0x7b, 0x7c, 0x89,
                         0x8f, 0x97, 0x9a, 0x9b, 0xa1, 0xa7, 0xa9, 0xac, 0xae, 0xb2, 0xb7,
                         0xc3, 0xc5, 0xc9, 0xcc, 0xcf, 0xd2, 0xd9, 0xe0, 0xe1, 0xe3, 0xee
                        };

    bool _animDemo = false;
    float _animDuration;

    public void Init(List<Transform> lowerPartTransforms,
                     List<Transform> upperPartTransforms,
                     List<VirtualOnAnimation> animLower,
                     List<VirtualOnAnimation> animUpper)
    {
        GameObject lowerBodyRoot = new GameObject("LowerRoot");
        GameObject upperBodyRoot = new GameObject("UpperRoot");

        _lowerAnim = CreateAnimationClips(lowerBodyRoot, lowerPartTransforms, animLower, false);

        if (upperPartTransforms.Count > 0)
        {
            _upperAnim = CreateAnimationClips(upperBodyRoot, upperPartTransforms, animUpper, true);

            _upperBodyLink = lowerPartTransforms[lowerPartTransforms.Count - 1];   // last transform of lower body is upper body link
            upperBodyRoot.transform.parent = _upperBodyLink;
            upperBodyRoot.transform.localPosition = Vector3.zero;
            upperBodyRoot.transform.localRotation = Quaternion.identity;
            upperBodyRoot.transform.localScale = Vector3.one;
        }

        if (_animDemo)
        {
            PlayAnimByIndex(_animTable[_animIndex]);
            _animDuration = 2f; 
        }
        else
        {
            _animIndex = 0;
            PlayAnimByIndex(_animIndex);
        }

        RotateObject rotate = gameObject.AddComponent<RotateObject>();
        rotate._rotateYSpeed = 90f;
    }

    void Start()
    {
    }

    void Update()
    {
        if (_animDemo)
        {
            _animDuration -= Time.deltaTime;
            if (_animDuration < 0f)
            {
                _animIndex++;
                PlayAnimByIndex(_animTable[_animIndex]);
                _animDuration += 2f;
            }
        }
        else
        {
            DebugAnimations();
        }
    }

    private Animation CreateAnimationClips(GameObject animRoot, List<Transform> partTransforms, List<VirtualOnAnimation> animList, bool invertRoot)
    {
        int animNr = 0;
        int transformCount = animList[0].Frames[0].Transforms.Count;

        animRoot.transform.parent = transform;
        animRoot.transform.localScale = Vector3.one;

        foreach (Transform partTransform in partTransforms)
        {
            if (partTransform != animRoot.transform)
            {
                partTransform.parent = animRoot.transform;
                partTransform.localScale = Vector3.one;
            }
        }

        Animation anim = animRoot.AddComponent<Animation>();

        float fps = 20f;

        for (int animIndex = 0; animIndex < animList.Count; animIndex++)
        {
            VirtualOnAnimation virtualOnAnimation = animList[animIndex];

            string animName = GetAnimationName(animNr);

            AnimationClip clip = new AnimationClip();
            clip.legacy = true;

            List<Keyframe>[] translateXList = new List<Keyframe>[transformCount];
            List<Keyframe>[] translateYList = new List<Keyframe>[transformCount];
            List<Keyframe>[] translateZList = new List<Keyframe>[transformCount];

            List<Keyframe>[] rotationXList = new List<Keyframe>[transformCount];
            List<Keyframe>[] rotationYList = new List<Keyframe>[transformCount];
            List<Keyframe>[] rotationZList = new List<Keyframe>[transformCount];
            List<Keyframe>[] rotationWList = new List<Keyframe>[transformCount];

            for (int count = 0; count < transformCount; count++)
            {
                translateXList[count] = new List<Keyframe>();
                translateYList[count] = new List<Keyframe>();
                translateZList[count] = new List<Keyframe>();

                rotationXList[count] = new List<Keyframe>();
                rotationYList[count] = new List<Keyframe>();
                rotationZList[count] = new List<Keyframe>();
                rotationWList[count] = new List<Keyframe>();
            }

            int frameCount = virtualOnAnimation.Frames.Count;

            float frameDuration = (1f / fps);

            for (int frameIndex = 0; frameIndex < frameCount; frameIndex++)
            {
                float frameTime = (frameIndex * frameDuration);

                VirtualOnAnimFrame animFrame = virtualOnAnimation.Frames[frameIndex];

                Quaternion rootRotation = Quaternion.identity;
                Vector3 rootPosition = Vector3.zero;

                for (int transformIndex = 0; transformIndex < transformCount; transformIndex++)
                {
                    VirtualOnTranform animTransform = animFrame.Transforms[transformIndex];

                    Quaternion rotation = animTransform.Rotation;
                    Vector3 position = animTransform.Position;

                    if (invertRoot)
                    {
                        if (transformIndex == 0)
                        {
                            rootRotation = rotation;
                            rootPosition = position;
                        }
                        else
                        {
                            Transform partTransform = partTransforms[transformIndex];
                            partTransform.parent = gameObject.transform;                    // part unter object root
                            partTransform.localScale = Vector3.one;

                            partTransform.localPosition = position;                         // part bekommt seine position (object world)
                            partTransform.localRotation = rotation;

                            animRoot.transform.parent = transform;                          // anim root unter object root
                            animRoot.transform.localScale = Vector3.one;

                            animRoot.transform.localPosition = rootPosition;                // anim root bekommt seine position (object world)
                            animRoot.transform.localRotation = rootRotation;

                            partTransform.parent = animRoot.transform;                      // part unter anim root -> pivot gesetzt -> local position kann ausgelesen werden
                            animRoot.transform.localScale = Vector3.one;

                            position = partTransform.localPosition;
                            rotation = partTransform.localRotation;
                        }
                    }

                    Keyframe animKey = new Keyframe(frameTime, rotation.x);
                    rotationXList[transformIndex].Add(animKey);
                    animKey = new Keyframe(frameTime, rotation.y);
                    rotationYList[transformIndex].Add(animKey);
                    animKey = new Keyframe(frameTime, rotation.z);
                    rotationZList[transformIndex].Add(animKey);
                    animKey = new Keyframe(frameTime, rotation.w);
                    rotationWList[transformIndex].Add(animKey);

                    animKey = new Keyframe(frameTime, position.x);
                    translateXList[transformIndex].Add(animKey);
                    animKey = new Keyframe(frameTime, position.y);
                    translateYList[transformIndex].Add(animKey);
                    animKey = new Keyframe(frameTime, position.z);
                    translateZList[transformIndex].Add(animKey);
                }
            }

            // create animation curves
            //
            AnimationCurve curve;

            for (int transformIndex = 0; transformIndex < transformCount; transformIndex++)
            {
                Transform transformObj = partTransforms[transformIndex];

                string childPath = GetChildPath(animRoot.transform, transformObj);

                curve = new AnimationCurve();
                curve.keys = rotationXList[transformIndex].ToArray();
                curve = AnimationCurveLinear.Convert(curve);
                clip.SetCurve(childPath, typeof(Transform), "m_LocalRotation.x", curve);

                curve = new AnimationCurve();
                curve.keys = rotationYList[transformIndex].ToArray();
                curve = AnimationCurveLinear.Convert(curve);
                clip.SetCurve(childPath, typeof(Transform), "m_LocalRotation.y", curve);

                curve = new AnimationCurve();
                curve.keys = rotationZList[transformIndex].ToArray();
                curve = AnimationCurveLinear.Convert(curve);
                clip.SetCurve(childPath, typeof(Transform), "m_LocalRotation.z", curve);

                curve = new AnimationCurve();
                curve.keys = rotationWList[transformIndex].ToArray();
                curve = AnimationCurveLinear.Convert(curve);
                clip.SetCurve(childPath, typeof(Transform), "m_LocalRotation.w", curve);

                curve = new AnimationCurve();
                curve.keys = translateXList[transformIndex].ToArray();
                curve = AnimationCurveLinear.Convert(curve);
                clip.SetCurve(childPath, typeof(Transform), "m_LocalPosition.x", curve);

                curve = new AnimationCurve();
                curve.keys = translateYList[transformIndex].ToArray();
                curve = AnimationCurveLinear.Convert(curve);
                clip.SetCurve(childPath, typeof(Transform), "m_LocalPosition.y", curve);

                curve = new AnimationCurve();
                curve.keys = translateZList[transformIndex].ToArray();
                curve = AnimationCurveLinear.Convert(curve);
                clip.SetCurve(childPath, typeof(Transform), "m_LocalPosition.z", curve);
            }

            clip.frameRate = fps;
            clip.EnsureQuaternionContinuity();
            clip.wrapMode = WrapMode.Loop;
            anim.AddClip(clip, animName);

            animNr++;
        }

        return anim;
    }

    private string GetChildPath(Transform rootTransform, Transform childTransform)
    {
        if (rootTransform == childTransform)
        {
            return "";
        }

        string path = childTransform.name;
        while (childTransform.parent != rootTransform)
        {
            childTransform = childTransform.parent;
            path = childTransform.name + "/" + path;
        }
        return path;
    }

    private string GetAnimationName(int index)
    {
        return "anim" + index;
    }

    private float PlayAnim(Animation anim, int index, bool loop = true, bool fade = true, float speed = 0.25f,
                           bool blend = false, float blendWeight = 0.5f, float fadeTime = 0.25f, bool pingPong = false)
    {
        string name = GetAnimationName(index);

        if (anim[name] == null)
        {
            return 0f;
        }

        if (loop == false)
        {
            if (pingPong == true)
            {
                anim[name].wrapMode = WrapMode.PingPong;
            }
            else
            {
                anim[name].wrapMode = WrapMode.ClampForever;
            }
        }
        else
        {
            anim[name].wrapMode = WrapMode.Loop;
        }

        anim[name].speed = speed;

        if (fade == true)
        {
            // cross fade
            if (_currentAnimIndex != -1)
            {
                anim[GetAnimationName(_currentAnimIndex)].weight = 1f;
            }
            anim[name].weight = 0f;

            anim.CrossFade(name, fadeTime);
        }
        else if (blend == true)
        {
            // blend
            anim[GetAnimationName(_currentAnimIndex)].weight = 0.5f;
            anim.Blend(name, blendWeight, 0.05f);
        }
        else
        {
            // switch anim (no fade or blend)
            anim.Stop();

            anim[name].weight = 1f;
            anim.Play(name);
        }

        _currentAnimIndex = index;

        return anim[name].length / speed;
    }

    private void DebugAnimations()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard != null)
        {
            if (keyboard.f12Key.wasPressedThisFrame)
            {
                _animIndex++;
                PlayAnimByIndex(_animIndex);
            }
            else if (keyboard.f11Key.wasPressedThisFrame)
            {
                _animIndex--;
                PlayAnimByIndex(_animIndex);
            }
        }
    }

    private void PlayAnimByIndex(int animIndex)
    {
        PlayAnim(_lowerAnim, animIndex, true, true, 1.0f);

        if (_upperAnim != null)
        {
            PlayAnim(_upperAnim, animIndex, true, true, 1.0f);
        }
    }
}