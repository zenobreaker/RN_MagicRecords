using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "SO_SkillData", menuName = "Scriptable Objects/SO_PassiveSkillData")]
public class SO_PassiveSkillData : SO_SkillData
{
    public int jobID; // 대상 직업군 

    [Header("Passive Modules (패시브 스킬 부품 조립)")]
    [Tooltip("이 패시브 스킬이 발동시킬 기능(모듈)들을 여기에 추가하세요.")]

    // 💡 [핵심] 다형성 직렬화를 위해 SerializeReference 사용!
    // 이를 통해 인스펙터에서 Module_Passive_RemovePierce, Module_Passive_SplitProjectile 등을 
    // 리스트에 섞어서 넣을 수 있습니다.
    [SerializeReference]
    public List<PassiveModule> Modules = new List<PassiveModule>();

    /// <summary>
    /// 게임 실행 중(런타임) 이 SO 데이터를 바탕으로 실제 동작할 스킬 객체를 생성합니다.
    /// (팩토리 패턴 역할)
    /// </summary>
    public override Skill CreateSkill()
    {
        // 조립된 모듈 데이터를 통째로 넘기며 제네릭 패시브 스킬 객체 생성
        return new GenericPassiveSkill(this);
    }
}
