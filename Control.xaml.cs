using System.Windows.Controls;

namespace blekenbleu
{
    /// <summary>
    /// Interaction code for SettingsControl.xaml
    /// </summary>
    public partial class SettingsControl : UserControl
    {
        public JSONio Plugin { get; }

        public SettingsControl()
        {
            InitializeComponent();
        }

        public SettingsControl(JSONio plugin) : this()
        {
            this.Plugin = plugin;
        }


    }
}
