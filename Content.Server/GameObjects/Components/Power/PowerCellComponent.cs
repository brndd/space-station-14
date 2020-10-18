using Content.Shared.GameObjects.Components.Power;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Power
{
    /// <summary>
    ///     Batteries that have update an <see cref="AppearanceComponent"/> based on their charge percent,
    ///     and have a size of slot that they fit into.
    /// </summary>
    [RegisterComponent]
    [ComponentReference(typeof(BatteryComponent))]
    public class PowerCellComponent : BatteryComponent
    {
        public override string Name => "PowerCell";

        [ViewVariables] public PowerCellSize PowerCellSize { get => _powerCellSize; }
        private PowerCellSize _powerCellSize = PowerCellSize.Small;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _powerCellSize, "powerCellSize", PowerCellSize.Small);
        }

        public override void Initialize()
        {
            base.Initialize();
            CurrentCharge = MaxCharge;
            UpdateVisuals();
        }

        protected override void OnChargeChanged()
        {
            base.OnChargeChanged();
            UpdateVisuals();
        }

        private void UpdateVisuals()
        {
            if (Owner.TryGetComponent(out AppearanceComponent appearance))
            {
                appearance.SetData(PowerCellVisuals.ChargeLevel, CurrentCharge / MaxCharge);
            }
        }
    }
}
