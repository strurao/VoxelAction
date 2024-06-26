using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EnemyCS : MonoBehaviour
{
    public int maxHealth;
    public int curHealth;
    public Transform target; // ������ ��� (�÷��̾� ĳ����)
    public bool isChase=false;
    public bool isAttack; // ���� ���Դϴ�
    public BoxCollider meleeArea; // �Ϲ��� ���� EnemyA Ÿ���� EnemyBullet
    private int damage = 5;

    Rigidbody rigid;
    BoxCollider boxCollider;
    Material mat;
    Animator animator;
    // NavMeshAgent nav;

    void Awake()
    {
        rigid = GetComponent<Rigidbody>();
        boxCollider = GetComponent<BoxCollider>();
        mat = GetComponentInChildren<MeshRenderer>().material;
        animator = GetComponentInChildren<Animator>();
        // nav = GetComponent<NavMeshAgent>();
        // Invoke("ChaseStart", 2); // �����Ǹ� 2�� �Ŀ� ������ ����
    }

    void ChaseStart()
    {
        isChase = true;
        animator.SetBool("isWalk", true);
    }

    void Update()
    {
        /*if(nav.enabled)
        {
            // nav.SetDestination(target.position)
            // Navigation.isStopped = !isChase;
        }*/
    }

    void FixedUpdate()
    {
        FreezeVelocity();
    }

    void FreezeVelocity()
    {
        if (isChase)
        {
            rigid.velocity = Vector3.zero;
            rigid.angularVelocity = Vector3.zero;
        }
    }

    public void HitByGrenade(Vector3 explosionPos, int damage)
    {
        transform.GetComponent<EnemyFSM>().TakeDamage(damage);
        //curHealth -= 100;
        //Vector3 reactVec = transform.position - explosionPos; 
        //StartCoroutine(OnDamage(reactVec, true));
    }

    void OnTriggerEnter(Collider other)
    {        
        if (other.tag == "Melee")
        {
            WeaponCS weapon = other.GetComponent<WeaponCS>();
            if (weapon != null)
            {
                curHealth -= weapon.damage;
                Vector3 reactVec = transform.position - other.transform.position;
                transform.GetComponent<EnemyFSM>().TakeDamage(weapon.damage);
            }
        }
        else if (other.CompareTag("Player"))
        {
            Debug.Log("Player HIT");
            other.GetComponent<CharacterControllerCS>().TakeDamage(damage);
            // Destroy(gameObject); // 자폭?!
        }
    }

    IEnumerator OnDamage(Vector3 reactVec, bool isGrenade)
    {
        mat.color = Color.red;
        yield return new WaitForSeconds(0.1f);

        if (curHealth > 0)
        {
            mat.color = Color.white;
        }
        else
        {
            mat.color = Color.gray;
            gameObject.layer = 12; // Layer Idx of "EnemyDead" 

            isChase = false;
            // nav.enabled = false;
            // animator.SetTrigger("doDie");
            // GameManager.instance.enemyCntA++;

            // ���ۿ� �˹�
            if (isGrenade)
            {
                reactVec = reactVec.normalized;
                reactVec += Vector3.up * 3f; // �˹� ���Ϳ� �ణ�� �� ���� �߰�
                
                rigid.freezeRotation = false;
                rigid.AddForce(reactVec * 5, ForceMode.Impulse);
                rigid.AddTorque(reactVec*15, ForceMode.Impulse);
            }
            else { 
             reactVec = reactVec.normalized;
             reactVec += Vector3.up * 2f; // �˹� ���Ϳ� �ణ�� �� ���� �߰�
             rigid.AddForce(reactVec * 5, ForceMode.Impulse);
            }
            // Destroy(gameObject, 3);
        }
    }


}
