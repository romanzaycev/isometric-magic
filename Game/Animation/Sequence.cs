using System.Collections.Generic;
using IsometricMagic.Engine;

namespace IsometricMagic.Game.Animation
{
    public class Sequence
    {
        private readonly string _name;
        public string Name => _name;
        
        private readonly IReadOnlyList<Sprite> _sprites;
        public IReadOnlyList<Sprite> Sprites => _sprites;
        private bool _isPlaying;
        private float _lastChangeTime;
        private float _time;
        
        public float FrameDelay = 0.1f;
        
        private int _currentFrameIndex = 0;
        public int CurrentFrameIndex => _currentFrameIndex;
        
        public Sprite CurrentSprite => _sprites[CurrentFrameIndex];

        public Sequence(string name, IReadOnlyList<Sprite> sprites)
        {
            _name = name;
            _sprites = sprites;

            foreach (var sprite in _sprites)
            {
                sprite.Visible = false;
            }
        }

        public void Update(float deltaTime)
        {
            if (!_isPlaying) return;
            
            _time += deltaTime;

            if (_lastChangeTime == 0)
            {
                _lastChangeTime = _time;
                UpdateVisibility();
            }
            else
            {
                if (_time - _lastChangeTime >= FrameDelay)
                {
                    if (_currentFrameIndex + 1 == _sprites.Count)
                    {
                        _currentFrameIndex = 0;
                    }
                    else
                    {
                        _currentFrameIndex++;
                    }
                        
                    _lastChangeTime = _time;
                        
                    UpdateVisibility();
                }
            }
        }

        public void Play()
        {
            _isPlaying = true;
            _currentFrameIndex = 0;
            _time = 0;
            _lastChangeTime = 0;
        }
        
        public void Stop()
        {
            _isPlaying = false;
            _sprites[_currentFrameIndex].Visible = false;
        }
        
        private void UpdateVisibility()
        {
            for (var i = 0; i < _sprites.Count; i++)
            {
                _sprites[i].Visible = i == _currentFrameIndex;
            }
        }
    }
}