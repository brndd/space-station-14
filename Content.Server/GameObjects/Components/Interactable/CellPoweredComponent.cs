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
    ///     Component that represents a device that has a replaceable power cell and uses it for power.
    /// </summary>
    [RegisterComponent]
    internal sealed class CellPoweredComponent : SharedCellPoweredComponent, IMapInit
    {
        /// <summary>
        ///     This is how many watts of power the device consumes.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)] public float Wattage { get; set; } = 10;

        /// <summary>
        ///     This is how many watts of power the device consumes when powered off.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)] public float WattageStandby { get; set; } = 0;
        [ViewVariables] private ContainerSlot _cellContainer = default!;

        private PowerCellComponent? Cell
        {
            get
            {
                if (_cellContainer.ContainedEntity == null) return null;
                return _cellContainer.ContainedEntity.TryGetComponent(out PowerCellComponent? cell) ? cell : null;
            }
        }

        /// <summary>
        ///     Whether the power cell is actively discharging or not.
        /// </summary>
        [ViewVariables] public bool Discharging { get; private set; }

        [ViewVariables] protected override bool HasCell => Cell != null;
        [ViewVariables] public override PowerCellSize PowerCellSlotSize { get; } = PowerCellSize.Small;

        /// <summary>
        /// Whether the cell in this component can be removed.
        /// </summary>
        [ViewVariables] public bool CanRemoveCell { get; set; }

        public override void Initialize()
        {
            base.Initialize();

            _cellContainer =
                ContainerManagerComponent.Ensure<ContainerSlot>("cellpowered_cell_container", Owner, out _);

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
        /// Causes the power cell to stop actively discharging, ie. turns the device off.
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
        /// Causes the power cell to begin actively discharging, ie. turns the device on.
        /// </summary>
        /// <returns>true, if discharging was started; false otherwise.</returns>
        public bool StartDischarging()
        {
            if (Discharging)
            {
                return true;
            }

            if (Cell == null)
            {
                return false;
            }

            // To prevent having to worry about frame time in here.
            // Let's just say you need a whole second of charge before you can turn it on.
            if (Wattage > Cell.CurrentCharge * 1000)
            {
                return false;
            }

            Discharging = true;
            return true;
        }

        public void OnUpdate(float frameTime)
        {
            if (Cell == null) return;
            var consumedWattage = Discharging ? Wattage : WattageStandby;
            if (!Cell.TryUseCharge(consumedWattage * frameTime)) StopDischarging();
            Dirty();
        }

        public override ComponentState GetComponentState()
        {
            if (Cell == null)
            {
                return new CellPoweredComponentState(null, null, false, PowerCellSlotSize);
            }

            return new CellPoweredComponentState(Cell.CurrentCharge, Cell.MaxCharge, HasCell, PowerCellSlotSize);
        }

        private void EjectCell([CanBeNull] IEntity user, bool force = false)
        {
            if (Cell == null || !CanRemoveCell)
            {
                return;
            }

            if (!force && !CanRemoveCell)
            {
                return;
            }

            if (!_cellContainer.Remove(Cell.Owner))
            {
                return;
            }
            Dirty();

            if (user != null)
            {
                if (user.TryGetComponent(out HandsComponent? hands))
                {
                    if (hands.PutInHand(Cell.Owner.GetComponent<ItemComponent>()))
                    {
                        return;
                    }
                    Cell.Owner.Transform.Coordinates = user.Transform.Coordinates;
                    return;
                }
            }
            Cell.Owner.Transform.Coordinates = Owner.Transform.Coordinates;
        }

        [Verb]
        public sealed class EjectCellVerb : Verb<CellPoweredComponent>
        {
            protected override void GetData(IEntity user, CellPoweredComponent component, VerbData data)
            {
                if (!ActionBlockerSystem.CanInteract(user))
                {
                    data.Visibility = VerbVisibility.Invisible;
                    return;
                }

                if (component.Cell == null)
                {
                    data.Text = Loc.GetString("Eject cell (cell missing)");
                    data.Visibility = VerbVisibility.Disabled;
                }
                else
                {
                    data.Text = Loc.GetString(("Eject cell"));
                }
            }

            protected override void Activate(IEntity user, CellPoweredComponent component)
            {
                component.EjectCell(user);
            }
        }

        void IMapInit.MapInit()
        {
            if (_cellContainer.ContainedEntity != null)
            {
                return;
            }

            string protoName = PowerCellSlotSize switch
            {
                PowerCellSize.Small => "PowerCellSmallStandard",
                PowerCellSize.Medium => "PowerCellMediumStandard",
                PowerCellSize.Large => "PowerCellLargeStandard",
                _ => throw new ArgumentOutOfRangeException()
            };
            var cell = Owner.EntityManager.SpawnEntity(protoName, Owner.Transform.Coordinates);
            _cellContainer.Insert(cell);
        }
    }
}
