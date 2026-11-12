# suika-game-benchmark
CAT&amp;DOG 기초스터디 최종과제(대체과제)입니다. 과제 안내와 채점 기준은 [과제 저장소](https://github.com/jiunkong/kucatdog-final-assignment-26-2)를 참고하세요.

## 실행할 씬

**`Assets/Scenes/SuikaGame.unity`**

- 프로젝트를 처음 열면 이 씬이 없을 경우 자동으로 만들어지고 열립니다.
- 수동으로 다시 만들려면 메뉴 **Suika > Build Play Scene**을 실행합니다. (`Assets/Editor/SuikaSceneBuilder.cs`)
- 동작 검사는 메뉴 **Suika > Run Self Test**로 실행합니다. 결과는 프로젝트 폴더의 `SelfTestReport.txt`에, 확인용 화면은 `Screenshots/`에 저장됩니다. (둘 다 git에는 올리지 않습니다)

## 조작

| 키 | 동작 |
| --- | --- |
| ← / → 또는 A / D | 낙하 위치 좌우 이동 (동시에 누르면 멈춤) |
| 마우스 이동 | 낙하 위치가 커서 x 위치를 바로 따라감 (바구니 안으로 제한) |
| Space 또는 마우스 왼쪽 클릭 | 과일 떨어뜨리기 (길게 눌러도 1개, 최소 0.4초 간격) |
| R | 게임오버 상태에서 재시작 |

## 구현 / 미구현 항목

### 공통 평가 항목 (100점)

| 번호 | 항목 | 상태 | 구현 내용 |
| --- | --- | --- | --- |
| 1 | 플레이 화면과 바구니 동작 | 구현 | 유리 바구니 그림의 안쪽 경계(x ±2.195, 바닥 -2.57유닛)를 측정해 좌우 벽·바닥 Collider를 맞춤. 벽은 위로 길게 늘려 과일이 넘어가지 않음. 가이드·제한선·현재/최고 점수·다음 과일 UI 배치 |
| 2 | 과일 5종 표시와 물리 동작 | 구현 | 제공 Prefab Variant(체리→귤→사과→멜론→수박) 사용, 몸통과 공통 표정 분리 |
| 3 | 과일 접촉에 따른 표정 전환 | 구현 | 제공 `FruitContactExpression` + `FruitFace` Animator(`IsTouching`) |
| 4 | 키보드 이동과 과일 낙하 | 구현 | `FruitDropManager` TODO 구현. 동시 입력 상쇄, 높이 유지, 바구니 안 제한, 가이드 동기화, `GetKeyDown` 단발 입력, 0.4초 간격. 추가로 마우스 이동(움직일 때만 커서 x를 따라감)과 왼쪽 클릭 낙하를 같은 규칙으로 지원 |
| 5 | 다음 과일 선택과 UI | 구현 | 체리·귤·사과 무작위 선택, 낙하 성공 시 재선택, 이미지와 이름 갱신, 선택 직후 위치 재보정 |
| 6 | 같은 과일 합체 | 구현 | `FruitMerge` TODO 구현. 중간 위치 생성, 평균 속도 전달, 중복 합체 방지, 수박 비합체, 합체 결과만 0.2초 연쇄 대기 |
| 7 | 점수 연결 | 구현 | 합체 결과 기준 1회 가산(귤 10, 사과 30, 멜론 80, 수박 200), 결과 패널 최종 점수, 재시작 시 현재 점수만 초기화 |
| 8 | 게임오버와 재시작 | 구현 | 생성 1초 유예 후 윗면이 제한선 위 1초 유지 시 게임오버, 물리 정지·가이드 숨김·입력 차단, R 재시작 시 과일 제거와 상태 복원 |

### 대체과제 추가 항목 (30점)

| 항목 | 상태 | 구현 내용 |
| --- | --- | --- |
| A) 낙하 위치 미리보기 | 구현 | `DropPreview`: 다음 과일 몸통을 같은 크기로 반투명 표시, 이동·선택 변경을 따라감, Collider 없음, 게임오버 시 숨김·재시작 시 복원 |
| B) 리더보드 | 구현 | `LeaderboardManager`: 판마다 1회 등록, 점수 내림차순(동점은 먼저 등록 우선) 상위 5개, 게임오버 화면과 플레이 화면 왼쪽 아래 LEADERBOARD 판에 표시, 첫 판 이전 빈 상태 표시, 재시작해도 기록 유지 |

