using Primary.Components;

namespace Game.Player
{
    [Component]
    public sealed class PlayerController : EntityBehaviour
    {
        private float _walkingMoveSpeed;

        public PlayerController()
        {
            _walkingMoveSpeed = 2.0f;
        }

        public float WalkingMoveSpeed { get => _walkingMoveSpeed; set => _walkingMoveSpeed = value; }
    }
}