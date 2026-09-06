using UnityEngine;

public class VFX_OnCollect : MonoBehaviour
{
    [SerializeField] private ParticleSystem particleSystem;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
      
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player") && particleSystem != null)
        {
            particleSystem.transform.position = transform.position;
            // Play the particle system attached to this GameObject.
            particleSystem.Play();
        }
    }
}
