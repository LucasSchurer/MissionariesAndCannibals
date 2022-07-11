using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles the camera movement.
/// </summary>
public class CameraController : MonoBehaviour
{
    private Transform _target;
    private bool _isFollowing = true;
    [SerializeField]
    private float _interpolationSpeed;
    [SerializeField]
    private float _speed;
    [SerializeField]
    private float _zoomSensitivity;

    private Camera cam;

    private void Awake()
    {
        cam = GetComponent<Camera>();
    }

    private void Update()
    {
        if (!_isFollowing)
        {
            Move();
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            _isFollowing = !_isFollowing;
        }

        cam.orthographicSize -= Input.GetAxisRaw("Mouse ScrollWheel") * _zoomSensitivity * Time.deltaTime;
    }
    private void LateUpdate()
    {
        if (_isFollowing)
        {
            Follow();
        }
    }
    public void SetTarget(Transform target)
    {
        if (target != null)
        {
            _target = target;
        }
    }

    /// <summary>
    /// Move the camera based on keyboard input.
    /// </summary>
    private void Move()
    {
        Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        Vector3 newPosition = (Vector2)transform.position + new Vector2(input.x * _speed * Time.deltaTime, input.y * _speed * Time.deltaTime);
        newPosition.z = -10;
        transform.position = newPosition;
    }

    /// <summary>
    /// Interpolate the camera position to the target position.
    /// </summary>
    private void Follow()
    {
        if (_target != null)
        {
            Vector3 newPosition = Vector3.Lerp(transform.position, _target.position, _interpolationSpeed);
            newPosition.z = -10;
            transform.position = newPosition;
        }
    }
}
