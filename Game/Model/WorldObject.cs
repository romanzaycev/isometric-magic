namespace IonMotion.Game.Model
{
    public abstract class WorldObject
    {
        public IsoWorldPosition Position = new(0f, 0f);

        public float X
        {
            get => Position.X;
            set => Position = Position with { X = value };
        }

        public float Y
        {
            get => Position.Y;
            set => Position = Position with { Y = value };
        }
    }
}
