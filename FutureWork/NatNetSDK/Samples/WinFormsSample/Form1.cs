//=============================================================================
// Copyright © 2009 NaturalPoint, Inc. All Rights Reserved.
// 
// This software is provided by the copyright holders and contributors "as is" and
// any express or implied warranties, including, but not limited to, the implied
// warranties of merchantability and fitness for a particular purpose are disclaimed.
// In no event shall NaturalPoint, Inc. or contributors be liable for any direct,
// indirect, incidental, special, exemplary, or consequential damages
// (including, but not limited to, procurement of substitute goods or services;
// loss of use, data, or profits; or business interruption) however caused
// and on any theory of liability, whether in contract, strict liability,
// or tort (including negligence or otherwise) arising in any way out of
// the use of this software, even if advised of the possibility of such damage.
//=============================================================================

using System;
using System.IO;
using System.Collections.Generic;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.Threading;
using System.Runtime.InteropServices;

using NatNetML;

/*
 *
 * Simple C# .NET sample showing how to use the NatNet managed assembly (NatNETML.dll).
 * 
 * It is designed to illustrate using NatNet.  There are some inefficiencies to keep the
 * code as simple to read as possible.
 * 
 * Sections marked with a [NatNet] are NatNet related and should be implemented in your code.
 * 
 * This sample uses the Microsoft Chart Controls for Microsoft .NET for graphing, which
 * requires the following assemblies:
 *   - System.Windows.Forms.DataVisualization.Design.dll
 *   - System.Windows.Forms.DataVisualization.dll
 * Make sure you have these in your path when building and redistributing.
 * 
 */

namespace WinFormTestApp
{
    public partial class Form1 : Form
    {
        // [NatNet] Our NatNet object
        private NatNetML.NatNetClientML m_NatNet;

        // [NatNet] Our NatNet Frame of Data object
        private NatNetML.FrameOfMocapData m_FrameOfData = new NatNetML.FrameOfMocapData();

        // [NatNet] Description of the Active Model List from the server (e.g. Motive)
        NatNetML.ServerDescription desc = new NatNetML.ServerDescription();

        // [NatNet] Queue holding our incoming mocap frames the NatNet server (e.g. Motive)
        private Queue<NatNetML.FrameOfMocapData> m_FrameQueue = new Queue<NatNetML.FrameOfMocapData>();

        // spreadsheet lookup
        Hashtable htMarkers = new Hashtable();
        Hashtable htRigidBodies = new Hashtable();
        List<RigidBody> mRigidBodies = new List<RigidBody>();
        Hashtable htSkelRBs = new Hashtable();

        Hashtable htForcePlates = new Hashtable();
        List<ForcePlate> mForcePlates = new List<ForcePlate>();

        // graphing support
        const int GraphFrames = 500;
        int m_iLastFrameNumber = 0;
        const int maxSeriesCount = 10;

        // frame timing information
        double m_fLastFrameTimestamp = 0.0f;
        float m_fCurrentMocapFrameTimestamp = 0.0f;
        float m_fFirstMocapFrameTimestamp = 0.0f;
        QueryPerfCounter m_FramePeriodTimer = new QueryPerfCounter();
        QueryPerfCounter m_UIUpdateTimer = new QueryPerfCounter();

        // server information
        double m_ServerFramerate = 1.0f;
        float m_ServerToMillimeters = 1.0f;
        int m_UpAxis = 1;   // 0=x, 1=y, 2=z (Y default)
        int mAnalogSamplesPerMocpaFrame = 0;
        int mDroppedFrames = 0;
        int mLastFrame = 0;

        private static object syncLock = new object();
        private delegate void OutputMessageCallback(string strMessage);
        private bool needMarkerListUpdate = false;
        private bool mPaused = false;

        // UI updating
        delegate void UpdateUICallback();
        bool mApplicationRunning = true;
        Thread UIUpdateThread;

        // polling
        delegate void PollCallback();
        Thread pollThread;
        bool mPolling = false;

        bool mRecording = false;
        TextWriter mWriter;


        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // Show available ip addresses of this machine
            String strMachineName = Dns.GetHostName();
            IPHostEntry ipHost = Dns.GetHostByName(strMachineName);
            foreach (IPAddress ip in ipHost.AddressList)
            {
                string strIP = ip.ToString();
                comboBoxLocal.Items.Add(strIP);
            }
            int selected = comboBoxLocal.Items.Add("127.0.0.1");
            comboBoxLocal.SelectedItem = comboBoxLocal.Items[selected];

            // create NatNet client
            int iConnectionType = 0;
            if (RadioUnicast.Checked)
                iConnectionType = 1;
            int iResult = CreateClient(iConnectionType);

            // create data chart
            chart1.Series.Clear();
            for (int i = 0; i < maxSeriesCount; i++)
            {
                System.Windows.Forms.DataVisualization.Charting.Series series = chart1.Series.Add("Series" + i.ToString());
                series.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.FastLine;
                chart1.Series[i].Points.Clear();
            }
            chart1.ChartAreas[0].CursorX.IsUserSelectionEnabled = true;

            // create and run an Update UI thread
            UpdateUICallback d = new UpdateUICallback(UpdateUI);
            UIUpdateThread = new Thread(() =>
            {
                while (mApplicationRunning)
                {
                    try
                    {
                        this.Invoke(d);
                        Thread.Sleep(15);
                    }
                    catch (System.Exception ex)
                    {
                        OutputMessage(ex.Message);
                        break;
                    }
                }
            });
            UIUpdateThread.Start();

            // create and run a polling thread
            PollCallback pd = new PollCallback(PollData);
            pollThread = new Thread(() =>
            {
                while (mPolling)
                {
                    try
                    {
                        this.Invoke(pd);
                        Thread.Sleep(15);
                    }
                    catch (System.Exception ex)
                    {
                        OutputMessage(ex.Message);
                        break;
                    }
                }
            });

        }

