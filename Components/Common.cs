using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Web;

using DotNetNuke.Common;
using DotNetNuke.Common.Utilities;
using DotNetNuke.Entities.Host;
using DotNetNuke.Entities.Modules;
using DotNetNuke.Entities.Portals;
using DotNetNuke.Entities.Tabs;
using DotNetNuke.Entities.Users;
using DotNetNuke.Framework;
using DotNetNuke.Security;
using DotNetNuke.Security.Permissions;
using DotNetNuke.Services.Exceptions;
using DotNetNuke.Services.Log.EventLog;
using DotNetNuke.UI.Skins;

namespace nBrane.Modules.AdministrationSuite.Components
{
    static class Common
    {
        internal static void SetCookie(this HttpResponseHeaders headers, Cookie cookie)
        {
            var cookieBuilder = new StringBuilder(HttpUtility.UrlEncode(cookie.Name) + "=" + HttpUtility.UrlEncode(cookie.Value));
            if (cookie.HttpOnly)
            {
                cookieBuilder.Append("; HttpOnly");
            }

            if (cookie.Secure)
            {
                cookieBuilder.Append("; Secure");
            }

            headers.Add("Set-Cookie", cookieBuilder.ToString());
        }

        internal static TabInfo GetParentTab(TabInfo relativeToTab, DTO.PagePositionMode location)
        {
            if (relativeToTab == null)
            {
                return null;
            }

            TabInfo parentTab = null;

            if (location == DTO.PagePositionMode.ChildOf)
            {
                parentTab = relativeToTab;
            }
            else if ((relativeToTab != null) && relativeToTab.ParentId != Null.NullInteger)
            {
                var tabCtrl = new TabController();
                parentTab = tabCtrl.GetTab(relativeToTab.ParentId, relativeToTab.PortalID, false);
            }

            return parentTab;
        }

        internal static List<DTO.GenericSelectableListItem> ListContainers(string HostOrSite, string SkinOrContainer)
        {
            var apiResponse = new List<DTO.GenericSelectableListItem>();

            try
            {
                string strRoot = string.Empty;
                string strFolder = null;
                string[] arrFolders = null;
                string strFile = null;
                string[] arrFiles = null;
                string strLastFolder = string.Empty;
                string strSeparator = "----------------------------------------";

                string dbPrefix = string.Empty;
                string currentSetting = string.Empty;

                switch (HostOrSite.ToLower())
                {
                    case "host":
                        if (SkinOrContainer.ToLower() == "skin")
                        {
                            strRoot = Globals.HostMapPath + SkinController.RootSkin;
                            dbPrefix = "[G]" + SkinController.RootSkin;
                        }
                        else
                        {
                            strRoot = Globals.HostMapPath + SkinController.RootContainer;
                            dbPrefix = "[G]" + SkinController.RootContainer;
                        }
                        break;
                    case "site":
                        if (SkinOrContainer.ToLower() == "skin")
                        {
                            strRoot = PortalSettings.Current.HomeDirectoryMapPath + SkinController.RootSkin;
                            dbPrefix = "[L]" + SkinController.RootSkin;
                        }
                        else
                        {
                            strRoot = PortalSettings.Current.HomeDirectoryMapPath + SkinController.RootContainer;
                            dbPrefix = "[L]" + SkinController.RootContainer;
                        }
                        break;
                }

                var siteDefault  = string.Empty;
                if (SkinOrContainer.ToLower() == "skin")
                {
                    var currentDefault = PortalSettings.Current.ActiveTab.SkinSrc;
                    if (string.IsNullOrWhiteSpace(currentDefault))
                    {
                        currentDefault = PortalSettings.Current.DefaultPortalSkin;
                    }

                    siteDefault = GetFriendySkinName(PortalSettings.Current.DefaultPortalSkin);
                    currentSetting = GetFriendySkinName(currentDefault);
                }
                else
                {
                    var currentDefault = PortalSettings.Current.ActiveTab.ContainerSrc;
                    if (string.IsNullOrWhiteSpace(currentDefault))
                    {
                        currentDefault = PortalSettings.Current.DefaultPortalContainer;
                    }

                    siteDefault = GetFriendySkinName(PortalSettings.Current.DefaultPortalContainer);
                    currentSetting = GetFriendySkinName(currentDefault);
                }

                if (string.IsNullOrEmpty(strRoot) == false && Directory.Exists(strRoot))
                {
                    apiResponse = new List<DTO.GenericSelectableListItem>();
                    arrFolders = Directory.GetDirectories(strRoot);
                    foreach (string strFolder_loopVariable in arrFolders)
                    {
                        strFolder = strFolder_loopVariable;
                        arrFiles = Directory.GetFiles(strFolder, "*.ascx");
                        foreach (string strFile_loopVariable in arrFiles)
                        {
                            strFile = strFile_loopVariable;
                            strFolder = strFolder.Substring(strFolder.LastIndexOf("\\") + 1);

                            //if (strLastFolder != strFolder)
                            //{
                            //    if (string.IsNullOrEmpty(strLastFolder) == false)
                            //    {
                            //        apiResponse.Add(new DTO.GenericSelectableListItem(strSeparator, "", false));
                            //    }
                            //    strLastFolder = strFolder;
                            //}
                            string skinName = FormatSkinName(strFolder, Path.GetFileNameWithoutExtension(strFile)).Replace("_", " ");
                            bool isSelected = skinName == currentSetting ? true : false;

                            apiResponse.Add(new DTO.GenericSelectableListItem(skinName, dbPrefix + "/" + strFolder + "/" + Path.GetFileName(strFile), isSelected));
                        }
                    }
                }


                if (apiResponse.Count > 0)
                {
                    apiResponse.Insert(0, new DTO.GenericSelectableListItem(strSeparator, "", false));
                    apiResponse.Insert(0, new DTO.GenericSelectableListItem("Default - " + siteDefault, "-1", false));
                }
                else
                {
                    apiResponse.Insert(0, new DTO.GenericSelectableListItem("ContainerNoneAvailable", "-1", false));
                }
            }
            catch (Exception err)
            {
                Exceptions.LogException(err);
            }

            return apiResponse;
        }

