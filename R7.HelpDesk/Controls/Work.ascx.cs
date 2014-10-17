﻿//
// webserver.com
// Copyright (c) 2009
// by Michael Washington
//
// redhound.ru
// Copyright (c) 2013
// by Roman M. Yagodin
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated 
// documentation files (the "Software"), to deal in the Software without restriction, including without limitation 
// the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and 
// to permit persons to whom the Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or substantial portions 
// of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED 
// TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL 
// THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF 
// CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
// DEALINGS IN THE SOFTWARE.
//
//

using System;
using System.Linq;
using System.Web;
using System.Web.UI.WebControls;
using System.Collections.Generic;
using DotNetNuke.Common;
using DotNetNuke.Security.Roles;
using DotNetNuke.Entities.Users;
using System.Collections;
using System.Drawing;
using Microsoft.VisualBasic;
using System.Text;
using System.IO;
using DotNetNuke.Services.Exceptions;
using DotNetNuke.Services.Localization;

namespace R7.HelpDesk
{
    public partial class Work : DotNetNuke.Entities.Modules.PortalModuleBase
    {

        #region Properties
        public int TaskID
        {
            get { return Convert.ToInt32(ViewState["TaskID"]); }
            set { ViewState["TaskID"] = value; }
        }

        public int ModuleID
        {
            get { return Convert.ToInt32(ViewState["ModuleID"]); }
            set { ViewState["ModuleID"] = value; }
        }

        public bool ViewOnly
        {
            get { return Convert.ToBoolean(ViewState["ViewOnly"]); }
            set { ViewState["ViewOnly"] = value; }
        }
        #endregion

        protected void Page_Load(object sender, EventArgs e)
        {
            try
            {
                cmdtxtStartCalendar1.NavigateUrl = DotNetNuke.Common.Utilities.Calendar.InvokePopupCal(txtStartDay);
                cmdtxtStartCalendar2.NavigateUrl = DotNetNuke.Common.Utilities.Calendar.InvokePopupCal(txtStopDay);
                cmdtxtStartCalendar3.NavigateUrl = DotNetNuke.Common.Utilities.Calendar.InvokePopupCal(txtStartDayEdit);
                cmdtxtStartCalendar4.NavigateUrl = DotNetNuke.Common.Utilities.Calendar.InvokePopupCal(txtStopDayEdit);

                pnlInsertComment.GroupingText = Localization.GetString("pnlInsertComment.Text", LocalResourceFile);

                if (!Page.IsPostBack)
                {
                    // Insert Default dates and times
                    txtStartDay.Text = DateTime.Now.ToShortDateString();
                    txtStopDay.Text = DateTime.Now.ToShortDateString();
                    txtStartTime.Text = DateTime.Now.AddHours(-1).ToShortTimeString();
                    txtStopTime.Text = DateTime.Now.ToShortTimeString();

                    SetView("Default");

                    if (ViewOnly)
                    {
                        SetViewOnlyMode();
                    }
                }
            }
            catch (Exception ex)
            {
                Exceptions.ProcessModuleLoadException(this, ex);
            }
        }

        #region SetView
        public void SetView(string ViewMode)
        {
            if (ViewMode == "Default")
            {
                pnlInsertComment.Visible = true;
                pnlTableHeader.Visible = true;
                pnlExistingComments.Visible = true;
                pnlEditComment.Visible = false;
            }

            if (ViewMode == "Edit")
            {
                pnlInsertComment.Visible = false;
                pnlTableHeader.Visible = false;
                pnlExistingComments.Visible = false;
                pnlEditComment.Visible = true;
            }
        }
        #endregion

        #region SetViewOnlyMode
        private void SetViewOnlyMode()
        {
            lnkDelete.Visible = false;
            Image5.Visible = false;
            lnkUpdate.Visible = false;
            Image4.Visible = false;
        }
        #endregion

        // Insert Comment

        #region btnInsertComment_Click
        protected void btnInsertComment_Click(object sender, EventArgs e)
        {
            InsertComment();
        }
        #endregion

        #region btnInsertCommentAndEmail_Click
        protected void btnInsertCommentAndEmail_Click(object sender, EventArgs e)
        {
            string strComment = txtComment.Text;
            InsertComment();
        }
        #endregion

