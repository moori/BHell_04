using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;

    public AudioSource sfxSource;
    public AudioSource bgmSource;

    [Header("Mixer")]
    public AudioMixerGroup BGM_Group;
    public AudioMixerGroup SFX_Group;

    [Header("Clips")]
    public List<AudioClip> levelBGMs;
    public List<AudioClip> bossBGMs;
    public AudioClip upgradePhaseMusic;
    public AudioClip ambienceMusic;


    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }

        bossBGMs.Shuffle();
        levelBGMs.Shuffle();
    }

    private void Start()
    {
        //InvokeRepeating(nameof(CheckPlaylistRoutine), 2f, 1f);
        //PlayLevelMusic();

        BGM_Group.audioMixer.SetFloat(BGM_Group.name, VolumeController.LinearToDecibel(PlayerPrefs.GetFloat(BGM_Group.name, 0.75f)));
        SFX_Group.audioMixer.SetFloat(SFX_Group.name, VolumeController.LinearToDecibel(PlayerPrefs.GetFloat(SFX_Group.name, 1f)));
    }

    private void OnDestroy()
    {
        instance = null;
    }

    public static AudioSource GetAudioSource()
    {
        return instance.sfxSource;
    }

    public AudioClip GetNextBGM()
    {
        var clip = instance.levelBGMs[0];
        instance.levelBGMs.RemoveAt(0);
        instance.levelBGMs.Add(clip);
        return clip;
    }
    //public AudioClip GetNextBossBGM()
    //{
    //    var clip = instance.bossBGMs[0];
    //    instance.bossBGMs.RemoveAt(0);
    //    instance.bossBGMs.Add(clip);
    //    return clip;
    //}

    //public static void PlayBossMusic()
    //{
    //    var src = instance.bgmSource;
    //    src.loop = true;
    //    src.DOFade(0, 1f).OnComplete(() =>
    //    {
    //        src.volume = 1f;
    //        src.clip = instance.GetNextBossBGM();
    //        src.Play();
    //    });
    //}
    public static void PlayLevelMusic()
    {
        instance.StopAllCoroutines();
        var src = instance.bgmSource;
        DOTween.Kill(src);
        src.loop = false;
        src.volume = 1f;
        src.clip = instance.GetNextBGM();
        src.Play();
        src.DOFade(1f, 1f).OnComplete(()=> { 
            instance.DelayAction(src.clip.length + 2f, () => {
                PlayLevelMusic();
            });
        });
    }
    public static void PlayAmbience()
    {
        instance.StopAllCoroutines();
        var src = instance.bgmSource;
        DOTween.Kill(src);
        src.loop = true;

        src.clip = instance.ambienceMusic;
        src.Play();
        return;
    }
    public static void PauseBGM(bool pause)
    {
        if(pause)
            instance.bgmSource.Pause();
        else
            instance.bgmSource.UnPause();
    }
    public static void FadeOut()
    {
        instance.StopAllCoroutines();
        var src = instance.bgmSource;
        DOTween.Kill(src);
        src.DOFade(0, 1f);
    }
    //public static void PlayUpgradePhaseMusic()
    //{
    //    var src = instance.bgmSource;
    //    src.loop = true;

    //    src.volume = 1f;
    //    src.clip = instance.upgradePhaseMusic;
    //    src.Play();
    //}
    //public static void PlayFullMusicList()
    //{
    //    var src = instance.bgmSource;
    //    src.loop = false;
    //    src.volume = 0f;
    //    src.clip = instance.GetNextBGM();
    //    src.DOFade(1f, 2f);
    //    src.Play();

    //    instance.DelayAction(src.clip.length - 4f, () => {
    //        Debug.Log("StartFade to next music");
    //        src.DOFade(0, 4f).OnComplete(() =>
    //        {
    //            PlayFullMusicList();
    //        });
    //    });
    //}

    //public void CheckPlaylistRoutine()
    //{
    //    if (GameController.IsGameOver) return;
    //    if (!bgmSource.isPlaying)
    //    {
    //        PlayLevelMusic();
    //    }
    //}

}
