using System.Collections.Generic;
using IsometricMagic.Game.Animation;
using IsometricMagic.Game.Model;

namespace IsometricMagic.Game.Character
{
    public enum CharacterState
    {
        Idle,
        Running,
        Dying,
    }
    
    public class Character : WorldObject
    {
        protected readonly Dictionary<string, Sequence> Animations = new();
        public Sequence CurrentSequence => Animations[_currentAnimation];

        private WorldDirection _direction = WorldDirection.N;
        public WorldDirection Direction
        {
            get => _direction;
            
            set {
                if (value != _direction)
                {
                    _state = State;
                    _direction = value;
                }
            }
        }

        private string _currentAnimation = "idle_0";
        private CharacterState _state = CharacterState.Idle; 
        public CharacterState State
        {
            get => _state;

            set
            {
                var nextAnimation = value switch
                {
                    CharacterState.Idle => "idle_" + (int) _direction,
                    CharacterState.Running => "running_" + (int) _direction,
                    CharacterState.Dying => "dying_" + (int) _direction,
                    _ => _currentAnimation
                };

                if (nextAnimation != _currentAnimation)
                {
                    CurrentSequence.Stop();
                    _currentAnimation = nextAnimation;
                    CurrentSequence.Play();
                }

                _state = value;
            }
        }
    }
}