using System;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Power
{
    [Serializable, NetSerializable]
    public enum PowerCellVisuals
    {
        ChargeLevel
    }

    [Serializable, NetSerializable]
    public enum PowerCellSize
    {
        Small,
        Medium,
        Large
    }
}
