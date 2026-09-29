# 저주 패시브 모듈 구성

기존 `Passive_ForbiddenCurse`, `Passive_ContemptuousWoe`, `Passive_AbominableHatred`, `Passive_RuinousWrath` 클래스와 스킬 에셋은 변경하지 않습니다. 새 패시브 에셋의 `Modules`에 아래 부품을 조립합니다.

| 기존 스킬 | 모듈 및 원본 수치 |
| --- | --- |
| 금단의 저주 | `Module_Passive_StatBonus`: ATTACK / FIXED / 10, CRIT_RATIO / FIXED / 0.05 두 개 + `Module_Passive_ApplyCurseOnHit`: 확률 1, 지속 5초 |
| 모멸하는 비탄 | `Module_Passive_MissingHealthDamage`: 잃은 체력의 0.02 |
| 가증스러운 증오 | `Module_Passive_HatredOnCursedTarget`: 1~3레벨 쿨타임 12/10/8초, 지속 4초, 공격력 계수 0.2 |
| 파멸하는 분노 | `Module_Passive_LevelStatBonus`: CRIT_DMG / FIXED / 1~3레벨 0.1/0.15/0.2 + `Module_Passive_DamagePerDebuff`: 디버프당 0.1 |

에디터의 `Passive/Curse`에서 적중 효과 4종, `Passive/Stat`에서 고정 및 레벨별 스탯 모듈을 추가합니다. 적중 모듈의 기본 트리거는 `OnHit`, 스탯 모듈은 `OnApplyStaticEffect`입니다. 위 기본값은 모듈 필드에서 변경할 수 있습니다. 레벨 배열 범위 밖에서는 원본 switch의 기본값과 같은 fallback 값을 사용합니다.

`Passive/Stat/공격력 증가`와 `Passive/Stat/치명타 확률 증가`는 공용 `Module_Passive_StatBonus`를 생성하는 에디터 프리셋입니다. 전용 모듈 클래스는 없습니다. 공격력을 선택하면 targetStat=ATTACK, value=10, valueType=FIXED로, 치명타 확률을 선택하면 targetStat=CRIT_RATIO, value=0.05, valueType=FIXED로 초기화합니다. 0.05(FIXED)는 5%p입니다. 두 메뉴 모두 기본 트리거는 OnApplyStaticEffect이며, 생성 후 스탯과 수치를 자유롭게 수정할 수 있습니다.

모듈 하나는 지정한 스탯 하나만 변경합니다. 다른 패시브에 원하는 프리셋만 추가하거나 다른 효과와 조합할 수 있으며, 둘을 함께 추가해야 두 스탯이 모두 올라갑니다. 저주 적중 모듈은 스탯을 올리지 않습니다. 다른 스탯은 `Passive/Stat/스탯 증가`에서 targetStat을 선택합니다. 기존 공용 모듈의 에셋 타입과 필드 이름은 유지합니다.

## 이벤트 전달

`GenericPassiveSkill`은 에셋의 모듈을 런타임별로 복사합니다. `PassiveContextModule`은 소유자와 스킬 레벨을 전달받고, `OnHit` 모듈은 `BattleManager.OnAnyAttackHit`의 `DamageEvent`를 전달받습니다. 이 이벤트는 `DamageHandleComponent`에서 최종 피해 계산 전에 발생합니다.

- `SetLevel` / `OnChangedLevel`: 모듈 레벨 동기화, 이미 적용된 레벨별 스탯 갱신.
- `OnApplyStaticEffect`: 스탯 모듈 적용. 반복 적용 시 이전 보너스를 교체.
- `OnAcquire`: 컨텍스트 초기화 및 적중 이벤트 구독. 반복 호출해도 중복 구독하지 않음.
- `OnLose`: 원래 구독한 BattleManager에서 해제하고 버프와 런타임 상태 정리.
- 동일 트리거의 모듈은 목록 순서로 실행. 한 에셋에서 저주와 증오를 조합할 때 저주 모듈을 먼저 배치하면 같은 적중에서 증오 조건을 만족할 수 있음.

원본의 피해 계산과 조건을 유지합니다. 비탄은 `MissingHPRatio`를 설정하고 분노는 `DamageAmp = 1 + DebuffCount × 계수`를 설정하며, 직접 추가 타격을 생성하지 않습니다. 증오는 최종 피해량이 아닌 공격자의 ATTACK을 사용하고, 발동한 공격과 동일한 `AttackInstanceID`는 쿨타임 중에도 다른 대상을 처리합니다. 실제 지속 피해의 틱과 중첩 정책은 기존 EffectManager/HatredEffect를 사용합니다.

원본 4개는 공격자와 패시브 소유자가 같은지 검사하지 않으므로 새 모듈도 해당 필터를 추가하지 않았습니다. 증오의 원본 조건 역시 유지하여 EffectComponent가 있으면 Curse를 확인합니다. EffectComponent가 없는 대상에 대한 조건 강화나 게임 규칙 변경은 포함하지 않았습니다. 원본의 구독 해제 누락과 스탯 적용 순서 문제는 복제하지 않고 공통 모듈 생명주기에서 정리합니다.

기존 스킬 에셋의 자동 변환 및 원본 스킬 클래스의 삭제는 포함하지 않습니다.

## 검증

- 프로젝트의 Unity 6000.3.19f1 컴파일러와 참조 어셈블리로 런타임 전체 컴파일.
- 외부 매니저와 Unity 의존성을 테스트 대역으로 바꾼 격리 검사 통과: 원본 피해 수치 비교, 레벨별 쿨타임 경계, 같은 공격의 다중 적중, 상태 분리, 구독/해제, 스탯 재적용/해제, 모듈 실행 순서. 공격력/치명타 확률 단독 적용 및 개별 해제, 두 모듈 조합, 에디터 프리셋 생성과 공용 모듈의 설정 복사 검사를 포함합니다.
- 이 검사는 실제 Unity Play Mode, Unity JSON 직렬화 실행 및 화면 조작 검증을 대체하지 않습니다.

