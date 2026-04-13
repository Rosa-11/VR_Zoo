using System;
using Core.Evnet;

namespace Core.Event
{
    public interface IEvent
    {
        public void Invoke(IEventParameter parameter);
    }

    public class Event : IEvent
    {
        private Action noneParamCallBack;
        public Event(Action noneParamCallBack)
        {
            this.noneParamCallBack = noneParamCallBack;
        }
        public void Invoke(IEventParameter parameter)
        {
            noneParamCallBack?.Invoke();
        }
    }

    public class Event<T1> : IEvent
    {
        private Action<T1> oneParamCallBack;
        public Event(Action<T1> oneParamCallBack)
        {
            this.oneParamCallBack = oneParamCallBack;
        }

        public void Invoke(IEventParameter parameter)
        {
            if (parameter == null) return;
            EventParameter<T1> param = parameter as EventParameter<T1>;
            oneParamCallBack?.Invoke(param.param1);
        }
    }
    public class Event<T1, T2> : IEvent
    {
        private Action<T1, T2> twoParamCallBack;
        public Event(Action<T1, T2> twoParamCallBack)
        {
            this.twoParamCallBack = twoParamCallBack;
        }

        public void Invoke(IEventParameter parameter)
        {
            if (parameter == null) return;
            EventParameter<T1, T2> param = parameter as EventParameter<T1, T2>;
            twoParamCallBack?.Invoke(param.param1, param.param2);
        }
    }

    public class Event<T1, T2, T3> : IEvent
    {
        private Action<T1, T2, T3> threeParamCallBack;
        public Event(Action<T1, T2, T3> threeParamCallBack)
        {
            this.threeParamCallBack = threeParamCallBack;
        }

        public void Invoke(IEventParameter parameter)
        {
            if (parameter == null) return;
            EventParameter<T1, T2, T3> param = parameter as EventParameter<T1, T2, T3>;
            threeParamCallBack?.Invoke(param.param1, param.param2, param.param3);
        }
    }
}