        /// <summary>
        /// Create a new NatNet client, which manages all communication with the NatNet server (e.g. Motive)
        /// </summary>
        /// <param name="iConnectionType">0 = Multicast, 1 = Unicast</param>
        /// <returns></returns>
        private int CreateClient(int iConnectionType)
        {
            // release any previous instance
            if (m_NatNet != null)
            {
                m_NatNet.Uninitialize();
            }

            // [NatNet] create a new NatNet instance
            m_NatNet = new NatNetML.NatNetClientML(iConnectionType);

            // [NatNet] set a "Frame Ready" callback function (event handler) handler that will be
            // called by NatNet when NatNet receives a frame of data from the server application
            m_NatNet.OnFrameReady += new NatNetML.FrameReadyEventHandler(m_NatNet_OnFrameReady);

            /*
            // [NatNet] for testing only - event signature format required by some types of .NET applications (e.g. MatLab)
            m_NatNet.OnFrameReady2 += new FrameReadyEventHandler2(m_NatNet_OnFrameReady2);
            */

            // [NatNet] print version info
            int[] ver = new int[4];
            ver = m_NatNet.NatNetVersion();
            String strVersion = String.Format("NatNet Version : {0}.{1}.{2}.{3}", ver[0], ver[1], ver[2], ver[3]);
            OutputMessage(strVersion);

            return 0;
        }

        /// <summary>
        /// Connect to a NatNet server (e.g. Motive)
        /// </summary>
        private void Connect()
        {
            // [NatNet] connect to a NatNet server
            int returnCode = 0;
            string strLocalIP = comboBoxLocal.SelectedItem.ToString();
            string strServerIP = textBoxServer.Text;
            returnCode = m_NatNet.Initialize(strLocalIP, strServerIP);
            if (returnCode == 0)
                OutputMessage("Initialization Succeeded.");
            else
            {
                OutputMessage("Error Initializing.");
                checkBoxConnect.Checked = false;
            }

            // [NatNet] validate the connection
            returnCode = m_NatNet.GetServerDescription(desc);
            if (returnCode == 0)
            {
                OutputMessage("Connection Succeeded.");
                OutputMessage("   Server App Name: " + desc.HostApp);
                OutputMessage(String.Format("   Server App Version: {0}.{1}.{2}.{3}", desc.HostAppVersion[0], desc.HostAppVersion[1], desc.HostAppVersion[2], desc.HostAppVersion[3]));
                OutputMessage(String.Format("   Server NatNet Version: {0}.{1}.{2}.{3}", desc.NatNetVersion[0], desc.NatNetVersion[1], desc.NatNetVersion[2], desc.NatNetVersion[3]));
                checkBoxConnect.Text = "Disconnect";

                // Tracking Tools and Motive report in meters - lets convert to millimeters
                if (desc.HostApp.Contains("TrackingTools") || desc.HostApp.Contains("Motive"))
                    m_ServerToMillimeters = 1000.0f;


                // [NatNet] [optional] Query mocap server for the current camera framerate
                int nBytes = 0;
                byte[] response = new byte[10000];
                int rc;
                rc = m_NatNet.SendMessageAndWait("FrameRate", out response, out nBytes);
                if (rc == 0)
                {
                    try
                    {
                        m_ServerFramerate = BitConverter.ToSingle(response, 0);
                        OutputMessage(String.Format("   Camera Framerate: {0}", m_ServerFramerate));
                    }
                    catch (System.Exception ex)
                    {
                        OutputMessage(ex.Message);
                    }
                }

                // [NatNet] [optional] Query mocap server for the current analog framerate
                rc = m_NatNet.SendMessageAndWait("AnalogSamplesPerMocapFrame", out response, out nBytes);
                if (rc == 0)
                {
                    try
                    {
                        mAnalogSamplesPerMocpaFrame = BitConverter.ToInt32(response, 0);
                        OutputMessage(String.Format("   Analog Samples Per Camera Frame: {0}", mAnalogSamplesPerMocpaFrame));
                    }
                    catch (System.Exception ex)
                    {
                        OutputMessage(ex.Message);
                    }
                }


                // [NatNet] [optional] Query mocap server for the current up axis
                rc = m_NatNet.SendMessageAndWait("UpAxis", out response, out nBytes);
                if (rc == 0)
                {
                    m_UpAxis = BitConverter.ToInt32(response, 0);
                }


                m_fCurrentMocapFrameTimestamp = 0.0f;
                m_fFirstMocapFrameTimestamp = 0.0f;
                mDroppedFrames = 0;
            }
            else
            {
                OutputMessage("Error Connecting.");
                checkBoxConnect.Checked = false;
                checkBoxConnect.Text = "Connect";
            }

        }

        private void Disconnect()
        {
            // [NatNet] disconnect
            // optional : for unicast clients only - notify Motive we are disconnecting
            int nBytes = 0;
            byte[] response = new byte[10000];
            int rc;
            rc = m_NatNet.SendMessageAndWait("Disconnect", out response, out nBytes);
            if (rc == 0)
            {

            }
            // shutdown our client socket
            m_NatNet.Uninitialize();
            checkBoxConnect.Text = "Connect";
        }

