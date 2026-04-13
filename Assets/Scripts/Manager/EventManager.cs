using System.Collections.Generic;
using Core.Event;
using Core.Evnet;

namespace Manager
{
    public class EventManager
    {
        private Dictionary<string, IEvent> events = new();

        public void Register(string name, IEvent gameEvent)
        {
            if (string.IsNullOrEmpty(name))
            {
                return;
            }
            events[name] = gameEvent;
        }

        public void Unregister(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return;
            }
            events.Remove(name);
        }

        public void Broadcast(string name, IEventParameter param)
        {
            if (string.IsNullOrEmpty(name))
            {
                return;
            }

            if (events.ContainsKey(name))
            {
                events[name].Invoke(param);
            }
        }
    }
}