# 수박게임 씬 구성

`Assets/Scenes/SuikaGame.unity`를 열고 Game 뷰를 16:9로 설정한다. 기준 해상도는 1600×900이다.

모든 UI 문구는 TextMeshProUGUI다. 기본 글꼴은 `Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset`을 사용하며 TMP Essential Resources가 프로젝트에 포함되어 있다. 점수와 다음 과일 이름을 받는 스크립트 필드는 `TMP_Text`로 선언되어 있다. UI를 직접 구성할 때도 TextMeshProUGUI를 배치하고 해당 필드에 연결한다.

## 오브젝트

- `HUD/ScorePanel`: 합체로 누적한 현재 점수와 실행 중의 최고 점수. ScoreManager가 갱신한다.
- `HUD/NextFruitPanel`: 다음에 떨어질 과일 이미지와 이름. 플레이 중 GameManager가 갱신한다.
- `HUD/EvolutionPanel`: 체리 → 귤 → 사과 → 멜론 → 수박 순서 안내.
- `Playfield/Basket`: 바구니 배경과 좌우 벽, 바닥. 벽과 바닥에는 BoxCollider2D가 있다.
- `Playfield/Basket/BasketSprite`: 투명 배경의 바구니 프레임 이미지. LeftWall·RightWall·Floor는 Collider 전용이고, 바구니 그림은 BasketSprite 한 장으로 표시한다. 사용법은 `Assets/Art/Basket/README.md`를 참고한다.
- `Playfield/GameOverLine`: 높이 제한 점선. GameOverManager가 이 오브젝트의 Y 좌표를 기준으로 높이를 판정한다.
- `Playfield/DropPoint`: 과일을 떨어뜨릴 기준 위치.
- `Playfield/DropGuide`: 낙하 위치를 나타내는 점선.
- `Playfield/SpawnedFruits`: 플레이 중 생성된 과일이 들어가는 부모 오브젝트.
- `GameManager`: 선택과 낙하의 `FruitDropManager`, 높이 제한과 재시작의 `GameOverManager`, 점수 집계와 표시의 `ScoreManager` 컴포넌트.
- `HUD/GameOverPanel`: 평소에는 숨겨져 있고 게임오버 시 표시되는 결과 화면과 R 재시작 안내.

스프라이트로 대체된 장식용 LeftRim·RightRim과 비활성 미리보기 과일은 삭제했다. 제한선은 높이 좌표로만 판정하므로 별도의 Trigger 없이 점선과 Transform을 사용한다. 바구니의 내부 배경과 그림자는 화면 디자인에 사용한다.

## 과일 선택과 낙하

GameManager의 Inspector에서 `Fruit Prefabs` 목록과 `Drop Point`, `Spawned Fruit Parent`를 지정한다. 직접 생성하는 과일은 체리·귤·사과 3종으로 한정했다. 멜론과 수박은 합체로 만드는 단계이며 수박이 최종 목표다. 프리팹은 5종 모두 유지하지만 무작위 생성 목록에는 앞의 3종만 넣는다.

플레이를 시작하면 다음 과일을 무작위로 선택해 UI에 표시한다. 방향키 좌우 또는 A/D를 누르고 있으면 DropPoint와 낙하 가이드가 이동한다. 양방향을 동시에 누르면 이동하지 않는다. 이동 속도는 Inspector의 `Move Speed`로 조절하며 기본값은 초당 4 유닛이다. 높이는 유지하고, 선택된 과일의 반지름과 양쪽 벽의 안쪽 경계를 고려해 과일이 바구니 밖에 생성되지 않도록 제한한다. Left Wall, Right Wall, Drop Guide는 씬에 연결되어 있다.

Space를 누르는 순간 현재 선택된 과일을 DropPoint의 위치와 회전으로 생성하고, Rigidbody2D의 물리 시뮬레이션을 켜서 떨어뜨린다. 키를 계속 누르고 있는 것만으로 반복 생성하지 않는다. 생성 후 다음 과일을 다시 선택한다. 연속 입력 간격은 기본 0.4초이며 Inspector의 `Drop Cooldown`으로 변경한다. 입력은 구버전 Input Manager의 `Input.GetKey`와 `Input.GetKeyDown`을 사용한다. Player Settings의 Active Input Handling은 `Input Manager (Old)`로 설정한다.

