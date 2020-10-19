#nullable enable
using System;
using Content.Server.GameObjects.Components.GUI;
using Content.Server.GameObjects.Components.Items.Storage;
using Content.Server.GameObjects.Components.Power;
using Content.Shared.GameObjects.Components;
using Content.Shared.GameObjects.Components.Power;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.GameObjects.Verbs;
using JetBrains.Annotations;
using Robust.Server.GameObjects.Components.Container;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Interactable
{
    /// <summary>
    ///     Component that represents a device that has a replaceable battery and uses it for power.
    /// </summary>
    [RegisterComponent]
    internal sealed class BatteryPoweredComponent : SharedBatteryPoweredComponent, IMapInit
    {
        /// <summary>
        ///     This is how many watts of power the device consumes.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)] public float Wattage { get; set; } = 10;

        /// <summary>
        ///     This is how many watts of power the device consumes when powered off.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)] public float WattageStandby { get; set; } = 0;
        [ViewVariables] private ContainerSlot _batteryContainer = default!;

        private BatteryComponent? Battery
        {
            get
            {
                if (_batteryContainer.ContainedEntity == null) return null;
                return _batteryContainer.ContainedEntity.TryGetComponent(out BatteryComponent? battery) ? battery : null;
            }
        }

        /// <summary>
        ///     Whether the battery is actively discharging or not.
        /// </summary>
        [ViewVariables] public bool Discharging { get; private set; }

        [ViewVariables] protected override bool HasBattery => Battery != null;
        [ViewVariables] public override BatterySize BatterySlotSize { get; } = BatterySize.Small;

        /// <summary>
        /// Whether the battery in this component can be removed.
        /// </summary>
        [ViewVariables] public bool CanRemoveBattery { get; set; }

        public override void Initialize()
        {
            base.Initialize();

            _batteryContainer =
                ContainerManagerComponent.Ensure<ContainerSlot>("batterypowered_battery_container", Owner, out _);

            Dirty();
        }

        public bool ToggleDischarging()
        {
            if (Discharging)
            {
                return StopDischarging();
            }
            else
            {
                return StartDischarging();
            }
        }

        /// <summary>
        /// Causes the battery to stop actively discharging, ie. turns the device off.
        /// </summary>
        /// <returns>true, if discharging was stopped; false otherwise.</returns>
        public bool StopDischarging()
        {
            if (Discharging)
            {
                Discharging = false;
            }
            return true;
        }

        /// <summary>
        /// Causes the battery to begin actively discharging, ie. turns the device on.
        /// </summary>
        /// <returns>true, if discharging was started; false otherwise.</returns>
        public bool StartDischarging()
        {
            if (Discharging)
            {
                return true;
            }

            if (Battery == null)
            {
                return false;
            }

            // To prevent having to worry about frame time in here.
            // Let's just say you need a whole second of charge before you can turn it on.
            if (Wattage > Battery.CurrentCharge * 1000)
            {
                return false;
            }

            Discharging = true;
            return true;
        }

        public void OnUpdate(float frameTime)
        {
            if (Battery == null) return;

            var consumedWattage = Discharging ? Wattage : WattageStandby;

            if (consumedWattage == 0)
            {
                return;
            }
            if (!Battery.TryDrawPower(consumedWattage, frameTime)) StopDischarging();
            Dirty();
        }

        public override ComponentState GetComponentState()
        {
            if (Battery == null)
            {
                return new BatteryPoweredComponentState(null, null, false, BatterySlotSize);
            }

            return new BatteryPoweredComponentState(Battery.CurrentCharge, Battery.MaxCharge, HasBattery, BatterySlotSize);
        }

        private void EjectBattery([CanBeNull] IEntity user, bool force = false)
        {
            if (Battery == null || !CanRemoveBattery)
            {
                return;
            }

            if (!force && !CanRemoveBattery)
            {
                return;
            }

            if (!_batteryContainer.Remove(Battery.Owner))
            {
                return;
            }
            Dirty();

            if (user != null)
            {
                if (user.TryGetComponent(out HandsComponent? hands))
                {
                    if (hands.PutInHand(Battery.Owner.GetComponent<ItemComponent>()))
                    {
                        return;
                    }
                    Battery.Owner.Transform.Coordinates = user.Transform.Coordinates;
                    return;
                }
            }
            Battery.Owner.Transform.Coordinates = Owner.Transform.Coordinates;
        }

        [Verb]
        public sealed class EjectBatteryVerb : Verb<BatteryPoweredComponent>
        {
            protected override void GetData(IEntity user, BatteryPoweredComponent component, VerbData data)
            {
                if (!ActionBlockerSystem.CanInteract(user))
                {
                    data.Visibility = VerbVisibility.Invisible;
                    return;
                }

                if (component.Battery == null)
                {
                    data.Text = Loc.GetString("Eject battery (battery missing)");
                    data.Visibility = VerbVisibility.Disabled;
                }
                else
                {
                    data.Text = Loc.GetString(("Eject battery"));
                }
            }

            protected override void Activate(IEntity user, BatteryPoweredComponent component)
            {
                component.EjectBattery(user);
            }
        }

        void IMapInit.MapInit()
        {
            if (_batteryContainer.ContainedEntity != null)
            {
                return;
            }

            string protoName = BatterySlotSize switch
            {
                BatterySize.Small => "PowerCellSmallStandard",
                BatterySize.Medium => "PowerCellMediumStandard",
                BatterySize.Large => "PowerCellLargeStandard",
                _ => throw new ArgumentOutOfRangeException()
            };
            var battery = Owner.EntityManager.SpawnEntity(protoName, Owner.Transform.Coordinates);
            _batteryContainer.Insert(battery);
        }
    }
}