### 추가 연출 (채점 외)

- **효과음**: 낙하, 첫 착지, 합체(단계별 음높이), 멜론·수박 생성, 게임오버, 최고 기록 갱신, 재시작 — Kenney CC0 효과음
- **합체 이펙트**: 별 반짝임, 빛 고리, 수박 생성 시 큰 폭발 — Kenney Particle Pack 텍스처 + Unity Particle System
- **UI**: 비 오는 야시장 밤 테마. 왼쪽에 SCORE·BEST·LEADERBOARD, 오른쪽에 NEXT와 세로 진화 순서 판(체리→귤→사과→멜론→수박, 9-slice로 늘린 판)을 두어 좌우 균형을 맞춤. 민트 유리 바구니, 분홍 점선 제한선, 흰 점선 안내선, Fredoka 폰트(제목 민트·숫자 크림), 게임오버 카드 등장, 점수 강조, 최고 기록 배지
- 합체 결과 과일이 부풀며 나타나는 연출과 점수 강조는 맞는 외부 애니메이션 에셋이 없어 코드로 처리했습니다. 물리 크기에 영향을 주지 않도록 Collider가 없는 몸통·표정만 키웁니다.
- 모든 연출은 이벤트를 듣기만 하며 합체·점수·게임오버 판정에 관여하지 않습니다. 게임오버 시 `Time.timeScale = 0`으로 입자도 함께 멈춥니다.

### 검증

- 작업과 검증은 Unity **6000.6.1f1** 에디터에서 했고, 컴파일 오류 없이 실행되는 것을 확인했습니다. 저장소의 `ProjectSettings/ProjectVersion.txt`는 과제 기준 버전 **6000.6.7f1**로 둡니다(6000.6.1f1로 열면 이 파일이 자동으로 바뀌므로 커밋에서 제외합니다).
- `Suika > Run Self Test`로 공통 항목 1~8과 대체과제 A·B의 동작 조건 55개를 실제 물리로 검사해 모두 통과했습니다(야시장 테마로 바꾼 뒤 다시 검사).
  - 낙하 간격(0.4초), 가장자리 제한, 다음 과일 일치, 합체 4단계와 단계별 점수, 수박 비합체, 같은 과일 3개 동시 접촉, 연쇄 합체, 게임오버 1초 유예와 1초 초과, 잠깐 넘었다 내려오는 경우, 재시작 초기화, 최고 점수 유지, 미리보기 표시·숨김, 리더보드 정렬과 중복 등록 방지 등
- 검사 장면은 확인을 위해 과일을 코드로 배치한 것이라 제출 영상으로는 쓰지 않습니다. 제출 영상은 실제 플레이로 촬영해야 합니다.

### 미구현 / 남은 작업

- **플레이 영상**: 제출용 영상은 아직 촬영하지 않았습니다.

## TODO

채점은 플레이 영상으로만 하므로, 기능이 구현되어 있어도 영상에 나오지 않으면 점수가 되지 않습니다.

### 제출 전 필수

- [ ] **플레이 영상 촬영** (정상 속도, 입력 키는 자막으로 설명, 미리 배치한 과일 장면 금지)
  - [ ] 1: 화면 전체 구성, 좌우 가장자리 낙하, 바닥·과일 위 적재
  - [ ] 2·6: 체리→귤→사과→멜론→수박 4단계 합체(멜론·수박은 실제 플레이로), 수박끼리 닿아도 안 합쳐지는 장면
  - [ ] 3: 서로 다른 과일이 닿기 전·후 표정 변화
  - [ ] 4: 좌우 이동, 양쪽 동시 입력 정지, 가장자리 제한, Space 길게 누르기, 빠른 연타(0.4초 간격) — 키보드로 촬영
  - [ ] 5: 여러 번 낙하해 체리·귤·사과가 모두 나오는 장면, NEXT 이미지·이름 일치
  - [ ] 7: 합체 전후 점수 변화(연쇄 합체 포함)
  - [ ] 8: 제한선을 잠깐 넘었다 내려오는 장면 → 1초 초과 게임오버 → 진행 중 R 무시 → 게임오버 후 R 재시작 → 두 번째 판 게임오버·재시작
  - [ ] A: 미리보기가 좌우 이동·다음 과일 변경을 따라가고, 게임오버 때 숨었다 재시작 때 돌아오는 장면
  - [ ] B: 6판 이상 이어서 리더보드 정렬과 상위 5개 유지
