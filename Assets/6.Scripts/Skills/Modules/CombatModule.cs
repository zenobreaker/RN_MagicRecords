using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;


public enum WarningSignType { Circle, Rectangle, Fan }


public interface ISkillEffect
{
    void SetDamageInfo(Character attacker, DamageData damageData, bool bExtraCrit = false, 
        float multiplier = 1.0f);
    void AddIgnore(GameObject ignore);
}

public interface ILifetimeSetup
{
    // 모듈이 생성 직후 이 함수를 호출해서 수명을 세팅해 줄 겁니다.
    void SetLifeTime(float time);
}

public interface ITargetableEffect
{
    void SetTargetPosition(Vector3 targetPosition);
}
public enum DamageApplyType
{
    Inherit,    // 부모 데이터 그대로 사용
    Multiply,   // 부모 데이터 기반으로 배율(%) 적용
    Override    // 완전히 덮어쓰기
}


