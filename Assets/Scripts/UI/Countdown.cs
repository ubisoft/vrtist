using UnityEngine;
using UnityEngine.Events;

namespace VRtist
{
    public class Countdown : MonoBehaviour
    {
        public Sprite[] sprites;
        public UnityEvent onCountdownFinished = new UnityEvent();

        private MeshRenderer meshRenderer = null;
        private bool countdownActive = false;
        private float accTime = 0.0f;
        private int prevIdx = -1;

        public void OnEnable()
        {
            meshRenderer = GetComponentInChildren<MeshRenderer>();
            meshRenderer.material.SetTexture("_UnlitColorMap", sprites[0].texture); // 3
            accTime = 0.0f;
            prevIdx = 0;
            countdownActive = true;
        }

        public void OnDisable()
        {
            countdownActive = false;
        }

        void Update()
        {
            if (countdownActive)
            {
                float dt = Time.unscaledDeltaTime;
                if (accTime >= 3.0f)
                {
                    onCountdownFinished.Invoke();
                    countdownActive = false;
                    gameObject.SetActive(false);
                }
                else 
                {
                    // TODO: pulse scale anim

                    int idx = Mathf.FloorToInt(accTime);
                    if (idx != prevIdx)
                    {
                        meshRenderer.material.SetTexture("_UnlitColorMap", sprites[idx].texture);
                        prevIdx = idx;
                    }
                    accTime += dt;
                }
            }
        }
    }
}
