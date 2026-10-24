using System;
using Unity.Netcode;

namespace SayItAgain
{
    /// <summary>
    /// "What I understood": one option index per slot, -1 = unknown. Also used for the true HQ order.
    /// </summary>
    [Serializable]
    public struct RelayCard : INetworkSerializeByMemcpy, IEquatable<RelayCard>
    {
        public sbyte route, avoid, gather, code;
        public bool submitted;

        public static RelayCard Empty => new RelayCard { route = -1, avoid = -1, gather = -1, code = -1 };

        public int this[OrderSlot slot]
        {
            get => slot switch
            {
                OrderSlot.Route => route,
                OrderSlot.Avoid => avoid,
                OrderSlot.Gather => gather,
                _ => code,
            };
            set
            {
                var v = (sbyte)value;
                switch (slot)
                {
                    case OrderSlot.Route: route = v; break;
                    case OrderSlot.Avoid: avoid = v; break;
                    case OrderSlot.Gather: gather = v; break;
                    default: code = v; break;
                }
            }
        }

        public int Matches(RelayCard truth)
        {
            int n = 0;
            foreach (var s in OrderData.Slots) if (this[s] == truth[s]) n++;
            return n;
        }

        public bool Equals(RelayCard o) => route == o.route && avoid == o.avoid && gather == o.gather && code == o.code && submitted == o.submitted;
        public override bool Equals(object obj) => obj is RelayCard o && Equals(o);
        public override int GetHashCode() => HashCode.Combine(route, avoid, gather, code, submitted);
    }
}
