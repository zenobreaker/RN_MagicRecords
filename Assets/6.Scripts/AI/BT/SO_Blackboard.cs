using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

# region ComparisonStaategy 
public interface IComparisonStrategy
{
    bool IsEqual(object a, object b);
    bool IsGreaterthan(object a, object b);
    bool IsLessthan(object a, object b);

    IComparisonStrategy DeepCopy(); // 깊은 복사 메서드 추가
}

public class Vector3ComparisonStrategy : IComparisonStrategy
{
    public bool IsEqual(object a, object b)
    {
        if (a is Vector3 vectorA && b is Vector3 vectorB)
        {
            return vectorA == vectorB;
        }
        return false;
    }


    public bool IsGreaterthan(object a, object b)
    {
        if (a is Vector3 vectorA && b is Vector3 vectorB)
        {
            return vectorA.magnitude > vectorB.magnitude;
        }
        return false;
    }

    public bool IsLessthan(object a, object b)
    {
        if (a is Vector3 vectorA && b is Vector3 vectorB)
        {
            return vectorA.magnitude < vectorB.magnitude;
        }
        return false;
    }

    public IComparisonStrategy DeepCopy()
    {
        return new Vector3ComparisonStrategy();
    }
}
public class GameObjectComparisonStrategy : IComparisonStrategy
{
    public bool IsEqual(object a, object b)
    {
        if (a is GameObject objA && b is GameObject objB)
        {
            return ReferenceEquals(objA, objB); // 또는 이름 등 다른 기준으로 비교 가능
        }
        return false;
    }

    public bool IsGreaterthan(object a, object b)
    {
        if (a is GameObject objA && b is GameObject objB)
        {
            int hashA = objA.GetHashCode();
            int hashB = objB.GetHashCode();

            return hashA > hashB; // 또는 이름 등 다른 기준으로 비교 가능
        }
        return false;
    }

    public bool IsLessthan(object a, object b)
    {
        if (a is GameObject objA && b is GameObject objB)
        {
            int hashA = objA.GetHashCode();
            int hashB = objB.GetHashCode();

            return hashA < hashB; // 또는 이름 등 다른 기준으로 비교 가능
        }
        return false;
    }

    public IComparisonStrategy DeepCopy()
    {
        return new GameObjectComparisonStrategy();
    }
}
public class NumericComparisonStrategy<T> : IComparisonStrategy where T : struct, IComparable
{
    public IComparisonStrategy DeepCopy()
    {
        return new NumericComparisonStrategy<T>();
    }

    public bool IsEqual(object a, object b)
    {
        if (a is T valueA && b is T valueB)
            return valueA.CompareTo(valueB) == 0;
        return false;
    }

    public bool IsGreaterthan(object a, object b)
    {
        if (a is T valueA && b is T valueB)
            return valueA.CompareTo(valueB) > 0;
        return false;
    }

    public bool IsLessthan(object a, object b)
    {
        if (a is T valueA && b is T valueB)
            return valueA.CompareTo(valueB) < 0;
        return false;
    }


}
public class StringComparisonStrategy : IComparisonStrategy
{
    public IComparisonStrategy DeepCopy()
    {
        return new StringComparisonStrategy();
    }

    public bool IsEqual(object a, object b)
    {
        if (a is string valueA && b is string valueB)
        {
            return valueA.Equals(valueB);
        }
        return false;
    }

    public bool IsGreaterthan(object a, object b)
    {
        if (a is string valueA && b is string valueB)
        {
            return valueA.CompareTo(valueB) > 0;
        }

        return false;
    }

    public bool IsLessthan(object a, object b)
    {
        if (a is string valueA && b is string valueB)
        {
            return valueA.CompareTo(valueB) < 0;
        }

        return false;
    }
}

public class EnumComparisonStrategy<T> : IComparisonStrategy where T : struct, Enum
{
    public IComparisonStrategy DeepCopy()
    {
        return new EnumComparisonStrategy<T>();
    }

    public bool IsEqual(object a, object b)
    {
        if (a == null || b == null)
        {
            return false;
        }

        if (a.GetType().IsEnum && b.GetType().IsEnum)
        {
            // 두 값이 동일한 Enum 타입인지 확인
            if (a.GetType() != b.GetType())
            {
                Debug.LogWarning($"Enum types do not match: {a.GetType()} != {b.GetType()}");
                return false;
            }

            return a.Equals(b);
        }
        else
        {
            Debug.LogWarning("Both values must be enums.");
            return false;
        }
    }

