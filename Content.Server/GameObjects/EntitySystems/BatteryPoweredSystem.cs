using Content.Server.GameObjects.Components.Interactable;
using JetBrains.Annotations;
using Robust.Shared.GameObjects.Systems;

namespace Content.Server.GameObjects.EntitySystems
{
    [UsedImplicitly]
    internal sealed class BatteryPoweredSystem : EntitySystem
    {
        public override void Update(float frameTime)
        {
            foreach (var comp in ComponentManager.EntityQuery<BatteryPoweredComponent>())
            {
                comp.OnUpdate(frameTime);
            }
        }
    }
}
