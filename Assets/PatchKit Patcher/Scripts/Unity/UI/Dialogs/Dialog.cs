using System.Threading;
using PatchKit.Patcher.Cancellation;
using PatchKit.Patcher.Debug;
using UnityEngine;

namespace PatchKit.Patcher.Unity.UI.Dialogs
{
    public class Dialog<T> : MonoBehaviour where T : Dialog<T>
    {
        private Thread _unityThread;

        public static T Instance { get; private set; }

        private readonly ManualResetEvent _dialogDisplayed = new ManualResetEvent(false);

        private bool _isDisplaying;

        private Animator _animator;

        protected void OnDisplayed()
        {
            _dialogDisplayed.Set();
        }

        protected void Display(CancellationToken cancellationToken)
        {
            Assert.IsFalse(_unityThread == Thread.CurrentThread, 
                "Display dialog can be only used on separate thread.");

            try
            {
                _isDisplaying = true;

                _dialogDisplayed.Reset();
                using (cancellationToken.Register(() => _dialogDisplayed.Set()))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    _dialogDisplayed.WaitOne();
                }
            }
            finally
            {
                _isDisplaying = false;
            }
        }

        protected virtual void Awake()
        {
            _unityThread = Thread.CurrentThread;
            Instance = (T)this;
            _animator = GetComponent<Animator>();
        }

        protected virtual void Update()
        {
            _animator.SetBool("IsOpened", _isDisplaying);
        }
    }
}