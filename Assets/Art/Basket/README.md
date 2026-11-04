# 바구니 프레임 스프라이트

`basket_frame.png`는 기존 게임 화면과 비슷한 녹색 U자 바구니 프레임이다. 위가 열려 있고 가운데와 바깥은 투명하다. 이미지 크기는 1254×1254이며 과일 앞에 겹쳐 사용할 수 있다.

## 가져오기와 배치

- Texture Type: Sprite (2D and UI)
- Sprite Mode: Single
- Pixels Per Unit: 200
- Pivot: Center
- Alpha Is Transparency: 켜기
- 참조 씬 Material: `Assets/Art/Layout/SpriteUnlit.mat`
- Sorting Order: 10 (과일 몸통 5, 표정 6보다 앞)

이미지를 Scene으로 드래그하면 SpriteRenderer로 배치할 수 있다. 충돌 영역은 바구니의 좌우 벽과 바닥에 맞춰 BoxCollider2D 3개로 구성한다. 스프라이트와 물리 영역을 별도로 배치하는 구조다.

## 현재 참조 씬의 배치

`Playfield/Basket`의 Transform은 위치 (0, 0, 0), 크기 (1, 1, 1)이다. 자식 `BasketSprite`의 로컬 위치는 (0.010088, -0.360313, 0), 로컬 크기는 (1.008811, 0.922301, 1)이다. 이미지의 안쪽 경계를 기존 벽·바닥 Collider에 맞춘 값이다.

| Collider | 로컬 위치 X, Y | Size X, Y |
|---|---|---|
| LeftWall | -2.38, -0.42 | 0.18, 4.65 |
| RightWall | 2.38, -0.42 | 0.18, 4.65 |
| Floor | 0, -2.67 | 4.93, 0.22 |

벽·바닥 오브젝트에는 Collider만 남기고 기존 SpriteRenderer는 제거했다. 장식용 LeftRim·RightRim도 삭제했다. 연한 녹색 내부 배경과 그림자는 별도 오브젝트로 남아 있다. 수강생은 제공된 이미지를 사용해 스프라이트와 Collider 배치를 직접 구성할 수 있다.

## 생성 정보

내장 image_gen 도구로 생성했다. 참조 이미지는 기존 SuikaGame 씬 화면이며, 중앙 바구니의 스타일만 참조했다.

최종 생성 프롬프트:

```text
Use case: stylized-concept. Asset type: one standalone transparent PNG basket-frame sprite for a Unity 2D Suika game. Input image is STYLE REFERENCE ONLY: match the simple muted green U-shaped container in the center of the screenshot. Generate ONLY that empty U-shaped frame, no screenshot, no interface, no fruit.
Design: straight parallel vertical left and right walls, a straight horizontal bottom connecting them, gently rounded bottom corners, small rounded horizontal lip caps on each upper wall end. The top is completely OPEN: absolutely no line connecting the two upper ends. Nearly square front elevation, perfectly symmetric, orthographic flat front view. Flat solid muted forest green #4D7959, smooth clean edges, minimal casual game style, no dark outline, no texture, no wicker weave, no handles, no decorative objects.
Composition on a square canvas: wall centerlines around x=0.115 and x=0.885; top ends near y=0.10; bottom centerline near y=0.89. Uniform wall thickness approximately 0.028 of canvas width. Upper lip caps approximately 0.065 canvas width. Leave transparent padding around all edges.
CRITICAL: the large entire center opening between walls is genuinely fully transparent alpha, as is everything outside the frame. NO pale fill, NO backdrop, NO checkerboard graphic, NO shadow, NO floor, NO perspective or 3D. Only the green U-shaped border itself has opaque pixels. This must be a single practical game sprite placed over existing pale green background and falling fruit. No labels, text, watermark, or additional panels.
```
