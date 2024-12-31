using BT.Nodes;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace BT.CustomBTNodes
{
    //TODO: 이 노드는 스크립트용으로 제작된 거여서 제네릭이 기여되어 있다.
    /// <summary>
    /// 데코레이터를 상속받는 클래스이다 
    /// 블랙보드와 연동을 주로 처리한다.
    /// </summary>
    public class Decorator_Blackboard<T> : DecoratorNode
    {
        public enum NotifyObserver
        {
            OnResultChange = 0 , OnValueChange
        }

        public enum ObserveAborts
        {
            None, Selft, Lower_Priority, Both
        }

        public enum BB_KeyQuery
        {
            Equals = 0, NotEquals, LessThan, LessThanOrEqual, GreaterThan, GreaterThanOrEqual,
        }

        protected NotifyObserver notifyObserver;
        protected ObserveAborts observerAborts;
        protected BB_KeyQuery keyQuery;

        private T key;
        public Decorator_Blackboard(string nodeName, BTNode childNode,
            GameObject owner = null,
            SO_Blackboard blackboard = null,
            NotifyObserver notifyObserver = NotifyObserver.OnResultChange,
            ObserveAborts observeAborts = ObserveAborts.Selft, //TODO: 임시로 세팅 
            string boardKey = null,
            T key = default(T),
            BB_KeyQuery keyQuery = BB_KeyQuery.Equals
           ) :
            base(nodeName, childNode, owner, blackboard, boardKey /*key, keyQuery*/)
        {
            this.notifyObserver = notifyObserver;
            this.observerAborts = observeAborts;
            this.keyQuery = keyQuery;
            this.key = key;

            if (blackboard != null)
            {
                blackboard.OnResultChange += OnResultChange;
                blackboard.OnValueChanged += OnValueChanged;
            }

        }

        protected override void OnEnd()
        {
            base.OnEnd();
            // 모든 판단을 끝냈으면 다시 원래대로 돌아간다.
            prevResult = false;
        }


        protected virtual bool CompareValueToQuery<U>(string changedKey)
        {
            if (blackboard == null)
                return false;

            //Debug.Log($"{nodeName} == Compare {key}");
            switch (keyQuery)
            {
                case BB_KeyQuery.Equals:
                return blackboard.CompareValue(changedKey, key);

                case BB_KeyQuery.NotEquals:
                return blackboard.CompareValue(changedKey, key) == false;

                case BB_KeyQuery.GreaterThan:
                return blackboard.GreaterThanValue(changedKey, key);

                case BB_KeyQuery.LessThan:
                return blackboard.LessThanValue(changedKey, key);

                case BB_KeyQuery.LessThanOrEqual:
                {
                    bool bCheck = false;
                    bCheck |= blackboard.LessThanValue(changedKey, key);
                    bCheck |= blackboard.CompareValue(changedKey, key);
                    return bCheck;
                }
                case BB_KeyQuery.GreaterThanOrEqual:
                {
                    bool bCheck = false;
                    bCheck |= blackboard.GreaterThanValue(changedKey, key);
                    bCheck |= blackboard.CompareValue(changedKey, key);
                    return bCheck;
                }
            }

            return false;
        }


        //해당 키값의 결과가 다르면 처리한다.
        protected bool prevResult = false;
        protected void OnResultChange(string changedKey)
        {
            if (isRunning == false)
                return;

            if (notifyObserver != NotifyObserver.OnResultChange)
                return;

            if (changedKey != boardKey)
                return;

            if (observerAborts == ObserveAborts.None)
                return;

            bool newResult = CompareValueToQuery<T>(changedKey) == false;
            
            if (changedKey == "DragonPattern")
            {
                var value = blackboard.GetValue<T>(changedKey);
                Debug.Log($"{nodeName} +  Examine {value} / {key}");
            }

            if (prevResult != newResult)
            {
                prevResult = newResult;
                AbortTask();
            }
        }

        //해당 키값에 변화가 호출되면 이 함수는 계속 호출된다. 
        protected void OnValueChanged(string changedKey)
        {
            if (isRunning == false)
                return;

            if (notifyObserver != NotifyObserver.OnValueChange)
                return;

            // 여기에 등록된 키값이랑 값이 변경되는 키값이랑 같은 경우 
            if (changedKey != boardKey)
                return;
         
            if (changedKey == "DragonPattern")
            {
                var value = blackboard.GetValue<T>(changedKey);
                Debug.Log($"{nodeName} +  Examine {value} / {key}");
            }

            // 중단을 아무것도 안시킨다면 여기서 로직 종료
            if (observerAborts == ObserveAborts.None)
                return; 

            // 비교 했을 때 다르다면?
            if (CompareValueToQuery<T>(changedKey) == false)
            {
                AbortTask();
            }
        }

        protected override bool ShouldExecute()
        {
            //T value = blackboard.GetValue<T>(key);

            bool result = CompareValueToQuery<T>(boardKey);

            return result;
        }

        public override void StopEvaluate()
        {
            isRunning = false;
            childNode.StopEvaluate();
        }

        // 삭제시 구독 해제 
        ~Decorator_Blackboard()
        {
            blackboard.OnResultChange -= OnResultChange;
            blackboard.OnValueChanged -= OnValueChanged;
        }
    }
}