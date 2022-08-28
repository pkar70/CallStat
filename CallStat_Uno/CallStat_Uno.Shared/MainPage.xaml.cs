/*

 2021.04.30
 * timer dobowy i zapis do DataLogs (WIN ONLY) - koniec prac
 * RemoteSystem (server): dla command line etc.

 2021.04.03
 * migracja Android 11 / Uno 3.6
 * timer dobowy i zapis do DataLogs (WIN ONLY) - początek prac

 2020.06.11
 * MsgBox z detalami statystyki z kontaktem (bo WL pokazywał 184, zamiast 2184 - nie mieściło się :) )

 2020.02.02
 * przeniesienie pod Uno (w tym: zmiana default lang z en-us na en)
 * zmiana ikonki
 * Page_Load zaznacza faktycznie używany zakres (a nie default)
 * wykorzystanie number jesli nie ma nazwy
 * numer wersji na głównym ekranie
 * zmniejszenie odległości między liniami statystyki (nad ListView)
 * "Calls since" - bierze z resw, a nie na sztywno
 * header do ListView
 * zmiana formatu pokazywanej daty (bez sekund, bez timezone; nie regionalne tylko ISO-podobne)
 * sumuje wedle sekund, dopiero przy pokazywaniu robi z tego minuty
 * dane w clipboard: nie total minutes, co daje ulamki idiotyczne, ale mm:ss (hh uwzglednione w mm)
 * jak nie ma nazwy, to pokazuje numer (nie powinno być pustych wpisów)
  
 */


using System.Threading.Tasks;
using Windows.UI.Xaml;
using System.Windows.Input;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.IO;
using System.Diagnostics;
using System.Linq;
using System.Collections.ObjectModel;
//using System.Collections.Generic;
using System.Collections;
using System;
using System.Xml.Linq;
using System.ComponentModel;
using Windows.UI.Xaml.Controls;
using System.Collections.Generic;

//#if __ANDROID__
//using CallLog = BeforeUno;
//#else
using Windows.ApplicationModel.Calls;
//#endif
/* 
  ANDROID

    https://developer.android.com/reference/android/Manifest.permission.html#READ_CALL_LOG


*/
namespace CallStat
{

    public class GrupaRozmow
    {
        public string Nazwa { get; set; }
        public int iMissed { get; set; } = 0;
        public int iCountOut { get; set; } = 0;
        public int iCountIn { get; set; } = 0;
        public int iMinOut { get; set; } = 0;
        public int iMinIn { get; set; } = 0;
        public double dMinOut { get; set; } = 0;
        public double dMinIn { get; set; } = 0;
    }


    public sealed partial class MainPage : Windows.UI.Xaml.Controls.Page
    {
        private System.Collections.Generic.List<GrupaRozmow> mlDane;
        private int miSort = 2;
        private bool mbSortDesc = true;
        private int miRange = 0;
        private int _iCallsNumIn, _iCallsNumOut, _iCallsTimeIn, _iCallsTimeOut;

        public MainPage()
        {
            this.InitializeComponent();
        }

#if __ANDROID__


