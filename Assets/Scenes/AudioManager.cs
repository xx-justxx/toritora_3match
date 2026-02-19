using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scenes
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance;        //供其他Manager腳本呼叫

        [Header("Audio Sources")]
        [SerializeField] private AudioSource SESource;
        [SerializeField] private AudioSource BGMSource;

        [Header("Audio Clip")]
        public AudioClip SEClip_Match;          //Match時的音效

        [Header("音量 Sliders")]
        public Slider SliderBGM;
        public Slider SliderSE;

        private float _defaultBGMVol = 0.5f;
        private float _defaultSEVol = 0.8f;
        private const string PREF_BGM = "Volume_BGM";
        private const string PREF_SE = "Volume_SE";

        private void Awake()
        {//場景清空時會一起清空場景上的GameObject，需要重設Instance
            Instance = this;
        }

        private void Start()
        {//有設DontDestroyOnLoad的情況，只有第一次進入時會做Start()
            //讀取存檔音量設定，PlayerPrefs會將狀態寫進硬碟（或瀏覽器緩存），用於讓下次啟動時能維持狀態
            float bgmVol = PlayerPrefs.GetFloat(PREF_BGM, _defaultBGMVol);
            float seVol = PlayerPrefs.GetFloat(PREF_SE, _defaultSEVol);

            if (SliderBGM != null && BGMSource != null)
            {
                BGMSource.volume = bgmVol;
                SliderBGM.value = BGMSource.volume;
                SliderBGM.onValueChanged.AddListener(SetBGMVolume);
            }
            if (SliderSE != null && SESource != null)
            {
                SESource.volume = seVol;
                SliderSE.value = SESource.volume;
                SliderSE.onValueChanged.AddListener(SetSEVolume);
            }
        }

        public void SetBGMVolume(float value)
        {//設定 BGM 音量
            BGMSource.volume = value;
            PlayerPrefs.SetFloat(PREF_BGM, value);
            PlayerPrefs.Save();
        }

        public void SetSEVolume(float value)
        {//設定 SE 音量
            SESource.volume = value;
            PlayerPrefs.SetFloat(PREF_SE, value);
            PlayerPrefs.Save();
        }

        public void PlayBGM()
        {//播放 BGM
            BGMSource.loop = true;
            BGMSource.Play();
        }
        

        public void PlaySE_Match()
        {//播放 SE
            SESource.PlayOneShot(SEClip_Match);
        }
    }
}
