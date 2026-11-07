using System;
using UnityEngine;

/// <summary>Resources의 클래스명과 같은 이름의 에셋을 공유 인스턴스로 로드합니다.</summary>
public abstract class SO_Singleton<T> : ScriptableObject where T : SO_Singleton<T>
{
    private static T instance;

    public static T Instance
    {
        get
        {
            if (instance == null)
            {
                instance = Resources.Load<T>(typeof(T).Name);
                if (instance == null)
                {
                    throw new InvalidOperationException(
                        $"Resources/{typeof(T).Name}.asset 싱글톤 SO 에셋이 필요합니다.");
                }
            }

            return instance;
        }
    }
}