        internal static string GetFriendySkinName(string param)
        {
            if (!string.IsNullOrWhiteSpace(param))
            {
                param = SkinController.FormatSkinSrc(param, PortalSettings.Current);

                return Path.GetDirectoryName(param).Split(Path.DirectorySeparatorChar).Last() + " - " + Path.GetFileNameWithoutExtension(param).Replace("_", " ");
            }

            return null;
        }

        internal static string FormatSkinName(string strSkinFolder, string strSkinFile)
        {
            if (strSkinFolder.ToLower() == "_default")
            {
                // host folder
                return strSkinFile;
            }
            else
            {
                // portal folder
                switch (strSkinFile.ToLower())
                {
                    case "skin":
                    case "container":
                    case "default":
                        return strSkinFolder;
                    default:
                        return strSkinFolder + " - " + strSkinFile;
                }
            }
        }

        internal static ModulePermissionInfo AddModulePermission(ModuleInfo objModule, PermissionInfo permission, int roleId, int userId, bool allowAccess)
        {
            var objModulePermission = new ModulePermissionInfo();
            objModulePermission.ModuleID = objModule.ModuleID;
            objModulePermission.PermissionID = permission.PermissionID;
            objModulePermission.RoleID = roleId;
            objModulePermission.UserID = userId;
            objModulePermission.PermissionKey = permission.PermissionKey;
            objModulePermission.AllowAccess = allowAccess;

            // add the permission to the collection
            if (!objModule.ModulePermissions.Contains(objModulePermission))
            {
                objModule.ModulePermissions.Add(objModulePermission);
            }

            return objModulePermission;
        }


        internal static bool CanUserImpersonateOtherUsers()
        {
            var objCurrentUser = UserController.Instance.GetCurrentUserInfo();

            if (objCurrentUser.IsSuperUser || objCurrentUser.IsInRole(PortalSettings.Current.AdministratorRoleName) || IsUserImpersonated())
            {
                return true;
            }

            return false;
        }

