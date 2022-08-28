using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using Windows.ApplicationModel.Activation;
//using Windows.UI.Xaml;
//using Windows.UI.Xaml.Controls;
//using Windows.UI.Xaml.Navigation;

namespace CallStat
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App : Windows.UI.Xaml.Application
    {
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            ConfigureFilters(global::Uno.Extensions.LogExtensionPoint.AmbientLoggerFactory);

            this.InitializeComponent();
            // this.Suspending += OnSuspending;
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
#if DEBUG
			if (System.Diagnostics.Debugger.IsAttached)
			{
				// this.DebugSettings.EnableFrameRateCounter = true;
			}
#endif
            Windows.UI.Xaml.Controls.Frame rootFrame = Windows.UI.Xaml.Window.Current.Content as Windows.UI.Xaml.Controls.Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Windows.UI.Xaml.Controls.Frame();

                rootFrame.NavigationFailed += OnNavigationFailed;

                //if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                //{
                //    //TODO: Load state from previously suspended application
                //}

                // Place the frame in the current Window
                Windows.UI.Xaml.Window.Current.Content = rootFrame;
            }

#if NETFX_CORE
            if (e != null && e.PrelaunchActivated == true) return;
#endif 
            if (rootFrame.Content == null)
            {
                // When the navigation stack isn't restored navigate to the first page,
                // configuring the new page by passing required information as a navigation
                // parameter
                rootFrame.Navigate(typeof(MainPage), e.Arguments);
            }
            // Ensure the current window is active
            Windows.UI.Xaml.Window.Current.Activate();
        }

        /// <summary>
        /// Invoked when Navigation to a certain page fails
        /// </summary>
        /// <param name="sender">The Frame which failed navigation</param>
        /// <param name="e">Details about the navigation failure</param>
        void OnNavigationFailed(object sender, Windows.UI.Xaml.Navigation.NavigationFailedEventArgs e)
        {
            throw new Exception($"Failed to load {e.SourcePageType.FullName}: {e.Exception}");
        }

        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        //private void OnSuspending(object sender, SuspendingEventArgs e)
        //{
        //    var deferral = e.SuspendingOperation.GetDeferral();
        //    //TODO: Save application state and stop any background activity
        //    deferral.Complete();
        //}


        /// <summary>
        /// Configures global logging
        /// </summary>
        /// <param name="factory"></param>
        static void ConfigureFilters(ILoggerFactory factory)
        {
            factory
                .WithFilter(new FilterLoggerSettings
                    {
                        { "Uno", LogLevel.Warning },
                        { "Windows", LogLevel.Warning },

						// Debug JS interop
						// { "Uno.Foundation.WebAssemblyRuntime", LogLevel.Debug },

						// Generic Xaml events
						// { "Windows.UI.Xaml", LogLevel.Debug },
						// { "Windows.UI.Xaml.VisualStateGroup", LogLevel.Debug },
						// { "Windows.UI.Xaml.StateTriggerBase", LogLevel.Debug },
						// { "Windows.UI.Xaml.UIElement", LogLevel.Debug },

						// Layouter specific messages
						// { "Windows.UI.Xaml.Controls", LogLevel.Debug },
						// { "Windows.UI.Xaml.Controls.Layouter", LogLevel.Debug },
						// { "Windows.UI.Xaml.Controls.Panel", LogLevel.Debug },
						// { "Windows.Storage", LogLevel.Debug },

						// Binding related messages
						// { "Windows.UI.Xaml.Data", LogLevel.Debug },

						// DependencyObject memory references tracking
						// { "ReferenceHolder", LogLevel.Debug },
					}
                )
#if DEBUG
				.AddConsole(LogLevel.Debug);
#else
                .AddConsole(LogLevel.Information);
#endif
        }

