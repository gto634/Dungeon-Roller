using UnityEngine;

public class FlamethrowerBrain : MonoBehaviour
{
    [SerializeField] Transform _transform;
    [SerializeField] float _speedRotation;


    bool _deteceted = false;

    void Update()
    {
        bool _isGrounded = Physics.Raycast(_transform.position, _transform.forward, 3f,6);
        Debug.DrawRay(_transform.position, _transform.forward * 1, Color.red);

        if (_isGrounded)
        {

        }

        if (_deteceted)
        {

        }
        else
        {
            float rot = _speedRotation * Time.deltaTime;
            Vector3 toRotate = new Vector3(0, rot, 0);
            _transform.Rotate(toRotate);

        }
    }
}