        #region InsertComment
        private void InsertComment()
        {
            if (txtComment.Text.Trim().Length > 0)
            {
                try
                {
                    // Try to Make Start and Stop Time
                    DateTime StartTime = Convert.ToDateTime(String.Format("{0} {1}", txtStartDay.Text, txtStartTime.Text));
                    DateTime StopTime = Convert.ToDateTime(String.Format("{0} {1}", txtStopDay.Text, txtStopTime.Text));
                }
                catch
                {
                    lblError.Text = Localization.GetString("MustProvideValidStarAndStopTimes.Text", LocalResourceFile);
                    return;
                }

                HelpDeskDALDataContext objHelpDeskDALDataContext = new HelpDeskDALDataContext();

                string strComment = txtComment.Text.Trim();

                // Save Task Details
                HelpDesk_TaskDetail objHelpDesk_TaskDetail = new HelpDesk_TaskDetail();

                objHelpDesk_TaskDetail.TaskID = TaskID;
                objHelpDesk_TaskDetail.Description = txtComment.Text.Trim();
                objHelpDesk_TaskDetail.InsertDate = DateTime.Now;
                objHelpDesk_TaskDetail.UserID = UserId;
                objHelpDesk_TaskDetail.DetailType = "Work";
                objHelpDesk_TaskDetail.StartTime = Convert.ToDateTime(String.Format("{0} {1}", txtStartDay.Text, txtStartTime.Text));
                objHelpDesk_TaskDetail.StopTime = Convert.ToDateTime(String.Format("{0} {1}", txtStopDay.Text, txtStopTime.Text));

                objHelpDeskDALDataContext.HelpDesk_TaskDetails.InsertOnSubmit(objHelpDesk_TaskDetail);
                objHelpDeskDALDataContext.SubmitChanges();
                txtComment.Text = "";

                // Insert Log
                Log.InsertLog(TaskID, UserId, String.Format(Localization.GetString("InsertedWorkComment.Text", LocalResourceFile), GetUserName()));

                gvComments.DataBind();
            }
            else
            {
                lblError.Text = Localization.GetString("MustProvideADescription.Text", LocalResourceFile);
            }
        }
        #endregion

        #region LDSComments_Selecting
        protected void LDSComments_Selecting(object sender, LinqDataSourceSelectEventArgs e)
        {
            HelpDeskDALDataContext objHelpDeskDALDataContext = new HelpDeskDALDataContext();
            var result = from HelpDesk_TaskDetails in objHelpDeskDALDataContext.HelpDesk_TaskDetails
                         where HelpDesk_TaskDetails.TaskID == TaskID
                         where (HelpDesk_TaskDetails.DetailType == "Work")
                         select HelpDesk_TaskDetails;

            e.Result = result;
        }
        #endregion

