﻿using Fiddler;
using FreeHttp.FreeHttpControl;
using FreeHttp.HttpHelper;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

/*******************************************************************************
* Copyright (c) 2018 lulianqi
* All rights reserved.
* 
* 文件名称: 
* 内容摘要: mycllq@hotmail.com
* 
* 历史记录:
* 日	  期:   20181103           创建人: lulianqi [mycllq@hotmail.com]
* 描    述: 创建
*
* 历史记录:
* 日	  期:                      修改:  
* 描    述: 
*******************************************************************************/

[assembly: Fiddler.RequiredVersion("2.3.5.0")]
namespace FreeHttp
{
    
    public class FiddlerFreeHttp : IAutoTamper ,IDisposable
    {
        private bool isOnLoad = false;
        private TabPage tabPage; 
        private FreeHttpWindow myFreeHttpWindow; 

        private void ShowMes(string mes)
        {
            if(!isOnLoad)
            {
                return;
            }
            myFreeHttpWindow.PutInfo(mes);
        }

        private void ShowError(string mes)
        {
            if (!isOnLoad)
            {
                return;
            }
            FiddlerObject.log(mes);
            myFreeHttpWindow.PutError(mes);
        }

        private void AddFiddlerObjectLog(string mes)
        {
            FiddlerObject.log(mes);
        }
        private void SetStatusText(string mes)
        {
            FiddlerObject.StatusText = mes;
        }

        private void MarkSession(Session oSession)
        {
            oSession["ui-backcolor"] = "Khaki";
            oSession["ui-bold"] = "true";
            oSession["ui-color"] = "Indigo";
            oSession.RefreshUI();
        }
        public void OnBeforeUnload()
        {
            SerializableHelper.SerializeRuleList(myFreeHttpWindow.RequestRuleListView, myFreeHttpWindow.ResponseRuleListView);
            SerializableHelper.SerializeData<FiddlerModificSettingInfo>(myFreeHttpWindow.ModificSettingInfo, "FreeHttpSetting.xml");
        }

        public void OnLoad()
        {
            FiddlerObject.log(string.Format("【FiddlerFreeHttp】:{0}", "OnLoad"));
            if (!isOnLoad)
            {

                tabPage = new TabPage();
                tabPage.Text = "Free Http";
                if (FiddlerApplication.UI.tabsViews.ImageList != null)
                {
                    Image myIco = FreeHttp.Resources.MyResource.freehttpico;
                    FiddlerApplication.UI.tabsViews.ImageList.Images.Add(myIco);
                    tabPage.ImageIndex = FiddlerApplication.UI.tabsViews.ImageList.Images.Count - 1;
                }
                myFreeHttpWindow = new FreeHttpWindow(SerializableHelper.DeserializeRuleList(), SerializableHelper.DeserializeData<FiddlerModificSettingInfo>("FreeHttpSetting.xml"));
                myFreeHttpWindow.OnGetSession += myFreeHttpWindow_OnGetSession;
                myFreeHttpWindow.OnGetSessionRawData += myFreeHttpWindow_OnGetSessionRawData;
                myFreeHttpWindow.Dock = DockStyle.Fill;
                tabPage.Controls.Add(myFreeHttpWindow);
                FiddlerApplication.UI.tabsViews.TabPages.Add(tabPage);
                Fiddler.FiddlerApplication.UI.Deactivate += UI_Deactivate;
                isOnLoad = true;
            }
        }

        void UI_Deactivate(object sender, EventArgs e)
        {
            myFreeHttpWindow.CloseEditRtb();
        }

        void myFreeHttpWindow_OnGetSessionRawData(object sender, FreeHttpWindow.GetSessionRawDataEventArgs e)
        {
            Session tempSession = Fiddler.FiddlerObject.UI.GetFirstSelectedSession();
            if (tempSession != null)
            {
                if(e.IsGetCookies)
                {
                    myFreeHttpWindow.SetClientCookies(tempSession.RequestHeaders["Cookie"]);
                }
                StringBuilder sbRawData = new StringBuilder("Get Raw Data\r\n");
                MemoryStream ms = new MemoryStream();
                //tempSession.WriteToStream(SmartAssembly, false);
                tempSession.WriteRequestToStream(true, true, ms);
                ms.Position = 0;
                StreamReader sr = new StreamReader(ms,Encoding.UTF8);
                sbRawData.Append(sr.ReadToEnd());
                sr.Close();
                ms.Close();
 
                if (tempSession.requestBodyBytes != null && tempSession.requestBodyBytes.Length>0)
                {
                    sbRawData.AppendLine(tempSession.GetRequestBodyAsString());
                    sbRawData.Append("\r\n");
                }
                if (e.IsShowResponse && tempSession.bHasResponse)
                {
                    sbRawData.AppendLine(tempSession.ResponseHeaders.ToString());
                    if (tempSession.responseBodyBytes != null && tempSession.responseBodyBytes.Length > 0)
                    {
                        sbRawData.AppendLine(tempSession.GetResponseBodyAsString());
                    }
                }
                ShowMes(sbRawData.ToString());
            }
            else
            {
                Fiddler.FiddlerObject.UI.ShowAlert(new frmAlert("STOP", "please select a session", "OK"));
                ((FreeHttpWindow)sender).MarkWarnControl(Fiddler.FiddlerApplication.UI.lvSessions);
            }
        }

