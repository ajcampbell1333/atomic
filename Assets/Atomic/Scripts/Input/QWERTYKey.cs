using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Atomic.Input
{
    public class QWERTYKey : MonoBehaviour
    {
        public bool highlightUnitTest1;

        public UnityAction<string> OnActivate;
        [SerializeField] private string _lowerKey;
        [SerializeField] private string _upperKey;
        [SerializeField] private Material _neutralMat, _clickedMat;

        private Image _backgroundImage;
        private Text _display;
        private Button _button;
        private string _currentKey;
        public string currentKey
        {
            get {
                return _currentKey;
            }
        }

        private void Awake()
        {
            _currentKey = _lowerKey;
            _backgroundImage = GetComponent<Image>();
            _button = GetComponent<Button>();
            _display = GetComponent<Text>();
        }

        //private void Update()
        //{
        //    if (highlightUnitTest1)
        //    {
        //        highlightUnitTest1 = false;
        //        StartCoroutine(ToggleKeyHighlight());
        //    }
        //}

        public void ToggleKeyCaps(bool on)
        {
            _currentKey = (on) ? _upperKey : _lowerKey;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.tag == "Activator_Right" || other.tag == "Activator_Left")
            {
                OnActivate?.Invoke(_currentKey);
                _button.onClick.Invoke();
                AtomicAudioManager.Instance.Play("SelectionClick");
                StartCoroutine(ToggleKeyHighlight());
            }
        }

        private IEnumerator ToggleKeyHighlight()
        {
            _backgroundImage.material = _clickedMat;
            yield return new WaitForSeconds(0.1f);
            _backgroundImage.material = _neutralMat;
        }
    }
}
