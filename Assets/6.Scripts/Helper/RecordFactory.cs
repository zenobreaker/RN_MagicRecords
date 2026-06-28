using System.Linq;
using UnityEngine;

public static class RecordFactory
{
    public static RecordPassive CreateRecordPassive(SO_RecordData data)
    {
        if (data == null) return null;

        // 💡 Triggers 리스트를 순회하며 클래스 이름이 적혀있는 트리거를 찾습니다.
        var targetTrigger = data.Triggers?.FirstOrDefault(t => !string.IsNullOrEmpty(t.ClassName));

        // 유효한 클래스 이름이 없다면 기본 RecordPassive를 생성합니다.
        if (targetTrigger == null || string.IsNullOrEmpty(targetTrigger.ClassName))
        {
            return new RecordPassive(data);
        }

        // 💡 찾은 클래스 이름을 기반으로 알맞은 커스텀 객체를 생성합니다.
        return targetTrigger.ClassName switch
        {
            "Record_AutoReload" => new Record_AutoReload(data),
            "Record_FinalEffort" => new Record_FinalEffort(data),
            _ => new RecordPassive(data)
        };
    }
}