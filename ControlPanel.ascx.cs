/*
' Copyright (c) 2016 Oliver Hine, nBrane llc
'  All rights reserved.
' 
' THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED
' TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL
' THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF
' CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
' DEALINGS IN THE SOFTWARE.
' 
*/

using System;
using DotNetNuke.Services.Exceptions;
using DotNetNuke.Framework.JavaScriptLibraries;
using DotNetNuke.Framework;
using DotNetNuke.Web.Client.ClientResourceManagement;
using DotNetNuke.Web.Client;
using DotNetNuke.UI.ControlPanels;
using DotNetNuke.Entities.Portals;
using DotNetNuke.Services.OutputCache;
using DotNetNuke.Entities.Host;

namespace nBrane.Modules.AdministrationSuite
{
    public partial class ControlPanel : ControlPanelBase
    {
        public override bool IsDockable { get; set; }

        public override bool IncludeInControlHierarchy
        {
            get
            {
                return IsUserImpersonated() || (IsPageAdmin() || IsModuleAdmin());
            }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            try
            {
                if (IsPageAdmin() || IsModuleAdmin() || IsUserImpersonated())
                {
                    ServicesFramework.Instance.RequestAjaxAntiForgerySupport();

                    JavaScript.RequestRegistration(CommonJs.jQuery);
                    JavaScript.RequestRegistration(CommonJs.Knockout);

                    if (PortalSettings.UserMode != PortalSettings.Mode.View)
                        JavaScript.RequestRegistration(CommonJs.DnnPlugins);

                    ClientResourceManager.RegisterStyleSheet(Page, "//fonts.googleapis.com/css?family=Montserrat", FileOrder.Css.ModuleCss);
                    ClientResourceManager.RegisterStyleSheet(Page, "//maxcdn.bootstrapcdn.com/font-awesome/4.5.0/css/font-awesome.min.css", FileOrder.Css.ModuleCss);
                    ClientResourceManager.RegisterStyleSheet(Page, "~/desktopmodules/nbrane/administrationsuite/stylesheet.css", FileOrder.Css.ModuleCss);

                    ClientResourceManager.RegisterScript(Page, "//" + PortalSettings.PortalAlias.HTTPAlias.Replace("http://", string.Empty).Replace("https://", string.Empty) + "/desktopmodules/nbrane/administrationsuite/api/controlpanel/loadjs?name=main", FileOrder.Js.DefaultPriority);
                    ClientResourceManager.RegisterScript(Page, "//cdnjs.cloudflare.com/ajax/libs/jquery.blockUI/2.66.0-2013.10.09/jquery.blockUI.min.js", FileOrder.Js.DefaultPriority);
                }
            }
            catch (Exception exc) //Module failed to load
            {
                Exceptions.ProcessModuleLoadException(this, exc);
            }
        }

        public bool IsUserImpersonated()
        {
            return Components.Common.IsUserImpersonated();
        }

        public bool ShowCachePanel()
        {
            var tabConfigCount = 0;
            var globalConfigCount = 0;
            var activeTab = PortalSettings.ActiveTab;

            var currentOutputCacheSetting = activeTab.TabSettings["CacheProvider"] == null ? string.Empty : activeTab.TabSettings["CacheProvider"].ToString();
            if (!string.IsNullOrWhiteSpace(currentOutputCacheSetting))
            {
                tabConfigCount = OutputCachingProvider.Instance(currentOutputCacheSetting).GetItemCount(activeTab.TabID);
            }

            if (Host.PageCachingMethod != currentOutputCacheSetting)
            {
                globalConfigCount = OutputCachingProvider.Instance(Host.PageCachingMethod).GetItemCount(activeTab.TabID);
            }

            if (globalConfigCount > 0 || tabConfigCount > 0)
            {
                return true;
            }

            return false;
        }
    }
}