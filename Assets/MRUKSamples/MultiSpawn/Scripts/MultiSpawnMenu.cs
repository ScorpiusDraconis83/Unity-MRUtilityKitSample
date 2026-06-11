// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.MRUtilityKit;
using Meta.XR.Samples;
using TMPro;
using UnityEngine;

namespace Meta.XR.MRUtilityKitSamples.FindMultiSpawn
{
    [MetaCodeSample("MRUKSample-FindMultiSpawn")]
    public class MultiSpawnMenu : MonoBehaviour
    {
        public enum GameplayState
        {
            Ready,
            Running,
            Finished
        }

        [SerializeField] private TextMeshProUGUI _remainingTargetsText;
        public GameplayState _gameplayState = GameplayState.Ready;
        public bool isTimerRunning;
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private TextMeshProUGUI _buttonText;
        [SerializeField] private AudioClip _allTargetsHitSound;
        [SerializeField] private FindSpawnPositions[] _findSpawnPositions;
        [SerializeField] private GameObject _introPanel;


        public Camera Camera;
        private int _remainingTargets;
        private float _timer;
        private GunShot _gunShot;
        private AudioSource _guns2DAudioSource;

        public int RemainingTargets
        {
            get => _remainingTargets;
            set
            {
                _remainingTargets = value;
                _remainingTargetsText.text = value.ToString();
            }
        }

        public float Timer
        {
            get => _timer;
            set
            {
                _timer = value;
                timerText.text = value.ToString("F2");
            }
        }

        // Start is called before the first frame update
        private void Start()
        {
            Camera = Camera.main;
            Invoke("InitializeMenu", .5f);
            _gunShot = FindAnyObjectByType<GunShot>();
            _guns2DAudioSource = _gunShot.GetComponent<AudioSource>();
        }

        // Update is called once per frame
        private void Update()
        {
            if (_gameplayState == GameplayState.Running)
            {
                if (_remainingTargets > 0)
                {
                    Timer += Time.deltaTime;
                }
                else
                {
                    _gameplayState = GameplayState.Finished;
                    FinishedGame();
                }
            }
            else
            {
                if (_remainingTargets == 0)
                {
                    Invoke("EnableAllShootingReticles", 2f);
                    _gameplayState = GameplayState.Ready;
                }
            }
        }

        public void StartSpawningTargets()
        {
            if (_remainingTargets != 0)
            {
                return;
            }

            foreach (var spawner in _findSpawnPositions)
            {
                spawner.StartSpawn(MRUK.Instance.GetCurrentRoom());
            }
        }

        //Invoked by the bullet hitting the menu button
        public void ToggleTimer()
        {
            print("Toggle timer");
            switch (_gameplayState)
            {
                case GameplayState.Ready:
                    print("Hit as Ready, setting to Running");
                    _gameplayState = GameplayState.Running;
                    StartTimer();
                    break;
                case GameplayState.Running:
                    print("Hit as Running, setting to Ready");
                    _gameplayState = GameplayState.Ready;
                    ResetTimer();
                    break;
                case GameplayState.Finished:
                    _gameplayState = GameplayState.Ready;
                    ResetTimer();
                    break;
            }
        }

        public void InitializeMenu()
        {
            var targets = FindObjectsByType<Target>(FindObjectsSortMode.None);
            RemainingTargets = targets.Length;
            _buttonText.text = "START";
        }

        public void ResetTimer()
        {
            print("Resetting timer");
            Timer = 0.0f;
            _buttonText.transform.parent.GetComponent<SimpleCurveBasedAnimation>().StartAnimation();
            isTimerRunning = false;
            _buttonText.text = "START";
            EnableAllShootingReticles();
        }

        public void StartTimer()
        {
            Timer = 0.0f;
            _buttonText.transform.parent.GetComponent<SimpleCurveBasedAnimation>().StartAnimation();
            isTimerRunning = true;
            _buttonText.text = "RESET";
            EnableAllShootingReticles();

            // Auto-dismiss the intro panel when the Start button is pressed
            if (_introPanel != null && _introPanel.activeSelf)
            {
                _introPanel.SetActive(false);
            }
        }

        private void FinishedGame()
        {
            _guns2DAudioSource.PlayOneShot(_allTargetsHitSound);
            isTimerRunning = false;
            Invoke("EnableAllShootingReticles", 2f);
            _buttonText.text = "NICE! \n PLAY AGAIN!";
        }

        private void EnableAllShootingReticles()
        {
            var targets = FindObjectsByType<Target>(FindObjectsSortMode.None);
            foreach (var target in targets)
            {
                target.EnableShootingTarget();
                if (target.GetComponentInChildren<SimpleCurveBasedAnimation>())
                {
                    target.GetComponentInChildren<SimpleCurveBasedAnimation>().StartAnimation();
                }
            }

            RemainingTargets = targets.Length;
        }
    }
}
