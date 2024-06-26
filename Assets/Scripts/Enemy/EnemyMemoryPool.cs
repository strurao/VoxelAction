using System.Buffers;
using System.Collections;
using UnityEngine;

// 메모리 풀을 이용한 적 캐릭터 관리
public class EnemyMemoryPool : MonoBehaviour
{
    [SerializeField]
    private GameObject target; // 적의 목표 (플레이어)
    [SerializeField]
    private GameObject enemySpawnPointPrefab; // 적이 등장하기 전 적의 등장 위치를 알려주는 프리팹
    [SerializeField]
    private GameObject enemyPrefab; // 생성되는 적 프리팹
    [SerializeField]
    private float enemySpawnTime = 1; // 적 생성 주기
    [SerializeField]
    private float enemySpawnLatency = 1; // 타일 생성 후 적이 등장하기까지 대기 시간

    private MemoryPool spawnPointMemoryPool; // 적 등장 위치를 알려주는 오브젝트 생성, 활성/비활성 관리
    private MemoryPool enemyMemoryPool; // 적 생성, 활성/비활성 관리
    private int numberOfEnemiesSpawnedAtOnce = 1; // 동시에 생성되는 적의 숫자
    private Vector2Int mapSize = new Vector2Int(150, 150); // 맵 크기

    private void Awake()
    {
        spawnPointMemoryPool = new MemoryPool(enemySpawnPointPrefab);
        enemyMemoryPool = new MemoryPool(enemyPrefab);
        StartCoroutine("SpawnTile");
    }
    
    public void Start()
    {
        target = GameObject.FindGameObjectWithTag("Player");
        if (target == null)
        {
            Debug.LogError("Player target not found in the scene.");
        }
    }

    // 임의의 위치에 적 생성 위치를 알려줍니다. 처음에는 하나, 시간이 지날수록 동시 생성 수가 증가합니다.
    private IEnumerator SpawnTile()
    {
        int currentNumber = 0;
        int maximumNumber = 3;

        while (true)
        {
            // 동시에 numberOfEnemiesSpawnedAtOnce 숫자만큼 적이 생성되도록 반복문 사용
            /*
            for(int i= 0; i < numberOfEnemiesSpawnedAtOnce; ++i)
            {
                // 맵 내 임의의 위치에 적 생성 위치를 알려주는 아이템 생성
                GameObject item = spawnPointMemoryPool.ActivatePoolItem();
                item.transform.position = new Vector3(Random.Range(-mapSize.x * 0.49f, mapSize.x * 0.49f), 1,
                                                                           Random.Range(-mapSize.y * 0.49f, mapSize.y * 0.49f));
                StartCoroutine("SpawnEnemy", item);
            }
            */

            for (int i = 0; i < numberOfEnemiesSpawnedAtOnce; ++i)
            {
                Vector3 spawnPosition = new Vector3(Random.Range(-mapSize.x * 0.49f, mapSize.x * 0.49f), 1,
                                                    Random.Range(-mapSize.y * 0.49f, mapSize.y * 0.49f));

                // 장애물 검사를 위한 반경 설정
                float checkRadius = 1.0f; // 이 값을 조정하여 검사 범위를 변경

                // 해당 위치 주변에 "Obstacle" 태그를 가진 오브젝트가 있는지 확인
                Collider[] hitColliders = Physics.OverlapSphere(spawnPosition, checkRadius);
                bool isObstacleNearby = false;
                foreach (var hitCollider in hitColliders)
                {
                    if (hitCollider.CompareTag("Obstacle"))
                    {
                        isObstacleNearby = true;
                        break;
                    }
                }

                // "Obstacle" 태그를 가진 오브젝트가 없는 경우에만 적 생성
                if (!isObstacleNearby)
                {
                    GameObject item = spawnPointMemoryPool.ActivatePoolItem();
                    item.transform.position = spawnPosition;
                    StartCoroutine("SpawnEnemy", item);
                }
            }

            currentNumber++;

            if(currentNumber >= maximumNumber)
            {
                currentNumber = 0;
                numberOfEnemiesSpawnedAtOnce++;
            }
            //enemySpawnTime 만큼 반복 수행
            yield return new WaitForSeconds(enemySpawnTime);
        }
    }

    private IEnumerator SpawnEnemy(GameObject point)
    {
        yield return new WaitForSeconds(enemySpawnLatency);
        if (target == null)
        {
            target = GameObject.Find("Character(Clone)");
            yield break; // 타겟이 없으면 적 생성 중단
        }
        // 적 오브젝트를 생성하고, 적의 위치를 현재 생성되어있는 타일과 같은 point의 위치로 설정
        GameObject item = enemyMemoryPool.ActivatePoolItem();
        item.transform.position = point.transform.position;

        item.GetComponent<EnemyFSM>().Setup(target, this);

        // 타일 오브젝트를 비활성화
        spawnPointMemoryPool.DeactivatePoolItem(point);
    }

    public void DeactivateEnemy(GameObject enemy)
    {
        enemyMemoryPool.DeactivatePoolItem(enemy);
    }
}
