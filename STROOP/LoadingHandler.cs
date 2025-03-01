﻿using STROOP.Forms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace STROOP
{
    class LoadingHandler
        {
            //Delegate for cross thread call to close
            private delegate void CloseDelegate();

            //The type of form to be displayed as the splash screen.
            public static MainLoadingForm LoadingForm;

            static public void ShowLoadingForm()
            {
                // Make sure it is only launched once.
                if (LoadingForm != null)
                    return;

                Thread thread = new Thread(new ThreadStart(LoadingHandler.ShowForm));
                thread.IsBackground = true;
                thread.Start();
            }

            static private void ShowForm()
            {
                LoadingForm = new MainLoadingForm(23); // TODO update max loading
                Application.Run(LoadingForm);
            }

            static public void CloseForm()
            {
                LoadingForm.Invoke(new CloseDelegate(LoadingHandler.CloseFormInternal));
            }

            static private void CloseFormInternal()
            {
                LoadingForm.Close();
            }
        }
}
