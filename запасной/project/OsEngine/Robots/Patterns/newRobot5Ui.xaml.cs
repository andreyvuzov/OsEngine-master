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

namespace OsEngine.Robots.Patterns
{
    /// <summary>
    /// Логика взаимодействия для newRobot5Ui.xaml
    /// </summary>
    public partial class newRobot5Ui : Window
    {
        public NewRobot5 _myRobot;
        public newRobot5Ui(NewRobot5 myRobot)
        {
            InitializeComponent();
            _myRobot = myRobot;
            //выводим текущие настройки в форму
            TextBoxStop.Text = _myRobot.Stop.ToString();
            TextBoxProfit.Text = _myRobot.Profit.ToString();
            TextBoxSlipage.Text = _myRobot.Sleepage.ToString();
            TextBoxVolume.Text = _myRobot.Volume.ToString();
            ChexkBoxOnOff.IsChecked = _myRobot.IsOn;

            ButtonSave.Click += ButtonSave_Click;
        }

        private void ButtonSave_Click(object sender, RoutedEventArgs e)
        {//сохраняем данные из текстбоксов
            _myRobot.Stop = Convert.ToInt32(TextBoxStop.Text);
            _myRobot.Profit = Convert.ToInt32(TextBoxProfit.Text);
            _myRobot.Sleepage = Convert.ToInt32(TextBoxSlipage.Text);
            _myRobot.Volume = Convert.ToInt32(TextBoxVolume.Text);
            _myRobot.IsOn = ChexkBoxOnOff.IsChecked.Value;

            //запрос сохраниения настроек в файл

            _myRobot.Save();

            //закрытие окна

            Close();
        }
    }
}
