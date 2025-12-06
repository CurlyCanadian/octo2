using UnityEngine;

[RequireComponent(typeof(Collider))]
public class LevelAudio : MonoBehaviour
{
    [SerializeField] 
    private AudioSource audioSource;

    private void Reset()
    {
        // Make sure this collider is set as a trigger automatically
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Optional: avoid restarting if it’s already playing
            if (!audioSource.isPlaying)
            {
                audioSource.Play();
            }
        }
    }
}
