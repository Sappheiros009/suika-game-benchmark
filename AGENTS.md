# 프로젝트 안내

- 유니티 기초 스터디 수강생을 위한 수박게임 과제를 만드는 프로젝트다.
- 과일 단계는 체리 → 귤 → 사과 → 멜론 → 수박이다. 체리는 사과와 구분되도록 짙은 자주색을 사용한다.
- 과일 몸통과 표정은 별도 스프라이트로 구성한다. 모든 과일이 공통 표정 리소스와 Animator를 재사용하며, 기본 Idle 표정과 다른 과일에 접촉 중인 `>.<` 표정을 전환하도록 한다. 벽과 바닥에 닿는 것만으로는 표정을 바꾸지 않는다.
- 과일 5종은 `Assets/Prefabs/FruitBase.prefab`을 상속하는 Prefab Variant다. 공통 애니메이션은 베이스의 `Face`에만 설정하며 Variant에서 별도 설정하지 않는다. 공통 Controller의 Bool 파라미터는 `IsTouching`이다.
- 베이스 루트의 `FruitContactExpression`이 실제 2D 접촉점을 확인해 공통 표정을 갱신한다. 같은 컴포넌트를 가진 다른 과일만 판별하며, 여러 과일과 동시에 접촉하거나 상대 Collider가 비활성화되는 경우에도 현재 접촉 여부를 반영한다. Trigger는 제외한다.
- 베이스 루트의 `FruitMerge`가 같은 단계의 두 과일을 다음 단계 하나로 합친다. 각 Variant에서 Fruit Type과 Next Fruit Prefab을 설정하며, 수박의 다음 프리팹은 비워둔다. 합체 전 양쪽 과일을 소비 중으로 표시해 중복 합체를 막는다.
- 합체 결과로 생긴 과일은 기본 0.2초 동안 재합체하지 않는다. 베이스의 Chain Merge Delay로 조절하며, 일반 낙하 과일에는 생성 대기를 적용하지 않는다. 대기 종료 시 현재 접촉을 다시 확인해 계속 닿아 있는 과일과 연쇄 합체한다.
- 씬의 `GameManager/FruitDropManager`가 체리·귤·사과 중에서만 무작위로 선택한다. 멜론과 수박은 직접 생성하지 않으며 수박은 합체 목표다. 방향키 좌우 또는 A/D로 DropPoint를 움직이고 Space를 눌러 생성한다. 이동 속도는 Move Speed로 조절한다. 과일 반지름을 고려해 바구니 안으로 낙하 위치를 제한하며 가이드와 다음 과일 UI도 갱신한다. 입력은 구버전 Input Manager의 `Input.GetKey`와 `Input.GetKeyDown`을 사용한다.
- `GameManager/GameOverManager`는 과일 Collider의 윗부분이 GameOverLine보다 높은지 확인한다. 새 과일은 기본 1초 유예 후, 선 위에 1초 연속 남아 있으면 게임오버다. 게임오버는 입력과 물리 진행을 멈추고 결과 패널을 표시하며, R로 과일과 상태를 초기화해 재시작한다.
- `GameManager/ScoreManager`는 싱글톤이며 `ScoreManager.Instance`로 접근한다. 합체 결과별로 귤 10점·사과 30점·멜론 80점·수박 200점을 누적한다. 현재/최고/결과 화면 점수를 갱신하며, R 재시작 시 현재 점수만 0으로 초기화한다. 최고 점수는 현재 ScoreManager가 살아 있는 동안 메모리에만 유지하고 외부에 저장하지 않는다.
- UI 텍스트는 TextMeshProUGUI를 사용하고 스크립트 참조는 `TMP_Text`로 선언한다. TMP Essential Resources와 기본 LiberationSans SDF 글꼴을 프로젝트에 포함한다.
- 최종과제와 대체과제를 준비한다. 대체과제는 최종과제에 추가 기능을 더한 형태다.
- 평가 항목별로 점수를 부여하고 합산 점수가 정해진 기준 이상이면 통과한다. 제시된 모든 기능을 구현해야만 통과하는 방식으로 전제하지 않는다.
- 구체적인 기능, 배점, 통과 점수는 아직 정해지지 않았다. 과제 안내나 평가표를 작성할 때는 확정된 내용과 제안을 구분한다.
- 수강생의 기초 수준을 고려해 요구사항과 평가 방법을 명확하고 확인 가능하게 작성한다.

# 프로젝트 환경

- Unity 버전: `6000.6.7f1` (`ProjectSettings/ProjectVersion.txt`).
- 바구니 이미지는 `Assets/Art/Basket/basket_frame.png`이며 가운데와 외부가 투명한 단일 스프라이트다. 그림과 좌우 벽·바닥의 BoxCollider2D 3개를 따로 배치한다. 참조 씬에는 그림을 BasketSprite로 적용했다.
- 2D 템플릿 기반 프로젝트이며 현재 `Assets/Scenes/SampleScene.unity`가 있다.
- 수박게임 씬은 `Assets/Scenes/SuikaGame.unity`다. 구성은 `Docs/SceneLayout.md`를 참고한다. 불필요한 미리보기 과일과 장식용 림은 삭제했고, 바구니 벽·바닥은 Collider 전용이다. `Assets/Prefabs/Fruits`의 원본 프리팹은 물리 시뮬레이션이 켜져 있다.
- Unity가 생성하는 `Library`, `Temp`, `Logs` 등의 파일은 과제 원본으로 취급하지 않는다.
