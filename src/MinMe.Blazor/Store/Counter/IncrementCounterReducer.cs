using System;
using global::Blazor.Fluxor;

namespace MinMe.Blazor.Store.Counter
{
    public class IncrementCounterReducer : Reducer<CounterState, IncrementCounterAction>
    {
        public override CounterState Reduce(CounterState state, IncrementCounterAction action)
            => new CounterState(state.CurrentCount + 1);
    }
}
