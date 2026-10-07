using UnityEngine;
using UnityEngine.UI;

namespace TheOneFramework.Audio
{
    // Put on a UI Slider (0..1) in a settings menu to control one mixer volume. The value is saved,
    // so it's the same next time the game starts.
    [RequireComponent(typeof(Slider))]
    public class VolumeSlider : MonoBehaviour
    {
        [SerializeField] private MixerVolume volume = MixerVolume.Master;

        private Slider slider;

        private void Awake()
        {
            slider = GetComponent<Slider>();
            slider.minValue = 0.0f;
            slider.maxValue = 1.0f;
        }

        private void OnEnable()
        {
            slider.SetValueWithoutNotify(GameAudio.GetVolume(volume));
            slider.onValueChanged.AddListener(OnChanged);
        }

        private void OnDisable()
        {
            slider.onValueChanged.RemoveListener(OnChanged);
        }

        private void OnChanged(float value)
        {
            GameAudio.SetVolume(volume, value);
        }
    }
}
