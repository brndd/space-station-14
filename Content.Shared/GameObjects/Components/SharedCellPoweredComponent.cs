using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Content.Shared.GameObjects.Components.Power;

namespace Content.Shared.GameObjects.Components
{
    public abstract class SharedCellPoweredComponent : Component
    {
        public sealed override string Name => "BatteryPowered";
        public sealed override uint? NetID => ContentNetIDs.BATTERY_POWERED;

        protected abstract bool HasCell { get; }

        public abstract PowerCellSize PowerCellSlotSize { get; }

        [Serializable, NetSerializable]
        protected sealed class CellPoweredComponentState : ComponentState
        {
            public CellPoweredComponentState(float? currentCharge, float? maxCharge, bool hasCell, PowerCellSize powerCellSlotSize) : base(ContentNetIDs.BATTERY_POWERED)
            {
                CurrentCharge = currentCharge;
                MaxCharge = maxCharge;
                HasCell = hasCell;
                PowerCellSlotSize = powerCellSlotSize;
            }

            public float? CurrentCharge { get; }
            public float? MaxCharge { get; }
            public bool HasCell { get; }
            public PowerCellSize PowerCellSlotSize { get; }
        }
    }
}
