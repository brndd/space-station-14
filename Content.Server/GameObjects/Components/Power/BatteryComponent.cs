using System;
using Content.Shared.GameObjects.Components.Power;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Power
{
    /// <summary>
    /// Batteries that hold a certain amount of charge in milliwatt hours
    /// and fit into battery slots of a given size in tools.
    /// </summary>
    [RegisterComponent]
    public class BatteryComponent : Component
    {
        public override string Name => "Battery";

        /// <summary>
        /// This is the maximum charge of the cell in milliwatt hours.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)] public int MaxCharge { get => _maxCharge; set => SetMaxCharge(value); }
        private int _maxCharge;

        /// <summary>
        /// This is the current charge of the cell in milliwatt hours.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public float CurrentCharge { get => _currentCharge; set => SetCurrentCharge(value); }

        private float _currentCharge;

        [ViewVariables] public BatteryState BatteryState { get; private set; }

        [ViewVariables] public BatterySize BatterySize { get => _batterySize; }
        private BatterySize _batterySize = BatterySize.Small;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _maxCharge, "maxCharge", 1000);
            serializer.DataField(ref _currentCharge, "startingCharge", 500);
            serializer.DataField(ref _batterySize, "batterySize", BatterySize.Small);
        }

        public override void Initialize()
        {
            base.Initialize();
            UpdateStorageState();
        }

        /// <summary>
        ///     If sufficient charge is avaiable on the battery, use it. Otherwise, don't.
        /// </summary>
        public bool TryUseCharge(float chargeToUse)
        {
            if (chargeToUse >= CurrentCharge)
            {
                return false;
            }
            else
            {
                CurrentCharge -= chargeToUse;
                return true;
            }
        }

        public float UseCharge(float toDeduct)
        {
            var chargeChangedBy = Math.Min(CurrentCharge, toDeduct);
            CurrentCharge -= chargeChangedBy;
            return chargeChangedBy;
        }

        public bool TryAddCharge(float chargeToAdd)
        {
            if (CurrentCharge + chargeToAdd > MaxCharge)
            {
                return false;
            }
            CurrentCharge += chargeToAdd;
            return true;
        }

        public float AddCharge(float toAdd)
        {
            var newCharge = Math.Min(CurrentCharge + toAdd, MaxCharge);
            var chargeDelta = CurrentCharge - newCharge;
            CurrentCharge = newCharge;
            return chargeDelta;
        }

        private float _wattsOverTimeToMilliWattHours(float watts, float time)
        {
            return watts * 1000 * time / 3600; // TODO: constantitize these numbers
        }

        /// <summary>
        /// Tries to draw <paramref name="watts"/> amount of power from the battery for <paramref name="time"/> seconds.
        /// If there is sufficient charge, it's drawn. Otherwise nothing happens.
        /// </summary>
        /// <param name="watts">Amount of power to draw.</param>
        /// <param name="time">Number of seconds to draw power for.</param>
        /// <returns>true if the battery had enough charge for the operation; false otherwise</returns>
        public bool TryDrawPower(float watts, float time)
        {
            var drawnCharge = _wattsOverTimeToMilliWattHours(watts, time);
            return TryUseCharge(drawnCharge);
        }

        /// <summary>
        /// Uses <paramref name="watts"/> amount of power from the battery for <paramref name="time"/> seconds.
        /// </summary>
        /// <param name="watts">Amount of power to draw.</param>
        /// <param name="time">Number of seconds to draw power for.</param>
        /// <returns>Amount of charge drawn from the battery. May be less than the actual amount of watt hours drawn
        /// if the battery ran out of power because of this draw.</returns>
        public float DrawPower(float watts, float time)
        {
            if (CurrentCharge == 0)
            {
                return 0;
            }
            var drawnCharge = _wattsOverTimeToMilliWattHours(watts, time);
            var newCharge = Math.Min(0, CurrentCharge - drawnCharge);
            var chargeDelta = CurrentCharge - newCharge;
            CurrentCharge = newCharge;
            return chargeDelta;
        }

        /// <summary>
        /// Tries to add <paramref name="watts"/> amount of power to the battery for <paramref name="time"/> seconds,
        /// effectively recharging it with a <paramref name="watts"/> Watt charger.
        /// If the battery has sufficient empty capacity, the capacity is incremented. Otherwise nothing happens.
        /// </summary>
        /// <param name="watts">Amount of power to supply.</param>
        /// <param name="time">Number of seconds to supply power for.</param>
        /// <returns>true if the battery had low enough charge for the operation; false otherwise</returns>
        public bool TryAddPower(float watts, float time)
        {
            var addedCharge = _wattsOverTimeToMilliWattHours(watts, time);
            return TryAddCharge(addedCharge);
        }

        /// <summary>
        /// Adds <paramref name="watts"/> amount of power from the battery for <paramref name="time"/> seconds,
        /// effectively recharging it with a <paramref name="watts"/> Watt charger.
        /// </summary>
        /// <param name="watts">Amount of power to supply.</param>
        /// <param name="time">Number of seconds to supply power for.</param>
        /// <returns>Amount of charge added to the battery. May be less than the actual amount of watt hours supplied
        /// if the battery reached its capacity because of this supply.</returns>
        public float AddPower(float watts, float time)
        {
            if (CurrentCharge == MaxCharge)
            {
                return 0;
            }

            var suppliedCharge = _wattsOverTimeToMilliWattHours(watts, time);
            var newCharge = Math.Min(MaxCharge, CurrentCharge + suppliedCharge);
            var chargeDelta = newCharge - CurrentCharge;
            CurrentCharge = newCharge;
            return chargeDelta;
        }

        public void FillFrom(BatteryComponent battery)
        {
            var powerDeficit = MaxCharge - CurrentCharge;
            if (battery.TryUseCharge(powerDeficit))
            {
                CurrentCharge += powerDeficit;
            }
            else
            {
                CurrentCharge += battery.CurrentCharge;
                battery.CurrentCharge = 0;
            }
        }

        protected virtual void OnChargeChanged() { }

        private void UpdateStorageState()
        {
            if (CurrentCharge == MaxCharge)
            {
                BatteryState = BatteryState.Full;
            }
            else if (CurrentCharge == 0)
            {
                BatteryState = BatteryState.Empty;
            }
            else
            {
                BatteryState = BatteryState.PartlyFull;
            }
        }

        private void SetMaxCharge(int newMax)
        {
            _maxCharge = Math.Max(newMax, 0);
            _currentCharge = Math.Min(_currentCharge, MaxCharge);
            UpdateStorageState();
            OnChargeChanged();
        }

        private void SetCurrentCharge(float newChargeAmount)
        {
            _currentCharge = MathHelper.Clamp(newChargeAmount, 0, MaxCharge);
            UpdateStorageState();
            OnChargeChanged();
        }
    }

    public enum BatteryState
    {
        Full,
        PartlyFull,
        Empty
    }
}
