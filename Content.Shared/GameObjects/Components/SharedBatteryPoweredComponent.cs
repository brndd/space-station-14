using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Content.Shared.GameObjects.Components.Power;

namespace Content.Shared.GameObjects.Components
{
    public abstract class SharedBatteryPoweredComponent : Component
    {
        public sealed override string Name => "BatteryPowered";
        public sealed override uint? NetID => ContentNetIDs.BATTERY_POWERED;

        protected abstract bool HasBattery { get; }

        public abstract BatterySize BatterySlotSize { get; }

        [Serializable, NetSerializable]
        protected sealed class BatteryPoweredComponentState : ComponentState
        {
            public BatteryPoweredComponentState(float? currentCharge, float? maxCharge, bool hasBattery, BatterySize batterySlotSize) : base(ContentNetIDs.BATTERY_POWERED)
            {
                CurrentCharge = currentCharge;
                MaxCharge = maxCharge;
                HasBattery = hasBattery;
                BatterySlotSize = batterySlotSize;
            }

            public float? CurrentCharge { get; }
            public float? MaxCharge { get; }
            public bool HasBattery { get; }
            public BatterySize BatterySlotSize { get; }
        }
    }
}
