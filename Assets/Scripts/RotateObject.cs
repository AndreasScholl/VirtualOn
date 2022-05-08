using UnityEngine;

public class RotateObject : MonoBehaviour
{
    Vector3 _angles = Vector3.zero;
    public float _rotateYSpeed = 180f;

    private bool _updateActive = true;

    public void Update()
    {
        _angles.y += Time.deltaTime * _rotateYSpeed;

        if (_updateActive == true)
        {
            transform.localEulerAngles = _angles;
        }
    }

    public void SetUpdateActive(bool active)
    {
        _updateActive = active;
    }

    public void Reset()
    {
        _angles = Vector3.zero;
    }
}