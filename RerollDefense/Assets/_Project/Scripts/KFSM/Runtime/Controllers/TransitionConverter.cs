using System;
using System.Collections.Generic;
using System.Linq;

namespace Kylin.FSM
{
    public static class TransitionConverter
    {
        public static Transition[] ConvertToRuntimeTransitions(List<TransitionEntry> entries)
        {
            if (entries == null || entries.Count == 0)
                return Array.Empty<Transition>();

            int count = entries.Count;
            var result = new Transition[count];

            for (int i = 0; i < count; i++)
            {
                var entry = entries[i];
                var t = new Transition
                {
                    FromStateId = entry.FromStateId,
                    ToStateId = entry.ToStateId,
                    Priority = entry.Priority,
                    RequiredMask = 0,
                    IgnoreMask = 0
                };

                if (entry.RequiredTriggers != null)
                {
                    foreach (var trig in entry.RequiredTriggers)
                        t.RequiredMask |= (int)trig;
                }

                if (entry.IgnoreTriggers != null)
                {
                    foreach (var trig in entry.IgnoreTriggers)
                        t.IgnoreMask |= (int)trig;
                }

                result[i] = t;
            }

            for (int i = 0; i < count - 1; i++)
            {
                int maxIdx = i;
                int maxPri = result[i].Priority;

                for (int j = i + 1; j < count; j++)
                {
                    if (result[j].Priority > maxPri)
                    {
                        maxPri = result[j].Priority;
                        maxIdx = j;
                    }
                }

                if (maxIdx != i)
                {
                    var tmp = result[i];
                    result[i] = result[maxIdx];
                    result[maxIdx] = tmp;
                }
            }

            return result;
        }

        public static TransitionEntry ConvertToEditorTransition(Transition transition)
        {
            var entry = new TransitionEntry
            {
                FromStateId = transition.FromStateId,
                ToStateId = transition.ToStateId,
                Priority = transition.Priority
            };

            var requiredTriggers = new List<Trigger>();
            var ignoreTriggers = new List<Trigger>();

            foreach (Trigger trigger in System.Enum.GetValues(typeof(Trigger)))
            {
                if (trigger == Trigger.None) continue;

                int triggerValue = (int)trigger;

                if ((transition.RequiredMask & triggerValue) == triggerValue)
                {
                    requiredTriggers.Add(trigger);
                }

                if ((transition.IgnoreMask & triggerValue) == triggerValue)
                {
                    ignoreTriggers.Add(trigger);
                }
            }

            entry.RequiredTriggers = requiredTriggers.ToArray();
            entry.IgnoreTriggers = ignoreTriggers.ToArray();

            return entry;
        }
    }
}