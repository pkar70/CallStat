// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

using System.Threading.Tasks;
using Windows.UI.Xaml;
using System.Windows.Input;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.IO;
using System.Diagnostics;
using Microsoft.VisualBasic;
using System.Linq;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Collections;
using System;
using System.Xml.Linq;
using System.ComponentModel;
using Windows.UI.Xaml.Controls;
using global::Windows.ApplicationModel.Calls;
using global::Windows.ApplicationModel.DataTransfer;

namespace CallStat
{
    /// <summary>

/// An empty page that can be used on its own or navigated to within a Frame.

/// </summary>

    public class GrupaRozmow
    {
        public string Nazwa { get; set; }
        public int iMissed { get; set; } = 0;
        public int iCountOut { get; set; } = 0;
        public int iCountIn { get; set; } = 0;
        public int iMinOut { get; set; } = 0;
        public int iMinIn { get; set; } = 0;
    }


    public sealed partial class MainPage : Page
    {
        private List mlDane;
        private global::System.Int32 miSort = 2;
        private global::System.Boolean mbSortDesc = true;
        private global::System.Int32 miRange = 0;



        private async Task ReadData()
        {
            mlDane = new List<GrupaRozmow>();

            PhoneCallHistoryStore oCallHist = null;
            global::System.String sError = "";
            try
            {
                oCallHist = await PhoneCallHistoryManager.RequestStoreAsync(Windows.ApplicationModel.Calls.PhoneCallHistoryStoreAccessType.AllEntriesLimitedReadWrite);
            }
            catch (Exception ex)
            {
                sError = ex.Message;
            }

            if (!string.IsNullOrEmpty(sError))
            {
                CallStat.App.DialogBoxRes("errGetHist", sError);
                return null;
            }

            if (oCallHist == null)
            {
                CallStat.App.DialogBoxRes("errCallHistNothing", null);
                return null;
            }

            var oHistReader = oCallHist.GetEntryReader();
            global::System.String sFirstDate = "";
            global::System.Int32 iCallsMissed = 0;
            global::System.Int32 iCallsNumOut = 0;
            global::System.Int32 iCallsNumIn = 0;
            global::System.Int32 iCallsTimeOut = 0;
            global::System.Int32 iCallsTimeIn = 0;
            global::System.String sFullData = "";
            IReadOnlyList oHistData;

            // 20180721, all/month/day stat (optymalizacja)
            global::System.Int32 iDzisDay = DateTime.Now.Day;
            global::System.Int32 iDzisMon = DateTime.Now.Month;
            DateTimeOffset oDzis30 = DateTime.Now.AddDays(-30);
            global::System.Int32 iZakres = CallStat.App.GetSettingsInt("RangeMode");

            global::System.Int32 iGuard = 0;

            while (iGuard < 1000)
            {
                iGuard += 1;

                oHistData = await oHistReader.ReadBatchAsync();
                if (oHistData.Count < 1)
                    break;

                foreach (PhoneCallHistoryEntry oCall in oHistData)
                {

                    // 20180721, all/month/day stat
                    switch (iZakres)
                    {
                        case 1  // miesiac
                       :
                            {
                                if (oCall.StartTime.Month != iDzisMon)
                                    break;
                                break;
                            }

                        case 2  // dzien
                 :
                            {
                                if (oCall.StartTime.Day != iDzisDay)
                                    break;
                                break;
                            }

                        case 3:
                            {
                                if (oCall.StartTime < oDzis30)
                                    break;
                                break;
                            }
                    }

                    sFirstDate = oCall.StartTime.ToString;   // bo zaczyna od najmlodszego, czyli najstarsze bedzie ostatnie

                    GrupaRozmow oItem = mlDane.Find(x => x.Nazwa.Equals(oCall.Address.DisplayName));
                    if (oItem == null)
                    {
                        // If Not mlDane.Exists(Function(x) x.Nazwa.Equals(oCall.Address.DisplayName)) Then
                        oItem = new GrupaRozmow();
                        oItem.Nazwa = oCall.Address.DisplayName;
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
                            oItem.iMinIn += oCall.Duration.Value.TotalMinutes;

                            iCallsNumIn += 1;
                            iCallsTimeIn += oCall.Duration.Value.TotalMinutes;
                        }
                        else
                        {
                            oItem.iCountOut += 1;
                            oItem.iMinOut += oCall.Duration.Value.TotalMinutes;

                            iCallsNumOut += 1;
                            iCallsTimeOut += oCall.Duration.Value.TotalMinutes;
                        };
#error Cannot convert AssignmentStatementSyntax - see comment for details
                        /* Cannot convert AssignmentStatementSyntax, System.NullReferenceException: Object reference not set to an instance of an object.
                           at ICSharpCode.CodeConverter.CSharp.ExpressionNodeVisitor.<VisitBinaryExpression>d__63.MoveNext()
                        --- End of stack trace from previous location where exception was thrown ---
                           at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
                           at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
                           at ICSharpCode.CodeConverter.CSharp.CommentConvertingVisitorWrapper`1.<Visit>d__5.MoveNext()
                        --- End of stack trace from previous location where exception was thrown ---
                           at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
                           at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
                           at ICSharpCode.CodeConverter.CSharp.SyntaxNodeVisitorExtensions.<AcceptAsync>d__0`1.MoveNext()
                        --- End of stack trace from previous location where exception was thrown ---
                           at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
                           at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
                           at System.Runtime.CompilerServices.TaskAwaiter.ValidateEnd(Task task)
                           at ICSharpCode.CodeConverter.CSharp.ExpressionNodeVisitor.<VisitBinaryExpression>d__63.MoveNext()
                        --- End of stack trace from previous location where exception was thrown ---
                           at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
                           at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
                           at ICSharpCode.CodeConverter.CSharp.CommentConvertingVisitorWrapper`1.<Visit>d__5.MoveNext()
                        --- End of stack trace from previous location where exception was thrown ---
                           at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
                           at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
                           at ICSharpCode.CodeConverter.CSharp.SyntaxNodeVisitorExtensions.<AcceptAsync>d__0`1.MoveNext()
                        --- End of stack trace from previous location where exception was thrown ---
                           at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
                           at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
                           at System.Runtime.CompilerServices.TaskAwaiter.ValidateEnd(Task task)
                           at ICSharpCode.CodeConverter.CSharp.ExpressionNodeVisitor.<VisitBinaryExpression>d__63.MoveNext()
                        --- End of stack trace from previous location where exception was thrown ---
                           at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
                           at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
                           at ICSharpCode.CodeConverter.CSharp.CommentConvertingVisitorWrapper`1.<Visit>d__5.MoveNext()
                        --- End of stack trace from previous location where exception was thrown ---
                           at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
                           at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
                           at ICSharpCode.CodeConverter.CSharp.SyntaxNodeVisitorExtensions.<AcceptAsync>d__0`1.MoveNext()
                        --- End of stack trace from previous location where exception was thrown ---
                           at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
                           at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
                           at System.Runtime.CompilerServices.TaskAwaiter.ValidateEnd(Task task)
                           at ICSharpCode.CodeConverter.CSharp.ExpressionNodeVisitor.<VisitBinaryExpression>d__63.MoveNext()
                        --- End of stack trace from previous location where exception was thrown ---
                           at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
                           at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
                           at ICSharpCode.CodeConverter.CSharp.CommentConvertingVisitorWrapper`1.<Visit>d__5.MoveNext()
                        --- End of stack trace from previous location where exception was thrown ---
                           at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
                           at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
                           at ICSharpCode.CodeConverter.CSharp.SyntaxNodeVisitorExtensions.<AcceptAsync>d__0`1.MoveNext()
                        --- End of stack trace from previous location where exception was thrown ---
                           at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
                           at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
                           at System.Runtime.CompilerServices.TaskAwaiter.ValidateEnd(Task task)
                           at ICSharpCode.CodeConverter.CSharp.ExpressionNodeVisitor.<VisitBinaryExpression>d__63.MoveNext()
                        --- End of stack trace from previous location where exception was thrown ---
                           at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
                           at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
                           at ICSharpCode.CodeConverter.CSharp.CommentConvertingVisitorWrapper`1.<Visit>d__5.MoveNext()
                        --- End of stack trace from previous location where exception was thrown ---
                           at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
                           at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
                           at ICSharpCode.CodeConverter.CSharp.SyntaxNodeVisitorExtensions.<AcceptAsync>d__0`1.MoveNext()
                        --- End of stack trace from previous location where exception was thrown ---
                           at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
                           at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
                           at System.Runtime.CompilerServices.TaskAwaiter.ValidateEnd(Task task)
                           at ICSharpCode.CodeConverter.CSharp.ExpressionNodeVisitor.<VisitBinaryExpression>d__63.MoveNext()
                        --- End of stack trace from previous location where exception was thrown ---
                           at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
                           at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
                           at ICSharpCode.CodeConverter.CSharp.CommentConvertingVisitorWrapper`1.<Visit>d__5.MoveNext()
                        --- End of stack trace from previous location where exception was thrown ---
                           at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
                           at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
                           at ICSharpCode.CodeConverter.CSharp.SyntaxNodeVisitorExtensions.<AcceptAsync>d__0`1.MoveNext()
                        --- End of stack trace from previous location where exception was thrown ---
                           at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
                           at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
                           at ICSharpCode.CodeConverter.CSharp.MethodBodyExecutableStatementVisitor.<VisitAssignmentStatement>d__32.MoveNext()
                        --- End of stack trace from previous location where exception was thrown ---
                           at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
                           at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
                           at ICSharpCode.CodeConverter.CSharp.ByRefParameterVisitor.<CreateLocals>d__7.MoveNext()
                        --- End of stack trace from previous location where exception was thrown ---
                           at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
                           at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
                           at ICSharpCode.CodeConverter.CSharp.ByRefParameterVisitor.<AddLocalVariables>d__6.MoveNext()
                        --- End of stack trace from previous location where exception was thrown ---
                           at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
                           at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
                           at ICSharpCode.CodeConverter.CSharp.CommentConvertingMethodBodyVisitor.<ConvertWithTrivia>d__4.MoveNext()
                        --- End of stack trace from previous location where exception was thrown ---
                           at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
                           at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
                           at ICSharpCode.CodeConverter.CSharp.CommentConvertingMethodBodyVisitor.<DefaultVisit>d__3.MoveNext()

                        Input:

                                            ' 20180907: przeniosłem z po If do wnętrza If - wszak Duration może nie być.
                                            sFullData = sFullData & oCall.Address.DisplayName & vbTab &
                                                            oCall.IsIncoming & vbTab &
                                                            oCall.StartTime.ToString & vbTab &
                                                            oCall.Duration.Value.TotalMinutes & vbCrLf

                         */
                    }
                }
            }

