using Android.App;
using Android.Widget;
using Android.OS;
using BandBridge.Sockets;

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
        private Button createFakeBandsButton;
        private ListView connectedBandsListView;
        private TextView debugLogText;
        #endregion

        #region Methods

        #endregion
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
            createFakeBandsButton = FindViewById<Button>(Resource.Id.CreateFakeBandsButton);
            connectedBandsListView = FindViewById<ListView>(Resource.Id.ConnectedBandsListView);
            debugLogText = FindViewById<TextView>(Resource.Id.DebugLogText);

            // create BBServer object:
            bbServer = new BBServer();
            UpdateUI();

            // Add behaviour to buttons:
            startServerToggle.Click += (object sender, System.EventArgs e) => {
                if (startServerToggle.Checked)
                {
                    debugLogText.Text = "start server";
                    StartBBServer();
                }
                else
                {
                    debugLogText.Text = "stop server";
                    StopBBServer();
                }
            };
            searchMSBandsButton.Click += (object sender, System.EventArgs e) => {
                debugLogText.Text = "search for MS Band devices...";
            };
            createFakeBandsButton.Click += (object sender, System.EventArgs e) => {
                debugLogText.Text = "create Fake Band devices...";
            };
        }

        #region Private methods
        /// <summary>
        /// Updates UI elements.
        /// </summary>
        private void UpdateUI()
        {
            serverAddressText.Text = bbServer.HostAddress.ToString();
            servicePortText.Text = bbServer.ServicePort.ToString();
            dataBufferSizeText.Text = bbServer.DataBufferSize.ToString();
            calibrationBufferSizeText.Text = bbServer.CalibrationBufferSize.ToString();
        }

        private void StartBBServer()
        {

        }

        private void StopBBServer()
        {

        }
        #endregion
    }
}

