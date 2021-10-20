using System;
using System.Collections.Generic;
using System.Linq;

namespace IsometricMagic.Engine
{
    public class SceneManager
    {
        private static readonly SceneManager Instance = new();
        private readonly Dictionary<string, Scene> _scenes = new();
        private readonly Scene _defaultScene = new("default");
        private Scene _currentScene;

        public static SceneManager GetInstance()
        {
            return Instance;
        }

        public void Add(Scene scene)
        {
            if (!_scenes.ContainsKey(scene.Name))
            {
                _scenes.Add(scene.Name, scene);
            }
        }

        public Scene GetCurrent()
        {
            if (_currentScene == null)
            {
                Load();
            }

            return _currentScene;
        }

        public void LoadByName(string name)
        {
            if (!_scenes.ContainsKey(name))
            {
                throw new ArgumentException($"Scene with name {name} not found");
            }

            _currentScene?.Unload();

            var scene = _scenes[name];

            scene.Load();
            _currentScene = scene;
        }

        private void Load()
        {
            if (_currentScene != null)
            {
                _currentScene.Unload();
            }
            
            if (_scenes.Count == 0)
            {
                _currentScene = _defaultScene;
                _defaultScene.Load();
                
                return;
            }

            // @TODO: Load with scenes sorting order
            
            var scene = _scenes[_scenes.First().Key];
            
            scene.Load();
            _currentScene = scene;
        }
    }
}