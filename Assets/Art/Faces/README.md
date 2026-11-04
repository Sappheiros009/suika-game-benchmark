# 공통 표정 리소스

- `face_idle.png`: 기본 눈과 미소.
- `face_contact.png`: 다른 물체에 접촉 중인 `>.<` 표정.
- 과일이 포함되지 않은 투명 배경 PNG다. 두 파일은 동일한 캔버스 크기를 사용한다.

`Assets/Prefabs/FruitBase.prefab`의 `Face` 자식에 SpriteRenderer와 공통 Animator가 설정되어 있다. 과일 5종은 이 베이스의 Variant이므로 베이스의 Face를 수정하면 모두 상속한다. 얼굴의 Sorting Order는 몸통보다 높으며, 두 스프라이트는 같은 Pixels Per Unit과 중앙 Pivot을 사용한다.

Animator는 `Idle`과 `Contact` 두 상태에서 얼굴 SpriteRenderer의 Sprite만 바꾸도록 구성한다. Bool 파라미터 `IsTouching`이 true이면 Contact, false이면 Idle로 전환한다. 두 전환은 Has Exit Time을 끄고 Transition Duration을 0으로 설정한다. 각 상태는 단일 스프라이트를 유지하는 클립으로 만들 수 있다.

여러 물체에 동시에 접촉할 수 있으므로, 접촉 대상이 하나 빠져나갔다고 바로 Idle로 돌아가지 않도록 한다. 모든 접촉이 끝났을 때 IsTouching을 false로 갱신한다.

이미지는 내장 이미지 생성 도구로 생성·수정했다. 최종 수정 프롬프트: 밝은 외곽선을 완전히 제거하고 눈과 입을 짙은 갈색으로 표현한다. 기존 표정, 위치, 간격, 캔버스와 투명 배경은 유지한다. Idle은 타원형 눈과 작은 미소, Contact는 `>`와 `<` 모양의 눈과 작은 점 모양 입.
