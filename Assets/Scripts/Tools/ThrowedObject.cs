using System.Collections;

using UnityEngine;


namespace VRtist
{
    public class ThrowedObject : MonoBehaviour
    {
        float timeout = 10f;
        float startTime;
        float epsilon = 0.01f;
        float scaleDuration = 0.2f;
        Vector3 prevPos;
        Vector3 force;
        float initialScale = 0.1f;
        float scale = 1f;

        Rigidbody rb;

        void Start()
        {
            prevPos = transform.position;
            startTime = Time.time;
            transform.localScale = Vector3.one * initialScale;

            Collider collider = GetComponent<Collider>();
            collider.isTrigger = false;
            rb = gameObject.AddComponent<Rigidbody>();
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            rb.AddForce(force, ForceMode.Impulse);

            SoundManager.Instance.PlayUISound(SoundManager.Sounds.Spawn, force: true);

            StartCoroutine(Rescale());
        }

        IEnumerator Rescale()
        {
            float startTime = Time.time;
            float dt = 0f;
            while (dt < scaleDuration)
            {
                float s = Mathf.Lerp(initialScale, scale, dt / scaleDuration);
                transform.localScale = Vector3.one * s;
                dt = Time.time - startTime;
                yield return null;
            }
            transform.localScale = Vector3.one * scale;
        }

        void Update()
        {
            if (Time.time - startTime < scaleDuration) { return; }

            if (Vector3.Distance(prevPos, transform.position) < epsilon || Time.time - startTime > timeout)
            {
                Destroy(rb);
                Destroy(this);
            }
        }

        public void AddForce(Vector3 force)
        {
            this.force = force;
        }

        public void SetScale(float value)
        {
            scale = value;
        }
    }
}