다른 컴포넌트나 버튼에서는 `FruitDropManager.DropFruit()`를 호출할 수 있다. 생성에 성공하면 과일 GameObject를 반환하고, 쿨다운 중이거나 일시정지 상태면 null을 반환한다. `MoveDropPoint`는 월드 좌표의 가로 이동량을 받아 낙하 위치를 움직이고 바구니 경계 안으로 제한한다.

## 과일 프리팹

`Assets/Prefabs/FruitBase.prefab`이 공통 베이스이고, `Assets/Prefabs/Fruits`의 5개 과일은 이 베이스를 상속하는 Prefab Variant다. 베이스에는 Rigidbody2D와 CircleCollider2D가 있으며, 프리팹 자체는 물리 시뮬레이션이 켜져 있다. 과일마다 몸통 이미지와 위치, 전체 크기, 무게를 덮어쓴다.

`Body`와 `Face`는 별도 SpriteRenderer다. 베이스의 `Face`에 `Assets/Animations/FruitFace/FruitFace.controller`를 연결했다. 모든 Variant는 Face와 Animator를 베이스에서 상속한다. 표정, Animator Controller, 공통 컴포넌트를 수정할 때는 FruitBase를 편집하고, Variant의 Face나 Animator에 불필요한 Override를 만들지 않는다.

공통 Controller는 Idle과 Contact 두 상태를 가지며 `IsTouching` Bool로 전환한다. true이면 `>.<`, false이면 기본 미소다. 각 클립은 Face의 SpriteRenderer만 변경하므로 몸통은 그대로 유지된다.

베이스 루트의 `FruitContactExpression` 컴포넌트가 루트 Collider2D의 실제 접촉점을 확인하고, 접촉 상대가 같은 컴포넌트를 가진 다른 과일인 경우에만 Face Animator의 IsTouching을 true로 갱신한다. 벽이나 바닥에만 닿으면 Idle이다. 여러 과일에 접촉 중이면 하나와 떨어져도 Contact를 유지하며, 다른 과일과의 모든 접촉이 끝나면 Idle로 돌아간다. Trigger는 표정 변경에 포함하지 않는다. 상대 과일의 Collider가 꺼지거나 제거된 경우에도 다음 물리 갱신 결과를 반영한다. 컴포넌트나 오브젝트를 비활성화하면 접촉 상태를 초기화한다. 실제 낙하 과일은 프리팹의 물리 시뮬레이션을 켠 상태로 사용한다.

씬 구성 도구도 같은 베이스와 Variant를 재사용한다. `Tools > Suika > Set Up Fruit Variants`는 기존 Variant와 공통 애니메이션을 덮어쓰지 않고 누락된 구성을 준비한다.

## 같은 과일 합체

베이스 루트의 `FruitMerge`를 모든 과일 Variant가 상속한다. 각 Variant의 Fruit Type과 Next Fruit Prefab은 체리 → 귤 → 사과 → 멜론 → 수박 순서로 연결했다. 수박의 다음 프리팹은 비어 있어 수박 두 개는 합쳐지지 않는다.

같은 단계의 과일 두 개가 실제 물리 충돌하면 두 과일의 중간 위치에 다음 단계 과일 하나를 생성하고, 원본 둘을 제거한다. 새 과일은 원본의 평균 이동 속도와 회전 속도를 이어받으며 같은 부모와 씬에서 생성된다. 다른 단계, 벽, 바닥, Trigger 접촉은 합체 대상이 아니다.

양쪽 과일을 먼저 소비 중으로 표시하고 즉시 비활성화하므로, 양쪽 충돌 콜백이 실행되거나 세 과일이 동시에 닿아도 같은 과일이 두 번 합쳐지지 않는다.

합체로 생긴 과일은 기본 0.2초 동안 재합체하지 않는다. 예를 들어 체리 두 개가 귤이 되면, 옆에 다른 귤이 있어도 잠시 기다린 뒤 사과로 합쳐진다. 대기 중인 과일은 어느 쪽 충돌 콜백에서도 소비할 수 없다. 대기 종료 시 현재 물리 접촉을 다시 확인하므로 계속 닿아 있거나 물리적으로 잠든 과일도 이어서 합쳐지고, 이미 떨어진 과일과는 합쳐지지 않는다.

