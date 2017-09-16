using Android.App;
using Android.Widget;
using Android.OS;

namespace AndroidBandBridge
{
    [Activity(Label = "BandBridge for Android", MainLauncher = true)]
    public class MainActivity : Activity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);
            
            // Get the UI controls from the loaded layout:
            TextView serverAddressText = FindViewById<TextView>(Resource.Id.ServerAddressText);
            EditText servicePortText = FindViewById<EditText>(Resource.Id.ServicePortText);
            EditText dataBufferSizeText = FindViewById<EditText>(Resource.Id.DataBufferSizeText);
            EditText callibrationBufferSizeText = FindViewById<EditText>(Resource.Id.CallibrationBufferSizeText);
            ToggleButton startServerToggle = FindViewById<ToggleButton>(Resource.Id.StartServerToggle);
            Button searchMSBandsButton = FindViewById<Button>(Resource.Id.SearchMSBandsButton);
            ListView connectedBandsListView = FindViewById<ListView>(Resource.Id.ConnectedBandsListView);

            // debug:
            TextView debugLogText = FindViewById<TextView>(Resource.Id.DebugLogText);

            // Add behaviour to buttons:
            startServerToggle.Click += (object sender, System.EventArgs e) => {
                if (startServerToggle.Checked)
                {
                    debugLogText.Text = "start server";
                }
                else
                {
                    debugLogText.Text = "stop server";
                }
            };
            searchMSBandsButton.Click += (object sender, System.EventArgs e) => {
                debugLogText.Text = "search for MS Band devices...";
            };
        }
    }
}

