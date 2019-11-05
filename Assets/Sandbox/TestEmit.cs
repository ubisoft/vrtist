using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestEmit : MonoBehaviour
{
    public void EmitParticles()
    {
        GetComponent<ParticleSystem>().Emit(100);
    }
}
