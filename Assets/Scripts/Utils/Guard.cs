using System;
using System.Collections.Generic;

// Use example :
// Guard<int> myInt = new Guard<int>(5); // 5 is the original value
// using (var guard = myInt.Guard(10))
// {
//   Console.WriteLine(myInt.Value);
// }
// Console.WriteLine(myInt.Value);

namespace VRtist
{
    public class Guard<T>
    {
        public T Value { get; private set; }

        private sealed class GuardHolder<TGuardable> : IDisposable where TGuardable : Guard<T>
        {
            private readonly TGuardable guardable;
            private readonly T originalValue;

            public GuardHolder(TGuardable guardable)
            {
                this.guardable = guardable;
                originalValue = guardable.Value;
            }

            public void Dispose()
            {
                guardable.Value = originalValue;
            }
        }

        public Guard(T value)
        {
            Value = value;
        }

        public IDisposable SetValue(T newValue)
        {
            GuardHolder<Guard<T>> guard = new GuardHolder<Guard<T>>(this);

            Value = newValue;

            return guard;
        }
    }

    public class OrderedGuard<T>
    {
        public T Value { get; private set; }
        private List<OrderedGuardHolder<OrderedGuard<T>>> guardHolders = new List<OrderedGuardHolder<OrderedGuard<T>>>();

        private sealed class OrderedGuardHolder<TGuardable> : IDisposable where TGuardable : OrderedGuard<T>
        {
            private readonly TGuardable guardable;
            private T originalValue;

            public OrderedGuardHolder(TGuardable guardable)
            {
                this.guardable = guardable;
                originalValue = guardable.Value;
            }

            public void Dispose()
            {
                OrderedGuardHolder<OrderedGuard<T>> item = this as OrderedGuardHolder<OrderedGuard<T>>;
                int count = guardable.guardHolders.Count;
                int itemIndex = guardable.guardHolders.IndexOf(item);
                if(itemIndex <= count - 2)
                {
                    OrderedGuardHolder<OrderedGuard<T>> nextItem = guardable.guardHolders[itemIndex + 1];
                    nextItem.originalValue = item.originalValue;
                }
                else
                {
                    guardable.Value = originalValue;
                }

                guardable.guardHolders.RemoveAt(itemIndex);
            }
        }

        public OrderedGuard(T value)
        {
            Value = value;
        }

        public IDisposable SetValue(T newValue)
        {
            OrderedGuardHolder<OrderedGuard<T>> guard = new OrderedGuardHolder<OrderedGuard<T>>(this);
            guardHolders.Add(guard);
            Value = newValue;

            return guard;
        }
    }

}