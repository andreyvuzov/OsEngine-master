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

namespace OsEngine.Robots.Trend
{
    /// <summary>
    /// Логика взаимодействия для PairBot2Ui.xaml
    /// </summary>
    public partial class PairBot2Ui : Window
    {
        private PairBot2 _myRobot;
        public PairBot2Ui(PairBot2 myRobot)
        {
            InitializeComponent();
            _myRobot = myRobot;
            //вводим текущие настройки на форму
            TextBox_ma_long.Text = _myRobot.n_long.ToString();
            TextBox_ma_short.Text = _myRobot.n_short.ToString();
            TextBox_w1.Text = _myRobot.w1.ToString();
            TextBox_w2.Text = _myRobot.w2.ToString();
            TextBox_in_long.Text = _myRobot.In_long.ToString();
            TextBox_in_short.Text = _myRobot.In_short.ToString();
            TextBox_out_long.Text = _myRobot.Out_long.ToString();
            TextBox_out_short.Text = _myRobot.Out_short.ToString();
            TextBox_depo.Text = _myRobot.depo.ToString();
            TextBox_Vgo.Text = _myRobot.Vgo.ToString();
            checkbox_only_long.IsChecked = _myRobot.only_long;
            checkbox_only_short.IsChecked = _myRobot.only_short;

            ButtonSave.Click += ButtonSave_Click;
        }

        private void ButtonSave_Click(object sender, RoutedEventArgs e)
        {
            //сохраняем данные из текстбоксов
            _myRobot.n_long = Convert.ToDecimal(TextBox_ma_long.Text);
            _myRobot.n_short = Convert.ToDecimal(TextBox_ma_short.Text);
            _myRobot.w1 = Convert.ToDecimal(TextBox_w1.Text);
            _myRobot.w2 = Convert.ToDecimal(TextBox_w2.Text);
            _myRobot.In_long = Convert.ToDouble(TextBox_in_long.Text);
            _myRobot.In_short = Convert.ToDouble(TextBox_in_short.Text);
            _myRobot.Out_long = Convert.ToDouble(TextBox_out_long.Text);
            _myRobot.Out_short = Convert.ToDouble(TextBox_out_short.Text);
            _myRobot.depo = Convert.ToDecimal(TextBox_depo.Text);
            _myRobot.Vgo = Convert.ToDouble(TextBox_Vgo.Text);
            _myRobot.only_long = checkbox_only_long.IsChecked.Value;
            _myRobot.only_short = checkbox_only_short.IsChecked.Value;

            _myRobot.Save();

            Close();
        }
    }
}
