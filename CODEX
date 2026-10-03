# The Wheeler — Codex Project Guide

이 문서는 The Wheeler Unity 프로젝트에서 Codex가 작업하기 전에 반드시 읽어야 하는 공통 작업 지침이다.
사용자가 짧은 명령만 주더라도 이 문서의 원칙을 기본 제약으로 적용한다.

## 1. 최우선 원칙

- 기존 코드를 먼저 조사한 뒤 작업한다.
- 최소 변경을 우선한다.
- 요청하지 않은 대규모 리팩터링, 시스템 교체, 새 Manager/System 추가를 하지 않는다.
- 사용자 예시의 클래스명/필드명/enum/API가 실제 프로젝트에 있다고 가정하지 않는다.
- 실제 클래스, 데이터 구조, 사용처를 확인한 뒤 구현한다.
- 재사용 가능한 기존 시스템이 있으면 새 시스템을 만들지 않는다.
- 일부 구현이 이미 있다면 덮어쓰지 말고 확장한다.
- 기능 범위를 “확장성”이나 “정리”를 이유로 넓히지 않는다.
- 기존 기능의 회귀 가능성을 확인한다.
- 컴파일이 깨지는 상태로 끝내지 않는다.

## 2. 작업 전 필수 조사

작업 요청을 받으면 바로 수정하지 말고 관련 코드를 먼저 확인한다.

- 기능 진입점
- 실제 데이터 소유 클래스
- Runtime 데이터 위치
- 관련 Manager / Component / Helper
- 관련 enum / SO / LevelData / RecordData / BuffData
- Object Pool 사용 여부
- Save/Load 영향 여부
- UI가 Scene/Prefab/Inspector 기반인지
- 같은 역할의 기존 구현이 있는지
- 관련 이벤트와 호출 순서

사용자가 예시로 `levelData.Duration`, `record.PassiveSkillID`, `skill.Runtime.Bullet` 같은 이름을 말해도 실제 코드에 있는지 먼저 확인한다.

## 3. 변경 범위 원칙

예: “Assist Guns 지속시간 추가” 요청이면 아래까지만 처리한다.

- 기존 Skill LevelData 조사
- 지속시간 필드 재사용
- Spawn 시 Duration 전달
- Lifetime 종료 시 기존 Despawn/Pool 반환

다음은 요청 없이는 하지 않는다.

- Assist Guns AI 재설계
- Targeting 변경
- Damage 밸런스 변경
- 새로운 Summon System 제작
- UI 추가

## 4. Active Skill / SkillModule

현재 구조는 가능한 한 다음 방향을 유지한다.

```text
GenericActiveSkill
  -> SkillModule 조합
```

단순한 기능 차이 때문에 별도 ActiveSkill subclass를 남발하지 않는다.

예:
- Module_ConsumeMagicBullet
- Module_BulletConsumeDamageBonus
- Module_GainLaserEnergy
- Module_SpawnWarningSign
- Module_DelayEvent

독립 기능은 모듈 조합으로 구성한다.

## 5. SkillModule Runtime 안전성

SkillModule은 Serializable class이고 Runtime clone 구조를 우선 유지한다.

Serialized 원본 Module에 다음 Runtime 상태를 저장하지 않는다.

- mutable List / Dictionary
- CancellationTokenSource
- 현재 대상
- 현재 타이머
- 소비한 탄환 목록
- 실행 중 임시 상태

`MemberwiseClone()`은 shallow copy이므로 mutable reference가 있다면 subclass `Clone()`에서 새로 초기화하거나 deep copy한다.

Shared serialized module instance에 직접 `Init(owner)`하지 않는다.

## 6. SkillRuntimeContext

SkillRuntimeContext는 한 번의 스킬 실행에 필요한 데이터만 관리한다.

적합:
- 이번 스킬에서 소비한 탄환
- Cast/Spawn 정보
- 이번 공격의 Modifier
- 이번 실행의 Combat Context

부적합:
- 여러 스킬에 걸쳐 유지되는 Laser Energy
- 장기 Buff
- Record 보유 목록
- 캐릭터 영구 상태

장기 유지 상태는 Character/Buff/해당 시스템이 관리한다.

## 7. Damage / Projectile

기존 `BaseProjectile`, `DamageEvent`, `DamageData`, `CombatHelper` 흐름을 먼저 확인하고 재사용한다.