    private async Task<PhoneCallHistoryStore> AndroidAskPermission()
        {
            var _histStore = new PhoneCallHistoryStore();

            // below API 16 (JellyBean), permission are granted
            if (Android.OS.Build.VERSION.SdkInt < Android.OS.BuildVersionCodes.JellyBean)
            {
                return _histStore;
            }

            // since API 29, we should do something more:
            // https://developer.android.com/reference/android/content/pm/PackageInstaller.SessionParams.html#setWhitelistedRestrictedPermissions(java.util.Set%3Cjava.lang.String%3E)

            // do we have declared this permission in Manifest?
            Android.Content.Context context = Android.App.Application.Context;
            Android.Content.PM.PackageInfo packageInfo =
                context.PackageManager.GetPackageInfo(context.PackageName, Android.Content.PM.PackageInfoFlags.Permissions);
            var requestedPermissions = packageInfo?.RequestedPermissions;
            if (requestedPermissions is null)
                return null;

            if (!Windows.Extensions.PermissionsHelper.IsDeclaredInManifest(Android.Manifest.Permission.ReadCallLog))
                return null;

            // required for contact name
            if (!Windows.Extensions.PermissionsHelper.IsDeclaredInManifest(Android.Manifest.Permission.ReadContacts))
                return null;

            List<string> requestPermission = new List<string>();

            // check what permission should be granted
            if (!await Windows.Extensions.PermissionsHelper.CheckPermission(System.Threading.CancellationToken.None, Android.Manifest.Permission.ReadCallLog))
            {
                requestPermission.Add(Android.Manifest.Permission.ReadCallLog);
            }

            if (!await Windows.Extensions.PermissionsHelper.CheckPermission(System.Threading.CancellationToken.None, Android.Manifest.Permission.ReadContacts))
            {
                requestPermission.Add(Android.Manifest.Permission.ReadContacts);
            }

            if (requestPermission.Count < 1)
                return _histStore;

            // system dialog asking for permission

            var tcs = new TaskCompletionSource<Uno.UI.BaseActivity.RequestPermissionsResultWithResultsEventArgs>();

            void handler(object sender, Uno.UI.BaseActivity.RequestPermissionsResultWithResultsEventArgs e)
            {

                if (e.RequestCode == 1)
                {
                    tcs.TrySetResult(e);
                }
            }

            var current = Uno.UI.BaseActivity.Current;

            try
            {
                current.RequestPermissionsResultWithResults += handler;

                // Android.Support.V4.App.ActivityCompat.RequestPermissions(Uno.UI.BaseActivity.Current,
                // powyzsze: Uno.945, ponizej: Uno.3.1.163
                AndroidX.Core.App.ActivityCompat.RequestPermissions(Uno.UI.BaseActivity.Current,
                    requestPermission.ToArray(), 1);

                var result = await tcs.Task;
                if (result.GrantResults.Length < 1)
                    return null;

                foreach(var oItem in result.GrantResults)
                {
                    if (oItem == Android.Content.PM.Permission.Denied)
                        return null;
                }

                return _histStore;

            }
            finally
            {
                current.RequestPermissionsResultWithResults -= handler;
            }


            return null;

        }

#endif

