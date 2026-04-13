using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Core.Evnet
{
    public class EventParameter<T1> : IEventParameter
    {
        public T1 param1;
        public EventParameter(T1 param1)
        {
            this.param1 = param1;
        }
    }
    public class EventParameter<T1, T2> : IEventParameter
    {
        public T1 param1;
        public T2 param2;
        public EventParameter(T1 param1, T2 param2)
        {
            this.param1 = param1;
            this.param2 = param2;
        }
    }
    public class EventParameter<T1, T2, T3> : IEventParameter
    {
        public T1 param1;
        public T2 param2;
        public T3 param3;
        public EventParameter(T1 param1, T2 param2, T3 param3)
        {
            this.param1 = param1;
            this.param2 = param2;
            this.param3 = param3;
        }
    }
    public interface IEventParameter
    {
    }
}