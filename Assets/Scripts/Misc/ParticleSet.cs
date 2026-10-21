using UnityEngine;

[System.Serializable]
public class ParticleSet
{
    [SerializeField] private ParticleSystem[] particleSystems;
    [SerializeField] private TrailRenderer[] trailRenderers;
    [SerializeField] private Transform[] objectsToFlip;

    private bool facingLeft = false;

    public void Play()
    {
        if (particleSystems != null)
        {
            foreach (var particle in particleSystems)
            {
                if (particle != null)
                {
                    if (!particle.isPlaying)
                    {
                        particle.Play();
                    }
                }
            }
        }

        if (trailRenderers != null)
        {
            foreach (var trail in trailRenderers)
            {
                if (trail != null)
                {
                    trail.emitting = true;
                }
            }
        }
    }

    public void Stop() // stops EMISSION, doesn't clear
    {
        if (particleSystems != null)
        {
            foreach (var particle in particleSystems)
            {
                if (particle != null && particle.isPlaying)
                {
                    particle.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                }
            }
        }

        if (trailRenderers != null)
        {
            foreach (var trail in trailRenderers)
            {
                if (trail != null)
                {
                    trail.emitting = false;
                }
            }
        }
    }

    public void StopAndClear()
    {
        if (particleSystems != null)
        {
            foreach (var particle in particleSystems)
            {
                if (particle != null)
                {
                    particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                }
            }
        }

        if (trailRenderers != null)
        {
            foreach (var trail in trailRenderers)
            {
                if (trail != null)
                {
                    trail.Clear();
                    trail.emitting = false;
                }
            }
        }
    }

    public void Emit(int count = -1)
    {
        if (particleSystems != null)
        {
            foreach (var particle in particleSystems)
            {
                if (particle != null)
                {
                    if (count > 0)
                    {
                        particle.Emit(count);
                    }
                    else
                    {
                        particle.Emit(particle.emission.burstCount > 0 ? (int) particle.emission.GetBurst(0).count.constant : 10);
                    }
                }
            }
        }
    }

    public void Flip(bool facingLeft)
    {
        if (this.facingLeft == facingLeft) return;
        this.facingLeft = facingLeft;

        float targetXScale = facingLeft ? -1 : 1; // 1 = base scale

        if (objectsToFlip != null)
        {
            foreach (var t in objectsToFlip)
            {
                if (t != null)
                {
                    Vector3 scale = t.localScale;
                    scale.x = Mathf.Abs(scale.x) * (facingLeft ? -1f : 1f);
                    t.localScale = scale;
                }
            }
        }
    }
}