        private async Task ReadData()
        {
            mlDane = new System.Collections.Generic.List<GrupaRozmow>();

            PhoneCallHistoryStore oCallHist = null;
            String sError = "";


            try
            {
#if __ANDROID__
                oCallHist = await AndroidAskPermission();
#else
                oCallHist = await PhoneCallHistoryManager.RequestStoreAsync(Windows.ApplicationModel.Calls.PhoneCallHistoryStoreAccessType.AllEntriesLimitedReadWrite);
#endif
            }
            catch (Exception ex)
            {
                sError = ex.Message;
            }



            if (!string.IsNullOrEmpty(sError))
            {
                await p.k.DialogBoxResAsync("errGetHist", sError);
                return;
            }

            if (oCallHist == null)
            {
                await p.k.DialogBoxResAsync("errCallHistNothing", null);
                return;
            }

            var oHistReader = oCallHist.GetEntryReader();
            // string sFirstDate = "";
            string sFirstUsedDate = "";
            int iCallsMissed = 0;
            int iCallsNumOut = 0;
            int iCallsNumIn = 0;
            double dCallsTimeOut = 0;
            double dCallsTimeIn = 0;
            //string sFullData = "";
            System.Collections.Generic.IReadOnlyList<PhoneCallHistoryEntry> oHistData;

            // 20180721, all/month/day stat (optymalizacja)
            int iDzisDay = DateTime.Now.Day;
            int iDzisMon = DateTime.Now.Month;
            DateTimeOffset oDzis30 = DateTime.Now.AddDays(-30);
            int iZakres = p.k.GetSettingsInt("RangeMode");

            int iGuard = 0;
            bool exitWhile = false;

            while (iGuard < 1000 && !exitWhile)
            {
                iGuard += 1;

                oHistData = await oHistReader.ReadBatchAsync();
                if (oHistData.Count() < 1)
                    break;

                foreach (PhoneCallHistoryEntry oCall in oHistData)
                {

                    // 20180721, all/month/day stat
                    switch (iZakres)
                    {
                        case 1:  // miesiac
                            if (oCall.StartTime.Month != iDzisMon)
                                exitWhile = true;
                            break;
                        case 2:  // dzien
                            if (oCall.StartTime.Day != iDzisDay)
                                exitWhile = true;
                            break;
                        case 3:
                            if (oCall.StartTime < oDzis30)
                                exitWhile = true;
                            break;
                    }
                    if (exitWhile) break; // z foreach

                    sFirstUsedDate = oCall.StartTime.ToString("yyyy.MM.dd H:mm");   // bo zaczyna od najmlodszego, czyli najstarsze bedzie ostatnie

                    string interlokutor = oCall.Address.DisplayName;
                    if (string.IsNullOrEmpty(interlokutor)) // gdy nie ma nazwy, to spróbuj numer
                        interlokutor = oCall.Address.RawAddress;

                    GrupaRozmow oItem = mlDane.Find(x => x.Nazwa.Equals(interlokutor));
                    if (oItem == null)
                    {
                        // If Not mlDane.Exists(Function(x) x.Nazwa.Equals(oCall.Address.DisplayName)) Then
                        oItem = new GrupaRozmow();
                        oItem.Nazwa = interlokutor;
                        mlDane.Add(oItem);
                    }

                    // 20180601: Crash [timespan_getvalue ze niby w PageLoaded, ale to moze byc tylko tu]
                    // "Or" na "OrElse", bez siegania do Duration gdy IsMissed
                    // oraz dodany warunek Is Nothing 
                    if (oCall.IsMissed || oCall.Duration == null || oCall.Duration.HasValue == false || oCall.Duration.Value.TotalSeconds == 0)
                    {
                        oItem.iMissed += 1;
                        iCallsMissed += 1;
                    }
                    else
                    {
                        if (oCall.IsIncoming)
                        {
                            oItem.iCountIn += 1;
                            oItem.dMinIn += oCall.Duration.Value.TotalMinutes;
                            oItem.iMinIn = (int)oItem.dMinIn;    // w zaokrągleniu

                            iCallsNumIn += 1;
                            dCallsTimeIn += oCall.Duration.Value.TotalMinutes;
                        }
                        else
                        {
                            oItem.iCountOut += 1;
                            oItem.dMinOut += oCall.Duration.Value.TotalMinutes;
                            oItem.iMinOut = (int)oItem.dMinOut;    // w zaokrągleniu
                            iCallsNumOut += 1;
                            dCallsTimeOut += oCall.Duration.Value.TotalMinutes;
                        };
                        // 20180907: przeniosłem z po If do wnętrza If - wszak Duration może nie być.
                        //sFullData = sFullData + interlokutor + "\t" +
                        //                oCall.IsIncoming + "\t" +
                        //                oCall.StartTime.ToString("yyyy.MM.dd H: mm") + "\t" +
                        //                Time2MinSec(oCall.Duration.Value) + "\n";

                    }
                }
                if (exitWhile) break; // exit z while
            }

            //p.k.ClipPut(sFullData);
            //var oClipCont = new DataPackage();
            //oClipCont.RequestedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Copy;
            //oClipCont.SetText(sFullData);
            //Clipboard.SetContent(oClipCont);


            _iCallsNumIn = iCallsNumIn;
            _iCallsNumOut = iCallsNumOut;
            _iCallsTimeIn = (int)dCallsTimeIn;
            _iCallsTimeOut = (int)dCallsTimeOut;

            Stat2Page(sFirstUsedDate, iCallsNumOut, iCallsNumIn, (int)dCallsTimeOut, (int)dCallsTimeIn);
            return;
        }

