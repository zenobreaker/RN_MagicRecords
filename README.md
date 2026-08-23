# The Wheeler

> Unity 6 기반 개인 프로젝트
> Skill Module Framework와 Passive Skill을 통한 Active Skill 확장 시스템을 중심으로 개발한 액션 게임 프로젝트

## 🎮 Project Overview

**The Wheeler**는 스킬 조합과 패시브에 의한 액티브 스킬 변형을 중심으로 설계한 Unity 개인 프로젝트입니다.

스킬을 개별 클래스 단위로 구현하는 대신 **Skill Module을 조합하여 하나의 스킬을 구성**하고,
**Passive Module을 통해 기존 Active Skill의 동작을 확장하거나 변경**할 수 있도록 설계했습니다.

이를 통해 콘텐츠가 증가해도 기존 코드의 수정 범위를 최소화하고,
ScriptableObject 기반 데이터 구성만으로 다양한 스킬을 제작할 수 있도록 했습니다.

---

## 🎥 Demo

### Passive Skill → Active Skill Modification

패시브를 획득하면 기존 Active Skill의 발사 방식, 투사체, 공격 방식 등의 동작이 변경됩니다.

<!-- TODO: Demo GIF / YouTube 영상 삽입 -->

![Skill Demo](Docs/Images/SkillDemo.gif)

---

# ⭐ Key Features

## 1. Skill Module Framework

### Problem

스킬마다 별도의 클래스를 생성하면 스킬 종류가 증가할수록 코드가 증가하고,
비슷한 동작을 가진 스킬을 추가할 때 기존 코드를 반복적으로 작성해야 합니다.

### Solution

스킬의 동작을 작은 단위의 **Skill Module**로 분리하고,
Phase와 Trigger에 Module을 조합하는 방식으로 스킬을 구성했습니다.

```text
Active Skill
│
├── Phase
│   ├── Trigger
│   │   ├── Module
│   │   ├── Module
│   │   └── Module
│   └── Trigger
│       └── Module
│
└── Phase
    └── ...
```
### 주요 구현
GenericActiveSkill  
SO_ActiveSkillData  
Skill Phase / Trigger  
Common / Combat / Animation Module  
Custom Skill Editor  
### Code
GenericActiveSkill.cs  
SO_ActiveSkillData.cs  
SkillModuleDrawer.cs  
PhaseSkillDrawer.cs  

## 2. Passive Skill → Active Skill Modification

이 프로젝트에서는 Passive Skill을 단순한 Stat Modifier가 아니라
Active Skill의 실제 동작을 변경하는 Module로 확장했습니다.
``` text
                 Active Skill
                      │
                      ▼
                Skill Module
                      ▲
                      │
                Passive Skill
                      │
                      ▼
             Behavior Modification
```
Example
``` text
Magic Bullet
    ↓
Projectile 1개 발사

Passive 획득 후:

Magic Bullet
    ↓
Bonus Projectile
    ↓
Projectile 추가 발사
```
또는:
```text 
Magic Bullet
    ↓
Homing Projectile
    ↓
적을 추적하는 투사체
```
### 구현한 Passive Module 예시
BonusProjectile  
HomingProjectile  
AssistDroneHoming  
AssistDronePierce  
RapidFireRush  
IncendiaryBullet
RemoveCharge  
FocusedFire  
SplitChildHoming  
IgnoreDefense  
### Code
GenericPassiveSkill.cs  
SO_PassiveSkillData.cs  
PassiveModuleDrawer.cs  

## 3. ScriptableObject 기반 Skill Data

스킬 실행 로직과 데이터를 분리하여,
새로운 스킬을 추가할 때 기존 코드를 수정하지 않고
기존 Module을 조합하여 스킬을 구성할 수 있도록 했습니다.
``` text
SO_ActiveSkillData
        │
        ▼
   Skill Definition
        │
        ├── Phase
        ├── Trigger
        └── Module
```
동일한 Module을 여러 스킬에서 재사용할 수 있으며,
새로운 콘텐츠 추가 시 기존 시스템의 수정 범위를 최소화했습니다.

## 4. Custom Skill Editor

Module 기반 구조에서는 많은 Module을 Inspector에서 직접 관리해야 하기 때문에
기본 Unity Inspector만으로 Skill Data를 편집하기 불편한 문제가 있었습니다.

이를 해결하기 위해 Skill 전용 Custom Editor를 구현했습니다.

### 목표

### Skill 구조를 한눈에 확인
Phase / Trigger / Module 계층 관리
필요한 Module만 직관적으로 설정
반복적인 Inspector 조작 최소화
### Code
SkillModuleDrawer.cs
PhaseSkillDrawer.cs
PassiveModuleDrawer.cs
SelectImplementationDrawer.cs
## 5. Async Stage Flow

Stage 진행을 여러 상태와 이벤트의 조합으로 관리하는 대신,
UniTask 기반의 순차적인 Async Flow로 구성했습니다.
```text
Load Stage
    ↓
Load Room
    ↓
Spawn Player
    ↓
Run Wave
    ↓
Wait Until Clear
    ↓
Next Wave
    ↓
Stage Result
await LoadRoomAsync();

await SpawnPlayerAsync();

await RunWaveAsync();

await WaitStageClearAsync();

await ShowStageResultAsync();
```
Stage 전체 실행 순서를 코드의 실행 흐름과 동일하게 표현하여
진행 로직을 한 곳에서 파악하기 쉽게 구성했습니다.

### 6. Inventory System

아이템 종류가 증가할 때 Inventory 코드를 수정해야 하는 문제를 줄이기 위해
아이템 타입별 처리 정책을 분리했습니다.
```text
Inventory
    │
    ├── Item
    ├── Equipment
    ├── Consumable
    └── ...
```
새로운 아이템 타입을 추가할 때 기존 Inventory 코드에 대한 변경을 최소화했습니다.

## 🧩 Architecture
``` text
                     ScriptableObject
                          Data
                           │
                           ▼
                      Game System
                           │
             ┌─────────────┼─────────────┐
             ▼             ▼             ▼
        Skill System   Stage System   Inventory
             │             │             │
             ▼             ▼             ▼
       Active/Passive   Async Flow    Item Policy
             │
             ▼
       Module Composition
```
## 🛠 Tech Stack
Category	Technology  
Engine	Unity 6  
Language	C#  
Async	Cysharp UniTask  
Data	ScriptableObject  
Editor	Unity Custom Editor  
Architecture	Module Composition  
Stage Flow	async / await  
Object Management	Object Pooling  

### 📂 Project Structure
```text
Assets
├── Editor
│   ├── SkillModuleDrawer.cs
│   ├── PhaseSkillDrawer.cs
│   ├── PassiveModuleDrawer.cs
│   └── SelectImplementationDrawer.cs
│
├── 6.Scripts
│   ├── ScriptableObjects
│   │   ├── SO_ActiveSkillData.cs
│   │   └── SO_PassiveSkillData.cs
│   │
│   └── Skills
│       ├── ActiveSkills
│       │   ├── GenericActiveSkill.cs
│       │   └── ...
│       ├── PassiveSkills
│       │   ├── GenericPassiveSkill.cs
│       │   └── ...
│       └── Modules
│           ├── Commons
│           ├── Combats
│           └── Passive
``` 
👤 Developer

Choi Je-seong

Unity Client Programmer

