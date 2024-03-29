/*
 * Copyright 2007-2012 Alfresco Software Limited.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 *
 * This file is part of an unsupported extension to Alfresco.
 *
 * [BRIEF DESCRIPTION OF FILE CONTENTS]
 */
using System;
using System.Drawing;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using Microsoft.VisualStudio.Tools.Applications.Runtime;
using Excel = Microsoft.Office.Interop.Excel;
using Office = Microsoft.Office.Core;
using System.Text;

namespace AlfrescoExcel2003
{
   public partial class ThisAddIn
   {
      private const string COMMANDBAR_NAME = "Alfresco";
      private const string COMMANDBAR_BUTTON_CAPTION = "Alfresco";
      private const string COMMANDBAR_BUTTON_DESCRIPTION = "Show/hide the Alfresco Add-In window";
      private const string COMMANDBAR_HELP_DESCRIPTION = "Alfresco Add-In online help";

      private AlfrescoPane m_AlfrescoPane;
      private string m_DefaultTemplate = "wcservice/office/";
      private string m_helpUrl = null;
      private AppWatcher m_AppWatcher;
      private Office.CommandBar m_CommandBar;
      private Office.CommandBarButton m_AlfrescoButton;
      private Office.CommandBarButton m_HelpButton;

      private void ThisAddIn_Startup(object sender, System.EventArgs e)
      {
         #region VSTO generated code

         this.Application = (Excel.Application)Microsoft.Office.Tools.Excel.ExcelLocale1033Proxy.Wrap(typeof(Excel.Application), this.Application);

         #endregion

         m_DefaultTemplate = Properties.Settings.Default.DefaultTemplate;

         // Register event interest with the Excel Application
         Application.WorkbookBeforeClose += new Microsoft.Office.Interop.Excel.AppEvents_WorkbookBeforeCloseEventHandler(Application_WorkbookBeforeClose);
         Application.WorkbookActivate += new Microsoft.Office.Interop.Excel.AppEvents_WorkbookActivateEventHandler(Application_WorkbookActivate);
         Application.WorkbookDeactivate += new Microsoft.Office.Interop.Excel.AppEvents_WorkbookDeactivateEventHandler(Application_WorkbookDeactivate);

         // Excel doesn't raise an event when it loses or gains app focus
         m_AppWatcher = new AppWatcher();
         m_AppWatcher.OnWindowHasFocus += new WindowHasFocus(AppWatcher_OnWindowHasFocus);
         m_AppWatcher.OnWindowLostFocus += new WindowLostFocus(AppWatcher_OnWindowLostFocus);
         m_AppWatcher.Start(Application.Hwnd);

         // Add the button to the Office toolbar
         AddToolbar();
      }

      void AppWatcher_OnWindowHasFocus(int hwnd)
      {
         if (m_AlfrescoPane != null)
         {
            m_AlfrescoPane.OnWindowActivate();
         }
      }

      void AppWatcher_OnWindowLostFocus(int hwnd)
      {
         if (m_AlfrescoPane != null)
         {
            m_AlfrescoPane.OnWindowDeactivate();
         }
      }

      private void ThisAddIn_Shutdown(object sender, System.EventArgs e)
      {
         SaveToolbarPosition();
         CloseAlfrescoPane();
         m_AlfrescoPane = null;
      }

      #region VSTO generated code

      /// <summary>
      /// Required method for Designer support - do not modify
      /// the contents of this method with the code editor.
      /// </summary>
      private void InternalStartup()
      {
         this.Startup += new System.EventHandler(ThisAddIn_Startup);
         this.Shutdown += new System.EventHandler(ThisAddIn_Shutdown);
      }

      #endregion