        internal static bool IsUserImpersonated()
        {

            if (HttpContext.Current.Request.Cookies[impersonationCookieKey] != null)
            {
                string cookieValue = GetUserImpersonationCookie();
                var cookieArray = cookieValue.Split(':');

                int originalUserId = -1;
                int targetUserId = -1;

                int.TryParse(cookieArray.First(), out originalUserId);
                int.TryParse(cookieArray.Last(), out targetUserId);

                if (originalUserId == 0)
                    originalUserId = -1;

                if (targetUserId == 0)
                    targetUserId = -1;

                if (!HttpContext.Current.Request.IsAuthenticated && (PortalSettings.Current.UserId != originalUserId && PortalSettings.Current.UserId != targetUserId))
                {
                    //if the request isn't authenticated, we need to clear our cookie
                    ClearUserImpersonationCookie();
                    return false;
                }
                return true;
            }
            return false;
        }

        internal const string impersonationCookieKey = "nbrane-user-impersonation";

        internal static Cookie ClearUserImpersonationCookie()
        {
            //reset the cookie value
            var cookie = new Cookie(impersonationCookieKey, string.Empty);
            cookie.Expires = DateTime.Now.AddDays(-1);

            return cookie;
        }

        internal static string GetUserImpersonationCookie()
        {
            string functionReturnValue = null;
            functionReturnValue = Null.NullString;

            var objPortalSecurity = new PortalSecurity();

            if (HttpContext.Current.Request.Cookies[impersonationCookieKey] != null)
            {
                string cookieValue = objPortalSecurity.Decrypt(Host.GUID.ToString(), HttpContext.Current.Request.Cookies[impersonationCookieKey].Value);

                functionReturnValue = cookieValue;
            }

            return functionReturnValue;
        }

        internal static int AddExistingModule(int moduleId, int tabId, string paneName, int position, string align, string container)
        {
            var objModules = new ModuleController();
            var objEventLog = new EventLogController();

            int UserId = PortalSettings.Current.UserId;

            var objModule = objModules.GetModule(moduleId, tabId, false);
            if (objModule != null)
            {
                // clone the module object ( to avoid creating an object reference to the data cache )
                var objClone = objModule.Clone();
                objClone.TabID = PortalSettings.Current.ActiveTab.TabID;
                objClone.ModuleOrder = position;
                objClone.PaneName = paneName;
                objClone.Alignment = align;
                objClone.ContainerSrc = container;

                int iNewModuleId = objModules.AddModule(objClone);
                //objEventLog.AddLog(objClone, PortalSettings.Current, UserId, "", DotNetNuke.Services.Log.EventLog.EventLogController.EventLogType.MODULE_CREATED);

                return iNewModuleId;
            }

            return -1;
        }

        internal static void AddModuleCopy(int iModuleId, int iTabId, int iOrderPosition, string sPaneName, string container)
        {
            var objModules = new ModuleController();
            var objModule = objModules.GetModule(iModuleId, iTabId, false);
            if (objModule != null)
            {
                //Clone module as it exists in the cache and changes we make will update the cached object
                var newModule = objModule.Clone();

                newModule.ModuleID = Null.NullInteger;
                newModule.TabID = PortalSettings.Current.ActiveTab.TabID;
                newModule.ModuleTitle = "Copy of " + objModule.ModuleTitle;
                newModule.ModuleOrder = iOrderPosition;
                newModule.PaneName = sPaneName;
                newModule.ContainerSrc = container;
                newModule.ModuleID = objModules.AddModule(newModule);

                if (string.IsNullOrEmpty(newModule.DesktopModule.BusinessControllerClass) == false)
                {
                    object objObject = Reflection.CreateObject(newModule.DesktopModule.BusinessControllerClass, newModule.DesktopModule.BusinessControllerClass);
                    if (objObject is IPortable)
                    {
                        string Content = Convert.ToString(((IPortable)objObject).ExportModule(iModuleId));
                        if (string.IsNullOrEmpty(Content) == false)
                        {
                            ((IPortable)objObject).ImportModule(newModule.ModuleID, Content, newModule.DesktopModule.Version, PortalSettings.Current.UserId);
                        }
                    }
                }
            }
        }
    }
}
