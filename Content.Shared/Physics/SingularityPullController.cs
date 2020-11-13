﻿#nullable enable
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Maths;
using Robust.Shared.Physics;

namespace Content.Server.GameObjects.Components.Singularity
{
    public class SingularityPullController : VirtualController
    {
        public override IPhysicsComponent? ControlledComponent { protected get; set; }

        public void Pull(Vector2 velocityDirection, float speed)
        {
            Impulse = velocityDirection * speed;
        }
    }
}
