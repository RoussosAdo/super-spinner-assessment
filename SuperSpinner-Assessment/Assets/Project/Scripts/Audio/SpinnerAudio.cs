using UnityEngine;

namespace SuperSpinner.Audio
{
    public sealed class SpinnerAudio : MonoBehaviour
    {
        [Header("Source")]
        [SerializeField] private AudioSource source;

        [Header("Clips")]
        [SerializeField] private AudioClip spinLoop;
        [SerializeField] private AudioClip spinStop;
        [SerializeField] private AudioClip win;
        [SerializeField] private AudioClip tick;


        [Header("Volumes")]
        [Range(0f, 1f)][SerializeField] private float loopVol = 0.6f;
        [Range(0f, 1f)][SerializeField] private float sfxVol = 0.6f;
        [Range(0f, 1f)][SerializeField] private float tickVol = 1f;


        private void Reset()
        {
            source = GetComponent<AudioSource>();
        }

        public void PlaySpinLoop()
        {
            if (source == null || spinLoop == null) return;

            source.Stop();
            source.clip = spinLoop;
            source.volume = loopVol;
            source.loop = true;
            source.Play();
        }

        public void StopSpinLoop()
        {
            if (source == null) return;

            if (source.loop)
            {
                source.loop = false;
                source.Stop();
            }
        }

        public void PlayTick()
        {
            if (source == null || tick == null) return;
            source.PlayOneShot(tick, tickVol);
        }


        public void PlayStop()
        {
            if (source == null || spinStop == null) return;
            source.PlayOneShot(spinStop, sfxVol);
        }

        public void PlayWin()
        {
            if (source == null || win == null) return;
            source.PlayOneShot(win, sfxVol);
        }
    }
}
