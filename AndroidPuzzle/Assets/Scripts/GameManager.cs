using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    [Header("---------[ Core ]")]
    public int maxLevel;
    public int score;
    public bool isOver;

    [Header("---------[ ObJect Pooling ]")]
    public Dongle lastDongle;
    public GameObject donglePrefab;
    public Transform dongleGroup;
    public GameObject effectPrefab;
    public Transform effectGroup;
    public List<Dongle> donglePools;
    public List<ParticleSystem> effectPools;
    [Range(1, 30)]
    public int poolSize;
    public int poolCursor;

    [Header("---------[ Audio ]")]
    public AudioSource bgmPlayer;
    public AudioSource[] sfxPlayers;
    public AudioClip[] sfxClips;

    [Header("---------[ UI ]")]
    public GameObject startGroup;
    public GameObject endGroup;
    public Text scoreText;
    public Text maxScoreText;
    public Text subScoreText;

    [Header("---------[ ETC ]")]
    public GameObject line;
    public GameObject bottom;


    public enum Sfx
    {
        LevelUp,
        Next,
        Attach,
        Button,
        GameOver
    }
    int sfxCurser;

    

    private void Awake()
    {
        Application.targetFrameRate = 60;

        donglePools = new List<Dongle>();
        effectPools = new List<ParticleSystem>();

        for(int i = 0; i < poolSize; i++)
        {
            MakeDongle();
        }


        if (!PlayerPrefs.HasKey("MaxScore"))
        {
            PlayerPrefs.SetInt("MaxScore", 0);
        }

        maxScoreText.text = PlayerPrefs.GetInt("MaxScore").ToString();
    }

    public void GameStart()
    {
        line.SetActive(true);
        bottom.SetActive(true);
        scoreText.gameObject.SetActive(true);
        maxScoreText.gameObject.SetActive(true);
        startGroup.SetActive(false);

        bgmPlayer.Play();
        SfxPlay(Sfx.Button);
        // 1.5초 뒤에 CreateDongle() 실행
        Invoke("CreateDongle", 1.5f);
    }

    Dongle MakeDongle()
    {
        // 이펙트 생성
        ParticleSystem instantEffect = Instantiate(effectPrefab, effectGroup).GetComponent<ParticleSystem>();
        instantEffect.name = "Effect" + effectPools.Count;
        effectPools.Add(instantEffect);

        // 동글 생성
        Dongle instantDongle = Instantiate(donglePrefab, dongleGroup).GetComponent<Dongle>();
        instantDongle.name = "Dongle" + donglePools.Count;
        instantDongle.gameManager = this;
        // 동글에 이펙트 등록
        instantDongle.effect = instantEffect;
        donglePools.Add(instantDongle);

        return instantDongle;
    }

    private Dongle GetDongle()
    {
        for(int i = 0; i < donglePools.Count; i++)
        {
            poolCursor = (poolCursor + 1) % donglePools.Count;

            if (!donglePools[poolCursor].gameObject.activeSelf)
            {
                return donglePools[poolCursor];
            }
        }
        return MakeDongle();
    }

    private void CreateDongle()
    {
        if (isOver)
        {
            return;
        }
     
        lastDongle = GetDongle();
        lastDongle.level = Random.Range(0, maxLevel);
        lastDongle.gameObject.SetActive(true);

        SfxPlay(Sfx.Next);
        StartCoroutine(WaitCreate());
    }

    // corrutine, 유니티에 제어 넘김
    IEnumerator WaitCreate()
    {
        while (lastDongle != null)
        {
            // 1프레임 휴식, 설정 안하면 유니티 멈춤
            yield return null;
        }

        // 2.5초 휴식
        yield return new WaitForSeconds(2.5f);

        CreateDongle();
    }

    public void TouchDown()
    {   
        if (lastDongle == null)
        {
            return;
        }

        lastDongle.Drag();
    }

    public void TouchUp()
    {
        if (lastDongle == null)
        {
            return;
        }

        lastDongle.Drop();
        lastDongle = null;
    }

    public void GameOver()
    {
        if (isOver)
        {
            return;
        }

        isOver = true;

        StartCoroutine(GameOverRoutine());
    }

    IEnumerator GameOverRoutine()
    {
        Dongle[] dongles = GameObject.FindObjectsOfType<Dongle>();

        // 물리 끄기(합치기 방지)
        for (int i = 0; i < dongles.Length; i++)
        {
            dongles[i].rigid.simulated = false;
        }

        // 하나씩 사라지게
        for (int i = 0; i < dongles.Length; i++)
        {
            // y값을 scene 밖으로 배치
            dongles[i].Hide(Vector3.up * 100);
            yield return new WaitForSeconds(0.1f);
        }

        yield return new WaitForSeconds(1f);

        // 최고점수
        int maxScore = Mathf.Max(score, PlayerPrefs.GetInt("MaxScore"));
        PlayerPrefs.SetInt("MaxScore", maxScore);

        // 게임오버 ui
        subScoreText.text = "점수: " + scoreText.text;
        endGroup.SetActive(true);

        bgmPlayer.Stop();
        SfxPlay(Sfx.GameOver);
    }

    public void Reset()
    {
        SfxPlay(Sfx.Button);
        StartCoroutine(ResetRoutine());

    }

    IEnumerator ResetRoutine()
    {
        yield return new WaitForSeconds(1f);
        SceneManager.LoadScene("Main");

    }

    public void SfxPlay(Sfx type)
    {
        switch (type)
        {
            case Sfx.LevelUp:
                sfxPlayers[sfxCurser].clip = sfxClips[Random.Range(0, 3)];
                break;
            case Sfx.Next:
                sfxPlayers[sfxCurser].clip = sfxClips[3];
                break;
            case Sfx.Attach:
                sfxPlayers[sfxCurser].clip = sfxClips[4];
                break;
            case Sfx.Button:
                sfxPlayers[sfxCurser].clip = sfxClips[5];
                break;
            case Sfx.GameOver:
                sfxPlayers[sfxCurser].clip = sfxClips[6];
                break;
        }

        sfxPlayers[sfxCurser].Play();
        sfxCurser = (sfxCurser + 1) % sfxPlayers.Length;
    }

    private void Update()
    {
        if(Input.GetButtonDown("Cancel"))
        {
            Application.Quit();
        }
    }

    private void LateUpdate()
    {
        scoreText.text = score.ToString();
    }
}