            var oClipCont = new DataPackage();
            oClipCont.RequestedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Copy;
            oClipCont.SetText(sFullData);
            Clipboard.SetContent(oClipCont);

            Stat2Page(sFirstDate, iCallsNumOut, iCallsNumIn, iCallsTimeOut, iCallsTimeIn);
            return default(Task);
        }

        private void Stat2Page(string sFirstDate, int iCallsNumOut, int iCallsNumIn, int iCallsTimeOut, int iCallsTimeIn)
        {
            this.uiFirstDate.Text = "First date: " + sFirstDate;

            this.uiCallsNumOut.Text = CallStat.App.GetLangString("uiTxtOutCnt") + ": " + iCallsNumOut;
            this.uiCallsNumIn.Text = CallStat.App.GetLangString("uiTxtInCnt") + ": " + iCallsNumIn;
            if (iCallsNumOut > 0)
                ;
#error Cannot convert AssignmentStatementSyntax - see comment for details
                /* Cannot convert AssignmentStatementSyntax, System.ArgumentNullException: Value cannot be null.
                Parameter name: destination
                   at Microsoft.CodeAnalysis.VisualBasic.VisualBasicCompilation.ClassifyConversion(ITypeSymbol source, ITypeSymbol destination)
                   at ICSharpCode.CodeConverter.CSharp.TypeConversionAnalyzer.TryAnalyzeCsConversion(ExpressionSyntax vbNode, ITypeSymbol csType, ITypeSymbol csConvertedType, Conversion vbConversion, ITypeSymbol vbConvertedType, ITypeSymbol vbType, VisualBasicCompilation vbCompilation, Boolean isConst, TypeConversionKind& typeConversionKind)
                   at ICSharpCode.CodeConverter.CSharp.TypeConversionAnalyzer.AnalyzeConversion(ExpressionSyntax vbNode, Boolean alwaysExplicit, Boolean isConst, ITypeSymbol forceTargetType)
                   at ICSharpCode.CodeConverter.CSharp.TypeConversionAnalyzer.AddExplicitConversion(ExpressionSyntax vbNode, ExpressionSyntax csNode, Boolean addParenthesisIfNeeded, Boolean defaultToCast, Boolean isConst, ITypeSymbol forceTargetType)
                   at ICSharpCode.CodeConverter.CSharp.ExpressionNodeVisitor.<VisitBinaryExpression>d__63.MoveNext()
                --- End of stack trace from previous location where exception was thrown ---
                   at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
                   at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
                   at ICSharpCode.CodeConverter.CSharp.CommentConvertingVisitorWrapper`1.<Visit>d__5.MoveNext()
                --- End of stack trace from previous location where exception was thrown ---
                   at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
                   at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
                   at ICSharpCode.CodeConverter.CSharp.SyntaxNodeVisitorExtensions.<AcceptAsync>d__0`1.MoveNext()
                --- End of stack trace from previous location where exception was thrown ---
                   at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
                   at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
                   at ICSharpCode.CodeConverter.CSharp.ExpressionNodeVisitor.<WithRemovedRedundantConversionOrNull>d__67.MoveNext()
                --- End of stack trace from previous location where exception was thrown ---
                   at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
                   at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
                   at ICSharpCode.CodeConverter.CSharp.ExpressionNodeVisitor.<VisitPredefinedCastExpression>d__32.MoveNext()
                --- End of stack trace from previous location where exception was thrown ---
                   at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
                   at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
                   at ICSharpCode.CodeConverter.CSharp.CommentConvertingVisitorWrapper`1.<Visit>d__5.MoveNext()
                --- End of stack trace from previous location where exception was thrown ---
                   at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
                   at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
                   at ICSharpCode.CodeConverter.CSharp.SyntaxNodeVisitorExtensions.<AcceptAsync>d__0`1.MoveNext()
                --- End of stack trace from previous location where exception was thrown ---
                   at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
                   at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
                   at System.Runtime.CompilerServices.TaskAwaiter.ValidateEnd(Task task)
                   at ICSharpCode.CodeConverter.CSharp.ExpressionNodeVisitor.<VisitBinaryExpression>d__63.MoveNext()
                --- End of stack trace from previous location where exception was thrown ---
                   at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
                   at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
                   at ICSharpCode.CodeConverter.CSharp.CommentConvertingVisitorWrapper`1.<Visit>d__5.MoveNext()
                --- End of stack trace from previous location where exception was thrown ---
                   at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
                   at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
                   at ICSharpCode.CodeConverter.CSharp.SyntaxNodeVisitorExtensions.<AcceptAsync>d__0`1.MoveNext()
                --- End of stack trace from previous location where exception was thrown ---
                   at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
                   at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
                   at ICSharpCode.CodeConverter.CSharp.MethodBodyExecutableStatementVisitor.<VisitAssignmentStatement>d__32.MoveNext()
                --- End of stack trace from previous location where exception was thrown ---
                   at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
                   at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
                   at ICSharpCode.CodeConverter.CSharp.ByRefParameterVisitor.<CreateLocals>d__7.MoveNext()
                --- End of stack trace from previous location where exception was thrown ---
                   at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
                   at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
                   at ICSharpCode.CodeConverter.CSharp.ByRefParameterVisitor.<AddLocalVariables>d__6.MoveNext()
                --- End of stack trace from previous location where exception was thrown ---
                   at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
                   at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
                   at ICSharpCode.CodeConverter.CSharp.CommentConvertingMethodBodyVisitor.<ConvertWithTrivia>d__4.MoveNext()
                --- End of stack trace from previous location where exception was thrown ---
                   at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
                   at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
                   at ICSharpCode.CodeConverter.CSharp.CommentConvertingMethodBodyVisitor.<DefaultVisit>d__3.MoveNext()

                Input:
                            Me.uiCallsTimeOut.Text = Global.CallStat.App.GetLangString("uiTxtOutMin") & ": " & iCallsTimeOut & Global.CallStat.App.GetLangString("uiTxtMinAvg") & CInt(iCallsTimeOut / iCallsNumOut)

                 */
            else
                this.uiCallsTimeOut.Text = CallStat.App.GetLangString("uiTxtOutMin") + ": " + iCallsTimeOut + CallStat.App.GetLangString("uiTxtMinAvg") + "0";
            if (iCallsNumIn > 0)
                ;
#error Cannot convert AssignmentStatementSyntax - see comment for details
                /* Cannot convert AssignmentStatementSyntax, System.ArgumentNullException: Value cannot be null.
                Parameter name: destination
                   at Microsoft.CodeAnalysis.VisualBasic.VisualBasicCompilation.ClassifyConversion(ITypeSymbol source, ITypeSymbol destination)
                   at ICSharpCode.CodeConverter.CSharp.TypeConversionAnalyzer.TryAnalyzeCsConversion(ExpressionSyntax vbNode, ITypeSymbol csType, ITypeSymbol csConvertedType, Conversion vbConversion, ITypeSymbol vbConvertedType, ITypeSymbol vbType, VisualBasicCompilation vbCompilation, Boolean isConst, TypeConversionKind& typeConversionKind)
                   at ICSharpCode.CodeConverter.CSharp.TypeConversionAnalyzer.AnalyzeConversion(ExpressionSyntax vbNode, Boolean alwaysExplicit, Boolean isConst, ITypeSymbol forceTargetType)
                   at ICSharpCode.CodeConverter.CSharp.TypeConversionAnalyzer.AddExplicitConversion(ExpressionSyntax vbNode, ExpressionSyntax csNode, Boolean addParenthesisIfNeeded, Boolean defaultToCast, Boolean isConst, ITypeSymbol forceTargetType)
                   at ICSharpCode.CodeConverter.CSharp.ExpressionNodeVisitor.<VisitBinaryExpression>d__63.MoveNext()
                --- End of stack trace from previous location where exception was thrown ---
                   at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
                   at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
                   at ICSharpCode.CodeConverter.CSharp.CommentConvertingVisitorWrapper`1.<Visit>d__5.MoveNext()
                --- End of stack trace from previous location where exception was thrown ---
                   at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
                   at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
                   at ICSharpCode.CodeConverter.CSharp.SyntaxNodeVisitorExtensions.<AcceptAsync>d__0`1.MoveNext()
                --- End of stack trace from previous location where exception was thrown ---
                   at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
                   at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
                   at ICSharpCode.CodeConverter.CSharp.ExpressionNodeVisitor.<WithRemovedRedundantConversionOrNull>d__67.MoveNext()
                --- End of stack trace from previous location where exception was thrown ---
                   at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
                   at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
                   at ICSharpCode.CodeConverter.CSharp.ExpressionNodeVisitor.<VisitPredefinedCastExpression>d__32.MoveNext()
                --- End of stack trace from previous location where exception was thrown ---
                   at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
                   at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
                   at ICSharpCode.CodeConverter.CSharp.CommentConvertingVisitorWrapper`1.<Visit>d__5.MoveNext()
                --- End of stack trace from previous location where exception was thrown ---
                   at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
                   at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
                   at ICSharpCode.CodeConverter.CSharp.SyntaxNodeVisitorExtensions.<AcceptAsync>d__0`1.MoveNext()
                --- End of stack trace from previous location where exception was thrown ---
                   at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
                   at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
                   at System.Runtime.CompilerServices.TaskAwaiter.ValidateEnd(Task task)
                   at ICSharpCode.CodeConverter.CSharp.ExpressionNodeVisitor.<VisitBinaryExpression>d__63.MoveNext()
                --- End of stack trace from previous location where exception was thrown ---
                   at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
                   at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
                   at ICSharpCode.CodeConverter.CSharp.CommentConvertingVisitorWrapper`1.<Visit>d__5.MoveNext()
                --- End of stack trace from previous location where exception was thrown ---
                   at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
                   at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
                   at ICSharpCode.CodeConverter.CSharp.SyntaxNodeVisitorExtensions.<AcceptAsync>d__0`1.MoveNext()
                --- End of stack trace from previous location where exception was thrown ---
                   at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
                   at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
                   at ICSharpCode.CodeConverter.CSharp.MethodBodyExecutableStatementVisitor.<VisitAssignmentStatement>d__32.MoveNext()
                --- End of stack trace from previous location where exception was thrown ---
                   at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
                   at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
                   at ICSharpCode.CodeConverter.CSharp.ByRefParameterVisitor.<CreateLocals>d__7.MoveNext()
                --- End of stack trace from previous location where exception was thrown ---
                   at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
                   at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
                   at ICSharpCode.CodeConverter.CSharp.ByRefParameterVisitor.<AddLocalVariables>d__6.MoveNext()
                --- End of stack trace from previous location where exception was thrown ---
                   at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
                   at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
                   at ICSharpCode.CodeConverter.CSharp.CommentConvertingMethodBodyVisitor.<ConvertWithTrivia>d__4.MoveNext()
                --- End of stack trace from previous location where exception was thrown ---
                   at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
                   at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
                   at ICSharpCode.CodeConverter.CSharp.CommentConvertingMethodBodyVisitor.<DefaultVisit>d__3.MoveNext()

                Input:
                            Me.uiCallsTimeIn.Text = Global.CallStat.App.GetLangString("uiTxtInMin") & ": " & iCallsTimeIn & Global.CallStat.App.GetLangString("uiTxtMinAvg") & CInt(iCallsTimeIn / iCallsNumIn)

                 */
            else
                this.uiCallsTimeIn.Text = CallStat.App.GetLangString("uiTxtInMin") + ": " + iCallsTimeIn + CallStat.App.GetLangString("uiTxtMinAvg") + "0";
        }
        private void ShowPersons()
        {
            switch (miSort)
            {
                case 1:
                    {
                        if (mbSortDesc)
                            ;
#error Cannot convert AssignmentStatementSyntax - see comment for details
                            /* Cannot convert AssignmentStatementSyntax, System.ArgumentNullException: Value cannot be null.
                            Parameter name: destination
                               at Microsoft.CodeAnalysis.VisualBasic.VisualBasicCompilation.ClassifyConversion(ITypeSymbol source, ITypeSymbol destination)
                               at ICSharpCode.CodeConverter.CSharp.TypeConversionAnalyzer.TryAnalyzeCsConversion(ExpressionSyntax vbNode, ITypeSymbol csType, ITypeSymbol csConvertedType, Conversion vbConversion, ITypeSymbol vbConvertedType, ITypeSymbol vbType, VisualBasicCompilation vbCompilation, Boolean isConst, TypeConversionKind& typeConversionKind)
                               at ICSharpCode.CodeConverter.CSharp.TypeConversionAnalyzer.AnalyzeConversion(ExpressionSyntax vbNode, Boolean alwaysExplicit, Boolean isConst, ITypeSymbol forceTargetType)
                               at ICSharpCode.CodeConverter.CSharp.TypeConversionAnalyzer.AddExplicitConversion(ExpressionSyntax vbNode, ExpressionSyntax csNode, Boolean addParenthesisIfNeeded, Boolean defaultToCast, Boolean isConst, ITypeSymbol forceTargetType)
                               at ICSharpCode.CodeConverter.CSharp.ExpressionNodeVisitor.<VisitBinaryExpression>d__63.MoveNext()
                            --- End of stack trace from previous location where exception was thrown ---
                               at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
                               at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
                               at ICSharpCode.CodeConverter.CSharp.CommentConvertingVisitorWrapper`1.<Visit>d__5.MoveNext()
                            --- End of stack trace from previous location where exception was thrown ---
                               at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
                               at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
                               at ICSharpCode.CodeConverter.CSharp.SyntaxNodeVisitorExtensions.<AcceptAsync>d__0`1.MoveNext()
                            --- End of stack trace from previous location where exception was thrown ---
                               at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
                               at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
                               at ICSharpCode.CodeConverter.CSharp.ExpressionNodeVisitor.<VisitOrdering>d__56.MoveNext()
                            --- End of stack trace from previous location where exception was thrown ---
                               at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
                               at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
                               at ICSharpCode.CodeConverter.CSharp.CommentConvertingVisitorWrapper`1.<Visit>d__5.MoveNext()
                            --- End of stack trace from previous location where exception was thrown ---
                               at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
                               at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
                               at ICSharpCode.CodeConverter.CSharp.SyntaxNodeVisitorExtensions.<AcceptAsync>d__0`1.MoveNext()
                            --- End of stack trace from previous location where exception was thrown ---
                               at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
                               at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
                               at ICSharpCode.CodeConverter.CSharp.QueryConverter.<<ConvertOrderByClause>b__27_0>d.MoveNext()
                            --- End of stack trace from previous location where exception was thrown ---
                               at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
                               at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
                               at ICSharpCode.CodeConverter.Shared.AsyncEnumerableTaskExtensions.<SelectAsync>d__4`2.MoveNext()
                            --- End of stack trace from previous location where exception was thrown ---
                               at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
                               at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
                               at ICSharpCode.CodeConverter.CSharp.QueryConverter.<ConvertOrderByClause>d__27.MoveNext()
                            --- End of stack trace from previous location where exception was thrown ---
                               at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
                               at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
                               at ICSharpCode.CodeConverter.CSharp.QueryConverter.<ConvertQueryBodyClause>d__21.MoveNext()
                            --- End of stack trace from previous location where exception was thrown ---
                               at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
                               at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
                               at ICSharpCode.CodeConverter.CSharp.QueryConverter.<GetQuerySegments>d__6.MoveNext()
                            --- End of stack trace from previous location where exception was thrown ---
                               at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
                               at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
                               at ICSharpCode.CodeConverter.CSharp.QueryConverter.<ConvertClauses>d__5.MoveNext()
                            --- End of stack trace from previous location where exception was thrown ---
                               at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
                               at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
                               at ICSharpCode.CodeConverter.CSharp.ExpressionNodeVisitor.<VisitQueryExpression>d__55.MoveNext()
                            --- End of stack trace from previous location where exception was thrown ---
                               at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
                               at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
                               at ICSharpCode.CodeConverter.CSharp.CommentConvertingVisitorWrapper`1.<Visit>d__5.MoveNext()
                            --- End of stack trace from previous location where exception was thrown ---
                               at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
                               at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
                               at ICSharpCode.CodeConverter.CSharp.SyntaxNodeVisitorExtensions.<AcceptAsync>d__0`1.MoveNext()
                            --- End of stack trace from previous location where exception was thrown ---
                               at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
                               at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
                               at ICSharpCode.CodeConverter.CSharp.MethodBodyExecutableStatementVisitor.<VisitAssignmentStatement>d__32.MoveNext()
                            --- End of stack trace from previous location where exception was thrown ---
                               at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
                               at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
                               at ICSharpCode.CodeConverter.CSharp.ByRefParameterVisitor.<CreateLocals>d__7.MoveNext()
                            --- End of stack trace from previous location where exception was thrown ---
                               at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
                               at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
                               at ICSharpCode.CodeConverter.CSharp.ByRefParameterVisitor.<AddLocalVariables>d__6.MoveNext()
                            --- End of stack trace from previous location where exception was thrown ---
                               at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
                               at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
                               at ICSharpCode.CodeConverter.CSharp.CommentConvertingMethodBodyVisitor.<ConvertWithTrivia>d__4.MoveNext()
                            --- End of stack trace from previous location where exception was thrown ---
                               at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
                               at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
                               at ICSharpCode.CodeConverter.CSharp.CommentConvertingMethodBodyVisitor.<DefaultVisit>d__3.MoveNext()

                            Input:
                                                Me.ListItemsSklepu.ItemsSource = From c In Me.mlDane Order By c.iCountIn + c.iCountOut Descending

                             */
                        else
                            ;
#error Cannot convert AssignmentStatementSyntax - see comment for details
                            /* Cannot convert AssignmentStatementSyntax, System.ArgumentNullException: Value cannot be null.
                            Parameter name: destination
                               at Microsoft.CodeAnalysis.VisualBasic.VisualBasicCompilation.ClassifyConversion(ITypeSymbol source, ITypeSymbol destination)
                               at ICSharpCode.CodeConverter.CSharp.TypeConversionAnalyzer.TryAnalyzeCsConversion(ExpressionSyntax vbNode, ITypeSymbol csType, ITypeSymbol csConvertedType, Conversion vbConversion, ITypeSymbol vbConvertedType, ITypeSymbol vbType, VisualBasicCompilation vbCompilation, Boolean isConst, TypeConversionKind& typeConversionKind)
                               at ICSharpCode.CodeConverter.CSharp.TypeConversionAnalyzer.AnalyzeConversion(ExpressionSyntax vbNode, Boolean alwaysExplicit, Boolean isConst, ITypeSymbol forceTargetType)
                               at ICSharpCode.CodeConverter.CSharp.TypeConversionAnalyzer.AddExplicitConversion(ExpressionSyntax vbNode, ExpressionSyntax csNode, Boolean addParenthesisIfNeeded, Boolean defaultToCast, Boolean isConst, ITypeSymbol forceTargetType)
                               at ICSharpCode.CodeConverter.CSharp.ExpressionNodeVisitor.<VisitBinaryExpression>d__63.MoveNext()
                            --- End of stack trace from previous location where exception was thrown ---
                               at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
                               at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
                               at ICSharpCode.CodeConverter.CSharp.CommentConvertingVisitorWrapper`1.<Visit>d__5.MoveNext()
                            --- End of stack trace from previous location where exception was thrown ---
                               at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
                               at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
                               at ICSharpCode.CodeConverter.CSharp.SyntaxNodeVisitorExtensions.<AcceptAsync>d__0`1.MoveNext()
                            --- End of stack trace from previous location where exception was thrown ---
                               at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
                               at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
                               at ICSharpCode.CodeConverter.CSharp.ExpressionNodeVisitor.<VisitOrdering>d__56.MoveNext()
                            --- End of stack trace from previous location where exception was thrown ---
                               at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
                               at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
                               at ICSharpCode.CodeConverter.CSharp.CommentConvertingVisitorWrapper`1.<Visit>d__5.MoveNext()
                            --- End of stack trace from previous location where exception was thrown ---
                               at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
                               at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
                               at ICSharpCode.CodeConverter.CSharp.SyntaxNodeVisitorExtensions.<AcceptAsync>d__0`1.MoveNext()
                            --- End of stack trace from previous location where exception was thrown ---
                               at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
                               at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
                               at ICSharpCode.CodeConverter.CSharp.QueryConverter.<<ConvertOrderByClause>b__27_0>d.MoveNext()
                            --- End of stack trace from previous location where exception was thrown ---
                               at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
                               at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
                               at ICSharpCode.CodeConverter.Shared.AsyncEnumerableTaskExtensions.<SelectAsync>d__4`2.MoveNext()
                            --- End of stack trace from previous location where exception was thrown ---
                               at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
                               at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
                               at ICSharpCode.CodeConverter.CSharp.QueryConverter.<ConvertOrderByClause>d__27.MoveNext()
                            --- End of stack trace from previous location where exception was thrown ---
                               at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
                               at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
                               at ICSharpCode.CodeConverter.CSharp.QueryConverter.<ConvertQueryBodyClause>d__21.MoveNext()
                            --- End of stack trace from previous location where exception was thrown ---
                               at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
                               at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
                               at ICSharpCode.CodeConverter.CSharp.QueryConverter.<GetQuerySegments>d__6.MoveNext()
                            --- End of stack trace from previous location where exception was thrown ---
                               at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
                               at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
                               at ICSharpCode.CodeConverter.CSharp.QueryConverter.<ConvertClauses>d__5.MoveNext()
                            --- End of stack trace from previous location where exception was thrown ---
                               at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
                               at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
                               at ICSharpCode.CodeConverter.CSharp.ExpressionNodeVisitor.<VisitQueryExpression>d__55.MoveNext()
                            --- End of stack trace from previous location where exception was thrown ---
                               at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
                               at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
                               at ICSharpCode.CodeConverter.CSharp.CommentConvertingVisitorWrapper`1.<Visit>d__5.MoveNext()
                            --- End of stack trace from previous location where exception was thrown ---
                               at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
                               at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
                               at ICSharpCode.CodeConverter.CSharp.SyntaxNodeVisitorExtensions.<AcceptAsync>d__0`1.MoveNext()
                            --- End of stack trace from previous location where exception was thrown ---
                               at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
                               at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
                               at ICSharpCode.CodeConverter.CSharp.MethodBodyExecutableStatementVisitor.<VisitAssignmentStatement>d__32.MoveNext()
                            --- End of stack trace from previous location where exception was thrown ---
                               at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
                               at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
                               at ICSharpCode.CodeConverter.CSharp.ByRefParameterVisitor.<CreateLocals>d__7.MoveNext()
                            --- End of stack trace from previous location where exception was thrown ---
                               at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
                               at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
                               at ICSharpCode.CodeConverter.CSharp.ByRefParameterVisitor.<AddLocalVariables>d__6.MoveNext()
                            --- End of stack trace from previous location where exception was thrown ---
                               at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
                               at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
                               at ICSharpCode.CodeConverter.CSharp.CommentConvertingMethodBodyVisitor.<ConvertWithTrivia>d__4.MoveNext()
                            --- End of stack trace from previous location where exception was thrown ---
                               at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
                               at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
                               at ICSharpCode.CodeConverter.CSharp.CommentConvertingMethodBodyVisitor.<DefaultVisit>d__3.MoveNext()

                            Input:
                                                Me.ListItemsSklepu.ItemsSource = From c In Me.mlDane Order By c.iCountIn + c.iCountOut

                             */
                        break;
                    }

                case 2:
                    {
                        if (mbSortDesc)
                            ;
#error Cannot convert AssignmentStatementSyntax - see comment for details
                            /* Cannot convert AssignmentStatementSyntax, System.ArgumentNullException: Value cannot be null.
                            Parameter name: destination
                               at Microsoft.CodeAnalysis.VisualBasic.VisualBasicCompilation.ClassifyConversion(ITypeSymbol source, ITypeSymbol destination)
                               at ICSharpCode.CodeConverter.CSharp.TypeConversionAnalyzer.TryAnalyzeCsConversion(ExpressionSyntax vbNode, ITypeSymbol csType, ITypeSymbol csConvertedType, Conversion vbConversion, ITypeSymbol vbConvertedType, ITypeSymbol vbType, VisualBasicCompilation vbCompilation, Boolean isConst, TypeConversionKind& typeConversionKind)
                               at ICSharpCode.CodeConverter.CSharp.TypeConversionAnalyzer.AnalyzeConversion(ExpressionSyntax vbNode, Boolean alwaysExplicit, Boolean isConst, ITypeSymbol forceTargetType)
                               at ICSharpCode.CodeConverter.CSharp.TypeConversionAnalyzer.AddExplicitConversion(ExpressionSyntax vbNode, ExpressionSyntax csNode, Boolean addParenthesisIfNeeded, Boolean defaultToCast, Boolean isConst, ITypeSymbol forceTargetType)
                               at ICSharpCode.CodeConverter.CSharp.ExpressionNodeVisitor.<VisitBinaryExpression>d__63.MoveNext()
                            --- End of stack trace from previous location where exception was thrown ---
                               at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
                               at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
                               at ICSharpCode.CodeConverter.CSharp.CommentConvertingVisitorWrapper`1.<Visit>d__5.MoveNext()
                            --- End of stack trace from previous location where exception was thrown ---
                               at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
                               at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
                               at ICSharpCode.CodeConverter.CSharp.SyntaxNodeVisitorExtensions.<AcceptAsync>d__0`1.MoveNext()
                            --- End of stack trace from previous location where exception was thrown ---
                               at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
                               at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
                               at ICSharpCode.CodeConverter.CSharp.ExpressionNodeVisitor.<VisitOrdering>d__56.MoveNext()
                            --- End of stack trace from previous location where exception was thrown ---
                               at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
                               at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
                               at ICSharpCode.CodeConverter.CSharp.CommentConvertingVisitorWrapper`1.<Visit>d__5.MoveNext()
                            --- End of stack trace from previous location where exception was thrown ---
                               at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
                               at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
                               at ICSharpCode.CodeConverter.CSharp.SyntaxNodeVisitorExtensions.<AcceptAsync>d__0`1.MoveNext()
                            --- End of stack trace from previous location where exception was thrown ---
                               at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
                               at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
                               at ICSharpCode.CodeConverter.CSharp.QueryConverter.<<ConvertOrderByClause>b__27_0>d.MoveNext()
                            --- End of stack trace from previous location where exception was thrown ---
                               at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
                               at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
                               at ICSharpCode.CodeConverter.Shared.AsyncEnumerableTaskExtensions.<SelectAsync>d__4`2.MoveNext()
                            --- End of stack trace from previous location where exception was thrown ---
                               at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
                               at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
                               at ICSharpCode.CodeConverter.CSharp.QueryConverter.<ConvertOrderByClause>d__27.MoveNext()
                            --- End of stack trace from previous location where exception was thrown ---
                               at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
                               at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
                               at ICSharpCode.CodeConverter.CSharp.QueryConverter.<ConvertQueryBodyClause>d__21.MoveNext()
                            --- End of stack trace from previous location where exception was thrown ---
                               at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
                               at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
                               at ICSharpCode.CodeConverter.CSharp.QueryConverter.<GetQuerySegments>d__6.MoveNext()
                            --- End of stack trace from previous location where exception was thrown ---
                               at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
                               at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
                               at ICSharpCode.CodeConverter.CSharp.QueryConverter.<ConvertClauses>d__5.MoveNext()
                            --- End of stack trace from previous location where exception was thrown ---
                               at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
                               at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
                               at ICSharpCode.CodeConverter.CSharp.ExpressionNodeVisitor.<VisitQueryExpression>d__55.MoveNext()
                            --- End of stack trace from previous location where exception was thrown ---
                               at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
                               at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
                               at ICSharpCode.CodeConverter.CSharp.CommentConvertingVisitorWrapper`1.<Visit>d__5.MoveNext()
                            --- End of stack trace from previous location where exception was thrown ---
                               at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
                               at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
                               at ICSharpCode.CodeConverter.CSharp.SyntaxNodeVisitorExtensions.<AcceptAsync>d__0`1.MoveNext()
                            --- End of stack trace from previous location where exception was thrown ---
                               at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
                               at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
                               at ICSharpCode.CodeConverter.CSharp.MethodBodyExecutableStatementVisitor.<VisitAssignmentStatement>d__32.MoveNext()
                            --- End of stack trace from previous location where exception was thrown ---
                               at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
                               at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
                               at ICSharpCode.CodeConverter.CSharp.ByRefParameterVisitor.<CreateLocals>d__7.MoveNext()
                            --- End of stack trace from previous location where exception was thrown ---
                               at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
                               at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
                               at ICSharpCode.CodeConverter.CSharp.ByRefParameterVisitor.<AddLocalVariables>d__6.MoveNext()
                            --- End of stack trace from previous location where exception was thrown ---
                               at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
                               at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
                               at ICSharpCode.CodeConverter.CSharp.CommentConvertingMethodBodyVisitor.<ConvertWithTrivia>d__4.MoveNext()
                            --- End of stack trace from previous location where exception was thrown ---
                               at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
                               at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
                               at ICSharpCode.CodeConverter.CSharp.CommentConvertingMethodBodyVisitor.<DefaultVisit>d__3.MoveNext()

                            Input:
                                                Me.ListItemsSklepu.ItemsSource = From c In Me.mlDane Order By c.iMinIn + c.iMinOut Descending

                             */
                        else
                            ;
#error Cannot convert AssignmentStatementSyntax - see comment for details
                            /* Cannot convert AssignmentStatementSyntax, System.ArgumentNullException: Value cannot be null.
                            Parameter name: destination
                               at Microsoft.CodeAnalysis.VisualBasic.VisualBasicCompilation.ClassifyConversion(ITypeSymbol source, ITypeSymbol destination)
                               at ICSharpCode.CodeConverter.CSharp.TypeConversionAnalyzer.TryAnalyzeCsConversion(ExpressionSyntax vbNode, ITypeSymbol csType, ITypeSymbol csConvertedType, Conversion vbConversion, ITypeSymbol vbConvertedType, ITypeSymbol vbType, VisualBasicCompilation vbCompilation, Boolean isConst, TypeConversionKind& typeConversionKind)
                               at ICSharpCode.CodeConverter.CSharp.TypeConversionAnalyzer.AnalyzeConversion(ExpressionSyntax vbNode, Boolean alwaysExplicit, Boolean isConst, ITypeSymbol forceTargetType)
                               at ICSharpCode.CodeConverter.CSharp.TypeConversionAnalyzer.AddExplicitConversion(ExpressionSyntax vbNode, ExpressionSyntax csNode, Boolean addParenthesisIfNeeded, Boolean defaultToCast, Boolean isConst, ITypeSymbol forceTargetType)
                               at ICSharpCode.CodeConverter.CSharp.ExpressionNodeVisitor.<VisitBinaryExpression>d__63.MoveNext()
                            --- End of stack trace from previous location where exception was thrown ---
                               at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
                               at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
                               at ICSharpCode.CodeConverter.CSharp.CommentConvertingVisitorWrapper`1.<Visit>d__5.MoveNext()
                            --- End of stack trace from previous location where exception was thrown ---
                               at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
                               at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
                               at ICSharpCode.CodeConverter.CSharp.SyntaxNodeVisitorExtensions.<AcceptAsync>d__0`1.MoveNext()
                            --- End of stack trace from previous location where exception was thrown ---
                               at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
                               at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
                               at ICSharpCode.CodeConverter.CSharp.ExpressionNodeVisitor.<VisitOrdering>d__56.MoveNext()
                            --- End of stack trace from previous location where exception was thrown ---
                               at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
                               at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
                               at ICSharpCode.CodeConverter.CSharp.CommentConvertingVisitorWrapper`1.<Visit>d__5.MoveNext()
                            --- End of stack trace from previous location where exception was thrown ---
                               at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
                               at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
                               at ICSharpCode.CodeConverter.CSharp.SyntaxNodeVisitorExtensions.<AcceptAsync>d__0`1.MoveNext()
                            --- End of stack trace from previous location where exception was thrown ---
                               at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
                               at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
                               at ICSharpCode.CodeConverter.CSharp.QueryConverter.<<ConvertOrderByClause>b__27_0>d.MoveNext()
                            --- End of stack trace from previous location where exception was thrown ---
                               at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
                               at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
                               at ICSharpCode.CodeConverter.Shared.AsyncEnumerableTaskExtensions.<SelectAsync>d__4`2.MoveNext()
                            --- End of stack trace from previous location where exception was thrown ---
                               at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
                               at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
                               at ICSharpCode.CodeConverter.CSharp.QueryConverter.<ConvertOrderByClause>d__27.MoveNext()
                            --- End of stack trace from previous location where exception was thrown ---
                               at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
                               at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
                               at ICSharpCode.CodeConverter.CSharp.QueryConverter.<ConvertQueryBodyClause>d__21.MoveNext()
                            --- End of stack trace from previous location where exception was thrown ---
                               at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
                               at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
                               at ICSharpCode.CodeConverter.CSharp.QueryConverter.<GetQuerySegments>d__6.MoveNext()
                            --- End of stack trace from previous location where exception was thrown ---
                               at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
                               at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
                               at ICSharpCode.CodeConverter.CSharp.QueryConverter.<ConvertClauses>d__5.MoveNext()
                            --- End of stack trace from previous location where exception was thrown ---
                               at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
                               at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
                               at ICSharpCode.CodeConverter.CSharp.ExpressionNodeVisitor.<VisitQueryExpression>d__55.MoveNext()
                            --- End of stack trace from previous location where exception was thrown ---
                               at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
                               at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
                               at ICSharpCode.CodeConverter.CSharp.CommentConvertingVisitorWrapper`1.<Visit>d__5.MoveNext()
                            --- End of stack trace from previous location where exception was thrown ---
                               at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
                               at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
                               at ICSharpCode.CodeConverter.CSharp.SyntaxNodeVisitorExtensions.<AcceptAsync>d__0`1.MoveNext()
                            --- End of stack trace from previous location where exception was thrown ---
                               at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
                               at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
                               at ICSharpCode.CodeConverter.CSharp.MethodBodyExecutableStatementVisitor.<VisitAssignmentStatement>d__32.MoveNext()
                            --- End of stack trace from previous location where exception was thrown ---
                               at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
                               at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
                               at ICSharpCode.CodeConverter.CSharp.ByRefParameterVisitor.<CreateLocals>d__7.MoveNext()
                            --- End of stack trace from previous location where exception was thrown ---
                               at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
                               at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
                               at ICSharpCode.CodeConverter.CSharp.ByRefParameterVisitor.<AddLocalVariables>d__6.MoveNext()
                            --- End of stack trace from previous location where exception was thrown ---
                               at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
                               at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
                               at ICSharpCode.CodeConverter.CSharp.CommentConvertingMethodBodyVisitor.<ConvertWithTrivia>d__4.MoveNext()
                            --- End of stack trace from previous location where exception was thrown ---
                               at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)
                               at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)
                               at ICSharpCode.CodeConverter.CSharp.CommentConvertingMethodBodyVisitor.<DefaultVisit>d__3.MoveNext()

                            Input:
                                                Me.ListItemsSklepu.ItemsSource = From c In Me.mlDane Order By c.iMinIn + c.iMinOut

                             */
                        break;
                    }

                case 3:
                    {
                        if (mbSortDesc)
                            this.ListItemsSklepu.ItemsSource = from c in mlDane
                                                               orderby c.iCountIn descending
                                                               select c;
                        else
                            this.ListItemsSklepu.ItemsSource = from c in mlDane
                                                               orderby c.iCountIn
                                                               select c;
                        break;
                    }

                case 4:
                    {
                        if (mbSortDesc)
                            this.ListItemsSklepu.ItemsSource = from c in mlDane
                                                               orderby c.iMinIn descending
                                                               select c;
                        else
                            this.ListItemsSklepu.ItemsSource = from c in mlDane
                                                               orderby c.iMinIn
                                                               select c;
                        break;
                    }

                case 5:
                    {
                        if (mbSortDesc)
                            this.ListItemsSklepu.ItemsSource = from c in mlDane
                                                               orderby c.iCountOut descending
                                                               select c;
                        else
                            this.ListItemsSklepu.ItemsSource = from c in mlDane
                                                               orderby c.iCountOut
                                                               select c;
                        break;
                    }

                case 6:
                    {
                        if (mbSortDesc)
                            this.ListItemsSklepu.ItemsSource = from c in mlDane
                                                               orderby c.iMinOut descending
                                                               select c;
                        else
                            this.ListItemsSklepu.ItemsSource = from c in mlDane
                                                               orderby c.iMinOut
                                                               select c;
                        break;
                    }
            }
        }

