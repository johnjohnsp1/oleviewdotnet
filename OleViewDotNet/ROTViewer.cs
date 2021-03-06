﻿//    This file is part of OleViewDotNet.
//    Copyright (C) James Forshaw 2014
//
//    OleViewDotNet is free software: you can redistribute it and/or modify
//    it under the terms of the GNU General Public License as published by
//    the Free Software Foundation, either version 3 of the License, or
//    (at your option) any later version.
//
//    OleViewDotNet is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//    GNU General Public License for more details.
//
//    You should have received a copy of the GNU General Public License
//    along with OleViewDotNet.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.ComTypes;
using System.Windows.Forms;

namespace OleViewDotNet
{
    public partial class ROTViewer : UserControl
    {
        private COMRegistry m_reg;

        struct MonikerInfo
        {
            public string strDisplayName;
            public Guid clsid;
            public IMoniker moniker;

            public MonikerInfo(string name, Guid guid, IMoniker mon)
            {
                strDisplayName = name;
                clsid = guid;
                moniker = mon;
            }
        }

        public ROTViewer(COMRegistry reg)
        {
            m_reg = reg;
            InitializeComponent();
        }

        void LoadROT()
        {
            IBindCtx bindCtx;

            listViewROT.Items.Clear();
            try
            {
                bindCtx = COMUtilities.CreateBindCtx(0);                
                IRunningObjectTable rot;
                IEnumMoniker enumMoniker;
                IMoniker[] moniker = new IMoniker[1];                    

                bindCtx.GetRunningObjectTable(out rot);
                rot.EnumRunning(out enumMoniker);
                while (enumMoniker.Next(1, moniker, IntPtr.Zero) == 0)
                {
                    string strDisplayName;
                    Guid clsid;

                    moniker[0].GetDisplayName(bindCtx, null, out strDisplayName);
                    moniker[0].GetClassID(out clsid);
                    ListViewItem item = listViewROT.Items.Add(strDisplayName);
                    item.Tag = new MonikerInfo(strDisplayName, clsid, moniker[0]);
                    
                    if (m_reg.Clsids.ContainsKey(clsid))
                    {
                        item.SubItems.Add(m_reg.Clsids[clsid].Name);
                    }
                    else
                    {
                        item.SubItems.Add(clsid.ToString("B"));
                    }
                }                
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.ToString());
            }

            listViewROT.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
        }

        private void ROTViewer_Load(object sender, EventArgs e)
        {
            listViewROT.Columns.Add("Display Name");
            listViewROT.Columns.Add("CLSID");
            LoadROT();
            Text = "ROT";
        }

        private void menuROT_Click(object sender, EventArgs e)
        {

        }

        private void menuROTRefresh_Click(object sender, EventArgs e)
        {
            LoadROT();
        }

        private void menuROTBindToObject_Click(object sender, EventArgs e)
        {
            if (listViewROT.SelectedItems.Count != 0)
            {
                MonikerInfo info = (MonikerInfo)(listViewROT.SelectedItems[0].Tag);

                Dictionary<string, string> props = new Dictionary<string, string>();
                props.Add("Display Name", info.strDisplayName);
                props.Add("CLSID", info.clsid.ToString("B"));

                try
                {
                    IBindCtx bindCtx = COMUtilities.CreateBindCtx(0);                                        
                    Guid unk = COMInterfaceEntry.IID_IUnknown;
                    object comObj;
                    Type dispType;

                    info.moniker.BindToObject(bindCtx, null, ref unk, out comObj);
                    dispType = COMUtilities.GetDispatchTypeInfo(comObj);
                    ObjectInformation view = new ObjectInformation(m_reg, info.strDisplayName, comObj, props, m_reg.GetInterfacesForObject(comObj));
                    Program.GetMainForm().HostControl(view);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}