    public bool IsGreaterthan(object a, object b)
    {
        if (a == null || b == null)
        {
            return false;
        }

        if (a.GetType().IsEnum && b.GetType().IsEnum)
        {
            if (a.GetType() != b.GetType())
            {
                Debug.LogWarning($"Enum types do not match: {a.GetType()} != {b.GetType()}");
                return false;
            }

            return (int)a > (int)b;
        }
        else
        {
            Debug.LogWarning("Both values must be enums.");
            return false;
        }
    }

    public bool IsLessthan(object a, object b)
    {
        if (a == null || b == null)
        {
            return false;
        }

        if (a.GetType().IsEnum && b.GetType().IsEnum)
        {
            if (a.GetType() != b.GetType())
            {
                Debug.LogWarning($"Enum types do not match: {a.GetType()} != {b.GetType()}");
                return false;
            }

            return (int)a < (int)b;
        }
        else
        {
            Debug.LogWarning("Both values must be enums.");
            return false;
        }
    }


}

#endregion

// 블랙보드에서 사용할 데이터의 기본 인터페이스 정의 
public interface IBlackboardKey
{
    string KeyName { get; } // 키의 이름
    object GetValue(); // 값을 반환할 땐 object 타입으로 반환
    T GetValue<T>(); // 제네릭 메서드 추가
    void SetValue(object value);  // 값을 설정할 때도 object 타입으로 받기
    Type GetValueType();    // 값의 타입 반환

    IBlackboardKey DeepCopy();
}


// 제네릭을 활용하여 다양한 타입의 블랙보드 키 구현
[System.Serializable]
public class BlackboardKey<T>
    : IBlackboardKey
{
    public string KeyName { get; set; }
    private T value;

    //Activator.CreateInstance 호출용 기본 생성자 정의
    public BlackboardKey()
    {

    }
    public BlackboardKey(string KeyName)
    {
        this.KeyName = KeyName;
    }

    public object GetValue() => value;
    // 제네릭 메서드 구현 
    public U GetValue<U>()
    {
        return (U)(object)value; // T로 캐스팅
    }

    public void SetValue(object newValue)
    {
        if (newValue is T typedValue)
        {
            value = typedValue;

            return;
        }

        value = default(T);
    }

    public Type GetValueType() => typeof(T);

    public IBlackboardKey DeepCopy()
    {
        BlackboardKey<T> copy = new BlackboardKey<T>(this.KeyName);
        copy.SetValue(this.GetValue()); // 깊은 복사: 값 복사
        return copy;
    }
}
//////////////////////////////////////////////////////////////////////////////////////////////////////////////// 
/// <summary>
/// Dictionary의 직렬화가 되지 않은 구조를 해결하기 위한 Serializble 클래스 생성
/// </summary>
[System.Serializable]
public class KeyValue
{
    public string Key;
    public string KeyTypeName;  // 키의 자료형 이름 이 이름으로 리플렉션을 이용하여 자로형 추론
    public IBlackboardKey Value;
}


//////////////////////////////////////////////////////////////////////////////////////////////////////////////// 
[CreateAssetMenu(fileName = "NewBlackboard", menuName = "AI/Blackboard")]
public class SO_Blackboard : ScriptableObject
{

    [SerializeField] private List<KeyValue> serializedKeys = new List<KeyValue>();
    private Dictionary<string, IBlackboardKey> keys = new Dictionary<string, IBlackboardKey>();
    public Dictionary<string, IBlackboardKey> GetAllKeys() => keys;

    private Dictionary<Type, IComparisonStrategy> comparisonStrategies = new Dictionary<Type, IComparisonStrategy>();

    // 블랙보드의 특정 키의 결과가 다를 때 발생
    public event Action<string> OnResultChange;
    // 블랙보드의 특정 키가 변경될 때 발생
    public event Action<string> OnValueChanged;

    private void OnEnable()
    {
        Initialize();

        // 직렬화된 데이터를 Dictionary로 변환
        foreach (var kv in serializedKeys)
        {
            if (string.IsNullOrEmpty(kv.Key) == false && string.IsNullOrEmpty(kv.KeyTypeName) == false)
            {
                Type type = Type.GetType("System." + kv.KeyTypeName);
                var blackboardKey = CreateBlackboardKey(type, kv.Key, kv.Value);

                if (keys.ContainsKey(kv.Key) == false)
                    keys[kv.Key] = blackboardKey;
            }
        }
    }

#if UNITY_EDITOR
    private void SyncSerializedKeys()
    {
        serializedKeys.Clear();
        foreach (var kvp in keys)
        {
            serializedKeys.Add(new KeyValue
            {
                Key = kvp.Key,
                KeyTypeName = kvp.Value.GetValueType().Name,
                Value = kvp.Value
            });
        }
    }

#endif 

