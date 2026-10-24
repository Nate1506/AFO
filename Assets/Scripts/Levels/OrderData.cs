using System;
using UnityEngine;

namespace SayItAgain
{
    public enum OrderSlot { Route = 0, Avoid = 1, Gather = 2, Code = 3 }

    /// <summary>
    /// The pool an HQ order is drawn from. One option per slot is picked each round.
    /// Option keys point into the localization table.
    /// </summary>
    [CreateAssetMenu(menuName = "Say It Again/Order Data", fileName = "OrderData")]
    public sealed class OrderData : ScriptableObject
    {
        [Tooltip("Safe causeway to the high hill.")]
        public string[] routeKeys = { "opt_route_east", "opt_route_west", "opt_route_north" };
        [Tooltip("River crossing out of the village that will collapse.")]
        public string[] avoidKeys = { "opt_avoid_bridge", "opt_avoid_school", "opt_avoid_market" };
        [Tooltip("Where the villagers are waiting.")]
        public string[] gatherKeys = { "opt_gather_market", "opt_gather_school", "opt_gather_mill" };
        [Tooltip("Code word that proves a message really came from HQ.")]
        public string[] codeKeys = { "code_tiger", "code_mango", "code_lotus", "code_kite" };

        public string[] Options(OrderSlot slot) => slot switch
        {
            OrderSlot.Route => routeKeys,
            OrderSlot.Avoid => avoidKeys,
            OrderSlot.Gather => gatherKeys,
            OrderSlot.Code => codeKeys,
            _ => Array.Empty<string>(),
        };

        public static readonly OrderSlot[] Slots = { OrderSlot.Route, OrderSlot.Avoid, OrderSlot.Gather, OrderSlot.Code };

        public static string SlotNameKey(OrderSlot slot) => "slot_" + slot.ToString().ToLowerInvariant();
    }
}