      /// <summary>
      /// Adds commandBars to Excel Application
      /// </summary>
      private void AddToolbar()
      {
         // VSTO API uses object-wrapped booleans
         object falseValue = false;
         object trueValue = true;

         // Try to get a handle to an existing COMMANDBAR_NAME CommandBar
         try
         {
            m_CommandBar = Application.CommandBars[COMMANDBAR_NAME];

            // If we found the CommandBar, then it's a permanent one we need to delete
            // Note: if the user has manually created a toolbar called COMMANDBAR_NAME it will get torched here
            if (m_CommandBar != null)
            {
               m_CommandBar.Delete();
            }
         }
         catch
         {
            // Benign - the CommandBar didn't exist
         }

         // Create a temporary CommandBar named COMMANDBAR_NAME
         m_CommandBar = Application.CommandBars.Add(COMMANDBAR_NAME, Office.MsoBarPosition.msoBarTop, falseValue, trueValue);

         if (m_CommandBar != null)
         {
            // Load any saved toolbar position
            LoadToolbarPosition();

            // Add our button to the command bar (as a temporary control) and an event handler.
            m_AlfrescoButton = (Office.CommandBarButton)m_CommandBar.Controls.Add(Office.MsoControlType.msoControlButton, missing, missing, missing, trueValue);
            if (m_AlfrescoButton != null)
            {
               m_AlfrescoButton.Style = Office.MsoButtonStyle.msoButtonIconAndCaption;
               m_AlfrescoButton.Caption = COMMANDBAR_BUTTON_CAPTION;
               m_AlfrescoButton.DescriptionText = COMMANDBAR_BUTTON_DESCRIPTION;
               m_AlfrescoButton.TooltipText = COMMANDBAR_BUTTON_DESCRIPTION;
               Bitmap bmpButton = new Bitmap(GetType(), "toolbar.ico");
               m_AlfrescoButton.Picture = new ToolbarPicture(bmpButton);
               Bitmap bmpMask = new Bitmap(GetType(), "toolbar_mask.ico");
               m_AlfrescoButton.Mask = new ToolbarPicture(bmpMask);
               m_AlfrescoButton.Tag = "AlfrescoButton";

               // Finally add the event handler and make sure the button is visible
               m_AlfrescoButton.Click += new Microsoft.Office.Core._CommandBarButtonEvents_ClickEventHandler(m_AlfrescoButton_Click);
            }

            // Add the help button to the command bar (as a temporary control) and an event handler.
            m_HelpButton = (Office.CommandBarButton)m_CommandBar.Controls.Add(Office.MsoControlType.msoControlButton, missing, missing, missing, trueValue);
            if (m_HelpButton != null)
            {
               m_HelpButton.Style = Office.MsoButtonStyle.msoButtonIcon;
               m_HelpButton.DescriptionText = COMMANDBAR_HELP_DESCRIPTION;
               m_HelpButton.TooltipText = COMMANDBAR_HELP_DESCRIPTION;
               m_HelpButton.FaceId = 984;
               m_HelpButton.Tag = "AlfrescoHelpButton";

               // Finally add the event handler and make sure the button is visible
               m_HelpButton.Click += new Microsoft.Office.Core._CommandBarButtonEvents_ClickEventHandler(m_HelpButton_Click);
            }

            // We need to find this toolbar later, so protect it from user changes
            m_CommandBar.Protection = Microsoft.Office.Core.MsoBarProtection.msoBarNoCustomize;
            m_CommandBar.Visible = true;
         }
      }

      /// <summary>
      /// Save CommandBar position
      /// </summary>
      private void SaveToolbarPosition()
      {
         if (m_CommandBar != null)
         {
            Properties.Settings.Default.ToolbarPosition = (int)m_CommandBar.Position;
            Properties.Settings.Default.ToolbarRowIndex = m_CommandBar.RowIndex;
            Properties.Settings.Default.ToolbarTop = m_CommandBar.Top;
            Properties.Settings.Default.ToolbarLeft = m_CommandBar.Left;
            Properties.Settings.Default.ToolbarSaved = true;
            Properties.Settings.Default.Save();
         }
      }

      /// <summary>
      /// Load CommandBar position
      /// </summary>
      private void LoadToolbarPosition()
      {
         if (m_CommandBar != null)
         {
            if (Properties.Settings.Default.ToolbarSaved)
            {
               m_CommandBar.Position = (Microsoft.Office.Core.MsoBarPosition)Properties.Settings.Default.ToolbarPosition;
               m_CommandBar.RowIndex = Properties.Settings.Default.ToolbarRowIndex;
               m_CommandBar.Top = Properties.Settings.Default.ToolbarTop;
               m_CommandBar.Left = Properties.Settings.Default.ToolbarLeft;
            }
         }
      }