        private void checkBoxConnect_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxConnect.Checked)
            {
                Connect();
            }
            else
            {
                Disconnect();
            }
        }

        private void OutputMessage(string strMessage)
        {
            if (mPaused)
                return;

            if(!mApplicationRunning)
                return;

            if (this.listView1.InvokeRequired)
            {
                // It's on a different thread, so use Invoke
                OutputMessageCallback d = new OutputMessageCallback(OutputMessage);
                this.Invoke(d, new object[] { strMessage });
            }
            else
            {
                // It's on the same thread, no need for Invoke
                DateTime d = DateTime.Now;
                String strTime = String.Format("{0}:{1}:{2}:{3}", d.Hour, d.Minute, d.Second, d.Millisecond);
                ListViewItem item = new ListViewItem(strTime, 0);
                item.SubItems.Add(strMessage);
                listView1.Items.Add(item);
            }
        }

        private RigidBody FindRB(int id, int parentID = -2)
        {
            foreach (RigidBody rb in mRigidBodies)
            {
                if (rb.ID == id)
                {
                    if(parentID != -2)
                    {
                        if(rb.parentID == parentID)
                            return rb;
                    }
                    else
                    {
                        return rb;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Redraw the graph using the data of the selected cell in the spreadsheet
        /// </summary>
        /// <param name="iFrame">Frame ID of mocap data</param>
        private void UpdateChart(long iFrame)
        {
            // Lets only show 500 frames at a time
            iFrame %= GraphFrames;

            // clear graph if we've wrapped, allow for fudge
            if ((m_iLastFrameNumber - iFrame) > 400)
            {
                for (int i = 0; i < chart1.Series.Count; i++)
                    chart1.Series[i].Points.Clear();
            }

            for (int i = 0; i < dataGridView1.SelectedCells.Count; i++)
            {
                // for simple performance only graph maxSeriesCount lines
                if (i >= maxSeriesCount)
                    break;

                DataGridViewCell cell = dataGridView1.SelectedCells[i];
                if (cell.Value == null)
                    return;
                double dValue = 0.0f;
                if (!Double.TryParse(cell.Value.ToString(), out dValue))
                    return;
                chart1.Series[i].Points.AddXY(iFrame, (float)dValue);
            }

            // update red 'cursor' line
            chart1.ChartAreas[0].CursorX.SetCursorPosition(iFrame);

            m_iLastFrameNumber = (int)iFrame;
        }

        /// <summary>
        /// Update the spreadsheet.  
        /// Note: This refresh is quite slow and provided here only as a complete example. 
        /// In a production setting this would be optimized.
        /// </summary>
        private void UpdateDataGrid()
        {
            // update MarkerSet data
            for (int i = 0; i < m_FrameOfData.nMarkerSets; i++)
            {
                NatNetML.MarkerSetData ms = m_FrameOfData.MarkerSets[i];
                for (int j = 0; j < ms.nMarkers; j++)
                {
                    string strUniqueName = ms.MarkerSetName + j.ToString();
                    int key = strUniqueName.GetHashCode();
                    if (htMarkers.Contains(key))
                    {
                        int rowIndex = (int)htMarkers[key];
                        if (rowIndex >= 0)
                        {
                            dataGridView1.Rows[rowIndex].Cells[1].Value = ms.Markers[j].x;
                            dataGridView1.Rows[rowIndex].Cells[2].Value = ms.Markers[j].y;
                            dataGridView1.Rows[rowIndex].Cells[3].Value = ms.Markers[j].z;
                        }
                    }
                }
            }

            // update RigidBody data
            for (int i = 0; i < m_FrameOfData.nRigidBodies; i++)
            {
                NatNetML.RigidBodyData rb = m_FrameOfData.RigidBodies[i];
                int key = rb.ID.GetHashCode();

                // note : must add rb definitions here one time instead of on get data descriptions because we don't know the marker list yet.
                if (!htRigidBodies.ContainsKey(key))
                {
                    // Add RigidBody def to the grid
                    if ((rb.Markers[0] != null) && (rb.Markers[0].ID != -1))
                    {
                        string name;
                        RigidBody rbDef = FindRB(rb.ID);
                        if (rbDef != null)
                        {
                            name = rbDef.Name;
                        }
                        else
                        {
                            name = rb.ID.ToString();
                        }

                        int rowIndex = dataGridView1.Rows.Add("RigidBody: " + name);
                        key = rb.ID.GetHashCode();
                        htRigidBodies.Add(key, rowIndex);
                        
                        // Add Markers associated with this rigid body to the grid
                        for (int j = 0; j < rb.nMarkers; j++)
                        {
                            String strUniqueName = name + "-" + rb.Markers[j].ID.ToString();
                            int keyMarker = strUniqueName.GetHashCode();
                            int newRowIndexMarker = dataGridView1.Rows.Add(strUniqueName);
                            htMarkers.Add(keyMarker, newRowIndexMarker);
                        }
                    }
                }
                else
                {
                    // update RigidBody data
                    int rowIndex = (int)htRigidBodies[key];
                    if (rowIndex >= 0)
                    {
                        bool tracked = rb.Tracked;
                        if (!tracked)
                        {
                            OutputMessage("RigidBody not tracked in this frame.");
                        }

                        dataGridView1.Rows[rowIndex].Cells[1].Value = rb.x * m_ServerToMillimeters;
                        dataGridView1.Rows[rowIndex].Cells[2].Value = rb.y * m_ServerToMillimeters;
                        dataGridView1.Rows[rowIndex].Cells[3].Value = rb.z * m_ServerToMillimeters;

                        // Convert quaternion to eulers.  Motive coordinate conventions: X(Pitch), Y(Yaw), Z(Roll), Relative, RHS
                        float[] quat = new float[4] { rb.qx, rb.qy, rb.qz, rb.qw };
                        float[] eulers = new float[3];
                        eulers = m_NatNet.QuatToEuler(quat, (int)NATEulerOrder.NAT_XYZr);
                        double x = RadiansToDegrees(eulers[0]);     // convert to degrees
                        double y = RadiansToDegrees(eulers[1]);
                        double z = RadiansToDegrees(eulers[2]);

                        /*
                        if (m_UpAxis == 2)
                        {
                            double yOriginal = y;
                            y = -z;
                            z = yOriginal;
                        }
                        */


                        dataGridView1.Rows[rowIndex].Cells[4].Value = x;
                        dataGridView1.Rows[rowIndex].Cells[5].Value = y;
                        dataGridView1.Rows[rowIndex].Cells[6].Value = z;

                        // update Marker data associated with this rigid body
                        for (int j = 0; j < rb.nMarkers; j++)
                        {
                            if (rb.Markers[j].ID != -1)
                            {
                                string name;
                                RigidBody rbDef = FindRB(rb.ID);
                                if (rbDef != null)
                                {
                                    name = rbDef.Name;
                                }
                                else
                                {
                                    name = rb.ID.ToString();
                                }

                                String strUniqueName = name + "-" + rb.Markers[j].ID.ToString();
                                int keyMarker = strUniqueName.GetHashCode();
                                if (htMarkers.ContainsKey(keyMarker))
                                {
                                    int rowIndexMarker = (int)htMarkers[keyMarker];
                                    NatNetML.Marker m = rb.Markers[j];
                                    dataGridView1.Rows[rowIndexMarker].Cells[1].Value = m.x;
                                    dataGridView1.Rows[rowIndexMarker].Cells[2].Value = m.y;
                                    dataGridView1.Rows[rowIndexMarker].Cells[3].Value = m.z;
                                }

                            }
                        }
                    }
                }
            }

            // update Skeleton data
            for (int i = 0; i < m_FrameOfData.nSkeletons; i++)
            {
                NatNetML.SkeletonData sk = m_FrameOfData.Skeletons[i];
                for (int j = 0; j < sk.nRigidBodies; j++)
                {
                    // note : skeleton rigid body ids are of the form:
                    // parent skeleton ID   : high word (upper 16 bits of int)
                    // rigid body id        : low word  (lower 16 bits of int)
                    NatNetML.RigidBodyData rb = sk.RigidBodies[j];
                    int skeletonID = HighWord(rb.ID);
                    int rigidBodyID = LowWord(rb.ID);
                    int uniqueID = skeletonID * 1000 + rigidBodyID;
                    int key = uniqueID.GetHashCode();
                    
                    // note : must add rb definitions here one time instead of on get data descriptions because we don't know the marker list yet.
                    if (!htRigidBodies.ContainsKey(key))
                    {
                        // Add RigidBody def to the grid
                        if ( (rb.Markers[0] != null) && (rb.Markers[0].ID != -1))
                        {
                            int key1 = sk.ID * 1000 + rigidBodyID;
                            RigidBody rbDef = (RigidBody) htSkelRBs[key1];
                            if (rbDef != null)
                            {
                                int rowIndex = dataGridView1.Rows.Add("Bone: " + rbDef.Name);
                                htRigidBodies.Add(key, rowIndex);
                                // Add Markers associated with this rigid body to the grid
                                for (int k = 0; k < rb.nMarkers; k++)
                                {
                                    String strUniqueName = rbDef.Name + "-" + rb.Markers[k].ID.ToString();
                                    int keyMarker = strUniqueName.GetHashCode();
                                    int newRowIndexMarker = dataGridView1.Rows.Add(strUniqueName);
                                    htMarkers.Add(keyMarker, newRowIndexMarker);
                                }

                            }
                        }
                    }
                    else 
                    {
                        int rowIndex = (int)htRigidBodies[key];
                        if (rowIndex >= 0)
                        {
                            dataGridView1.Rows[rowIndex].Cells[1].Value = rb.x;
                            dataGridView1.Rows[rowIndex].Cells[2].Value = rb.y;
                            dataGridView1.Rows[rowIndex].Cells[3].Value = rb.z;

                            // Convert quaternion to eulers.  Motive coordinate conventions: X(Pitch), Y(Yaw), Z(Roll), Relative, RHS
                            float[] quat = new float[4] { rb.qx, rb.qy, rb.qz, rb.qw };
                            float[] eulers = new float[3];
                            eulers = m_NatNet.QuatToEuler(quat, (int)NATEulerOrder.NAT_XYZr);
                            double x = RadiansToDegrees(eulers[0]);     // convert to degrees
                            double y = RadiansToDegrees(eulers[1]);
                            double z = RadiansToDegrees(eulers[2]);

                            dataGridView1.Rows[rowIndex].Cells[4].Value = x;
                            dataGridView1.Rows[rowIndex].Cells[5].Value = y;
                            dataGridView1.Rows[rowIndex].Cells[6].Value = z;

                            // Marker data associated with this rigid body
                            int key1 = sk.ID * 1000 + rigidBodyID;
                            RigidBody rbDef = (RigidBody)htSkelRBs[key1];
                            if (rbDef != null)
                            {
                                for (int k = 0; k < rb.nMarkers; k++)
                                {
                                    String strUniqueName = rbDef.Name + "-" + rb.Markers[k].ID.ToString();
                                    int keyMarker = strUniqueName.GetHashCode();
                                    if (htMarkers.ContainsKey(keyMarker))
                                    {
                                        int rowIndexMarker = (int)htMarkers[keyMarker];
                                        NatNetML.Marker m = rb.Markers[k];
                                        dataGridView1.Rows[rowIndexMarker].Cells[1].Value = m.x;
                                        dataGridView1.Rows[rowIndexMarker].Cells[2].Value = m.y;
                                        dataGridView1.Rows[rowIndexMarker].Cells[3].Value = m.z;
                                    }
                                }
                            }
                        }
                    }
                }
            }   // end skeleton update

            // update ForcePlate data
            if (htForcePlates.Count > 0)
            {
                for (int i = 0; i < m_FrameOfData.nForcePlates; i++)
                {
                    NatNetML.ForcePlateData fp = m_FrameOfData.ForcePlates[i];
                    int key = fp.ID.GetHashCode();
                    int rowIndex = (int)htForcePlates[key];
                    if (rowIndex >= 0)
                    {
                        for (int iChannel = 0; iChannel < fp.nChannels; iChannel++)
                        {
                            if (fp.ChannelData[iChannel].nFrames > 0)
                                dataGridView1.Rows[rowIndex].Cells[iChannel + 1].Value = fp.ChannelData[iChannel].Values[0];
                        }
                    }
                }
            }

            // update labeled markers data
            // remove previous dynamic marker list
            // for testing only - this simple approach to grid updating too slow for large marker count use
            int rowOffset = htMarkers.Count + htRigidBodies.Count + htForcePlates.Count + 1;
            int labeledCount = 0;
            if (false)
            {
                int nTotalRows = dataGridView1.Rows.Count;
                for (int i = rowOffset; i < nTotalRows; i++)
                    dataGridView1.Rows.RemoveAt(rowOffset);
                for (int i = 0; i < m_FrameOfData.nMarkers; i++)
                {
                    NatNetML.Marker m = m_FrameOfData.LabeledMarkers[i];

                    int modelID, markerID;
                    m_NatNet.DecodeID(m.ID, out modelID, out markerID);
                    string name = "Labeled Marker (ModelID: " + modelID + "  MarkerID: " + markerID + ")";
                    if (modelID == 0)
                        name = "UnLabeled Marker ( ID: " + markerID + ")";
                    int rowIndex = dataGridView1.Rows.Add(name);
                    dataGridView1.Rows[rowIndex].Cells[1].Value = m.x;
                    dataGridView1.Rows[rowIndex].Cells[2].Value = m.y;
                    dataGridView1.Rows[rowIndex].Cells[3].Value = m.z;
                    labeledCount++;
                }
            }

            // DEPRECATED
            // update unlabeled markers data
            // remove previous dynamic marker list
            // for testing only - this simple approach to grid updating too slow for large marker count use
            rowOffset += labeledCount;
            if (false)
            {
                int nTotalRows = dataGridView1.Rows.Count;
                for (int i = rowOffset; i < nTotalRows; i++)
                    dataGridView1.Rows.RemoveAt(rowOffset);
                for (int i = 0; i < m_FrameOfData.nOtherMarkers; i++)
                {
                    NatNetML.Marker m = m_FrameOfData.OtherMarkers[i];
                    int rowIndex = dataGridView1.Rows.Add("Unlabeled Marker (ID: " + m.ID + ")");
                    dataGridView1.Rows[rowIndex].Cells[1].Value = m.x;
                    dataGridView1.Rows[rowIndex].Cells[2].Value = m.y;
                    dataGridView1.Rows[rowIndex].Cells[3].Value = m.z;
                }
            }


        }

        /// <summary>
        /// [NatNet] Request a description of the Active Model List from the server (e.g. Motive) and build up a new spreadsheet  
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonGetDataDescriptions_Click(object sender, EventArgs e)
        {
            mForcePlates.Clear();
            htForcePlates.Clear();
            mRigidBodies.Clear();
            dataGridView1.Rows.Clear();
            htMarkers.Clear();
            htRigidBodies.Clear();
            htSkelRBs.Clear();
            needMarkerListUpdate = true;

            OutputMessage("Retrieving Data Descriptions....");
            List<NatNetML.DataDescriptor> descs = new List<NatNetML.DataDescriptor>();
            bool bSuccess = m_NatNet.GetDataDescriptions(out descs);
            if (bSuccess)
            {
                OutputMessage(String.Format("Retrieved {0} Data Descriptions....", descs.Count));
                int iObject = 0;
                foreach (NatNetML.DataDescriptor d in descs)
                {
                    iObject++;

                    // MarkerSets
                    if (d.type == (int)NatNetML.DataDescriptorType.eMarkerSetData)
                    {
                        NatNetML.MarkerSet ms = (NatNetML.MarkerSet)d;
                        OutputMessage("Data Def " + iObject.ToString() + " [MarkerSet]");

                        OutputMessage(" Name : " + ms.Name);
                        OutputMessage(String.Format(" Markers ({0}) ", ms.nMarkers));
                        dataGridView1.Rows.Add("MarkerSet: " + ms.Name);
                        for (int i = 0; i < ms.nMarkers; i++)
                        {
                            OutputMessage(("  " + ms.MarkerNames[i]));
                            int rowIndex = dataGridView1.Rows.Add("  " + ms.MarkerNames[i]);
                            // MarkerNameIndexToRow map
                            String strUniqueName = ms.Name + i.ToString();
                            int key = strUniqueName.GetHashCode();
                            htMarkers.Add(key, rowIndex);
                        }
                    }
                    // RigidBodies
                    else if (d.type == (int)NatNetML.DataDescriptorType.eRigidbodyData)
                    {
                        NatNetML.RigidBody rb = (NatNetML.RigidBody)d;

                        OutputMessage("Data Def " + iObject.ToString() + " [RigidBody]");
                        OutputMessage(" Name : " + rb.Name);
                        OutputMessage(" ID : " + rb.ID);
                        OutputMessage(" ParentID : " + rb.parentID);
                        OutputMessage(" OffsetX : " + rb.offsetx);
                        OutputMessage(" OffsetY : " + rb.offsety);
                        OutputMessage(" OffsetZ : " + rb.offsetz);

                        mRigidBodies.Add(rb);

                        int rowIndex = dataGridView1.Rows.Add("RigidBody: " + rb.Name);
                        // RigidBodyIDToRow map
                        int key = rb.ID.GetHashCode();
                        try
                        {
                            htRigidBodies.Add(key, rowIndex);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("Duplicate RigidBody ID Detected : " + ex.Message);
                        }

                    }
                    // Skeletons
                    else if (d.type == (int)NatNetML.DataDescriptorType.eSkeletonData)
                    {
                        NatNetML.Skeleton sk = (NatNetML.Skeleton)d;

                        OutputMessage("Data Def " + iObject.ToString() + " [Skeleton]");
                        OutputMessage(" Name : " + sk.Name);
                        OutputMessage(" ID : " + sk.ID);
                        dataGridView1.Rows.Add("Skeleton: " + sk.Name);
                        for (int i = 0; i < sk.nRigidBodies; i++)
                        {
                            RigidBody rb = sk.RigidBodies[i];
                            OutputMessage(" RB Name  : " + rb.Name);
                            OutputMessage(" RB ID    : " + rb.ID);
                            OutputMessage(" ParentID : " + rb.parentID);
                            OutputMessage(" OffsetX  : " + rb.offsetx);
                            OutputMessage(" OffsetY  : " + rb.offsety);
                            OutputMessage(" OffsetZ  : " + rb.offsetz);

                            //mRigidBodies.Add(rb);
                            int key = sk.ID * 1000 + rb.ID;
                            htSkelRBs.Add(key, rb);
#if false
                            int rowIndex = dataGridView1.Rows.Add("Bone: " + rb.Name);
                            // RigidBodyIDToRow map
                            int uniqueID = sk.ID * 1000 + rb.ID;
                            int key = uniqueID.GetHashCode();
                            if (htRigidBodies.ContainsKey(key))
                                MessageBox.Show("Duplicate RigidBody ID");
                            else
                                htRigidBodies.Add(key, rowIndex);
#endif

                        }
                    }
                    // ForcePlates
                    else if (d.type == (int)NatNetML.DataDescriptorType.eForcePlateData)
                    {
                        NatNetML.ForcePlate fp = (NatNetML.ForcePlate)d;

                        
                        OutputMessage("Data Def " + iObject.ToString() + " [ForcePlate]");
                        OutputMessage(" Name : " + fp.Serial);
                        OutputMessage(" ID : " + fp.ID);
                        OutputMessage(" Width : " + fp.Width);
                        OutputMessage(" Length : " + fp.Length);

                        mForcePlates.Add(fp);

                        int rowIndex = dataGridView1.Rows.Add("ForcePlate: " + fp.Serial);
                        // ForcePlateIDToRow map
                        int key = fp.ID.GetHashCode();
                        htForcePlates.Add(key, rowIndex);
                    }

                    else
                    {
                        OutputMessage("Unknown DataType");
                    }
                }
            }
            else
            {
                OutputMessage("Unable to retrieve DataDescriptions");
            }
        }

        void ProcessFrameOfData(ref NatNetML.FrameOfMocapData data)
        {
            // detect and reported any 'reported' frame drop (as reported by server)
            if (m_fLastFrameTimestamp != 0.0f)
            {
                double framePeriod = 1.0f / m_ServerFramerate;
                double thisPeriod = data.fTimestamp - m_fLastFrameTimestamp;
                double fudgeFactor = 0.002f; // 2 ms
                if ((thisPeriod - framePeriod) > fudgeFactor)
                {
                    //OutputMessage("Frame Drop: ( ThisTS: " + data.fTimestamp.ToString("F3") + "  LastTS: " + m_fLastFrameTimestamp.ToString("F3") + " )");
                    mDroppedFrames++;
                }
            }

            // check and report frame drop (frame id based)
            if (mLastFrame != 0)
            {
                if ((data.iFrame - mLastFrame) != 1)
                {
                    //OutputMessage("Frame Drop: ( ThisFrame: " + data.iFrame.ToString() + "  LastFrame: " + mLastFrame.ToString() + " )");
                    //mDroppedFrames++;
                }
            }

            // recording : write packet to data file
            if (mRecording)
            {
                WriteFrame(data);
            }

            // [NatNet] Add the incoming frame of mocap data to our frame queue,  
            // Note: the frame queue is a shared resource with the UI thread, so lock it while writing
            lock (syncLock)
            {
                // [optional] clear the frame queue before adding a new frame
                m_FrameQueue.Clear();
                FrameOfMocapData deepCopy = new FrameOfMocapData(data);
                m_FrameQueue.Enqueue(deepCopy);
            }

            mLastFrame = data.iFrame;
            m_fLastFrameTimestamp = data.fTimestamp;

        }

        /// <summary>
        /// [NatNet] m_NatNet_OnFrameReady will be called when a frame of Mocap
        /// data has is received from the server application.
        ///
        /// Note: This callback is on the network service thread, so it is
        /// important to return from this function quickly as possible 
        /// to prevent incoming frames of data from buffering up on the
        /// network socket.
        ///
        /// Note: "data" is a reference structure to the current frame of data.
        /// NatNet re-uses this same instance for each incoming frame, so it should
        /// not be kept (the values contained in "data" will become replaced after
        /// this callback function has exited).
        /// </summary>
        /// <param name="data">The actual frame of mocap data</param>
        /// <param name="client">The NatNet client instance</param>
        void m_NatNet_OnFrameReady(NatNetML.FrameOfMocapData data, NatNetML.NatNetClientML client)
        {
            double elapsedIntraMS = 0.0f;
            QueryPerfCounter intraTimer = new QueryPerfCounter();
            intraTimer.Start();

            // detect and report and 'measured' frame drop (as measured by client)
            m_FramePeriodTimer.Stop();
            double elapsedMS = m_FramePeriodTimer.Duration();

            ProcessFrameOfData(ref data);

            // report if we are taking too long, which blocks packet receiving, which if long enough would result in socket buffer drop
            intraTimer.Stop();
            elapsedIntraMS = intraTimer.Duration();
            if (elapsedIntraMS > 5.0f)
            {
                OutputMessage("Warning : Frame handler taking too long: " + elapsedIntraMS.ToString("F2"));
            }

            m_FramePeriodTimer.Start();

        }

        // [NatNet] [optional] alternate function signatured frame ready callback handler for .NET applications/hosts
        // that don't support the m_NatNet_OnFrameReady defined above (e.g. MATLAB)
        void m_NatNet_OnFrameReady2(object sender, NatNetEventArgs e)
        {
            m_NatNet_OnFrameReady(e.data, e.client);
        }

        private void PollData()
        {
            FrameOfMocapData data = m_NatNet.GetLastFrameOfData();
            ProcessFrameOfData(ref data);
        }

        private void SetDataPolling(bool poll)
        {
            if (poll)
            {
                // disable event based data handling
                m_NatNet.OnFrameReady -= m_NatNet_OnFrameReady;

                // enable polling 
                mPolling = true;
                pollThread.Start();
            }
            else
            {
                // disable polling
                mPolling = false;

                // enable event based data handling
                m_NatNet.OnFrameReady += new NatNetML.FrameReadyEventHandler(m_NatNet_OnFrameReady);
            }
        }

        private void GetLastFrameOfData()
        {
            FrameOfMocapData data = m_NatNet.GetLastFrameOfData();
            ProcessFrameOfData(ref data);
        }

        private void GetLastFrameOfDataButton_Click(object sender, EventArgs e)
        {
            // [NatNet] GetLastFrameOfData can be used to poll for the most recent avail frame of mocap data.
            // This mechanism is slower than the event handler mechanism, and in general is not recommended,
            // since it must wait for a frame to become available and apply a lock to that frame while it copies
            // the data to the returned value.

            // get a copy of the most recent frame of data
            // returns null if not available or cannot obtain a lock on it within a specified timeout
            FrameOfMocapData data = m_NatNet.GetLastFrameOfData();
            if (data != null)
            {
                // do something with the data
                String frameInfo = String.Format("FrameID : {0}", data.iFrame);
                OutputMessage(frameInfo);
            }
        }


        private void WriteFrame(FrameOfMocapData data)
        {
            String str =  "";

            str += data.fTimestamp.ToString("F3") + "\t";

            // 'all' markerset data
            for (int i = 0; i < m_FrameOfData.nMarkerSets; i++)
            {
                NatNetML.MarkerSetData ms = m_FrameOfData.MarkerSets[i];
                if(ms.MarkerSetName == "all")
                {
                   for (int j = 0; j < ms.nMarkers; j++)
                    {
                       str += ms.Markers[j].x.ToString("F3") + "\t";
                       str += ms.Markers[j].y.ToString("F3") + "\t";
                       str += ms.Markers[j].z.ToString("F3") + "\t";
                    }
                }
            }

            // force plates
            // just write first subframe from each channel (fx[0], fy[0], fz[0], mx[0], my[0], mz[0])
            for (int i = 0; i < m_FrameOfData.nForcePlates; i++)
            {
                NatNetML.ForcePlateData fp = m_FrameOfData.ForcePlates[i];
                for(int iChannel=0; iChannel < fp.nChannels; iChannel++)
                {
                    if(fp.ChannelData[iChannel].nFrames == 0)
                    {
                        str += 0.0f;    // empty frame
                    }
                    else
                    {
                        str += fp.ChannelData[iChannel].Values[0] + "\t";
                    }
                }
            }

            mWriter.WriteLine(str);
        }

        private void RecordDataButton_CheckedChanged(object sender, EventArgs e)
        {
            if (RecordDataButton.Checked)
            {
                try
                {
                    mWriter = File.CreateText("WinFormsData.txt");
                    mRecording = true;
                }
                catch (System.Exception ex)
                {
                	
                }
            }
            else
            {
                mWriter.Close();
                mRecording = false;
            }

        }

        private void UpdateUI()
        {
            m_UIUpdateTimer.Stop();
            double interframeDuration = m_UIUpdateTimer.Duration();

            QueryPerfCounter uiIntraFrameTimer = new QueryPerfCounter();
            uiIntraFrameTimer.Start();

            // the frame queue is a shared resource with the FrameOfMocap delivery thread, so lock it while reading
            // note this can block the frame delivery thread.  In a production application frame queue management would be optimized.
            lock (syncLock)
            {
                while (m_FrameQueue.Count > 0)
                {
                    m_FrameOfData = m_FrameQueue.Dequeue();

                    if (m_FrameQueue.Count > 0)
                        continue;

                    if (m_FrameOfData != null)
                    {
                        // for servers that only use timestamps, not frame numbers, calculate a 
                        // frame number from the time delta between frames
                        if (desc.HostApp.Contains("TrackingTools"))
                        {
                            m_fCurrentMocapFrameTimestamp = m_FrameOfData.fLatency;
                            if (m_fCurrentMocapFrameTimestamp == m_fLastFrameTimestamp)
                            {
                                continue;
                            }
                            if (m_fFirstMocapFrameTimestamp == 0.0f)
                            {
                                m_fFirstMocapFrameTimestamp = m_fCurrentMocapFrameTimestamp;
                            }
                            m_FrameOfData.iFrame = (int)((m_fCurrentMocapFrameTimestamp - m_fFirstMocapFrameTimestamp) * m_ServerFramerate);

                        }

                        // update the data grid
                        UpdateDataGrid();

                        // update the chart
                        UpdateChart(m_FrameOfData.iFrame);

                        // only redraw chart when necessary, not for every frame
                        if (m_FrameQueue.Count == 0)
                        {
                            chart1.ChartAreas[0].RecalculateAxesScale();
                            chart1.ChartAreas[0].AxisX.Minimum = 0;
                            chart1.ChartAreas[0].AxisX.Maximum = GraphFrames;
                            chart1.Invalidate();
                        }

                        // Mocap server timestamp (in seconds)
                        //m_fLastFrameTimestamp = m_FrameOfData.fTimestamp;
                        TimestampValue.Text = m_FrameOfData.fTimestamp.ToString("F3");
                        DroppedFrameCountLabel.Text = mDroppedFrames.ToString();

                        // SMPTE timecode (if timecode generator present)
                        int hour, minute, second, frame, subframe;
                        bool bSuccess = m_NatNet.DecodeTimecode(m_FrameOfData.Timecode, m_FrameOfData.TimecodeSubframe, out hour, out minute, out second, out frame, out subframe);
                        if (bSuccess)
                            TimecodeValue.Text = string.Format("{0:D2}:{1:D2}:{2:D2}:{3:D2}.{4:D2}", hour, minute, second, frame, subframe);

                        if (m_FrameOfData.bRecording)
                            chart1.BackColor = Color.Red;
                        else
                            chart1.BackColor = Color.White;
                    }
                }
            }

            uiIntraFrameTimer.Stop();
            double uiIntraFrameDuration = uiIntraFrameTimer.Duration();
            m_UIUpdateTimer.Start();

        }


        public int LowWord(int number)
        {
            return number & 0xFFFF;
        }

        public int HighWord(int number)
        {
            return ((number >> 16) & 0xFFFF);
        }

        double RadiansToDegrees(double dRads)
        {
            return dRads * (180.0f / Math.PI);
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            mApplicationRunning = false;

            if(UIUpdateThread.IsAlive)
                UIUpdateThread.Abort();

            m_NatNet.Uninitialize();
        }

        private void RadioMulticast_CheckedChanged(object sender, EventArgs e)
        {
            bool bNeedReconnect = checkBoxConnect.Checked;
            int iResult = CreateClient(0);
            if (bNeedReconnect)
                Connect();
        }

        private void RadioUnicast_CheckedChanged(object sender, EventArgs e)
        {
            bool bNeedReconnect = checkBoxConnect.Checked;
            int iResult = CreateClient(1);
            if (bNeedReconnect)
                Connect();
        }

        private void RecordButton_Click(object sender, EventArgs e)
        {
            string command = "StartRecording";

            int nBytes = 0;
            byte[] response = new byte[10000];
            int rc = m_NatNet.SendMessageAndWait(command, 3, 100, out response, out nBytes);
            if (rc != 0)
            {
                OutputMessage(command + " not handled by server");
            }
            else
            {
                int opResult = System.BitConverter.ToInt32(response, 0);
                if (opResult == 0)
                    OutputMessage(command + " handled and succeeded.");
                else
                    OutputMessage(command + " handled but failed.");
            }
        }

        private void StopRecordButton_Click(object sender, EventArgs e)
        {
            string command = "StopRecording";

            int nBytes = 0;
            byte[] response = new byte[10000];
            int rc = m_NatNet.SendMessageAndWait(command, out response, out nBytes);
             
            if (rc != 0)
            {
                OutputMessage(command + " not handled by server");
            }
            else
            {
                int opResult = System.BitConverter.ToInt32(response, 0);
                if (opResult == 0)
                    OutputMessage(command + " handled and succeeded.");
                else
                    OutputMessage(command + " handled but failed.");
            }
        }

        private void LiveModeButton_Click(object sender, EventArgs e)
        {
            int nBytes = 0;
            byte[] response = new byte[10000];
            int rc = m_NatNet.SendMessageAndWait("LiveMode", out response, out nBytes);
        }

        private void EditModeButton_Click(object sender, EventArgs e)
        {
            int nBytes = 0;
            byte[] response = new byte[10000];
            int rc = m_NatNet.SendMessageAndWait("EditMode", out response, out nBytes);
        }

        private void TimelinePlayButton_Click(object sender, EventArgs e)
        {
            int nBytes = 0;
            byte[] response = new byte[10000];
            int rc = m_NatNet.SendMessageAndWait("TimelinePlay", out response, out nBytes);
        }

        private void TimelineStopButton_Click(object sender, EventArgs e)
        {
            int nBytes = 0;
            byte[] response = new byte[10000];
            int rc = m_NatNet.SendMessageAndWait("TimelineStop", out response, out nBytes);
        }

        private void SetRecordingTakeButton_Click(object sender, EventArgs e)
        {
            int nBytes = 0;
            byte[] response = new byte[10000];
            String strCommand = "SetRecordTakeName," + RecordingTakeNameText.Text;
            int rc = m_NatNet.SendMessageAndWait(strCommand, out response, out nBytes);
        }

        private void SetPlaybackTakeButton_Click(object sender, EventArgs e)
        {
            int nBytes = 0;
            byte[] response = new byte[10000];
            String strCommand = "SetPlaybackTakeName," + PlaybackTakeNameText.Text;
            int rc = m_NatNet.SendMessageAndWait(strCommand, out response, out nBytes);
        }

        private void TestButton_Click(object sender, EventArgs e)
        {
#if true
            int nBytes = 0;
            byte[] response = new byte[10000];
            int rc;
            rc = m_NatNet.SendMessageAndWait("FrameRate", out response, out nBytes);
            if (rc == 0)
            {
                try
                {
                    m_ServerFramerate = BitConverter.ToSingle(response, 0);
                    OutputMessage(String.Format("   Camera Framerate: {0}", m_ServerFramerate));
                }
                catch (System.Exception ex)
                {
                    OutputMessage(ex.Message);
                }
            }

#else
            int nBytes = 0;
            byte[] response = new byte[10000];
            int testVal;
            String command;
            int returnCode;

            command = "SetPlaybackTakeName," + PlaybackTakeNameText.Text;
            OutputMessage("Sending " + command);
            returnCode = m_NatNet.SendMessageAndWait(command, out response, out nBytes);
            // process return codes
            if (returnCode != 0)
            {
                OutputMessage(command + " not handled by server");
            }
            else
            {
                int opResult = System.BitConverter.ToInt32(response, 0);
                if (opResult == 0)
                    OutputMessage(command + " handled and succeeded.");
                else
                    OutputMessage(command + " handled but failed.");
            }

            testVal = 25;
            command = "SetPlaybackStartFrame," + testVal.ToString();
            OutputMessage("Sending " + command);
            returnCode = m_NatNet.SendMessageAndWait(command, out response, out nBytes);
            // process return codes
            if (returnCode != 0)
            {
                OutputMessage(command + " not handled by server");
            }
            else
            {
                int opResult = System.BitConverter.ToInt32(response, 0);
                if(opResult==0)
                    OutputMessage(command + " handled and succeeded.");
                else
                    OutputMessage(command +  " handled but failed.");
            }
               
            testVal = 50;
            command = "SetPlaybackStopFrame," + testVal.ToString();
            OutputMessage("Sending " + command);
            returnCode = m_NatNet.SendMessageAndWait(command, out response, out nBytes);
            if (returnCode != 0)
            {
                OutputMessage("SetPlaybackStartFrame not handled by server");
            }
            else
            {
                int opResult = System.BitConverter.ToInt32(response, 0);
                if (opResult == 0)
                    OutputMessage(command + " handled and succeeded.");
                else
                    OutputMessage(command + " handled but failed.");
            }

            testVal = 0;
            command = "SetPlaybackLooping," + testVal.ToString();
            OutputMessage("Sending " + command);
            returnCode = m_NatNet.SendMessageAndWait(command, out response, out nBytes);
            if (returnCode != 0)
            {
                OutputMessage("SetPlaybackStartFrame not handled by server");
            }
            else
            {
                int opResult = System.BitConverter.ToInt32(response, 0);
                if (opResult == 0)
                    OutputMessage(command + " handled and succeeded.");
                else
                    OutputMessage(command + " handled but failed.");
            }

            testVal = 35;
            OutputMessage("Sending " + command);
            command = "SetPlaybackCurrentFrame," + testVal.ToString();
            returnCode = m_NatNet.SendMessageAndWait(command, out response, out nBytes);
            if (returnCode != 0)
            {
                OutputMessage("SetPlaybackStartFrame not handled by server");
            }
            else
            {
                int opResult = System.BitConverter.ToInt32(response, 0);
                if (opResult == 0)
                    OutputMessage(command + " handled and succeeded.");
                else
                    OutputMessage(command + " handled but failed.");
            }

#endif

        }

        private void contextMenuStrip1_Opening(object sender, CancelEventArgs e)
        {

        }

        private void menuClear_Click(object sender, EventArgs e)
        {
            listView1.Items.Clear();
        }

        private void menuPause_Click(object sender, EventArgs e)
        {
            mPaused = menuPause.Checked;
        }

        private void GetTakeRangeButton_Click(object sender, EventArgs e)
        {
            int nBytes = 0;
            byte[] response = new byte[10000];
            int rc;
            rc = m_NatNet.SendMessageAndWait("CurrentTakeLength", out response, out nBytes);
            if (rc == 0)
            {
                try
                {
                    int takeLength = BitConverter.ToInt32(response, 0);
                    OutputMessage(String.Format("Current Take Length: {0}", takeLength));
                }
                catch (System.Exception ex)
                {
                    OutputMessage(ex.Message);
                }
            }
        }

        private void GetModeButton_Click(object sender, EventArgs e)
        {
            int nBytes = 0;
            byte[] response = new byte[10000];
            int rc;
            rc = m_NatNet.SendMessageAndWait("CurrentMode", out response, out nBytes);
            if (rc == 0)
            {
                try
                {
                    String strMode = "";
                    int mode = BitConverter.ToInt32(response, 0);
                    if (mode == 0)
                        strMode = String.Format("Mode : Live");
                    else if (mode == 1)
                        strMode = String.Format("Mode : Recording");
                    else if (mode == 2)
                        strMode = String.Format("Mode : Edit");
                    OutputMessage(strMode);
                }
                catch (System.Exception ex)
                {
                    OutputMessage(ex.Message);
                }
            }
        }



        private void PollCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            SetDataPolling(PollCheckBox.Checked);
        }

    }

    // Wrapper class for the windows high performance timer QueryPerfCounter
    // ( adapted from MSDN https://msdn.microsoft.com/en-us/library/ff650674.aspx )
    public class QueryPerfCounter
    {
        [DllImport("KERNEL32")]
        private static extern bool QueryPerformanceCounter(out long lpPerformanceCount);

        [DllImport("Kernel32.dll")]
        private static extern bool QueryPerformanceFrequency(out long lpFrequency);

        private long start;
        private long stop;
        private long frequency;
        Decimal multiplier = new Decimal(1.0e9);

        public QueryPerfCounter()
        {
            if (QueryPerformanceFrequency(out frequency) == false)
            {
                // Frequency not supported
                throw new Win32Exception();
            }
        }

        public void Start()
        {
            QueryPerformanceCounter(out start);
        }

        public void Stop()
        {
            QueryPerformanceCounter(out stop);
        }

        // return elapsed time between start and stop, in milliseconds.
        public double Duration()
        {
            double val = ((double)(stop - start) * (double)multiplier) / (double)frequency;
            val = val / 1000000.0f;   // convert to ms
            return val;
        }
    }

}