대기 시간은 `FruitBase/FruitMerge`의 `Chain Merge Delay`에서 조절하며 모든 Variant가 상속한다. 고정 물리 시간 단위로 대기하므로 일시정지 중에는 대기도 멈춘다. 일반 낙하로 생성한 과일에는 이 대기를 적용하지 않는다.

## 높이 제한과 게임오버

GameOverManager는 SpawnedFruits 아래의 활성 과일만 확인한다. 과일 Collider의 윗부분이 GameOverLine보다 높으면 초과 시간을 누적하고, 선 아래로 내려오면 해당 과일의 초과 시간을 초기화한다. 과일 전체가 선 위에 있어 Trigger와 겹치지 않는 경우도 감지한다. 비활성 과일, 합체로 제거된 과일, Trigger 과일과 물리 시뮬레이션이 꺼진 과일은 판정에서 제외한다.

낙하 위치가 제한선 위에 있으므로 새로 생성된 과일은 기본 1초 동안 판정을 유예한다. 그 이후 선 위에 기본 1초 연속 남아 있으면 게임오버다. 합체로 새로 생긴 과일에도 생성 유예가 적용된다. GameManager의 GameOverManager에서 `Spawn Grace Period`와 `Overflow Duration`으로 조절할 수 있다.

게임오버 시 FruitDropManager를 비활성화하고 Time Scale을 0으로 설정해 조준, 낙하, 물리와 합체 대기를 멈춘다. 결과 패널을 표시하고 낙하 가이드를 숨긴다. R을 누르면 생성된 과일과 높이 판정 기록을 제거하고, 낙하 위치와 다음 과일, 낙하 쿨다운을 초기화해 새 판을 시작한다. 별도 씬 로드 없이 재시작하므로 현재 씬을 Build Settings에 추가하지 않아도 동작한다.

## 누적 점수

합체할 때마다 결과 과일의 보상을 현재 점수에 더한다. 귤 생성은 10점, 사과 생성은 30점, 멜론 생성은 80점, 수박 생성은 200점이다. 귤 두 개를 만들어 얻은 20점에 둘을 사과로 합친 30점을 더하면 총 50점이다. GameManager의 ScoreManager에서 각 단계 보상을 조절할 수 있다.

ScoreManager는 GameManager에 붙은 싱글톤이다. FruitMerge는 실제 합체 결과가 생성되었을 때 `ScoreManager.Instance.AddMergeScore(결과종류)`를 한 번 호출한다. 단순 낙하, 다른 종류의 접촉, 수박 두 개의 접촉에는 점수가 없다. 연쇄 합체는 각 합체가 끝날 때마다 순서대로 점수를 더한다. 한 씬에서 한 판을 진행하는 구조이며, 중복 ScoreManager가 생성되면 중복 컴포넌트만 제거한다.

HUD의 SCORE와 BEST, 게임오버 패널의 최종 점수를 함께 갱신한다. GameOverManager가 게임오버 시 `ScoreManager.Instance.FinishRound()`를 호출해 가산을 막고, R 재시작 시 `ResetScore()`를 호출해 현재 점수만 0으로 초기화한다. 최고 점수는 메모리에만 유지한다. 현재 ScoreManager가 제거되거나 씬을 다시 로드하거나 앱을 다시 실행하면 현재 점수와 최고 점수 모두 0부터 시작한다.

## 구현 범위

현재 씬은 레이아웃, 물리 영역, 과일끼리 접촉 시 표정 전환, 체리부터 사과까지 무작위 선택, 키보드 좌우 조준과 Space 낙하, 다음 과일 UI 갱신, 같은 과일 합체, 연쇄 합체 대기, 높이 제한 게임오버와 R 재시작, 싱글톤의 누적 점수와 실행 중 최고 점수를 구현한 상태다.

씬은 일반 GameObject로 저장되므로 Inspector에서 직접 수정할 수 있다. 에디터 전용 구성 코드는 `Assets/Editor/SuikaSceneLayout.cs`에 있으며, 플레이 중 씬을 생성하는 방식이 아니다.