      /// <summary>
      /// Alfresco toolbar button event handler
      /// </summary>
      /// <param name="Ctrl"></param>
      /// <param name="CancelDefault"></param>
      void m_AlfrescoButton_Click(Office.CommandBarButton Ctrl, ref bool CancelDefault)
      {

         if (m_AlfrescoPane != null)
         {
            m_AlfrescoPane.OnToggleVisible();
         }
         else
         {
            OpenAlfrescoPane(true);
         }
      }

      /// <summary>
      /// Help toolbar button event handler
      /// </summary>
      /// <param name="Ctrl"></param>
      /// <param name="CancelDefault"></param>
      void m_HelpButton_Click(Microsoft.Office.Core.CommandBarButton Ctrl, ref bool CancelDefault)
      {
         if (m_helpUrl == null)
         {
            StringBuilder helpUrl = new StringBuilder(Properties.Resources.HelpURL);
            Assembly asm = Assembly.GetExecutingAssembly();
            Version version = asm.GetName().Version;
            string edition = "Community";
            object[] attr = asm.GetCustomAttributes(typeof(AssemblyConfigurationAttribute), false);
            if (attr.Length > 0)
            {
               AssemblyConfigurationAttribute aca = (AssemblyConfigurationAttribute)attr[0];
               edition = aca.Configuration;
            }
            helpUrl.Replace("{major}", version.Major.ToString());
            helpUrl.Replace("{minor}", version.Minor.ToString());
            helpUrl.Replace("{edition}", edition);

            m_helpUrl = helpUrl.ToString();
         }
         System.Diagnostics.Process.Start(m_helpUrl);
      }

      /// <summary>
      /// Fired when active workbook changes
      /// </summary>
      void Application_WorkbookActivate(Microsoft.Office.Interop.Excel.Workbook Wb)
      {
         if (m_AlfrescoPane != null)
         {
            m_AlfrescoPane.OnDocumentChanged();
         }
      }

      /// <summary>
      /// Fired when active workbook changes
      /// </summary>
      void Application_WorkbookDeactivate(Microsoft.Office.Interop.Excel.Workbook Wb)
      {
         if (m_AlfrescoPane != null)
         {
            m_AlfrescoPane.OnDocumentBeforeClose();
         }
      }

      /// <summary>
      /// Fired when Excel Application becomes the active window
      /// </summary>
      /// <param name="Doc"></param>
      /// <param name="Wn"></param>
      void Application_WindowActivate(Microsoft.Office.Interop.Excel.Workbook Wb, Microsoft.Office.Interop.Excel.Window Wn)
      {
         if (m_AlfrescoPane != null)
         {
            m_AlfrescoPane.OnWindowActivate();
         }
      }

      /// <summary>
      /// Fired when the Excel Application loses focus
      /// </summary>
      /// <param name="Doc"></param>
      /// <param name="Wn"></param>
      void Application_WindowDeactivate(Microsoft.Office.Interop.Excel.Workbook Wb, Microsoft.Office.Interop.Excel.Window Wn)
      {
         if (m_AlfrescoPane != null)
         {
            m_AlfrescoPane.OnWindowDeactivate();
         }
      }

      /// <summary>
      /// Fired as a workbook is being closed
      /// </summary>
      /// <param name="Doc"></param>
      /// <param name="Cancel"></param>
      void Application_WorkbookBeforeClose(Microsoft.Office.Interop.Excel.Workbook Wb, ref bool Cancel)
      {
         if (m_AlfrescoPane != null)
         {
            m_AlfrescoPane.OnDocumentBeforeClose();
         }
      }

      public void OpenAlfrescoPane()
      {
         OpenAlfrescoPane(true);
      }
      public void OpenAlfrescoPane(bool Show)
      {
         if (m_AlfrescoPane == null)
         {
            m_AlfrescoPane = new AlfrescoPane();
            m_AlfrescoPane.DefaultTemplate = m_DefaultTemplate;
            m_AlfrescoPane.ExcelApplication = Application;
         }

         if (Show)
         {
            m_AlfrescoPane.Show();
            if (Application.Workbooks.Count > 0)
            {
               m_AlfrescoPane.showDocumentDetails();
            }
            else
            {
               m_AlfrescoPane.showHome(false);
            }
            m_AppWatcher.AlfrescoWindow = m_AlfrescoPane.Handle.ToInt32();
         }
      }

      public void CloseAlfrescoPane()
      {
         if (m_AlfrescoPane != null)
         {
            m_AlfrescoPane.Hide();
         }
      }
   }
}