- 파생 Projectile마다 별도 Damage 시스템을 만들지 않는다.
- 산탄/연속발사처럼 여러 Projectile이 나와도 Skill 자원 소비는 스킬 실행 단위로 처리한다.
- Projectile 생성마다 같은 탄환을 다시 소비하지 않는다.
- 기존 projectile behavior/interface 구조가 있으면 재사용한다.
- Pool projectile의 Runtime 값은 Spawn 시 reset한다.

## 8. 마법 탄환 시스템

### 책임 분리

`Module_ConsumeMagicBullet`의 책임:
- 현재 탄환 확인
- 최대 소비 수 이하로 실제 소비
- 소비한 BulletData를 Runtime에 기록
- 소비 개수를 Runtime에 기록

담당하지 않음:
- Damage 증가
- Laser Energy 증가
- Crit 증가
- Cooldown 변경

탄환 소비와 소비 보상을 하나의 만능 모듈로 합치지 않는다.

### 탄환은 Skill Cost가 아님

```text
MaxConsume = 3
현재 탄환 2 -> 2개 소비
현재 탄환 0 -> 0개 소비, 스킬은 정상 실행
```

Normal Bullet도 소비 개수에 포함한다.

### 소비 효과

독립 Module로 해석한다.

```text
Module_BulletConsumeDamageBonus
  -> 소비 개수 기반 Damage 증가

Module_GainLaserEnergy
  -> 소비 개수 기반 Energy 획득
```

`BulletConsumePurpose` 같은 enum으로 Damage/Energy/Crit 등을 한 구조에 몰아넣지 않는다.

## 9. Laser Energy

Laser Energy는 별도 EnergyManager/ResourceSystem을 만들지 않는다.

기존 Buff 시스템의 Stack으로 관리한다.

```text
LaserEnergy Buff
Stack = 현재 Energy
```

SkillRuntime에는 “이번 스킬에서 탄환 몇 개 소비했는가”만 저장한다.

`Module_GainLaserEnergy`는 대략 다음 개념을 따른다.

```text
gain = BaseGain + ConsumedBulletCount * GainPerConsumedBullet
```

그리고 기존 Buff API로 Stack을 증가시킨다.

Module 내부에 `currentEnergy`를 저장하지 않는다.

현재 요청에 포함되지 않으면 Energy 소비 후 방어무시/충전감소 같은 고유 효과까지 임의로 구현하지 않는다.

## 10. Buff 시스템

Buff 관련 기능은 기존 Buff 시스템을 먼저 조사한다.

- Buff 식별 방식
- Stack 저장 방식
- Add/Increase/Remove API
- Duration
- Permanent/Infinite 처리
- 전투/탐사 종료 시 초기화

가능하면 외부에서 내부 stack field를 직접 수정하지 말고 기존 공개 API를 사용한다.

Laser Energy는 시간제 Buff보다 누적 자원형 Stack으로 취급한다.

## 11. Record / Passive

`RecordData -> SO_PassiveSkillData -> GenericPassiveSkill` 흐름을 우선 유지한다.

Record 후보 판정 시 반드시 기존 Record 관련 enum/type을 먼저 확인한다.

### 일반 / Modifier Record
한 번 획득하면 기존 정책대로 랜덤 후보에서 제외.

### Passive 획득/증가형 Record
Passive가 MaxLevel이 될 때까지 랜덤 후보에 남긴다.

```text
Passive 미보유 -> 후보 포함
Passive Level < MaxLevel -> 후보 포함
Passive Level >= MaxLevel -> 후보 제외
```

Modifier 데이터가 있다는 이유만으로 Passive 증가형으로 취급하지 않는다.
Record enum/type을 기준으로 구분한다.

### EmptyRecord
직업/보유/Passive MaxLevel/Exclusive 조건 등을 모두 적용한 최종 후보가 0개일 때만 fallback으로 사용한다.

## 12. PassiveSystem

특정 Job의 Passive 제거 시 전체 리스트를 `Clear()`해서 다른 Job까지 제거하지 않는다.

탐사 전용 Passive/Record는 Run 종료 시 기존 초기화 정책에 따라 정리한다.

Static effect가 StatusComponent를 직접 변경하는 경우 단순 객체 제거만으로 원상복구되지 않을 수 있으니 기존 modifier/rebuild 구조를 확인한다.

