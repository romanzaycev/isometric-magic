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
        private Scene _loadingScene;
        private Scene _currentScene;
        private Scene _isWaitingScene;

        public static SceneManager GetInstance()
        {
            return Instance;
        }

        public void SetLoadingScene(Scene scene)
        {
            _loadingScene = scene;
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

        public void FinishAsync()
        {
            if (_currentScene != null && _isWaitingScene != null)
            {
                _currentScene.Unload();
                _currentScene = _isWaitingScene;
            }
        }

        public void LoadByName(string name)
        {
            if (!_scenes.ContainsKey(name))
            {
                throw new ArgumentException($"Scene with name {name} not found");
            }

            _currentScene?.Unload();

            var scene = _scenes[name];

            LoadInternal(scene);
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
            LoadInternal(scene);
        }

        private void LoadInternal(Scene scene)
        {
            if (scene.IsAsyncInitializer)
            {
                _loadingScene.Load();
                _currentScene = _loadingScene;
                _isWaitingScene = scene;

                Application.GetInstance().LoadingCoroutinePush(scene.LoadAsync());
            }
            else
            {
                scene.Load();
                _currentScene = scene;
            }
        }
    }
}