        private async void uiPage_Loaded(object sender, RoutedEventArgs e)
        {
            await ReadData(); // wraz z global stats
            uiResetMarks(CallStat.App.GetSettingsInt("SortMode", 2), CallStat.App.GetSettingsBool("SortDesc", true));
        }

        private void uiRefresh_Click(object sender, RoutedEventArgs e)
        {
            ShowPersons();
        }

        private void uiResetMarks(int iMode, bool bDesc)
        {
            this.uiSortAllCnt.IsChecked = iMode == 1 ? true : false;
            this.uiSortAllMin.IsChecked = iMode == 2 ? true : false;
            this.uiSortInCnt.IsChecked = iMode == 3 ? true : false;
            this.uiSortInMin.IsChecked = iMode == 4 ? true : false;
            this.uiSortOutCnt.IsChecked = iMode == 5 ? true : false;
            this.uiSortOutMin.IsChecked = iMode == 6 ? true : false;
            this.uiSortDesc.IsChecked = bDesc ? true : false;

            miSort = iMode;
            mbSortDesc = bDesc;
            CallStat.App.SetSettingsInt("SortMode", miSort);
            CallStat.App.SetSettingsBool("SortDesc", mbSortDesc);
            uiRefresh_Click(null, null); // pokaż stat
        }
        private void uiResetZakres(int iMode)
        {
            this.uiZakresAll.IsChecked = iMode == 0 ? true : false;
            this.uiZakresMonth.IsChecked = iMode == 1 ? true : false;
            this.uiZakresDay.IsChecked = iMode == 2 ? true : false;
            this.uiZakres30d.IsChecked = iMode == 3 ? true : false;
            miRange = iMode;
            CallStat.App.SetSettingsInt("RangeMode", miRange);
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
            uiResetZakres(0);
        }

        private void uiZakresMonth_Click(object sender, RoutedEventArgs e)
        {
            uiResetZakres(1);
        }

        private void uiZakresDay_Click(object sender, RoutedEventArgs e)
        {
            uiResetZakres(2);
        }

        private void uiZakres30d_Click(object sender, RoutedEventArgs e)
        {
            uiResetZakres(3);
        }
    }
}