    public void Initialize()
    {
        // 비교 전략 등록
        RegisterComparisonStrategy<Vector3>(new Vector3ComparisonStrategy());
        RegisterComparisonStrategy<GameObject>(new GameObjectComparisonStrategy());
        RegisterComparisonStrategy<string>(new StringComparisonStrategy());
        RegisterComparisonStrategy<int>(new NumericComparisonStrategy<int>());
        RegisterComparisonStrategy<float>(new NumericComparisonStrategy<float>());
        RegisterComparisonStrategy<double>(new NumericComparisonStrategy<double>());
    }

    // 동적 등록을 위한 팩토리 메서드 
    public void RegisterComparisonStrategy<T>(IComparisonStrategy strategy)
    {
        comparisonStrategies[typeof(T)] = strategy;
    }


    public void AddEnumComparisonStrategy<T>() where T : struct, Enum
    {
        comparisonStrategies[typeof(T)] = new EnumComparisonStrategy<T>();
    }

    // 새로운 키 추가
    public void AddKey<T>(string keyName)
    {
        if (!keys.ContainsKey(keyName))
        {
            keys[keyName] = new BlackboardKey<T>(keyName);
        }
        else
        {
            Debug.LogWarning($"Key {keyName} already exists.");
        }
    }

    public void AddKey(Type type, string keyName)
    {
        if (type == null)
            return;

        if (!keys.ContainsKey(keyName))
        {
            // Type에 따라 다르게 생성하기 위해 리플렉션 사용
            var keyType = typeof(BlackboardKey<>).MakeGenericType(type);
            var keyInstance = Activator.CreateInstance(keyType);

            // keyName을 설정할 수 있도록 캐스팅 후 설정
            if (keyInstance is BlackboardKey<object> blackboardKey)
            {
                blackboardKey.KeyName = keyName;
            }

            // keys 딕셔너리에 추가 
            keys[keyName] = (IBlackboardKey)keyInstance;

#if UNITY_EDITOR
            SyncSerializedKeys();
#endif
        }
    }

    public void RemoveKey(string key)
    {
        keys.Remove(key);
#if UNITY_EDITOR
        SyncSerializedKeys();
#endif
    }

    public void ClearKeys()
    {
        keys.Clear();
    }

    public Type GeteKeyType(string selectedKey)
    {
        return keys[selectedKey].GetValueType();
    }

    // 데이터 설정 메서드
    public void SetValue(string keyName, object value)
    {
        Type valueType = value.GetType();

        if (keys.ContainsKey(keyName))
        {
            IBlackboardKey existingKey = keys[keyName];

            // 기존 키가 동일한 타입인지 확인
            if (existingKey.GetValueType() == valueType)
            {
                object oldValue = existingKey.GetValue();
                if (EqualityComparer<object>.Default.Equals(oldValue, value) == false)
                {
                    // 값 설정 및 이벤트 트리거
                    existingKey.SetValue(value);
                    OnResultChange?.Invoke(keyName);
                    OnValueChanged?.Invoke(keyName);
                }
            }
            else
            {
                Debug.LogError($"Key {keyName} exists but with a different type.");
            }
        }
        else
        {
            // 새로운 키 생성
            IBlackboardKey newKey = CreateBlackboardKey(valueType, keyName, value);

            // 값 설정
            keys[keyName] = newKey;
            OnResultChange?.Invoke(keyName);
            OnValueChanged?.Invoke(keyName);  // 새로 추가된 키에도 이벤트 트리거
        }
    }

    // 제네릭 버전의 데이터 설정 메서드
    public void SetValue<T>(string keyName, T value)
    {

        if (keys.TryGetValue(keyName, out IBlackboardKey key))
        {
            if (key is BlackboardKey<T> typedKey)
            {
                T oldValue = typedKey.GetValue<T>();
                if (!EqualityComparer<T>.Default.Equals(oldValue, value))
                {

                    typedKey.SetValue(value);
                    OnResultChange?.Invoke(keyName);
                    OnValueChanged?.Invoke(keyName); // 값 변경 시 트리거
                }
            }
            else
            {
                Debug.LogError($"Key {keyName} is not of Type {typeof(T)}");
            }
        }
        else
        {
            // 새로운 키 추가
            BlackboardKey<T> newKey = new BlackboardKey<T>(keyName);
            newKey.SetValue(value);
            keys.Add(keyName, newKey);

            OnResultChange?.Invoke(keyName);
            OnValueChanged?.Invoke(keyName); // 새로 추가된 키도 트리거
        }
    }