        void myFreeHttpWindow_OnGetSession(object sender, EventArgs e)
        {
            Session tempSession = Fiddler.FiddlerObject.UI.GetFirstSelectedSession();
            if (tempSession != null)
            {
                ShowMes(string.Format("Get http session in {0}",tempSession.fullUrl));
                ((FreeHttpWindow)sender).SetModificSession(tempSession);
            }
            else
            {
                Fiddler.FiddlerObject.UI.ShowAlert(new frmAlert("STOP", "please select a session", "OK"));
                //((FreeHttpWindow)sender).MarkWarnControl(Fiddler.FiddlerApplication.UI.Controls[0]);
                ((FreeHttpWindow)sender).MarkWarnControl(Fiddler.FiddlerApplication.UI.lvSessions);
            }
        }

        public void AutoTamperRequestAfter(Session oSession)
        {
            //throw new NotImplementedException();
            
        }

        public void AutoTamperRequestBefore(Session oSession)
        {

            if (oSession.HTTPMethodIs("CONNECT") && oSession.HostnameIs("api.map.baidu.com"))
            {
                oSession["x-OverrideSslProtocols"] = "ssl3.0";
            }
            oSession.oRequest["AddOrigin"] = "from lijie PC";
            if (!isOnLoad)
            {
                return;
            }
            if (myFreeHttpWindow.IsRequestRuleEnable)
            {
                //IsRequestRuleEnable is more efficient then string comparison (so if not IsRequestRuleEnable the string comparison will not execute)
                if (myFreeHttpWindow.ModificSettingInfo.IsSkipTlsHandshake && oSession.RequestMethod == "CONNECT")
                {
                    return;
                }
                List<ListViewItem> matchItems = FiddlerSessionHelper.FindMatchTanperRule(oSession, myFreeHttpWindow.RequestRuleListView);
                if (matchItems != null && matchItems.Count>0)
                {
                    foreach (var matchItem in matchItems)
                    {
                        FiddlerRequsetChange nowFiddlerRequsetChange = ((FiddlerRequsetChange)matchItem.Tag);
                        myFreeHttpWindow.MarkMatchRule(matchItem);
                        MarkSession(oSession);
                        ShowMes(string.Format("macth the [requst rule {0}] with {1}", matchItem.SubItems[0].Text, oSession.fullUrl));
                        FiddlerSessionTamper.ModificSessionRequest(oSession, nowFiddlerRequsetChange,ShowError);
                        if(myFreeHttpWindow.ModificSettingInfo.IsOnlyMatchFistTamperRule)
                        {
                            break;
                        }
                    }
                }
            }

            if (myFreeHttpWindow.IsResponseRuleEnable)
            {
                if (myFreeHttpWindow.ModificSettingInfo.IsSkipTlsHandshake && oSession.RequestMethod == "CONNECT")
                {
                    return;
                }
                List<ListViewItem> matchItems = FiddlerSessionHelper.FindMatchTanperRule(oSession, myFreeHttpWindow.ResponseRuleListView);
                if (matchItems != null && matchItems.Count>0)
                {
                    foreach (var matchItem in matchItems)
                    {
                        FiddlerResponseChange nowFiddlerResponseChange = ((FiddlerResponseChange)matchItem.Tag);
                        if (nowFiddlerResponseChange.IsIsDirectRespons)
                        {
                            myFreeHttpWindow.MarkMatchRule(matchItem);
                            MarkSession(oSession);
                            ShowMes(string.Format("macth the [reponse rule {0}] with {1}", matchItem.SubItems[0].Text, oSession.fullUrl));
                            FiddlerSessionTamper.ReplaceSessionResponse(oSession, nowFiddlerResponseChange,ShowError);
                            //oSession.state = SessionStates.Done;
                            if (myFreeHttpWindow.ModificSettingInfo.IsOnlyMatchFistTamperRule)
                            {
                                break;
                            }
                        }
                    }
                }
            }
        }

        public void AutoTamperResponseAfter(Session oSession)
        {
            if (!isOnLoad)
            {
                return;
            }
            if (myFreeHttpWindow.IsResponseRuleEnable)
            {
                if (myFreeHttpWindow.ModificSettingInfo.IsSkipTlsHandshake && oSession.RequestMethod == "CONNECT")
                {
                    return;
                }
                List<ListViewItem> matchItems = FiddlerSessionHelper.FindMatchTanperRule(oSession, myFreeHttpWindow.ResponseRuleListView);
                if (matchItems != null && matchItems.Count>0)
                {
                    foreach (var matchItem in matchItems)
                    {
                        FiddlerResponseChange nowFiddlerResponseChange = ((FiddlerResponseChange)matchItem.Tag);
                        if (!(nowFiddlerResponseChange.IsRawReplace && nowFiddlerResponseChange.IsIsDirectRespons))
                        {
                            myFreeHttpWindow.MarkMatchRule(matchItem);
                            MarkSession(oSession);
                            ShowMes(string.Format("macth the [reponse rule {0}] with {1}", matchItem.SubItems[0].Text, oSession.fullUrl));
                            FiddlerSessionTamper.ModificSessionResponse(oSession, nowFiddlerResponseChange,ShowError);
                        }
                        if (nowFiddlerResponseChange.LesponseLatency > 0)
                        {
                            ShowMes(string.Format("[reponse rule {0}] is modified , now lesponse {1} ms", matchItem.SubItems[0].Text, nowFiddlerResponseChange.LesponseLatency));
                            System.Threading.Thread.Sleep(nowFiddlerResponseChange.LesponseLatency);
                        }
                        if (myFreeHttpWindow.ModificSettingInfo.IsOnlyMatchFistTamperRule)
                        {
                            break;
                        }
                    }
                }
            }

        }

        public void AutoTamperResponseBefore(Session oSession)
        {
            //throw new NotImplementedException();
        }

        public void OnBeforeReturningError(Session oSession)
        {
            //throw new NotImplementedException();
        }


        public void Dispose()
        {
            tabPage.Dispose();
            myFreeHttpWindow.Dispose();
        }
    }
}
