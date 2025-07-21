using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

// UI 기초 클래스
public abstract class UiBase : MonoBehaviour
{
    private GameObject content;
    private GameObject childObject;

    public event Action UIOpend;
    public event Action UIClosed;

    protected virtual void OnEnable()
    {
        UIOpend?.Invoke(); 
    }

    protected virtual void OnDisable()
    {
        UIClosed?.Invoke();
    }

    public virtual void CloseUI()
    {
        gameObject.SetActive(false);
    }

    // 갱신 시 아래 함수가 호출되어 진다.
    public virtual void RefreshUI() { }

    // 스크롤 오브젝트 배치
    public void InitScrollviewObject(int count = 0)
    {
        if (content == null || childObject == null) return;

        // 이미 자식들이 잇는지 검사 
        if (content.transform.childCount > 0)
        {
            for (int i = 0; i < content.transform.childCount; i++)
            {
                if (content.transform.GetChild(i) != null)
                {
                    content.transform.GetChild(i).gameObject.SetActive(false);
                }
            }
        }


        // 이미 원하는 자식들이 있다면 가지고 있는 자식을 쓰도록
        if (content.transform.childCount >= count)
        {
            return;
        }

        for (int i = 0; i < count; i++)
        {
            Instantiate(childObject, content.transform);
        }
    }

    // 스크롤뷰 오브젝트에 자식 추가하기 
    public void AddScrollviewObject(int count = 0)
    {
        if (content == null || childObject == null) return;

        for (int i = 0; i < count; i++)
        {
            Instantiate(childObject, content.transform);
        }
    }




    // 자식 오브젝트들을 설정하고 콜백 기능을 할당한다.
    public virtual void SetScrollviewChildObjectCallback(Action callback)
    {
        if (content == null || childObject == null) return;

        for (int i = 0; i < content.transform.childCount; i++)
        {
            var childObject = content.transform.GetChild(i);

        }
    }

    public virtual void SetScrollviewChildObjectsCallback<T>(Action<T> callback)
    {
        if (content == null || childObject == null) return;

        for (int i = 0; i < content.transform.childCount; i++)
        {
            var childObject = content.transform.GetChild(i);

            if (childObject.TryGetComponent<T>(out T component))
            {
                if (callback != null)
                {
                    callback(component);
                }

            }

        }
    }

}
