namespace IsometricMagic.Engine.Tweening
{
    public readonly struct TweenHandle
    {
        internal static readonly TweenHandle Invalid = new(null, 0);

        private readonly TweenManager? _manager;
        private readonly int _id;

        internal TweenHandle(TweenManager? manager, int id)
        {
            _manager = manager;
            _id = id;
        }

        public bool IsValid => _manager != null && _id != 0 && _manager.IsActive(_id);

        public void Cancel()
        {
            _manager?.Cancel(_id);
        }
    }
}
