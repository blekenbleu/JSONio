using System.Windows.Controls;

namespace blekenbleu
{
    /// <summary>
    /// Logique d'interaction pour SettingsControl.xaml
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
