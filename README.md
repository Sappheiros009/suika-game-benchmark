# suika-game-benchmark
CAT&amp;DOG 기초스터디 최종과제(대체과제)입니다. 과제 안내는 [ASSIGNMENT.md](ASSIGNMENT.md), 채점 기준은 [과제 저장소](https://github.com/jiunkong/kucatdog-final-assignment-26-2)를 참고하세요.

## 실행할 씬

**`Assets/Scenes/SuikaGame.unity`**

- 프로젝트를 처음 열면 이 씬이 없을 경우 자동으로 만들어지고 열립니다.
- 수동으로 다시 만들려면 메뉴 **Suika > Build Play Scene**을 실행합니다. (`Assets/Editor/SuikaSceneBuilder.cs`)
- 동작 검사는 메뉴 **Suika > Run Self Test**로 실행합니다. 결과는 프로젝트 폴더의 `SelfTestReport.txt`에, 확인용 화면은 `Screenshots/`에 저장됩니다. (둘 다 git에는 올리지 않습니다)

## 조작

| 키 | 동작 |
| --- | --- |
| ← / → 또는 A / D | 낙하 위치 좌우 이동 (동시에 누르면 멈춤) |
| Space | 과일 떨어뜨리기 (길게 눌러도 1개, 최소 0.4초 간격) |
| R | 게임오버 상태에서 재시작 |

## 구현 / 미구현 항목

### 공통 평가 항목 (100점)

| 번호 | 항목 | 상태 | 구현 내용 |
| --- | --- | --- | --- |
| 1 | 플레이 화면과 바구니 동작 | 구현 | 바구니 그림 안쪽 경계를 측정해 좌우 벽·바닥 Collider를 맞춤. 벽은 위로 길게 늘려 과일이 넘어가지 않음. 가이드·제한선·현재/최고 점수·다음 과일 UI 배치 |
| 2 | 과일 5종 표시와 물리 동작 | 구현 | 제공 Prefab Variant(체리→귤→사과→멜론→수박) 사용, 몸통과 공통 표정 분리 |
| 3 | 과일 접촉에 따른 표정 전환 | 구현 | 제공 `FruitContactExpression` + `FruitFace` Animator(`IsTouching`) |
| 4 | 키보드 이동과 과일 낙하 | 구현 | `FruitDropManager` TODO 구현. 동시 입력 상쇄, 높이 유지, 바구니 안 제한, 가이드 동기화, `GetKeyDown` 단발 입력, 0.4초 간격 |
| 5 | 다음 과일 선택과 UI | 구현 | 체리·귤·사과 무작위 선택, 낙하 성공 시 재선택, 이미지와 이름 갱신, 선택 직후 위치 재보정 |
| 6 | 같은 과일 합체 | 구현 | `FruitMerge` TODO 구현. 중간 위치 생성, 평균 속도 전달, 중복 합체 방지, 수박 비합체, 합체 결과만 0.2초 연쇄 대기 |
| 7 | 점수 연결 | 구현 | 합체 결과 기준 1회 가산(귤 10, 사과 30, 멜론 80, 수박 200), 결과 패널 최종 점수, 재시작 시 현재 점수만 초기화 |
| 8 | 게임오버와 재시작 | 구현 | 생성 1초 유예 후 윗면이 제한선 위 1초 유지 시 게임오버, 물리 정지·가이드 숨김·입력 차단, R 재시작 시 과일 제거와 상태 복원 |

### 대체과제 추가 항목 (30점)

| 항목 | 상태 | 구현 내용 |
| --- | --- | --- |
| A) 낙하 위치 미리보기 | 구현 | `DropPreview`: 다음 과일 몸통을 같은 크기로 반투명 표시, 이동·선택 변경을 따라감, Collider 없음, 게임오버 시 숨김·재시작 시 복원 |
| B) 리더보드 | 구현 | `LeaderboardManager`: 판마다 1회 등록, 점수 내림차순(동점은 먼저 등록 우선) 상위 5개, 게임오버 화면과 플레이 화면 옆에 표시, 첫 판 이전 빈 상태 표시, 재시작해도 기록 유지 |