## 13. Assist Guns

Assist Guns는 무제한 유지하지 않는다.

- 현재 Skill LevelData에 Duration/Lifetime/EffectDuration 등 적절한 값이 있는지 먼저 찾는다.
- 있으면 재사용한다.
- 없을 때만 공용 LevelData용 Duration 추가를 검토한다.
- 레벨별 시간을 switch로 하드코딩하지 않는다.
- Spawn 시 현재 레벨의 Duration을 전달한다.
- Assist Guns Runtime Component가 Lifetime을 관리하는 방식을 우선한다.
- SkillModule이 장시간 타이머를 붙잡지 않는다.
- Pool 객체라면 Duration 종료 시 Destroy하지 말고 기존 Despawn/ReturnToPool을 사용한다.
- Pool 재사용 시 타이머/owner/CTS 등 Runtime 상태를 reset한다.

## 14. UI

### Runtime new GameObject 금지

UI 구조를 코드에서 `new GameObject()`로 만들지 않는다.

금지 예:
```csharp
new GameObject("Button", ...)
new GameObject("Text", ...)
new GameObject("Image", ...)
```

UI 구조는 Scene/Prefab/Inspector에서 구성한다.

코드는 데이터 바인딩과 상태 갱신만 담당한다.

### Inspector / Prefab 우선

Anchor, Pivot, Size, Padding, Font, Color, Spacing 등을 C#에 하드코딩하지 않는다.

가능하면:
```csharp
[SerializeField] private RectTransform parent;
[SerializeField] private UICharacterButton buttonPrefab;
```
형태로 명시적 참조를 둔다.

`transform.GetChild(0)`, `GetComponentInChildren<T>()`처럼 Hierarchy 구조에 강하게 의존하는 코드는 최소화한다.

### UIList / Template

Template GameObject가 target parent의 child이면서 동시에 prefab 역할을 하는 구조는 피한다.
가능하면 Project Asset Prefab을 사용하고 parent에는 Runtime slot만 둔다.

## 15. Character Status UI

- 첫 캐릭터 자동 선택
- 실제 StatusComponent에 존재하는 값만 표시
- 표시만 정수 포맷 사용
- Runtime float 값 자체는 바꾸지 않음
- Open/Selection 변경 시 Refresh
- 매 Update마다 Refresh하지 않음

Passive를 RecordData로 억지 변환하지 않는다.
필요하면 UI ViewData에서 Record/Passive를 통합 표현한다.

## 16. Object Pooling

Pool 객체는 `Awake()`에서 한 번만 초기화된다고 가정하지 않는다.

Spawn 시 reset 확인 대상:
- timer
- CTS
- owner
- target
- pierce count
- ignore list
- temporary state
- active flag

Pool 객체는 특별한 이유가 없으면 Destroy하지 않는다.

## 17. WarningSign

특정 warning shape 때문에 공통 parent 클래스를 과도하게 수정하지 않는다.

- WarningSign_Fan
- WarningSign_Circle
- WarningSign_Rect

같은 특수 구현은 child에서 처리하는 방향을 우선한다.

Gameplay hit detection과 warning VFX는 분리한다.

실제 공격 위치와 Warning 위치가 다르면 동일 spawn resolver 재사용 가능 여부를 먼저 확인한다.

## 18. Exploration / Run Save

RunStatus 흐름을 기존 구조대로 유지한다.

```text
NoSave
SetupIncomplete
MidRun
ChapterCleared
FinalRunCleared
```

Save 시 현재 RunStatus를 transient object 상태로 임의 재계산하지 않는다.

Chapter clear 시 reward chapter가 틀어지지 않도록 “클리어한 chapter” 값을 먼저 보존한 뒤 다음 chapter로 진행한다.

Reward는 중복 지급되지 않게 기존 RewardManager/AppManager 책임을 확인한다.

## 19. UI 시간 / 전투 시간

Pause 상태에서 UI만 계속 움직여야 할 때만 unscaled time을 사용한다.

전투 오브젝트 Lifetime에 임의로 unscaled time을 적용하지 않는다.

기존 시간 정책을 따른다.

## 20. Addressables / Resources

Addressables:
- Group과 Address는 별개
- Runtime lookup은 Address 기준
- 기존 loader 재사용