#if false
        private static async System.Threading.Tasks.Task<bool> AndroidCheckPermission()
        {
#if __ANDROID__
            // below API 16 (JellyBean), permission are granted
            if (Android.OS.Build.VERSION.SdkInt < Android.OS.BuildVersionCodes.JellyBean) return true;

            if (!Windows.Extensions.PermissionsHelper.IsDeclaredInManifest(Android.Manifest.Permission.ReadCallLog))
                return false;
            if (!Windows.Extensions.PermissionsHelper.IsDeclaredInManifest(Android.Manifest.Permission.ReadContacts))
                return false;

            System.Collections.Generic.List<string> requestPermission = new System.Collections.Generic.List<string>();

            // check what permission should be granted
            if (!await Windows.Extensions.PermissionsHelper.CheckPermission(System.Threading.CancellationToken.None, Android.Manifest.Permission.ReadCallLog))
                requestPermission.Add(Android.Manifest.Permission.ReadCallLog);
            if (!await Windows.Extensions.PermissionsHelper.CheckPermission(System.Threading.CancellationToken.None, Android.Manifest.Permission.ReadContacts))
                requestPermission.Add(Android.Manifest.Permission.ReadContacts);

            if (requestPermission.Count < 1) return true;
            return false;

#endif
        }
#endif

        public static DateTime GetLastSaveDate()
        {
            string sDateLast = p.k.GetSettingsString("maxSavedDate", "1970.01.01 00:00:00");
            return DateTime.ParseExact(sDateLast, "yyyy.MM.dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
        }

        public static async System.Threading.Tasks.Task<bool> TimerSaveCallLog()
        {
            DateTime oDateLast = GetLastSaveDate();
            //p.k.CrashMessageAdd("sDateLast=" + sDateLast);
            Windows.Storage.StorageFile oFile = await p.k.GetLogFileMonthlyAsync("calllog", "txt");
            if (oFile is null)
            {
                p.k.CrashMessageAdd("no save file!");
                return false;
            }

            return await SaveCallLog(oFile, oDateLast);
        }

        private static string Time2MinSec(TimeSpan ots)
        {
            return (ots.Hours * 60 + ots.Minutes).ToString() + ":" + ots.Seconds.ToString();
        }

        private static string Call2Text(Windows.ApplicationModel.Calls.PhoneCallHistoryEntry oCall)
        {
            string sDumpCall = oCall.StartTime.ToString("yyyy.MM.dd HH:mm") + "\t" +
                Time2MinSec(oCall.Duration.Value) + "\t";

            if (oCall.IsIncoming)
                sDumpCall += "incoming\t";
            else
                sDumpCall += "outgoing\t";

            sDumpCall += oCall.Address.RawAddress + "\t";

            if (!string.IsNullOrEmpty(oCall.Address.DisplayName))
                sDumpCall += oCall.Address.DisplayName;

            return sDumpCall;
        }

        public static async System.Threading.Tasks.Task<bool> SaveCallLog(Windows.Storage.StorageFile oFile, DateTime oDateSince)
        {

            Windows.ApplicationModel.Calls.PhoneCallHistoryStore oCallHist = null;
            oCallHist = await Windows.ApplicationModel.Calls.PhoneCallHistoryManager.RequestStoreAsync(Windows.ApplicationModel.Calls.PhoneCallHistoryStoreAccessType.AllEntriesLimitedReadWrite);
            if (oCallHist is null) return false;

            var oHistReader = oCallHist.GetEntryReader();
            if (oHistReader is null) return false;

            System.Collections.Generic.IReadOnlyList<Windows.ApplicationModel.Calls.PhoneCallHistoryEntry> oHistData;
            System.Collections.Generic.List<Windows.ApplicationModel.Calls.PhoneCallHistoryEntry> oDataToSave = new System.Collections.Generic.List<Windows.ApplicationModel.Calls.PhoneCallHistoryEntry>();

            int iGuard = 0;

            // UWP jest od najnowszego wgłąb historii, 500 sztuk w paczkach po 20.
            // ale też może być w ogóle bez sortu (np. Android czy coś)
            // nie jest to duży koszt przeglądnąć komplet, więc nie robię STOP przeglądania w trakcie
            while (iGuard < 1000)
            {
                iGuard += 1;

                oHistData = await oHistReader.ReadBatchAsync();
                if (oHistData.Count < 1)
                    break;

                foreach (Windows.ApplicationModel.Calls.PhoneCallHistoryEntry oCall in oHistData)
                {
                    // UWP oraz Android ma "date DESC", więc nie musimy chodzić przez całe
                    if (oCall.StartTime < oDateSince) break;

                    // dla innych platform moze byc potrzebne to:
                    //if (oCall.StartTime > oDateSince)
                    oDataToSave.Add(oCall);
                }
            }

            //p.k.CrashMessageAdd("wyciagnalem wszystkie");

            if (oDataToSave.Count < 1)
            {
                //p.k.CrashMessageAdd("nic nie znalazlem!");
                return true; // jednak nic nowego nie było
            }

            DateTimeOffset oDateLast = DateTime.Now.AddYears(-100);   // to będzie MAX z dat rozmów
            string sFullData = "";

            foreach (Windows.ApplicationModel.Calls.PhoneCallHistoryEntry oCall in 
                from c in oDataToSave orderby c.StartTime select c)
            {
                if (oCall.StartTime > oDateLast)
                    oDateLast = oCall.StartTime;

                sFullData += Call2Text(oCall) + "\n";
            }

            if (sFullData != "")
                await oFile.AppendStringAsync(sFullData);
            else
                p.k.CrashMessageAdd("sFullData empty");

            //p.k.CrashMessageAdd("new last save: " + oDateLast.ToString("yyyy.MM.dd HH:mm:ss"));

            p.k.SetSettingsString("maxSavedDate", oDateLast.ToString("yyyy.MM.dd HH:mm:ss"));
        return true;
        }


        public static async System.Threading.Tasks.Task<string> DumpLastCalls(bool bAll20)
        {
            DateTime oDateLast = GetLastSaveDate();

            Windows.ApplicationModel.Calls.PhoneCallHistoryStore oCallHist = null;
            oCallHist = await Windows.ApplicationModel.Calls.PhoneCallHistoryManager.RequestStoreAsync(Windows.ApplicationModel.Calls.PhoneCallHistoryStoreAccessType.AllEntriesLimitedReadWrite);
            if (oCallHist is null) return "ERROR: oCallHist is null";

            var oHistReader = oCallHist.GetEntryReader();
            if (oHistReader is null) return "ERROR: oHistReader is null";

            System.Collections.Generic.IReadOnlyList<Windows.ApplicationModel.Calls.PhoneCallHistoryEntry> oHistData;
            System.Collections.Generic.List<Windows.ApplicationModel.Calls.PhoneCallHistoryEntry> oDataToSave = new System.Collections.Generic.List<Windows.ApplicationModel.Calls.PhoneCallHistoryEntry>();

            oHistData = await oHistReader.ReadBatchAsync();

            foreach (Windows.ApplicationModel.Calls.PhoneCallHistoryEntry oCall in oHistData)
            {
                if (bAll20 || (oCall.StartTime > oDateLast)) oDataToSave.Add(oCall);
            }

            if (oDataToSave.Count < 1)
            {
                return "Empty result set"; // jednak nic nowego nie było
            }

            string sFullData;
            if (bAll20)
                sFullData = "Last calls, one ReadBatchAsync\n\n";
            else
                sFullData = "Last calls, one ReadBatchAsync, since " + oDateLast.ToString("yyyy.MM.dd HH:mm:ss") + "\n\n";

            foreach (Windows.ApplicationModel.Calls.PhoneCallHistoryEntry oCall in
                from c in oDataToSave orderby c.StartTime select c)
            {
                sFullData += Call2Text(oCall) + "\n";
            }

            return sFullData;
        }



        #region "trigger północny"
        private static Windows.ApplicationModel.Background.BackgroundTaskDeferral moTaskDeferal = null;

        protected override async void OnBackgroundActivated(BackgroundActivatedEventArgs args)
        {
            bool bNoComplete = false;
            moTaskDeferal = args.TaskInstance.GetDeferral();

            if (p.k.IsThisTriggerPolnocny(args))
            {
                //p.k.CrashMessageAdd("OnBackgroundActivated-IsThisTriggerPolnocny");
                await TimerSaveCallLog();
            }
            else
            {
                if (p.k.IsTriggerAppService(args))
                {
                    Windows.ApplicationModel.AppService.AppServiceTriggerDetails oDetails =
                            args.TaskInstance.TriggerDetails as Windows.ApplicationModel.AppService.AppServiceTriggerDetails;

                    if (oDetails != null)
                    {
                        args.TaskInstance.Canceled += OnTaskCanceled;
                        moAppConn = oDetails.AppServiceConnection;
                        moAppConn.RequestReceived += OnRequestReceived;
                        moAppConn.ServiceClosed += OnServiceClosed;
                        bNoComplete = true;
                    }

                }
            }

            if (!bNoComplete)
                moTaskDeferal.Complete();

            }

#if NETFX_CORE
        private static Windows.ApplicationModel.AppService.AppServiceConnection moAppConn;

        private static void OnServiceClosed(Windows.ApplicationModel.AppService.AppServiceConnection appCon, Windows.ApplicationModel.AppService.AppServiceClosedEventArgs args)
        {
            if (appCon != null)
                appCon.Dispose();
        }

        private static void OnTaskCanceled(Windows.ApplicationModel.Background.IBackgroundTaskInstance sender, Windows.ApplicationModel.Background.BackgroundTaskCancellationReason reason)
        {
            if (moTaskDeferal != null)
            {
                moTaskDeferal.Complete();
                moTaskDeferal = null;
            }
            //            'If oAppConn IsNot Nothing Then
            //        '    oAppConn.Dispose()
            //        '    oAppConn = Nothing
            //        'End If
            //
        }

        private async void OnRequestReceived(Windows.ApplicationModel.AppService.AppServiceConnection sender, Windows.ApplicationModel.AppService.AppServiceRequestReceivedEventArgs args)
        {
            // 'Get a deferral so we can use an awaitable API to respond to the message 
            Windows.ApplicationModel.AppService.AppServiceDeferral messageDeferral = args.GetDeferral();
            Windows.Foundation.Collections.ValueSet oInputMsg = args.Request.Message;
            string sStatus = "ERROR while processing command";
            string sResult = "";
            object oCommand;

            if (oInputMsg.TryGetValue("command", out oCommand))
            {
                string sCommand = oCommand as string;
                string sLocalCmds = "save new\t save data now, not waiting for Timer\n" +
                                "show top\t show last calls' Batch (20 calls) \n" +
                                "show new\t show calls newer than last save";

                sResult = p.k.AppServiceStdCmd(sCommand, sLocalCmds);
                if (sResult == "")
                {
                    // komendy własne
                    switch(sCommand.ToLower())
                    {
                        //save new - to samo co w timer, ale bez ruszania timerów : await TimerSaveCallLog
                        case "save new":
                            if (await TimerSaveCallLog())
                                sResult = "New calls saved";
                            else
                                sResult = "Cannot save new calls data";
                            break;

                        //show top -pokazuje ostatnie 20(znaczy jeden Batch)
                        case "show top":
                            sResult = await DumpLastCalls(true);
                            break;

                        //show new - pokazuje to co bylo od ostatniego, bez ruszania znmiennej "co ostatnio" - ale tylko jeden Batch
                        case "show new":
                            sResult = await DumpLastCalls(false);
                            break;

                        //show all -pokazuje pelne dane


                    }

                }
            }

            if (sResult != "") sStatus = "OK";

            Windows.Foundation.Collections.ValueSet oResultMsg = new Windows.Foundation.Collections.ValueSet();
            oResultMsg.Add("status", sStatus);
            oResultMsg.Add("result", sResult);

            await args.Request.SendResponseAsync(oResultMsg);
            
            messageDeferral.Complete();
            moTaskDeferal.Complete();

        }

#endif

        #endregion
    }
}
