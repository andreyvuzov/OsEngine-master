using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.IO;

namespace OsEngine.Robots.Patterns
{
    /// <summary>
    /// Логика взаимодействия для NewRobotHW3ui.xaml
    /// </summary>
    public partial class NewRobotHW3ui : Window
    {
        public NewRobotHW3 _myRobot;
        public NewRobotHW3ui(NewRobotHW3 myRobot)
        {
            InitializeComponent();

            _myRobot = myRobot;
            TextBoxStop.Text = myRobot.Stop.ToString();
            TextBoxProfit.Text = myRobot.Profit.ToString();
            TextBoxVolume.Text = myRobot.Volume.ToString();
            TextBoxSlipageIn.Text = myRobot.SlipageIn.ToString();
            TextBoxtSlipageOut.Text = myRobot.SlipageOut.ToString();
            CheckBoxOnOff.IsChecked = myRobot.OnOff;

            ButtonSave.Click += ButtonSave_Click;
        }

        private void ButtonSave_Click(object sender, RoutedEventArgs e)
        {
            _myRobot.Stop = Convert.ToInt32(TextBoxStop.Text);
            _myRobot.Profit = Convert.ToInt32(TextBoxProfit.Text);
            _myRobot.SlipageIn = Convert.ToInt32(TextBoxSlipageIn.Text);
            _myRobot.SlipageOut = Convert.ToInt32(TextBoxtSlipageOut.Text);
            _myRobot.Volume = Convert.ToInt32(TextBoxVolume.Text);
            _myRobot.OnOff = CheckBoxOnOff.IsChecked.Value;

            //запрос сохраниения настроек в файл

            _myRobot.Save();

            //закрытие окна

            Close();
        }
    }
}