    // 동적으로 키를 생성하는 헬퍼 메서드
    private IBlackboardKey CreateBlackboardKey(Type valueType, string keyName, object value)
    {
        Type keyType = typeof(BlackboardKey<>).MakeGenericType(valueType);
        IBlackboardKey newKey = (IBlackboardKey)Activator.CreateInstance(keyType, keyName);

        // 값 설정
        if (newKey != null)
        {
            newKey.SetValue(value);
        }

        return newKey;
    }

    // 키의 값 가져오기 
    public T GetValue<T>(string keyName)
    {
        if (keys.TryGetValue(keyName, out IBlackboardKey key))
        {
            if (key is BlackboardKey<T> typedKey)
            {
                return typedKey.GetValue<T>();
            }
            else
            {
                Debug.Log($"Key {keyName} is not of type {typeof(T)}");
            }
        }
        else
        {
            Debug.Log($"Key {keyName} not found in Blackboard");
        }

        return default(T);
    }

    public bool CompareValue<T>(string keyName1, string keyName2)
    {

        if (keys.TryGetValue(keyName1, out IBlackboardKey key))
        {
            if (key.GetValueType() == typeof(T))
            {
                IComparisonStrategy strategy = GetComparisonStrategy(typeof(T));
                if (strategy != null)
                {
                    object storedValue = key.GetValue();
                    return strategy.IsEqual(storedValue, keyName2);
                }

            }
        }

        return false;
    }

    public bool CompareValue<T>(string keyName, T value)
    {
        if (keys.TryGetValue(keyName, out IBlackboardKey key))
        {
            if (key.GetValueType() == typeof(T))
            {
                IComparisonStrategy strategy = GetComparisonStrategy(typeof(T));
                if (strategy != null)
                {
                    object storedValue = key.GetValue();
                    return strategy.IsEqual(storedValue, value);
                }
            }
        }
        return false;
    }

    public bool GreaterThanValue<T>(string keyName1, string keyName2)
    {
        if (keys.TryGetValue(keyName1, out IBlackboardKey key))
        {
            if (key.GetValueType() == typeof(T))
            {
                IComparisonStrategy strategy = GetComparisonStrategy(typeof(T));
                if (strategy != null)
                {
                    object storedValue = key.GetValue();
                    return strategy.IsGreaterthan(storedValue, keyName2);
                }
            }
        }

        return false;
    }

    public bool GreaterThanValue<T>(string keyName, T value)
    {
        if (keys.TryGetValue(keyName, out IBlackboardKey key))
        {
            if (key.GetValueType() == typeof(T))
            {
                IComparisonStrategy strategy = GetComparisonStrategy(typeof(T));
                if (strategy != null)
                {
                    object storedValue = key.GetValue();
                    return strategy.IsGreaterthan(storedValue, value);
                }
            }
        }
        return false;
    }

    public bool LessThanValue<T>(string keyName1, string keyName2)
    {
        if (keys.TryGetValue(keyName1, out IBlackboardKey key))
        {
            if (key.GetValueType() == typeof(T))
            {
                IComparisonStrategy strategy = GetComparisonStrategy(typeof(T));
                if (strategy != null)
                {
                    object storedValue = key.GetValue();
                    return strategy.IsLessthan(storedValue, keyName2);
                }
            }
        }

        return false;
    }

    public bool LessThanValue<T>(string keyName, T value)
    {
        if (keys.TryGetValue(keyName, out IBlackboardKey key))
        {
            if (key.GetValueType() == typeof(T))
            {
                IComparisonStrategy strategy = GetComparisonStrategy(typeof(T));
                if (strategy != null)
                {
                    object storedValue = key.GetValue();
                    return strategy.IsLessthan(storedValue, value);
                }
            }
        }
        return false;
    }

    private IComparisonStrategy GetComparisonStrategy(Type type)
    {
        comparisonStrategies.TryGetValue(type, out IComparisonStrategy strategy);
        return strategy;
    }

    public SO_Blackboard Clone()
    {
        SO_Blackboard soBB = ScriptableObject.CreateInstance<SO_Blackboard>();
        soBB.keys = new Dictionary<string, IBlackboardKey>();
        soBB.comparisonStrategies = new Dictionary<Type, IComparisonStrategy>();

        foreach (KeyValuePair<string, IBlackboardKey> pair in this.keys)
        {
            IBlackboardKey value = pair.Value.DeepCopy();
            soBB.keys[pair.Key] = value;
        }

        foreach (KeyValuePair<Type, IComparisonStrategy> pair in this.comparisonStrategies)
        {
            IComparisonStrategy value = pair.Value.DeepCopy();
            soBB.comparisonStrategies[pair.Key] = value;
        }

        return soBB;
    }


}
