using Android.App;
using Android.Widget;
using Android.OS;
using BandBridge.Sockets;
using System.Threading.Tasks;


namespace AndroidBandBridge
{
    [Activity(Label = "BandBridge for Android", MainLauncher = true)]
    public class MainActivity : Activity
    {
        #region Private fields
        private BBServer bbServer;
        private TextView serverAddressText;
        private EditText servicePortText;
        private EditText dataBufferSizeText;
        private EditText calibrationBufferSizeText;
        private ToggleButton startServerToggle;
        private Button searchMSBandsButton;
        private TextView serverDebugLogText;
        private TextView msBandDebugLogText;
        private TextView msBandNameText;
        private TextView msBandHrText;
        private TextView msBandGsrText;
        #endregion


        #region Activity methods
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);

            // Get the UI controls from the loaded layout:
            serverAddressText = FindViewById<TextView>(Resource.Id.ServerAddressText);
            servicePortText = FindViewById<EditText>(Resource.Id.ServicePortText);
            dataBufferSizeText = FindViewById<EditText>(Resource.Id.DataBufferSizeText);
            calibrationBufferSizeText = FindViewById<EditText>(Resource.Id.CalibrationBufferSizeText);
            startServerToggle = FindViewById<ToggleButton>(Resource.Id.StartServerToggle);
            searchMSBandsButton = FindViewById<Button>(Resource.Id.SearchMSBandsButton);
            serverDebugLogText = FindViewById<TextView>(Resource.Id.ServerDebugLogText);
            msBandDebugLogText = FindViewById<TextView>(Resource.Id.MSBandDebugLogText);
            msBandNameText = FindViewById<TextView>(Resource.Id.MSBandNameText);
            msBandHrText = FindViewById<TextView>(Resource.Id.MSBandHrText);
            msBandGsrText = FindViewById<TextView>(Resource.Id.MSBandGsrText);

            // create BBServer object:
            bbServer = new BBServer();
            bbServer.BandInfoChanged += () =>
            {
                RunOnUiThread(() => {
                    if (bbServer.ConnectedBand != null)
                    {
                        msBandNameText.Text = bbServer.ConnectedBand.Name;
                        msBandHrText.Text = bbServer.ConnectedBand.HrReading.ToString();
                        msBandGsrText.Text = bbServer.ConnectedBand.GsrReading.ToString();
                    }
                });
            };
            UpdateServerSettingsUI();

            // Add behaviour to buttons:
            startServerToggle.Click += (object sender, System.EventArgs e) =>
            {
                if (startServerToggle.Checked)
                {
                    // show dialog to confirm decision:
                    var startServerDialog = new AlertDialog.Builder(this);
                    startServerDialog.SetMessage("Start BandBridge server?");
                    startServerDialog.SetNeutralButton("Yes", delegate {
                        bbServer.UpdateServerSettings(servicePortText.Text, dataBufferSizeText.Text, calibrationBufferSizeText.Text);
                        Task.Factory.StartNew(() => bbServer.StartListening());
                        serverDebugLogText.Text = "started server";
                    });
                    startServerDialog.SetNegativeButton("No", delegate {
                        startServerToggle.Checked = false;
                    });
                    startServerDialog.Show();
                }
                else
                {
                    // show dialog to confirm decision:
                    var stopServerDialog = new AlertDialog.Builder(this);
                    stopServerDialog.SetMessage("Stop BandBridge server?");
                    stopServerDialog.SetNeutralButton("Yes", delegate {
                        Task.Factory.StartNew(() => bbServer.StopServer());
                        serverDebugLogText.Text = "stopped server";
                    });
                    stopServerDialog.SetNegativeButton("No", delegate {
                        startServerToggle.Checked = true;
                    });
                    stopServerDialog.Show();
                }
            };
            searchMSBandsButton.Click += async (object sender, System.EventArgs e) =>
            {
                await bbServer.GetMSBandDevices();
                msBandDebugLogText.Text = bbServer.MSBandLog;
            };
        }
        #endregion


        #region Private methods
        /// <summary>
        /// Updates server settings UI elements.
        /// </summary>
        private void UpdateServerSettingsUI()
        {
            serverAddressText.Text = bbServer.HostAddress.ToString();
            servicePortText.Text = bbServer.ServicePort.ToString();
            dataBufferSizeText.Text = bbServer.DataBufferSize.ToString();
            calibrationBufferSizeText.Text = bbServer.CalibrationBufferSize.ToString();
        }
        #endregion
    }
}