        private void Stat2Page(string sFirstDate, int iCallsNumOut, int iCallsNumIn, int iCallsTimeOut, int iCallsTimeIn)
        {
            uiFirstUsedDate.Text = p.k.GetLangString("uiFirstUsedDate") + ": " + sFirstDate;

            uiCallsNumOut.Text = p.k.GetLangString("uiTxtOutCnt") + ": " + iCallsNumOut;
            uiCallsNumIn.Text = p.k.GetLangString("uiTxtInCnt") + ": " + iCallsNumIn;
            if (iCallsNumOut > 0)
                uiCallsTimeOut.Text = p.k.GetLangString("uiTxtOutMin") + ": " + iCallsTimeOut + p.k.GetLangString("uiTxtMinAvg") + (int)(iCallsTimeOut / iCallsNumOut);

            else
                uiCallsTimeOut.Text = p.k.GetLangString("uiTxtOutMin") + ": " + iCallsTimeOut + p.k.GetLangString("uiTxtMinAvg") + "0";

            if (iCallsNumIn > 0)
                uiCallsTimeIn.Text = p.k.GetLangString("uiTxtInMin") + ": " + iCallsTimeIn + p.k.GetLangString("uiTxtMinAvg") + (int)(iCallsTimeIn / iCallsNumIn);
            else
                uiCallsTimeIn.Text = p.k.GetLangString("uiTxtInMin") + ": " + iCallsTimeIn + p.k.GetLangString("uiTxtMinAvg") + "0";
        }

        private void ShowPersons()
        {
            if (mlDane is null) return; // tak jest gdy ustawiamy znaczniki przed wczytaniem :)

            switch (miSort)
            {
                case 1:
                    {
                        if (mbSortDesc)
                            ListItemsSklepu.ItemsSource = from c in mlDane orderby c.iCountIn + c.iCountOut descending select c;
                        else
                            ListItemsSklepu.ItemsSource = from c in mlDane orderby c.iCountIn + c.iCountOut select c;
                        break;
                    }

                case 2:
                    {
                        if (mbSortDesc)
                            ListItemsSklepu.ItemsSource = from c in mlDane orderby c.iMinIn + c.iMinOut descending select c;
                        else
                            ListItemsSklepu.ItemsSource = from c in mlDane orderby c.iMinIn + c.iMinOut select c;
                        break;
                    }

                case 3:
                    {
                        if (mbSortDesc)
                            ListItemsSklepu.ItemsSource = from c in mlDane
                                                               orderby c.iCountIn descending
                                                               select c;
                        else
                            ListItemsSklepu.ItemsSource = from c in mlDane
                                                               orderby c.iCountIn
                                                               select c;
                        break;
                    }

                case 4:
                    {
                        if (mbSortDesc)
                            ListItemsSklepu.ItemsSource = from c in mlDane
                                                               orderby c.iMinIn descending
                                                               select c;
                        else
                            ListItemsSklepu.ItemsSource = from c in mlDane
                                                               orderby c.iMinIn
                                                               select c;
                        break;
                    }

                case 5:
                    {
                        if (mbSortDesc)
                            ListItemsSklepu.ItemsSource = from c in mlDane
                                                               orderby c.iCountOut descending
                                                               select c;
                        else
                            ListItemsSklepu.ItemsSource = from c in mlDane
                                                               orderby c.iCountOut
                                                               select c;
                        break;
                    }

                case 6:
                    {
                        if (mbSortDesc)
                            ListItemsSklepu.ItemsSource = from c in mlDane
                                                               orderby c.iMinOut descending
                                                               select c;
                        else
                            ListItemsSklepu.ItemsSource = from c in mlDane
                                                               orderby c.iMinOut
                                                               select c;
                        break;
                    }
            }
        }