        #region gvComments_RowDataBound
        protected void gvComments_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType == DataControlRowType.DataRow)
            {
                GridViewRow objGridViewRow = (GridViewRow)e.Row;

                // Comment
                Label lblComment = (Label)objGridViewRow.FindControl("lblComment");
                if (lblComment.Text.Trim().Length > 100)
                {
                    lblComment.Text = String.Format("{0}...", Utils.StringLeft(lblComment.Text, 100));
                }

                // User
                Label gvlblUser = (Label)objGridViewRow.FindControl("gvlblUser");
                if (gvlblUser.Text != "-1")
                {
                    UserInfo objUser = //UserController.GetUser(PortalId, Convert.ToInt32(gvlblUser.Text), false);
						UserController.GetUserById(PortalId, Convert.ToInt32(gvlblUser.Text));

                    if (objUser != null)
                    {
                        string strDisplayName = objUser.DisplayName;

                        if (strDisplayName.Length > 25)
                        {
                            gvlblUser.Text = String.Format("{0}...", Utils.StringLeft(strDisplayName, 25));
                        }
                        else
                        {
                            gvlblUser.Text = strDisplayName;
                        }
                    }
                    else
                    {
                        gvlblUser.Text = "[User Deleted]";
                    }
                }
                else
                {
                    gvlblUser.Text = Localization.GetString("Requestor.Text", LocalResourceFile);
                }

                
                // Time
                Label lblTimeSpan = (Label)objGridViewRow.FindControl("lblTimeSpan");
                try
                {
                    
                    Label lblStartTime = (Label)objGridViewRow.FindControl("lblStartTime");
                    Label lblStopTime = (Label)objGridViewRow.FindControl("lblStopTime");

                    DateTime StartDate = Convert.ToDateTime(lblStartTime.Text);
                    DateTime StopDate = Convert.ToDateTime(lblStopTime.Text);
                    TimeSpan TimeDifference = StopDate.Subtract(StartDate);

                    // if no Days
                    if (TimeDifference.Days == 0)
                    {
                        if (TimeDifference.Hours == 0)
                        {
                            lblTimeSpan.Text = String.Format(Localization.GetString("Minute.Text", LocalResourceFile), TimeDifference.Minutes.ToString(), ((TimeDifference.Minutes > 1) ? "s" : ""));
                        }
                        else
                        {
                            lblTimeSpan.Text = String.Format(Localization.GetString("HoursandMinute.Text", LocalResourceFile), TimeDifference.Hours.ToString(), TimeDifference.Minutes.ToString(), ((TimeDifference.Minutes > 1) ? "s" : ""));
                        }
                    }
                    else
                    {
                        lblTimeSpan.Text = String.Format(Localization.GetString("DaysHoursMinutes.Text", LocalResourceFile), TimeDifference.Days.ToString(), ((TimeDifference.Days > 1) ? "s" : ""), TimeDifference.Hours.ToString(), TimeDifference.Minutes.ToString(), ((TimeDifference.Minutes > 1) ? "s" : ""));
                    }
                }
                catch (Exception ex)
                {
                    lblTimeSpan.Text = ex.Message;
                    Exceptions.ProcessModuleLoadException(this, ex);
                }
            }
        }
        #endregion

        #region GetRandomPassword
        public string GetRandomPassword()
        {
            StringBuilder builder = new StringBuilder();
            Random random = new Random();
            int intElements = random.Next(10, 26);

            for (int i = 0; i < intElements; i++)
            {
                int intRandomType = random.Next(0, 2);
                if (intRandomType == 1)
                {
                    char ch;
                    ch = Convert.ToChar(Convert.ToInt32(Math.Floor(26 * random.NextDouble() + 65)));
                    builder.Append(ch);
                }
                else
                {
                    builder.Append(random.Next(0, 9));
                }
            }
            return builder.ToString();
        }
        #endregion

        #region GetUserName
        private string GetUserName()
        {
            string strUserName = Localization.GetString("Anonymous.Text", LocalResourceFile);

            if (UserId > -1)
            {
                strUserName = UserInfo.DisplayName;
            }

            return strUserName;
        }

        private string GetUserName(int intUserID)
        {
            string strUserName = Localization.GetString("Anonymous.Text", LocalResourceFile);

            if (intUserID > -1)
            {
                UserInfo objUser = //UserController.GetUser(PortalId, intUserID, false);
					UserController.GetUserById(PortalId, intUserID);

                if (objUser != null)
                {
                    strUserName = objUser.DisplayName;
                }
                else
                {
                    strUserName = Localization.GetString("Anonymous.Text", LocalResourceFile);
                }
            }

            return strUserName;
        }
        #endregion

        // GridView

        #region gvComments_RowCommand
        protected void gvComments_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName == "Select")
            {
                SetView("Edit");
                lblDetailID.Text = Convert.ToString(e.CommandArgument);
                DisplayComment();
            }
        }
        #endregion

        // Comment Edit

        #region lnkBack_Click
        protected void lnkBack_Click(object sender, EventArgs e)
        {
            SetView("Default");
        }
        #endregion

        #region DisplayComment
        private void DisplayComment()
        {
            HelpDeskDALDataContext objHelpDeskDALDataContext = new HelpDeskDALDataContext();

            var objHelpDesk_TaskDetail = (from HelpDesk_TaskDetails in objHelpDeskDALDataContext.HelpDesk_TaskDetails
                                              where HelpDesk_TaskDetails.DetailID == Convert.ToInt32(lblDetailID.Text)
                                              select HelpDesk_TaskDetails).FirstOrDefault();

            if (objHelpDesk_TaskDetail != null)
            {
                txtDescription.Text = objHelpDesk_TaskDetail.Description;
                lblDisplayUser.Text = GetUserName(objHelpDesk_TaskDetail.UserID);
                txtStartDayEdit.Text = objHelpDesk_TaskDetail.StartTime.Value.ToShortDateString();
                txtStopDayEdit.Text = objHelpDesk_TaskDetail.StopTime.Value.ToShortDateString();
                txtStartTimeEdit.Text = objHelpDesk_TaskDetail.StartTime.Value.ToShortTimeString();
                txtStopTimeEdit.Text = objHelpDesk_TaskDetail.StopTime.Value.ToShortTimeString();
                lblInsertDate.Text = String.Format("{0} {1}", objHelpDesk_TaskDetail.InsertDate.ToLongDateString(), objHelpDesk_TaskDetail.InsertDate.ToLongTimeString());
            }
        }
        #endregion

        #region lnkDelete_Click
        protected void lnkDelete_Click(object sender, EventArgs e)
        {
            HelpDeskDALDataContext objHelpDeskDALDataContext = new HelpDeskDALDataContext();

            var objHelpDesk_TaskDetail = (from HelpDesk_TaskDetails in objHelpDeskDALDataContext.HelpDesk_TaskDetails
                                              where HelpDesk_TaskDetails.DetailID == Convert.ToInt32(lblDetailID.Text)
                                              select HelpDesk_TaskDetails).FirstOrDefault();

            // Delete the Record
            objHelpDeskDALDataContext.HelpDesk_TaskDetails.DeleteOnSubmit(objHelpDesk_TaskDetail);
            objHelpDeskDALDataContext.SubmitChanges();

            // Insert Log
            Log.InsertLog(TaskID, UserId, String.Format(Localization.GetString("DeletedWorkComment.Text", LocalResourceFile), GetUserName(), txtDescription.Text));

            SetView("Default");
            gvComments.DataBind();
        }
        #endregion

        #region lnkUpdate_Click
        protected void lnkUpdate_Click(object sender, EventArgs e)
        {
            UpdateComment();
        }
        #endregion

        #region UpdateComment
        private void UpdateComment()
        {
            try
            {
                // Try to Make Start and Stop Time
                DateTime StartTime = Convert.ToDateTime(String.Format("{0} {1}", txtStartDayEdit.Text, txtStartTimeEdit.Text));
                DateTime StopTime = Convert.ToDateTime(String.Format("{0} {1}", txtStopDayEdit.Text, txtStopTimeEdit.Text));
            }
            catch
            {
                lblErrorEditComment.Text = Localization.GetString("MustProvideValidStarAndStopTimes.Text", LocalResourceFile);
                return;
            }

            if (txtDescription.Text.Trim().Length > 0)
            {
                HelpDeskDALDataContext objHelpDeskDALDataContext = new HelpDeskDALDataContext();

                string strComment = txtDescription.Text.Trim();

                // Save Task Details
                var objHelpDesk_TaskDetail = (from HelpDesk_TaskDetails in objHelpDeskDALDataContext.HelpDesk_TaskDetails
                                                  where HelpDesk_TaskDetails.DetailID == Convert.ToInt32(lblDetailID.Text)
                                                  select HelpDesk_TaskDetails).FirstOrDefault();

                if (objHelpDesk_TaskDetail != null)
                {

                    objHelpDesk_TaskDetail.TaskID = TaskID;
                    objHelpDesk_TaskDetail.Description = txtDescription.Text.Trim();
                    objHelpDesk_TaskDetail.UserID = UserId;
                    objHelpDesk_TaskDetail.DetailType = "Work";
                    objHelpDesk_TaskDetail.StartTime = Convert.ToDateTime(String.Format("{0} {1}", txtStartDayEdit.Text, txtStartTimeEdit.Text));
                    objHelpDesk_TaskDetail.StopTime = Convert.ToDateTime(String.Format("{0} {1}", txtStopDayEdit.Text, txtStopTimeEdit.Text));

                    objHelpDeskDALDataContext.SubmitChanges();
                    txtDescription.Text = "";

                    // Insert Log
					Log.InsertLog(TaskID, UserId, String.Format(Localization.GetString ("UpdatedWorkComment.Text", LocalResourceFile), GetUserName()));

                    SetView("Default");
                    gvComments.DataBind();
                }
            }
            else
            {
                lblErrorEditComment.Text = Localization.GetString("MustProvideADescription.Text", LocalResourceFile);
            }
        }
        #endregion

        // Utility

        #region GetAssignedRole
        private int GetAssignedRole()
        {
            int intRole = -1;

            HelpDeskDALDataContext objHelpDeskDALDataContext = new HelpDeskDALDataContext();
            var result = from HelpDesk_TaskDetails in objHelpDeskDALDataContext.HelpDesk_Tasks
                         where HelpDesk_TaskDetails.TaskID == Convert.ToInt32(Request.QueryString["TaskID"])
                         select HelpDesk_TaskDetails;

            if (result != null)
            {
                intRole = result.FirstOrDefault().AssignedRoleID;
            }

            return intRole;
        }
        #endregion

        #region GetDescriptionOfTicket
        private string GetDescriptionOfTicket()
        {
            string strDescription = "";
            int intTaskId = Convert.ToInt32(Request.QueryString["TaskID"]);

            HelpDeskDALDataContext objHelpDeskDALDataContext = new HelpDeskDALDataContext();
            var result = (from HelpDesk_TaskDetails in objHelpDeskDALDataContext.HelpDesk_Tasks
                          where HelpDesk_TaskDetails.TaskID == Convert.ToInt32(Request.QueryString["TaskID"])
                          select HelpDesk_TaskDetails).FirstOrDefault();

            if (result != null)
            {
                strDescription = result.Description;
            }

            return strDescription;
        }
        #endregion

        #region GetSettings
        private List<HelpDesk_Setting> GetSettings()
        {
            // Get Settings
            HelpDeskDALDataContext objHelpDeskDALDataContext = new HelpDeskDALDataContext();

            List<HelpDesk_Setting> colHelpDesk_Setting = (from HelpDesk_Settings in objHelpDeskDALDataContext.HelpDesk_Settings
                                                                  where HelpDesk_Settings.PortalID == PortalId
                                                                  select HelpDesk_Settings).ToList();

            if (colHelpDesk_Setting.Count == 0)
            {
                // Create Default vaules
                HelpDesk_Setting objHelpDesk_Setting1 = new HelpDesk_Setting();

                objHelpDesk_Setting1.PortalID = PortalId;
                objHelpDesk_Setting1.SettingName = "AdminRole";
                objHelpDesk_Setting1.SettingValue = "Administrators";

                objHelpDeskDALDataContext.HelpDesk_Settings.InsertOnSubmit(objHelpDesk_Setting1);
                objHelpDeskDALDataContext.SubmitChanges();

                HelpDesk_Setting objHelpDesk_Setting2 = new HelpDesk_Setting();

                objHelpDesk_Setting2.PortalID = PortalId;
                objHelpDesk_Setting2.SettingName = "UploFilesPath";
				objHelpDesk_Setting2.SettingValue = Server.MapPath("~/DesktopModules/R7.HelpDesk/R7.HelpDesk/Upload");

                objHelpDeskDALDataContext.HelpDesk_Settings.InsertOnSubmit(objHelpDesk_Setting2);
                objHelpDeskDALDataContext.SubmitChanges();

                colHelpDesk_Setting = (from HelpDesk_Settings in objHelpDeskDALDataContext.HelpDesk_Settings
                                           where HelpDesk_Settings.PortalID == PortalId
                                           select HelpDesk_Settings).ToList();
            }

            // Upload Permission
            HelpDesk_Setting UploadPermissionHelpDesk_Setting = (from HelpDesk_Settings in objHelpDeskDALDataContext.HelpDesk_Settings
                                                                         where HelpDesk_Settings.PortalID == PortalId
                                                                         where HelpDesk_Settings.SettingName == "UploadPermission"
                                                                         select HelpDesk_Settings).FirstOrDefault();

            if (UploadPermissionHelpDesk_Setting != null)
            {
                // Add to collection
                colHelpDesk_Setting.Add(UploadPermissionHelpDesk_Setting);
            }
            else
            {
                // Add Default value
                HelpDesk_Setting objHelpDesk_Setting = new HelpDesk_Setting();
                objHelpDesk_Setting.SettingName = "UploadPermission";
                objHelpDesk_Setting.SettingValue = "All";
                objHelpDesk_Setting.PortalID = PortalId;
                objHelpDeskDALDataContext.HelpDesk_Settings.InsertOnSubmit(objHelpDesk_Setting);
                objHelpDeskDALDataContext.SubmitChanges();

                // Add to collection
                colHelpDesk_Setting.Add(objHelpDesk_Setting);
            }

            return colHelpDesk_Setting;
        }
        #endregion

        #region GetAdminRole
        private string GetAdminRole()
        {
            HelpDeskDALDataContext objHelpDeskDALDataContext = new HelpDeskDALDataContext();

            List<HelpDesk_Setting> colHelpDesk_Setting = (from HelpDesk_Settings in objHelpDeskDALDataContext.HelpDesk_Settings
                                                                  where HelpDesk_Settings.PortalID == PortalId
                                                                  select HelpDesk_Settings).ToList();

            HelpDesk_Setting objHelpDesk_Setting = colHelpDesk_Setting.Where(x => x.SettingName == "AdminRole").FirstOrDefault();

            string strAdminRoleID = "Administrators";
            if (objHelpDesk_Setting != null)
            {
                strAdminRoleID = objHelpDesk_Setting.SettingValue;
            }

            return strAdminRoleID;
        }
        #endregion
    }
}