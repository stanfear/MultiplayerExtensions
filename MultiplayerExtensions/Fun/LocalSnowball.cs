using MultiplayerExtensions.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using VRUIControls;
using Zenject;

namespace MultiplayerExtensions.Fun
{
    class LocalSnowball : MonoBehaviour
    {
        [Inject]
        protected PacketManager packetManager;

        protected const float MaxLaserDistance = 5;
        protected static Vector3 SpawnPosition = new Vector3(0.9f, 0.1f, 0.9f);

        protected VRPointer _vrPointer;
        protected Rigidbody _rigidbody;

        protected bool _grabbed;

        protected virtual void Start()
        {
            _vrPointer = Resources.FindObjectsOfTypeAll<VRPointer>().First();
            if (!TryGetComponent<Rigidbody>(out _rigidbody))
                _rigidbody = gameObject.AddComponent<Rigidbody>();

            transform.position = SpawnPosition;
            transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
            _rigidbody.isKinematic = true;
        }

        protected Vector3 lastPosition = SpawnPosition;

        protected virtual void Update()
        {
            VRPointer pointer = _vrPointer;
            if (pointer?.vrController != null)
            {
                if (!_grabbed && (pointer.vrController.triggerValue > 0.9f || Input.GetMouseButton(0)))
                {
                    if (Physics.Raycast(pointer.vrController.position, pointer.vrController.forward, out RaycastHit hit, MaxLaserDistance))
                    {
                        if (hit.transform == transform)
                            _grabbed = true;
                    }
                }

                if (_grabbed)
                {
                    transform.position = pointer.vrController.position;
                    transform.rotation = pointer.vrController.rotation;

                    if (pointer.vrController.triggerValue < 0.9f && !Input.GetMouseButton(0))
                    {
                        _grabbed = false;
                        _rigidbody.isKinematic = false;
                        _rigidbody.velocity = ((transform.position - lastPosition) / Time.deltaTime) * 2f;
                    }
                }
            }

            if (transform.position.y <= 0)
            {
                _rigidbody.isKinematic = true;
                transform.position = SpawnPosition;
            }

            lastPosition = transform.position;
        }
    }
}
