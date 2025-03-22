using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

// UI ���� Ŭ����
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


    // ���� �� �Ʒ� �Լ��� ȣ��Ǿ� ����.
    public virtual void RefreshUI()
    {

    }

    // ��ũ�� ������Ʈ ��ġ
    public void InitScrollviewObject(int count = 0)
    {
        if (content == null || childObject == null) return;

        // �̹� �ڽĵ��� �մ��� �˻� 
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


        // �̹� ���ϴ� �ڽĵ��� �ִٸ� ������ �ִ� �ڽ��� ������
        if (content.transform.childCount >= count)
        {
            return;
        }

        for (int i = 0; i < count; i++)
        {
            Instantiate(childObject, content.transform);
        }
    }

    // ��ũ�Ѻ� ������Ʈ�� �ڽ� �߰��ϱ� 
    public void AddScrollviewObject(int count = 0)
    {
        if (content == null || childObject == null) return;

        for (int i = 0; i < count; i++)
        {
            Instantiate(childObject, content.transform);
        }
    }




    // �ڽ� ������Ʈ���� �����ϰ� �ݹ� ����� �Ҵ��Ѵ�.
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
