using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Dongle : MonoBehaviour
{
    public GameManager gameManager;
    public ParticleSystem effect;

    public int level;
    public bool isDrag;
    public bool isMerge;
    private bool isAttach;

    public Rigidbody2D rigid;
    CircleCollider2D circleCollider;
    Animator animator;
    SpriteRenderer spriteRenderer;

    float deadTime;
    

    private void Awake()
    {
        rigid = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        circleCollider = GetComponent<CircleCollider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        deadTime = 0;
    }

    private void OnEnable()
    {
        animator.SetInteger("Level", level);
    }

    private void OnDisable()
    {
        level = 0;
        isDrag = false;
        isMerge = false;
        isAttach = false;

        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        transform.localScale = Vector3.one;

        rigid.velocity = Vector2.zero;
        rigid.simulated = false;
        rigid.angularVelocity = 0;
        circleCollider.enabled = true;
    }

    void Update()
    {   
        if (isDrag)
        {
            // 스크린 좌표(게임 화면) -> 월드 좌표(게임화면 밖도 포함)
            Vector3 mousePostion = Camera.main.ScreenToWorldPoint(Input.mousePosition);

            // 좌우 경계: 벽 위치, 두께, 반지름 고려
            float leftBoarder = -4.2f + transform.localScale.x / 2;
            float rightBoarder = 4.2f - transform.localScale.x / 2;

            if (mousePostion.x < leftBoarder)
            {
                mousePostion.x = leftBoarder;
            }
            else if (mousePostion.x > rightBoarder)
            {
                mousePostion.x = rightBoarder;
            }

            // y축 고정
            mousePostion.y = 8;
            // z축 고정(안하면 카메라 위치 따라가서 안보임)
            // 화면에 보여야 함
            mousePostion.z = 0;
            // 내 위치 = 마우스 위치
            // Lerf 출발지점 -> 목표지점까지 지정한 속도로 이동
            transform.position = Vector3.Lerp(transform.position, mousePostion, 0.2f);
        }
        
    }

    public void Drag()
    {
        isDrag = true;
    }

    public void Drop()
    {
        isDrag=false;

        // 중력 적용
        rigid.simulated = true;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        StartCoroutine(AttachRoutine());
    }

    IEnumerator AttachRoutine()
    {
        if (isAttach)
        {
            yield break;
        }

        isAttach = true;
        gameManager.SfxPlay(GameManager.Sfx.Attach);

        yield return new WaitForSeconds(0.2f);

        isAttach = false;
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if(collision.gameObject.tag == "Dongle")
        {
            Dongle otherDongle = collision.gameObject.GetComponent<Dongle>();

            // 동일 레벨, 합치는 중 X, 레벨 7 미만
            if (otherDongle.level == level && !isMerge && !otherDongle.isMerge && level < 7)
            {
                float myX = transform.position.x;
                float myY = transform.position.y;
                float otherX = otherDongle.transform.position.x;
                float otherY = otherDongle.transform.position.y;


                // 내가 아래, 상대보다 오른쪽(동일 높이)
                if(myY < otherY || (myY == otherY && myX > otherX))
                {
                    // 상대 숨기기
                    otherDongle.Hide(transform.position);
                    LevelUp();
                }
            }
        }
    }

    public void Hide(Vector3 targetPosition)
    {
        isMerge = true;

        rigid.simulated = false;
        circleCollider.enabled = false;

        if(targetPosition == Vector3.up * 100)
        {
            PlayEffect();
        }

        StartCoroutine(HideRoutine(targetPosition));
    }

    IEnumerator HideRoutine(Vector3 targetPosition)
    {
        int frameCount = 0;

        while(frameCount < 20)
        {
            frameCount++;

            // 동글 합쳐질 때
            if(targetPosition != Vector3.up * 100)
            {
                transform.position = Vector3.Lerp(targetPosition, targetPosition, 0.5f);
            }
            // 게임오버 시
            else if(targetPosition == Vector3.up * 100)
            {
                transform.localScale = Vector3.Lerp(transform.localScale, Vector3.zero, 0.2f);
            }
            
            // 1프레임 대기
            yield return null;
        }

        gameManager.score += (int)Mathf.Pow(2, level);
        isMerge = false;

        // 비활성화
        gameObject.SetActive(false);
    }

    private void LevelUp()
    {
        isMerge = true;

        // 이동속도 0
        rigid.velocity = Vector2.zero;
        // 회전속도 0
        rigid.angularVelocity = 0;

        StartCoroutine(LevelUpRoutine());
;   }

    IEnumerator LevelUpRoutine()
    {
        yield return new WaitForSeconds(0.2f);

        animator.SetInteger("Level", level + 1);
        PlayEffect();
        gameManager.SfxPlay(GameManager.Sfx.LevelUp);
        yield return new WaitForSeconds(0.3f);

        level++;

        if (level < 5)
        {
            gameManager.maxLevel = Mathf.Max(gameManager.maxLevel, level);

        }

        isMerge = false;
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if(collision.tag == "Finish")
        {
            deadTime += Time.deltaTime;
        }

        if(deadTime > 2)
        {
            spriteRenderer.color = Color.red;

            if(deadTime > 5)
            {
                gameManager.GameOver();
            }
        }

    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.tag == "Finish")
        {
            deadTime = 0;
            spriteRenderer.color = Color.white;
        }
    }

    private void PlayEffect()
    {
        effect.transform.position = transform.position;
        effect.transform.localScale = transform.transform.localScale;
        effect.Play();
    }
}