        private async void uiPage_Loaded(object sender, RoutedEventArgs e)
        {
            //p.k.ClipPut("test");
            //await Windows.Storage.FileIO.WriteTextAsync(null, "alamakota");

            uiVersion.Text = p.k.GetAppVers();
            p.k.ProgRingInit(true, false);

            // p.k.GetSettingsBool(uiAutoSave, "dailyLog");
            uiAutoSave.IsChecked = p.k.IsTriggersRegistered();
            uiResetMarks(p.k.GetSettingsInt("SortMode", 2), p.k.GetSettingsBool("SortDesc", true));
            uiResetZakres(p.k.GetSettingsInt("RangeMode"), false);  // w tym jest reload 

            if (!await AskPermissions()) return;
            await ReadData(); // wraz z global stats

            ShowPersons();
        }

        private string GetPersonDetailString(string sResHdr, int iValThis, int iValTotal)
        {
            int iPct = (int)(100.0 * iValThis / iValTotal);
            string sTxt = p.k.GetLangString(sResHdr) + ": " + iValThis + " (" + iPct + " %)\n";
            return sTxt;
        }
        private string GetPersonDetailStringAvg(int iCntThis, int iCntTotal, int iTimeThis, int iTimeTotal)
        {
            if (iCntThis==0) return "";

            double dAvgTo = iTimeThis / iCntThis;
            double dAvgOthers = (iTimeTotal - iTimeThis) / (iCntTotal - iCntThis);
            string sTxt = string.Format(p.k.GetLangString("msgAverageCall"), (int)dAvgTo, (int)dAvgOthers) + "\n";
            return sTxt;
        }

        private void uiShowPerson_Tapped(object sender, RoutedEventArgs e)
        {
            Grid oGrid = sender as Grid;
            GrupaRozmow oItem = oGrid.DataContext as GrupaRozmow;

            string sTxt = oItem.Nazwa + "\n\n\n";
            sTxt = sTxt + GetPersonDetailString("uiTxtInCnt", oItem.iCountIn, _iCallsNumIn);
            sTxt = sTxt + GetPersonDetailString("uiTxtInMin", oItem.iMinIn, _iCallsTimeIn);

            sTxt = sTxt + GetPersonDetailStringAvg(oItem.iCountIn, _iCallsNumIn, oItem.iMinIn, _iCallsTimeIn);

            sTxt += "\n";   // jako separator pomiedzy in a out
            sTxt = sTxt + GetPersonDetailString("uiTxtOutCnt", oItem.iCountOut, _iCallsNumOut);
            sTxt = sTxt + GetPersonDetailString("uiTxtOutMin", oItem.iMinOut, _iCallsTimeOut);

            sTxt = sTxt + GetPersonDetailStringAvg(oItem.iCountOut, _iCallsNumOut, oItem.iMinOut, _iCallsTimeOut);

            p.k.DialogBox(sTxt);

        }


#region "Przelaczniki UI"


        private void uiResetMarks(int iMode, bool bDesc)
        {
            uiSortAllCnt.IsChecked = iMode == 1 ? true : false;
            uiSortAllMin.IsChecked = iMode == 2 ? true : false;
            uiSortInCnt.IsChecked = iMode == 3 ? true : false;
            uiSortInMin.IsChecked = iMode == 4 ? true : false;
            uiSortOutCnt.IsChecked = iMode == 5 ? true : false;
            uiSortOutMin.IsChecked = iMode == 6 ? true : false;
            uiSortDesc.IsChecked = bDesc ? true : false;

            miSort = iMode;
            mbSortDesc = bDesc;
            p.k.SetSettingsInt("SortMode", miSort);
            p.k.SetSettingsBool("SortDesc", mbSortDesc);
            ShowPersons(); // pokaż stat
        }
        private void uiResetZakres(int iMode, bool bReread)
        {
            uiZakresAll.IsChecked = iMode == 0 ? true : false;
            uiZakresMonth.IsChecked = iMode == 1 ? true : false;
            uiZakresDay.IsChecked = iMode == 2 ? true : false;
            uiZakres30d.IsChecked = iMode == 3 ? true : false;
            miRange = iMode;
            p.k.SetSettingsInt("RangeMode", miRange);
            if(bReread)
                uiPage_Loaded(null, null); // odczytaj historie, pokaż stat
        }
        private void uiSortDesc_Click(object sender, RoutedEventArgs e)
        {
            uiResetMarks(miSort, !mbSortDesc);
        }

