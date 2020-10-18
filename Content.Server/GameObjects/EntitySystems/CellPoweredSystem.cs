﻿using Content.Server.GameObjects.Components.Interactable;
using JetBrains.Annotations;
using Robust.Shared.GameObjects.Systems;

namespace Content.Server.GameObjects.EntitySystems
{
    [UsedImplicitly]
    internal sealed class CellPoweredSystem : EntitySystem
    {
        public override void Update(float frameTime)
        {
            foreach (var comp in ComponentManager.EntityQuery<CellPoweredComponent>())
            {
                comp.OnUpdate(frameTime);
            }
        }
    }
}