Resources:
- Resources 기준 상대경로 사용
- 기존 Resources/Addressables 혼용 정책을 확인

요청하지 않았으면 임의 마이그레이션하지 않는다.

## 21. 하드코딩 금지

다음은 가능하면 데이터 기반으로 처리한다.

- Skill Level별 Duration
- Damage
- Cooldown
- Range
- MaxConsumeCount
- Passive/Record 연결
- Buff ID
- Spawn Prefab

레벨별 switch 하드코딩은 피한다.

## 22. enum 원칙

새 enum을 만들기 전에 기존 enum과 사용처를 확인한다.

기존 enum으로 충분하면 재사용한다.

독립 기능 조합을 거대한 enum 하나로 몰아넣지 않는다.
모듈 조합이 가능한 경우 Module을 사용한다.

## 23. Save 구조

Runtime 기능 때문에 Save 포맷을 불필요하게 수정하지 않는다.

새 저장 field가 반드시 필요하면:
- 기존 Save 호환
- default/null 처리
- Load
- Run reset
을 확인한다.

## 24. Vertical Slice 우선

현재 프로젝트는 Core Loop와 기본 Shooter 기믹이 상당 부분 구현된 상태다.

새 대형 시스템을 계속 추가하기보다 다음 순서를 우선한다.

```text
Core Loop 안정화
-> Shooter Build 완성
-> 실제 Run 플레이테스트
-> 콘텐츠 밀도 증가
```

요청 없이는 Guide/Memory Drone 전체 시스템, General Magic 전체 시스템, 신규 Job, 신규 Character, Endgame 등을 앞당겨 구현하지 않는다.

## 25. 코드 스타일

현재 프로젝트의 naming, SafeInvoke, access modifier, SerializeField 스타일을 따른다.

요청 없이 다음을 하지 않는다.

- 대규모 class rename
- namespace 전면 도입
- 폴더 구조 전면 변경
- interface/DI 구조로 전환
- architecture layer 추가

## 26. 불확실한 경우

정보가 부족하면:
1. 관련 코드 사용처를 더 조사한다.
2. 실제 기존 설계의 근거를 찾는다.
3. 그래도 불분명하면 최소 구현만 한다.
4. 추측으로 큰 구조를 만들지 말고 확인 필요 사항으로 보고한다.

## 27. 작업 완료 후 보고

작업 후 다음 형식으로 간단히 보고한다.

```text
수정 파일
- ...

신규 파일
- ...

핵심 변경
- ...

재사용한 기존 시스템
- ...

Inspector / SO 설정 필요
- ...

테스트
- ...

주의 / 남은 사항
- ...
```

## 28. 명시적 금지 요약

- 요청하지 않은 대규모 리팩터링
- 새 Manager/System 남발
- UI를 runtime `new GameObject()`로 생성
- 예시 field/API 이름을 확인 없이 추가
- 기존 enum을 확인하지 않고 새 enum 생성
- Module 하나에 여러 책임 몰아넣기
- Pool 객체를 무조건 Destroy
- SkillRuntime에 장기 자원 저장
- Save 구조 불필요 변경
- UI layout C# 하드코딩
- Hierarchy index 의존
- Projectile마다 Skill 자원 중복 소비
- “향후 확장성”을 이유로 현재 범위를 초과한 과설계

## 29. 짧은 요청을 받았을 때

사용자가 예를 들어:

```text
어시스트 건즈 지속시간 추가해줘.
```

라고만 해도 다음 순서로 작업한다.

```text
1. 이 문서 확인
2. 관련 코드 조사
3. 기존 데이터/시스템 재사용 여부 확인
4. 최소 변경
5. 회귀 테스트
6. 수정 내용 보고
```

## 30. 우선순위

충돌 시 우선순위는 다음과 같다.

```text
1. 현재 사용자의 명시적 요청
2. CODEX_PROJECT_GUIDE.md
3. 기존 프로젝트 관례
4. Codex의 자체 판단
```

사용자가 특정 작업에서 이 문서와 다른 방향을 명시하면 현재 요청을 따른다.

향후 Codex 프롬프트에는 다음 한 줄만 추가해도 된다.

```text
작업 전에 CODEX_PROJECT_GUIDE.md를 먼저 읽고 해당 지침을 준수해.
```