        private void uiSortOutMin_Click(object sender, RoutedEventArgs e)
        {
            uiResetMarks(6, mbSortDesc);
        }

        private void uiSortOutCnt_Click(object sender, RoutedEventArgs e)
        {
            uiResetMarks(5, mbSortDesc);
        }

        private void uiSortInMin_Click(object sender, RoutedEventArgs e)
        {
            uiResetMarks(4, mbSortDesc);
        }

        private void uiSortInCnt_Click(object sender, RoutedEventArgs e)
        {
            uiResetMarks(3, mbSortDesc);
        }

        private void uiSortAllMin_Click(object sender, RoutedEventArgs e)
        {
            uiResetMarks(2, mbSortDesc);
        }

        private void uiSortAllCnt_Click(object sender, RoutedEventArgs e)
        {
            uiResetMarks(1, mbSortDesc);
        }

        private void uiZakresAll_Click(object sender, RoutedEventArgs e)
        {
            uiResetZakres(0,true);
        }

        private void uiZakresMonth_Click(object sender, RoutedEventArgs e)
        {
            uiResetZakres(1, true);
        }

        private void uiZakresDay_Click(object sender, RoutedEventArgs e)
        {
            uiResetZakres(2, true);
        }

        private void uiZakres30d_Click(object sender, RoutedEventArgs e)
        {
            uiResetZakres(3, true);
        }

        private void uiPrivacy_Click(object sender, RoutedEventArgs e)
        {
            p.k.DialogBoxRes("msgPrivacy");
        }
        #endregion

        private async Task<bool> AskPermissions()
        {
            PhoneCallHistoryStore oCallHist = await PhoneCallHistoryManager.RequestStoreAsync(Windows.ApplicationModel.Calls.PhoneCallHistoryStoreAccessType.AllEntriesLimitedReadWrite);
            if (oCallHist is null) return false;
            return true;
        }

        private async void uiAutoSave_Click(object sender, RoutedEventArgs e)
        {
            if (!p.k.GetPlatform("uwp")) return;
            if (uiAutoSave.IsChecked is null) return;   // nie wiem kiedy, ale ponoc moze (jest to nullable bool)

            if (uiAutoSave.IsChecked.Value )   
            {
                if(!p.k.IsTriggersRegistered())
                {
                    if(! await p.k.CanRegisterTriggersAsync())
                    {
                        p.k.DialogBoxRes("msgNoBackgroundPermission");
                        uiAutoSave.IsChecked = false;
                        return;
                    }

                    await AskPermissions();     // bo musi zapytac przeciez z UI thread, nie dopiero o północy
                    await p.k.DodajTriggerPolnocny();

                    Windows.Storage.StorageFolder oLogFolder = await p.k.GetLogFolderRootAsync(true);
                    if(! await oLogFolder.FileExistsAsync("callog.initial.txt"))
                    {
                        if (await p.k.DialogBoxResYNAsync("askCreateInitFile"))
                            await CreateInitFile();
                    }
                }
            }
            else
            {
                p.k.UnregisterTriggers();
            }

        }

        private async Task<bool> CreateInitFile()
        {
            Windows.Storage.StorageFolder oLogFolder = await p.k.GetLogFolderRootAsync(true);
            if (oLogFolder is null) return false;
            Windows.Storage.StorageFile oFile = await oLogFolder.CreateFileAsync("callog.initial.txt", Windows.Storage.CreationCollisionOption.GenerateUniqueName);
            if (oFile is null) return false;

            p.k.ProgRingShow(true);
            bool bRet = await App.SaveCallLog(oFile, DateTime.Now.AddYears(-100));
            p.k.ProgRingShow(false);
            return bRet;
        }

    }
}
