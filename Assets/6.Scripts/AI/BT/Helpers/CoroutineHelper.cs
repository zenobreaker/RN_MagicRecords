using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace BT.Helpers
{
    public class CoroutineHelper : MonoBehaviour
    {
        private static CoroutineHelper instance;

        public static CoroutineHelper Instance
        {
            get
            {
                if(instance == null)
                {
                    GameObject go = new GameObject("CoroutineHelper");
                    instance = go.AddComponent<CoroutineHelper>();
                    DontDestroyOnLoad(go);
                }

                return instance;
            }
        }

        public Coroutine StartHelperCoroutine(IEnumerator coroutine)
        {
            return StartCoroutine(coroutine);
        }

        public void StopHelperCoroutine(Coroutine coroutine)
        {
            if(coroutine != null)
            {
                StopCoroutine(coroutine);
            }
        }
    }

}