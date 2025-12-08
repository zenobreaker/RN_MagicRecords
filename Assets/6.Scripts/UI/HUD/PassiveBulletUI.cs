using System;
using System.Collections.Generic;
using UnityEngine;

public class PassiveBulletUI : MonoBehaviour
{
    [Header("HUD Handler")]
    [SerializeField] private SO_SkillEventHandler handler;
    private readonly string path = "Skills/SO_SkillEventHandler";


    [Header("Bullet UI Object")]
    [SerializeField] private BulletUI bulletUIObj;
    private BulletUI[] bulletUIs;

    private void Awake()
    {
        if (handler == null)
            handler = Resources.Load<SO_SkillEventHandler>(path);
    }

    private void OnEnable()
    {
        SetHUDHandler(handler);
    }

    private void OnDisable()
    {
        if (handler == null) return;

        handler.OnUpdateMagicBulletLoad -= OnMagicBulletInit;
        handler.OnChangeBullets -= OnChangeBullets;
    }

    protected void SetHUDHandler(SO_SkillEventHandler handler)
    {
        if (handler == null) return;

        handler.OnUpdateMagicBulletLoad += OnMagicBulletInit;
        handler.OnChangeBullets += OnChangeBullets;
    }

    private void OnMagicBulletInit(int maxBullets)
    {
        bulletUIs = new BulletUI[maxBullets];

        for (int i = 0; i < bulletUIs.Length; i++)
        {
            var ui = Instantiate(bulletUIObj, transform);
            bulletUIs[i] = ui;
        }
    }

    private void OnChangeBullets(Queue<BulletData> bullets)
    {
        int bulletCount = bullets.Count;
        var arr = bullets.ToArray();

        for (int i = 0; i < bulletUIs.Length; i++)
        {
            if (i < bulletCount)
            {
                // 존재하는 탄환
                bulletUIs[i].gameObject.SetActive(true);
                bulletUIs[i].DrawBulletUI(arr[i]);
            }
            else
            {
                // 남은 탄환 없음
                bulletUIs[i].gameObject.SetActive(false);
            }
        }

        //gameObject.SetActive(bulletCount > 0);
    }
}