- [ ] **영상 타임스탬프**를 이 README에 항목별로 기록
- [ ] **커밋 정리**: 내용은 같고 메타데이터만 바뀐 이미지·소리 파일, 6000.6.1f1 에디터가 바꾼 `ProjectVersion.txt`·`packages-lock.json`, 원격에서 이미 지운 Kenney 폰트는 커밋에서 제외
- [ ] **`feature/suika-implementation` → `main` 병합** (제출 브랜치는 `main`)
- [ ] 새 위치에 클론해서 컴파일·실행 확인
- [ ] [제출 폼](https://forms.gle/oTSR66AMixCEepw47)에 영상과 저장소 링크 제출

### 촬영 시 주의

- 마우스 왼쪽 클릭도 과일을 떨어뜨립니다. 녹화 중 Game 창에 포커스를 주려고 클릭하면 과일이 하나 떨어집니다.
- 4번 항목은 키보드 기준이므로, 그 장면을 찍는 동안에는 마우스를 움직이지 않습니다.

### 선택 (채점 외 다듬기)

- [ ] 낙하 위치 미리보기 과일이 어두운 배경에서 탁해 보임 → 투명도 조정
- [ ] 다음 과일 이름을 한국어로 표시(한글 폰트 필요)
- [ ] 세로 진화 순서 판 위쪽에 제목(예: EVOLUTION) 추가
- [ ] NEW BEST 별 아이콘을 테마에 맞는 그림으로 교체
- [ ] 씬을 다시 만들 때마다 `SuikaGame.unity.meta`의 GUID가 바뀌는 문제(빌더가 씬을 지우고 다시 만듦) 정리

## 사용한 외부 에셋 (CC0, 폰트는 OFL)

| 에셋 | 제작 | 받은 곳 | 사용 파일 |
| --- | --- | --- | --- |
| Interface Sounds 1.0 | Kenney (kenney.nl) | [Calinou/kenney-interface-sounds](https://github.com/Calinou/kenney-interface-sounds) | drop_002, pluck_001·002, confirmation_001·002·004, error_006, select_002, maximize_006 |
| Impact Sounds | Kenney (kenney.nl) | [Boyquotes/kenney-impact-sounds-for-godot](https://github.com/Boyquotes/kenney-impact-sounds-for-godot) | impact_soft_medium_000~002 |
| Particle Pack 1.1 | Kenney (kenney.nl) | [Calinou/kenney-particle-pack](https://github.com/Calinou/kenney-particle-pack) | star_06, light_02, circle_05, magic_02 |
| UI Pack | Kenney (kenney.nl) | [lpfd/kenney-uipack](https://github.com/lpfd/kenney-uipack) | 별, 재시작 아이콘 |

각 팩의 라이선스 원문은 `Assets/ThirdParty/Kenney/Licenses/`에 있습니다. (Kenney Future 폰트와 회색·노란색 패널은 이전 버전에서 쓰던 것으로, 지금은 쓰지 않습니다)

| 에셋 | 제작·라이선스 | 받은 곳 | 사용 파일 |
| --- | --- | --- | --- |
| Fredoka SemiBold | The Fredoka Project Authors, SIL Open Font License 1.1 | [Google Fonts](https://fonts.google.com/specimen/Fredoka) | `Assets/ThirdParty/Fredoka/Fredoka-SemiBold.ttf` (라이선스 원문 `OFL.txt`) |
| 야시장 테마 그림 | 직접 준비한 생성 이미지 세트 | - | `Art/Background/background_night_market.png`, `Art/Layout/panel_score·best·next·top5.png`, `Art/Basket/basket_glass.png`, `Art/Guides/limit_line_dashed.png`, `Art/Guides/drop_guide_dotted.png` |
| 진화 순서 띠 판 | `panel_best.png`에서 제목 칸·구분선을 잘라 내고 이어 붙임 | - | `Art/Layout/panel_strip.png` |

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
| `Assets/Scripts/BackgroundFitter.cs` | 배경 이미지를 화면 크기에 맞춤 |
| `Assets/Editor/SuikaSceneBuilder.cs` | 플레이 씬 자동 구성 |
| `Assets/Scripts/Testing/SuikaSelfTest.cs`, `Assets/Editor/SuikaSelfTestRunner.cs` | 채점 시나리오 자체 검사 (에디터 전용) |
