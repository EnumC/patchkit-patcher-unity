﻿using PatchKit.Unity.Utilities;
using UnityEngine.UI;

namespace PatchKit.Unity.Patcher.UI.Dialogs
{
    public class ErrorDialog : Dialog<ErrorDialog>
    {
        public Text ErrorText;

        public void Confirm()
        {
            OnDisplayed();
        }

        public void Display(PatcherError error)
        {
            Dispatcher.Invoke(() => UpdateMessage(error)).WaitOne();

            Display();
        }

        private void UpdateMessage(PatcherError error)
        {
            switch (error)
            {
                case PatcherError.NoInternetConnection:
                    ErrorText.text = "Please check your internet connection.";
                    break;
                case PatcherError.NoPermissions:
                    ErrorText.text = "Please check write permissions in application directory.";
                    break;
                case PatcherError.Other:
                    ErrorText.text = "An error has occured.";
                    break;
            }
        }
    }
}