### 추가 연출 (채점 외)

- **효과음**: 낙하, 첫 착지, 합체(단계별 음높이), 멜론·수박 생성, 게임오버, 최고 기록 갱신, 재시작 — Kenney CC0 효과음
- **합체 이펙트**: 별 반짝임, 빛 고리, 수박 생성 시 큰 폭발 — Kenney Particle Pack 텍스처 + Unity Particle System
- **UI**: Kenney UI Pack 패널과 Kenney Future 폰트, 게임오버 카드 등장, 점수 강조, 최고 기록 배지
- 합체 결과 과일이 부풀며 나타나는 연출과 점수 강조는 맞는 외부 애니메이션 에셋이 없어 코드로 처리했습니다. 물리 크기에 영향을 주지 않도록 Collider가 없는 몸통·표정만 키웁니다.
- 모든 연출은 이벤트를 듣기만 하며 합체·점수·게임오버 판정에 관여하지 않습니다. 게임오버 시 `Time.timeScale = 0`으로 입자도 함께 멈춥니다.

### 미구현 / 남은 작업

- **배경 이미지**: 자리(`Background (이미지 필요)` 오브젝트, `BackgroundFitter`)만 만들어 두었습니다. 이미지를 넣으면 화면 크기에 맞게 자동으로 채워집니다.
- **플레이 영상**: 제출용 영상은 아직 촬영하지 않았습니다.

## 사용한 외부 에셋 (모두 CC0)

| 에셋 | 제작 | 받은 곳 | 사용 파일 |
| --- | --- | --- | --- |
| Interface Sounds 1.0 | Kenney (kenney.nl) | [Calinou/kenney-interface-sounds](https://github.com/Calinou/kenney-interface-sounds) | drop_002, pluck_001·002, confirmation_001·002·004, error_006, select_002, maximize_006 |
| Impact Sounds | Kenney (kenney.nl) | [Boyquotes/kenney-impact-sounds-for-godot](https://github.com/Boyquotes/kenney-impact-sounds-for-godot) | impact_soft_medium_000~002 |
| Particle Pack 1.1 | Kenney (kenney.nl) | [Calinou/kenney-particle-pack](https://github.com/Calinou/kenney-particle-pack) | star_06, light_02, circle_05, magic_02 |
| UI Pack | Kenney (kenney.nl) | [lpfd/kenney-uipack](https://github.com/lpfd/kenney-uipack) | 회색·노란색 패널, 막대, 별, 재시작 아이콘 |
| Kenney Fonts | Kenney (kenney.nl) | [ereborstudios/kenney-fonts](https://github.com/ereborstudios/kenney-fonts) | Kenney Future |

각 팩의 라이선스 원문은 `Assets/ThirdParty/Kenney/Licenses/`에 있습니다.

## 주요 파일

| 파일 | 역할 |
| --- | --- |
| `Assets/Scripts/FruitDropManager.cs` | 입력, 낙하, 다음 과일 선택, 위치 이동·제한 |
| `Assets/Scripts/FruitMerge.cs` | 충돌 감지와 합체, 속도 전달, 점수 요청 |
| `Assets/Scripts/GameOverManager.cs` | 제한선 판정, 게임오버 전환, 재시작 |
| `Assets/Scripts/ScoreManager.cs` | 점수 계산·표시 (제공 코드) |
| `Assets/Scripts/FruitContactExpression.cs` | 접촉 표정 전환 (제공 코드) |
| `Assets/Scripts/DropPreview.cs` | 대체과제 A |
| `Assets/Scripts/LeaderboardManager.cs` | 대체과제 B |
| `Assets/Scripts/GameFeedback.cs`, `FruitImpactSound.cs` | 효과음과 연출 |
| `Assets/Scripts/BackgroundFitter.cs` | 배경 이미지 자리 |
| `Assets/Editor/SuikaSceneBuilder.cs` | 플레이 씬 자동 구성 |
| `Assets/Scripts/Testing/SuikaSelfTest.cs`, `Assets/Editor/SuikaSelfTestRunner.cs` | 채점 시나리오 자체 검사 (에디터 전용) |
