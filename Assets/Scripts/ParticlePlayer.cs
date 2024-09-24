using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticlePlayer : MonoBehaviour
{
    public ParticleSystem[] allParticles;
    public float lifetime = 1f;
    public bool destroyImmediatly = true;
    void Start()
    {
        allParticles = GetComponentsInChildren<ParticleSystem>();
        if (destroyImmediatly)
            Destroy(gameObject, lifetime);
    }
    public void Play()
    {
        foreach (ParticleSystem ps  in allParticles)
        {
            ps.Stop();
            ps.Play();
        }

        Destroy(gameObject, lifetime);
    }
}
