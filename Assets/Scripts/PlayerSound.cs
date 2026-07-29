using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(AudioSource))]

public class PlayerSound : MonoBehaviour
{
    [Header("Audio Source")]
    [SerializeField] private AudioSource audioSource;

    [Header("Sound Effects")]
    [SerializeField] private AudioClip whoosh;

    [SerializeField] private AudioClip[] player_hit_wall;
    [SerializeField] private AudioClip ball_hit_wall;
    [SerializeField] private AudioClip teleport;
    [SerializeField] private AudioClip player_hit_laser;
    [SerializeField] private AudioClip socket;
    [SerializeField] private float volume = 1f;

    private int lastPlayedIndex = -1;

    private void Awake()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }

    public void PlayWhoosh()
    {
        PlaySound(whoosh);
    }

    public void PlayPlayerHitWall()
    {
        if (player_hit_wall == null || player_hit_wall.Length == 0)
        {
            return;
        }

        int randomIndex;

        if (player_hit_wall.Length == 1)
        {
            randomIndex = 0;
        }
        else
        {
            do
            {
                randomIndex = Random.Range(0, player_hit_wall.Length);
            }
            while (randomIndex == lastPlayedIndex);
        }

        lastPlayedIndex = randomIndex;

        AudioClip selectedClip = player_hit_wall[randomIndex];

        if (selectedClip != null)
        {
            audioSource.PlayOneShot(selectedClip, volume);
        }
    }

    public void PlayBallHitWall()
    {
        PlaySound(ball_hit_wall);
    }

    public void PlayTeleport()
    {
        PlaySound(teleport);
    }

    public void PlayPlayerHitLaser()
    {
        PlaySound(player_hit_laser);
    }
    public void PlaySocket()
    {
        PlaySound(socket);
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource == null || clip == null)
        {
            return;
        }

        audioSource.PlayOneShot(clip);
    }
}