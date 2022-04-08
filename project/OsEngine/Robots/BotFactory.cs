/*
 * Your rights to use code governed by this license https://github.com/AlexWan/OsEngine/blob/master/LICENSE
 * Ваши права на использование кода регулируются данной лицензией http://o-s-a.net/doc/license_simple_engine.pdf
*/

using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Microsoft.CSharp;
using OsEngine.Entity;
using OsEngine.OsTrader.Panels;
using OsEngine.OsTrader.Panels.Attributes;
using OsEngine.Robots.CounterTrend;
using OsEngine.Robots.Engines;
using OsEngine.Robots.High_Frequency;
using OsEngine.Robots.MarketMaker;
using OsEngine.Robots.Patterns;
using OsEngine.Robots.Trend;
using OsEngine.Robots.OnScriptIndicators;
using OsEngine.Robots.Screeners;
using OsEngine.OsTrader.Panels.Tab;
using OsEngine.Charts.CandleChart.Indicators;
using OsEngine.Indicators;
using OsEngine.Language;
using OsEngine.Logging;
using OsEngine.Market;
using OsEngine.Market.Servers;
using System.Drawing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using ContextMenu = System.Windows.Forms.ContextMenu;
using DataGrid = System.Windows.Forms.DataGrid;
using MenuItem = System.Windows.Forms.MenuItem;
using MessageBox = System.Windows.Forms.MessageBox;
using Point = System.Drawing.Point;

namespace OsEngine.Robots
{
    public class NewRobot5 : BotPanel
    {
        public int Stop;
        public int Profit;
        public int Sleepage;
        public int Volume;
        public bool IsOn;
        public void Save()
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(@"Engine\" + NameStrategyUniq + @"SettingsBot.txt", false))
                {
                    writer.WriteLine(Stop);
                    writer.WriteLine(Profit);
                    writer.WriteLine(Sleepage);
                    writer.WriteLine(Volume);
                    writer.WriteLine(IsOn);

                    writer.Close();
                }
            }
            catch (Exception)
            {
                // ignore
            }
        }
        private void Load()
        {
            if (!File.Exists(@"Engine\" + NameStrategyUniq + @"SettingsBot.txt"))
            {
                return;
            }
            try
            {
                using (StreamReader reader = new StreamReader(@"Engine\" + NameStrategyUniq + @"SettingsBot.txt"))
                {
                    Stop = Convert.ToInt32(reader.ReadLine());
                    Profit = Convert.ToInt32(reader.ReadLine());
                    Sleepage = Convert.ToInt32(reader.ReadLine());
                    Volume = Convert.ToInt32(reader.ReadLine());
                    IsOn = Convert.ToBoolean(reader.ReadLine());

                    reader.Close();
                }
            }
            catch (Exception)
            {
                // ignore
            }
        }
        


        public NewRobot5(string name, StartProgram startProgram) : base(name, startProgram)
        {
            Stop = 300;//данные в шагах цены
            Profit = 500;
            Sleepage = 2;
            Volume = 2;
            IsOn = true;

            Load();//загружаем настройки

            TabCreate(BotTabType.Simple);
            TabsSimple[0].CandleFinishedEvent += NewRobot5_CandleFinishedEvent;
            TabsSimple[0].PositionOpeningSuccesEvent += NewRobot5_PositionOpeningSuccesEvent;
        }

        private void NewRobot5_PositionOpeningSuccesEvent(Position position)
        {
            TabsSimple[0].CloseAtStop(position, position.EntryPrice - Stop * TabsSimple[0].Securiti.PriceStep,
                position.EntryPrice - Stop * TabsSimple[0].Securiti.PriceStep - Sleepage * TabsSimple[0].Securiti.PriceStep);
            TabsSimple[0].CloseAtProfit(position, position.EntryPrice + Profit * TabsSimple[0].Securiti.PriceStep,
                position.EntryPrice + Profit * TabsSimple[0].Securiti.PriceStep + Sleepage * TabsSimple[0].Securiti.PriceStep);
        }

        private void NewRobot5_CandleFinishedEvent(List<Candle> candles)
        {
            if(candles.Count < 5)
            {//недостаточно свечей
                return;
            }
            if(IsOn == false)
            {//робот выключен
                return;
            }
            if(TabsSimple[0].PositionsOpenAll != null && TabsSimple[0].PositionsOpenAll.Count > 0)
            {//уже есть позиция
                return;
            }
            //1.текущая свеча растущая, а предыдущая падающая
            //2.тело растущей свечи минимум в три раза больше падающей
            //3.пять свечей назад хай был выше хая последней свечи, т.е имеет локальный лой и разворот от него

            Candle lastcandle = candles[candles.Count - 1];
            Candle secondcandle = candles[candles.Count - 2];

            if(lastcandle.Close > lastcandle.Open && secondcandle.Open < lastcandle.Close)
            {//прошли первую проверку
                if((lastcandle.Close - lastcandle.Open)/3 > (secondcandle.Open - secondcandle.Close))
                {//прошли вторую проверку
                    if(candles[candles.Count-5].High > lastcandle.High)
                    {//прошли третью проверку

                        TabsSimple[0].BuyAtLimit(Volume, lastcandle.Close + Sleepage * TabsSimple[0].Securiti.PriceStep);
                    }

                }


            }
        }

        public override string GetNameStrategyType()
        {
            return "NewRobot5";
        }

        public override void ShowIndividualSettingsDialog()
        {
            newRobot5Ui ui = new newRobot5Ui(this);
            ui.ShowDialog();

        }
    }
    public class RobotHammer : BotPanel
    {
        public RobotHammer(string name, StartProgram startProgram) : base(name, startProgram)
        {
            TabCreate(BotTabType.Simple); //создание вкладки
            TabsSimple[0].CandleFinishedEvent += RobotHammer_CandleFinishedEvent;
            TabsSimple[0].PositionOpeningSuccesEvent += RobotHammer_PositionOpeningSuccesEvent;
        }

        private void RobotHammer_PositionOpeningSuccesEvent(Position position) //евент с уже открытой позицией(вызывается когда ордер по позиции уже исполнился)
        {
            TabsSimple[0].CloseAtStop(position, _stopPrice, _stopPrice);   //закрываем позицию по стоплосу
        }

        private void RobotHammer_CandleFinishedEvent(List<Candle> candles) //событие завершения свечи
        {
            if(TabsSimple[0].PositionsOpenAll != null && TabsSimple[0].PositionsOpenAll.Count != 0) //проверяем есть ли открытие позиции (да)
            {
                if(candles[candles.Count-1].TimeStart >= _timeToClose)//если время последней свечи больше чем время закрытия, то закрываем свечу
                {
                    TabsSimple[0].CloseAllAtMarket();
                }
            }

            if(candles.Count < 22)
            {//если свечей меньше 22, то мы не входим
                return;
            }
            if(candles[candles.Count-1].Close <= candles[candles.Count-1].Open)
            {//если последняя свеча не растущая
                return;
            }
            //проверяем чтобы последний лой был самым нижним за последние 20 свечей
            decimal lastlow = candles[candles.Count - 1].Low;

            for(int i = candles.Count-1; i > candles.Count - 20; i--)
            {
                if(lastlow > candles[i].Low)
                { 
                    return;
                }
            }
            //проверяем чтобы тело было меньше хвоста в три раза снизу и не больше тела сверху
            Candle candle = candles[candles.Count - 1]; //берем последнюю свечу из массива

            decimal body = candle.Close - candle.Open;//тело свечи
            decimal shadowlow = candle.Open - candle.Low;//тень снизу
            decimal shadowhigh = candle.High - candle.Close;//тень сверху

            if(body < shadowhigh)
            {
                return;
            }
            if(shadowlow / 3 < body)
            {
                return;
            }
            TabsSimple[0].BuyAtMarket(1);//открытие позиции
            _timeToClose = candle.TimeStart.AddMinutes(5);//закрытие через 5 минут после открытия позиции
            _stopPrice = candle.Low - TabsSimple[0].Securiti.PriceStep;// стоп равен лою молота


        }
        public DateTime _timeToClose;

        public decimal _stopPrice;

        public override string GetNameStrategyType()
        {
            return "HummerBot";
        }

        public override void ShowIndividualSettingsDialog()
        {
            //пока не требуется
        }
    }
    public class RobotHW1 : BotPanel
    {
        public RobotHW1(string name, StartProgram startProgram) : base(name, startProgram)
        {
            TabCreate(BotTabType.Simple);
            TabsSimple[0].CandleFinishedEvent += RobotHW1_CandleFinishedEvent; //обозначаем эвент закрытия свечи
            TabsSimple[0].PositionOpeningSuccesEvent += RobotHW1_PositionOpeningSuccesEvent;//обозначаем евент с открытой позицией
        }

        private void RobotHW1_PositionOpeningSuccesEvent(Position position)
        {
               
        }

        private void RobotHW1_CandleFinishedEvent(List<Candle> candles)
        {
            if (TabsSimple[0].PositionsOpenAll != null && TabsSimple[0].PositionsOpenAll.Count != 0) //проверяем есть ли открытие позиции (да)
            {
                if (candles[candles.Count - 1].Close <= candles[candles.Count - 1].Open &&
                    candles[candles.Count - 2].Close <= candles[candles.Count - 2].Open &&
                    candles[candles.Count - 3].Close <= candles[candles.Count - 3].Open)//если время последней свечи больше чем время закрытия, то закрываем свечу
                {
                    TabsSimple[0].CloseAllAtMarket();
                }
            }
            if (TabsSimple[0].PositionsOpenAll != null && TabsSimple[0].PositionsOpenAll.Count != 0)
            {
                return;
            }
            if (candles.Count < 22)
            {
                return;
            }
            //проверяем чтобы последний close был самым высоким за последние 20 свечей
            for (int i = candles.Count - 1; i > candles.Count - 20; i--)
            {
                if (candles[candles.Count - 1].Close < candles[i].Close)
                {
                    return;
                }
            }
            
            if (candles[candles.Count - 1].Close > candles[candles.Count - 1].Open &&
                candles[candles.Count - 2].Close > candles[candles.Count - 2].Open &&
                candles[candles.Count - 3].Close > candles[candles.Count - 3].Open)
            {//условие на вход
                TabsSimple[0].BuyAtMarket(1);//открытие позиции
            }
            

        }

        public override string GetNameStrategyType()
        {
            return "RobotHW1";
        }

        public override void ShowIndividualSettingsDialog()
        {
            //не используется
        }
    }
    public class NewRobot2 : BotPanel         
    {
        private BotTabSimple _tabOne;
        private BotTabSimple _tabTwo;
        public NewRobot2(string name, StartProgram startProgram) : base(name, startProgram)
        {
            TabCreate(BotTabType.Simple);
            TabCreate(BotTabType.Simple); //создали две вкладки

            _tabOne = TabsSimple[0];
            _tabTwo = TabsSimple[1];

            _tabOne.CandleFinishedEvent += _tabOne_CandleFinishedEvent;
            _tabTwo.CandleFinishedEvent += _tabTwo_CandleFinishedEvent;
        }

        private void _tabTwo_CandleFinishedEvent(List<Candle> candles)
        {
            if(_tabTwo.PositionsOpenAll != null && _tabTwo.PositionsCloseAll.Count != 0)
            {
                _tabTwo.BuyAtMarket(1);
            }
            else
            {
                _tabTwo.CloseAllAtMarket();
            }
            
        }

        private void _tabOne_CandleFinishedEvent(List<Candle> candles)
        {
            if (_tabOne.PositionsOpenAll != null && _tabOne.PositionsCloseAll.Count != 0)
            {
                _tabOne.BuyAtMarket(1);
            }
            else
            {
                _tabOne.CloseAllAtMarket();
            }
        }

        public override string GetNameStrategyType()
        {
            return "NewRobot2";
        }

        public override void ShowIndividualSettingsDialog()
        {
            //пока не требуется
        }
    }
    public class NewRobot3 : BotPanel
    {
        public NewRobot3(string name, StartProgram startProgram) : base(name, startProgram)
        {
            TabCreate(BotTabType.Simple);

            TabsSimple[0].CandleFinishedEvent += NewRobot3_CandleFinishedEvent;
            TabsSimple[0].PositionOpeningSuccesEvent += NewRobot3_PositionOpeningSuccesEvent;

        }

        private void NewRobot3_PositionOpeningSuccesEvent(Position position)
        {
            if(position.SignalTypeOpen == "PatternOne")
            {
                TabsSimple[0].CloseAtStop(position, position.EntryPrice - TabsSimple[0].Securiti.PriceStep * 100,
                    position.EntryPrice - TabsSimple[0].Securiti.PriceStep * 110);
                TabsSimple[0].CloseAtProfit(position, position.EntryPrice + TabsSimple[0].Securiti.PriceStep * 50,
                    position.EntryPrice + TabsSimple[0].Securiti.PriceStep * 40);
            }
            if (position.SignalTypeOpen == "PatternTwo")
            {
                TabsSimple[0].CloseAtStop(position, position.EntryPrice - TabsSimple[0].Securiti.PriceStep * 200,
                    position.EntryPrice - TabsSimple[0].Securiti.PriceStep * 210);
                TabsSimple[0].CloseAtProfit(position, position.EntryPrice + TabsSimple[0].Securiti.PriceStep * 100,
                    position.EntryPrice + TabsSimple[0].Securiti.PriceStep * 90);
            }

        }

        private void NewRobot3_CandleFinishedEvent(List<Candle> candles)
        {
            if(candles.Count < 5)
            {
                return;
            }
            List<Position> _openposition = TabsSimple[0].PositionsOpenAll;
            
            if(_openposition == null || _openposition.Count == 0)
            {//логика входа для первого паттерна
                MethodToFindPatternOne(candles);
            }
            else if (_openposition.Count == 1)
            {//логичка входа для паттерна номер два
                MethodToFindPatternTwo(candles);
            }
        }
        public void MethodToFindPatternOne(List<Candle>candles)
        {//две подряд растущие свечи
            Candle candle1 = candles[candles.Count - 1];//присвоили candle1 последнюю свечу
            Candle candle2 = candles[candles.Count - 2];//присвоили candle2 препоследнюю свечу
            
            if(candle1.Close > candle1.Open && candle2.Close > candle2.Open)
            {
                TabsSimple[0].BuyAtLimit(1, candle1.Close + TabsSimple[0].Securiti.PriceStep * 2,"PatternOne");//вход в лонг с проскальзываем в два шага
            }
        }
        public void MethodToFindPatternTwo(List<Candle> candles)
        {//две падающие и одна растущая
            Candle candle1 = candles[candles.Count - 1];
            Candle candle2 = candles[candles.Count - 2];
            Candle candle3 = candles[candles.Count - 3];

            if(candle1.Close > candle1.Open &&
                candle2.Close < candle2.Open &&
                candle3.Close < candle3.Open)
            {
                TabsSimple[0].BuyAtLimit(1, candle1.Close + TabsSimple[0].Securiti.PriceStep * 2,"PatternTwo");//вход в лонг с проскальзываем в два шага
            }
        }

        public override string GetNameStrategyType()
        {
            return "NewRobot3";
        }

        public override void ShowIndividualSettingsDialog()
        {
            //не требуется
        }
    }
    public class NewRobot4 : BotPanel
    {
        public NewRobot4(string name, StartProgram startProgram) : base(name, startProgram)
        {
            TabCreate(BotTabType.Simple);//первая вкладка
            TabCreate(BotTabType.Simple);//вторая вкладка

            TabsSimple[0].CandleFinishedEvent += NewRobot4tab0_CandleFinishedEvent;//событие при закрытии свечи на первой вкладке
            TabsSimple[1].CandleFinishedEvent += NewRobot4tab1_CandleFinishedEvent;//событие при закрытии свечи на второй вкладке

            TabsSimple[0].PositionOpeningSuccesEvent += tab0_PositionOpeningSuccesEvent;
            TabsSimple[1].PositionOpeningSuccesEvent += tab1_PositionOpeningSuccesEvent;
        }

        private void tab1_PositionOpeningSuccesEvent(Position position)
        {
            TabsSimple[1].CloseAtStop(position, position.EntryPrice - TabsSimple[1].Securiti.PriceStep * 100,
                position.EntryPrice - TabsSimple[1].Securiti.PriceStep * 100);
            TabsSimple[1].CloseAtProfit(position, position.EntryPrice + TabsSimple[1].Securiti.PriceStep * 100,
                position.EntryPrice + TabsSimple[1].Securiti.PriceStep * 100);
        }

        private void tab0_PositionOpeningSuccesEvent(Position position)
        {
            TabsSimple[0].CloseAtStop(position, position.EntryPrice + TabsSimple[0].Securiti.PriceStep * 100,
                position.EntryPrice + TabsSimple[0].Securiti.PriceStep * 100);
            TabsSimple[0].CloseAtProfit(position, position.EntryPrice - TabsSimple[0].Securiti.PriceStep * 100,
                position.EntryPrice - TabsSimple[0].Securiti.PriceStep * 100);

        }

        public void TradeLogic(List<Candle> candlesOneTab, List<Candle> candlesTwoTab)
        {//если у первой вкладки три расстущие свечи, а во второй три падающие, то в первой вкладке в шорт, во второй в лонг

            Candle candletab11 = candlesOneTab[candlesOneTab.Count - 1];
            Candle candletab12 = candlesOneTab[candlesOneTab.Count - 2];
            Candle candletab13 = candlesOneTab[candlesOneTab.Count - 3];
            Candle candletab21 = candlesTwoTab[candlesTwoTab.Count - 1];
            Candle candletab22 = candlesTwoTab[candlesTwoTab.Count - 2];
            Candle candletab23 = candlesTwoTab[candlesTwoTab.Count - 3];

            if(candletab11.Close > candletab11.Open && 
                candletab12.Close > candletab12.Open &&
                candletab13.Close > candletab13.Open &&
                candletab21.Close < candletab21.Open &&
                candletab22.Close < candletab22.Open &&
                candletab23.Close < candletab23.Open)
            {
                TabsSimple[0].SellAtMarket(1);
                TabsSimple[1].BuyAtMarket(1);
            }
        }

        private void NewRobot4tab1_CandleFinishedEvent(List<Candle> candles)
        {
            if(candles.Count < 10)
            {
                return;
            }

            List<Candle> candles2 = TabsSimple[0].CandlesFinishedOnly;
            if(candles[candles.Count-1].TimeStart == candles2[candles2.Count-1].TimeStart) 
                /*если время начала свечи в первой вкладке равно времени начала свечи на второй вкладке,
                то заходим в логику торговли*/
{
    TradeLogic(candles2, candles);
            }
            
        }

        private void NewRobot4tab0_CandleFinishedEvent(List<Candle> candles)
        {
            if (candles.Count < 10)
            {
                return;
            }
            List<Candle> candles2 = TabsSimple[1].CandlesFinishedOnly;
            if (candles[candles.Count - 1].TimeStart == candles2[candles2.Count - 1].TimeStart)
            /*если время начала свечи в первой вкладке равно времени начала свечи на второй вкладке,
            то заходим в логику торговли*/
            {
                TradeLogic(candles, candles2);
            }

        }

        public override string GetNameStrategyType()
        {
            return "NewRobot4";
        }

        public override void ShowIndividualSettingsDialog()
        {
            //не требуется
        }
    }
    public class NewRobotHW2 : BotPanel
    {
        public NewRobotHW2(string name, StartProgram startProgram) : base(name, startProgram)
        {
            TabCreate(BotTabType.Simple);
            TabCreate(BotTabType.Simple);

            TabsSimple[0].CandleFinishedEvent += tab0_CandleFinishedEvent;
            TabsSimple[1].CandleFinishedEvent += tab1_CandleFinishedEvent;

            TabsSimple[0].PositionOpeningSuccesEvent += tab0_PositionOpeningSuccesEvent;
            TabsSimple[1].PositionOpeningSuccesEvent += tab1_PositionOpeningSuccesEvent;
        }

        private void tab0_PositionOpeningSuccesEvent(Position position)
        {            
        }
        private void tab1_PositionOpeningSuccesEvent(Position position)
        {           
        }

        public void TradeLogic(List<Candle> candleOneTab, List<Candle> candleTwoTab)
        {//если молот с рукояткой сверху на одной вкладке, и рукоятной снизу на другой то первую продаём, а вторую покупаем
            decimal body1 = candleOneTab[candleOneTab.Count - 1].Close - candleOneTab[candleOneTab.Count - 1].Open;
            decimal shadowHigh1 = candleOneTab[candleOneTab.Count - 1].High - candleOneTab[candleOneTab.Count - 1].Close;
            decimal shadowLow1 = candleOneTab[candleOneTab.Count - 1].Open - candleOneTab[candleOneTab.Count - 1].Low;

            decimal body2 = candleTwoTab[candleTwoTab.Count - 1].Open - candleTwoTab[candleTwoTab.Count - 1].Close;
            decimal shadowHigh2 = candleTwoTab[candleTwoTab.Count - 1].High - candleTwoTab[candleTwoTab.Count - 1].Open;
            decimal shadowLow2 = candleTwoTab[candleTwoTab.Count - 1].Close - candleTwoTab[candleTwoTab.Count - 1].Low;
                        
            if(shadowHigh1 / 3 > body1 && body1 > shadowLow1 && shadowLow2/3 > body2 && body2 > shadowHigh2)
            {
                TabsSimple[0].SellAtLimit(1, candleOneTab[candleOneTab.Count - 1].Close);
                TabsSimple[1].BuyAtLimit(1, candleTwoTab[candleTwoTab.Count - 1].Close);
            }
        }

        private void tab0_CandleFinishedEvent(List<Candle> candles)
        {
            if(candles.Count < 10)
            {
                return;
            }
            List<Candle> candles2 = TabsSimple[0].CandlesFinishedOnly;
            if(candles[candles.Count - 1].TimeStart == candles2[candles2.Count-1].TimeStart)
            {
                TradeLogic(candles2, candles);
            }
            if (candles[candles.Count - 1].TimeStart >= candles[candles.Count -1].TimeStart.AddMinutes(5))//если время последней свечи больше чем время закрытия, то закрываем свечу
            {
                TabsSimple[0].CloseAllAtMarket();
                TabsSimple[1].CloseAllAtMarket();
            }

        }

        private void tab1_CandleFinishedEvent(List<Candle> candles)
        {
            if(candles.Count < 10)
            {
                return;
            }
            List<Candle> candles2 = TabsSimple[1].CandlesFinishedOnly;
            if(candles[candles.Count-1].TimeStart == candles2[candles2.Count-1].TimeStart)
            {
                TradeLogic(candles, candles2);
            }
            if (candles[candles.Count - 1].TimeStart >= candles[candles.Count - 1].TimeStart.AddMinutes(5))//если время последней свечи больше чем время закрытия, то закрываем свечу
            {
                TabsSimple[0].CloseAllAtMarket();
                TabsSimple[1].CloseAllAtMarket();
            }
        }

        public override string GetNameStrategyType()
        {
            return "NewRobotHW2";
        }

        public override void ShowIndividualSettingsDialog()
        {
            //не требуется
        }
    }
    public class NewRobotHW3 : BotPanel
    {
        public int Stop;
        public int Profit;
        public int Volume;
        public int SlipageIn;
        public int SlipageOut;
        public bool OnOff;

        public void Save()
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(@"Engine\" + NameStrategyUniq + @"SettingsBot.txt", false))
                {
                    writer.WriteLine(Stop);
                    writer.WriteLine(Profit);
                    writer.WriteLine(Volume);
                    writer.WriteLine(SlipageIn);
                    writer.WriteLine(SlipageOut);                    
                    writer.WriteLine(OnOff);

                    writer.Close();
                }
            }
            catch (Exception)
            {
                // ignore
            }
        }
        private void Load()
        {
            if (!File.Exists(@"Engine\" + NameStrategyUniq + @"SettingsBot.txt"))
            {
                return;
            }
            try
            {
                using (StreamReader reader = new StreamReader(@"Engine\" + NameStrategyUniq + @"SettingsBot.txt"))
                {
                    Stop = Convert.ToInt32(reader.ReadLine());
                    Profit = Convert.ToInt32(reader.ReadLine());
                    Volume = Convert.ToInt32(reader.ReadLine());
                    SlipageIn = Convert.ToInt32(reader.ReadLine());
                    SlipageOut = Convert.ToInt32(reader.ReadLine());
                    OnOff = Convert.ToBoolean(reader.ReadLine());

                    reader.Close();
                }
            }
            catch (Exception)
            {
                // ignore
            }
        }
        public NewRobotHW3(string name, StartProgram startProgram) : base(name, startProgram)
        {
            Stop = 100;
            Profit = 200;
            Volume = 5;
            SlipageIn = 2;
            SlipageOut = 5;
            OnOff = false;

            Load();

            TabCreate(BotTabType.Simple);
            TabsSimple[0].CandleFinishedEvent += NewRobotHW3_CandleFinishedEvent;
            TabsSimple[0].PositionOpeningSuccesEvent += NewRobotHW3_PositionOpeningSuccesEvent;

        }

        private void NewRobotHW3_PositionOpeningSuccesEvent(Position position)
        {
            TabsSimple[0].CloseAtProfit(position, position.EntryPrice - Profit * TabsSimple[0].Securiti.PriceStep,
                position.EntryPrice - Profit - SlipageOut * TabsSimple[0].Securiti.PriceStep);
            TabsSimple[0].CloseAtStop(position, position.EntryPrice + Stop * TabsSimple[0].Securiti.PriceStep,
                position.EntryPrice + Stop + SlipageOut * TabsSimple[0].Securiti.PriceStep);
        }

        private void NewRobotHW3_CandleFinishedEvent(List<Candle> candles)
        {
            if(candles.Count < 5)
            {
                return;
            }
            if(TabsSimple[0].PositionsOpenAll != null && TabsSimple[0].PositionsOpenAll.Count > 0)
            {
                return;
            }
            if(OnOff == false)
            {
                return;
            }
            //1.текущая свеча падающая, а предыдущая растущая
            //2.тело падающей свечи минимум в три раза больше растущей
            //3.пять свечей назад лоу был ниже лоя последней свечи, т.е имеет локальный хай и разворот от него

            if(candles[candles.Count-1].Close < candles[candles.Count-1].Open &&
                (candles[candles.Count-1].Open - candles[candles.Count-1].Close)/3 > candles[candles.Count-2].Close - candles[candles.Count-2].Open &&
                candles[candles.Count-5].Low < candles[candles.Count-1].Low)
            {
                TabsSimple[0].SellAtLimit(Volume, candles[candles.Count - 1].Close - SlipageIn * TabsSimple[0].Securiti.PriceStep);
            }

        }

        public override string GetNameStrategyType()
        {
            return "NewRobotHW5";
        }

        public override void ShowIndividualSettingsDialog()
        {
            NewRobotHW3ui ui = new NewRobotHW3ui(this);
                ui.ShowDialog();


        }
    }
    public class NewRobot6 : BotPanel
    {
        public MovingAverage _moving;
        public NewRobot6(string name, StartProgram startProgram) : base(name, startProgram)
        {
            TabCreate(BotTabType.Simple);
            _moving = new MovingAverage("moving1",false);
            _moving = (MovingAverage)TabsSimple[0].CreateCandleIndicator(_moving, "Prime");
            _moving.Save();

            TabsSimple[0].CandleFinishedEvent += NewRobot6_CandleFinishedEvent;
        }

        private void NewRobot6_CandleFinishedEvent(List<Candle> candles)
        {//1.входим только в лонг
         //2.входим по закрытию свечи выше МА и выходим когда цена опустилась ниже МА
            
            if(_moving.Lenght >= candles.Count)
            {
                return;
            }

            List<Position> positions = TabsSimple[0].PositionsOpenAll;

            if(positions == null || positions.Count == 0)
            {
                if (candles[candles.Count - 1].Close > _moving.Values[_moving.Values.Count - 1])//если закрытие свечи больше МА
                {
                    TabsSimple[0].BuyAtLimit(1, candles[candles.Count - 1].Close);
                }
            }
            else
            {
                if (candles[candles.Count - 1].Close < _moving.Values[_moving.Values.Count - 1])
                {
                    if(positions[0].State != PositionStateType.Open)
                    {
                        return;
                    }                    
                    TabsSimple[0].CloseAtLimit(positions[0], candles[candles.Count - 1].Close, positions[0].OpenVolume);
                }
            }
        }

        public override string GetNameStrategyType()
        {
            return "NewRobot6";
        }

        public override void ShowIndividualSettingsDialog()
        {
            
        }
    }

    public class NewRobot7 : BotPanel
    {
        public MovingAverage _moving;
        public Atr _atr;
        public Envelops _envelops;

        public NewRobot7(string name, StartProgram startProgram) : base(name, startProgram)
        {
            TabCreate(BotTabType.Simple);

            _moving = new MovingAverage("moving1", false);
            _moving = (MovingAverage)TabsSimple[0].CreateCandleIndicator(_moving, "Prime");
            _moving.Save();

            _atr = new Atr("atr1", false);
            _atr = (Atr)TabsSimple[0].CreateCandleIndicator(_atr, "NewArea");
            _atr.Save();

            _envelops = new Envelops("envelops1", false);
            _envelops = (Envelops)TabsSimple[0].CreateCandleIndicator(_envelops, "Prime");
            _envelops.Save();
            
            TabsSimple[0].CandleFinishedEvent += NewRobot7_CandleFinishedEvent;

        }

        private void NewRobot7_CandleFinishedEvent(List<Candle> candles)
        {
            if(candles[candles.Count-1].TimeStart.Hour < 11)
            {
                return;
            }
            
            if (_moving.Lenght > candles.Count ||
                _atr.Lenght > candles.Count)
            {
                return;
            }
            //входим в лонг когда значение закрытие свечи выше верхнего значения конверта
            //выходим когда закрытие свечи ниже мувинг - атр * 2

            List<Position> positions = TabsSimple[0].PositionsOpenAll;

            if (positions != null && positions.Count != 0)
            {//если позиция есть,но ещё не открылась
                if (positions[0].State != PositionStateType.Open)
                {
                    return;
                }
            }
            if (positions == null || positions.Count == 0)
            {//логика открытия позиции
                if(candles[candles.Count - 1].Close > _envelops.ValuesUp[_envelops.ValuesUp.Count-1])
                {
                    TabsSimple[0].BuyAtLimit(1, candles[candles.Count - 1].Close);
                }
            }
            else
            {//логика закрытия
                if(candles[candles.Count-1].Close < _moving.Values[_moving.Values.Count-1] - _atr.Values[_atr.Values.Count-1]*2)
                {
                    TabsSimple[0].CloseAtLimit(positions[0], candles[candles.Count - 1].Close, positions[0].OpenVolume);
                }
            }

        }

        public override string GetNameStrategyType()
        {
            return "NewRobot7";
        }

        public override void ShowIndividualSettingsDialog()
        {
            
        }
    }

    public class BotMarketDepth : BotPanel
    {
        public BotMarketDepth(string name, StartProgram startProgram) : base(name, startProgram)
        {
            TabCreate(BotTabType.Simple);
            TabsSimple[0].MarketDepthUpdateEvent += BotMarketDepth_MarketDepthUpdateEvent;//создаем вкладку работы по стакану
            TabsSimple[0].ServerTimeChangeEvent += BotMarketDepth_ServerTimeChangeEvent;
        }

        private void BotMarketDepth_ServerTimeChangeEvent(DateTime time)
        {//выход через 20 секунд
            List<Position> positions = TabsSimple[0].PositionsOpenAll;

            if(positions == null ||
                positions.Count == 0)
            {
                return;
            }
            if(positions[0].State != PositionStateType.Open)
            {
                return;
            }
            if(positions[0].TimeOpen.AddSeconds(20) < time)
            {
                TabsSimple[0].CloseAtMarket(positions[0],positions[0].OpenVolume);
            }
        }

        private void BotMarketDepth_MarketDepthUpdateEvent(MarketDepth marketDepth)
        {//лонг когда в продажах меньше объема чем в покупках в 5 раз
            List<Position> positions = TabsSimple[0].PositionsOpenAll;
            if(positions != null && positions.Count != 0)
            {//если есть позиции - выходим
                return;
            }
            if (marketDepth.AskSummVolume * 5 < marketDepth.BidSummVolume)
            {
                TabsSimple[0].BuyAtMarket(1);
            }            
        }

        public override string GetNameStrategyType()
        {
            return "BotMarketDepth";
        }

        public override void ShowIndividualSettingsDialog()
        {
            

        }
    }

    public class BotAllTrades : BotPanel
    {
        public BotAllTrades(string name, StartProgram startProgram) : base(name, startProgram)
        {
            TabCreate(BotTabType.Simple);
            TabsSimple[0].NewTickEvent += BotAllTrades_NewTickEvent;
            TabsSimple[0].PositionOpeningSuccesEvent += BotAllTrades_PositionOpeningSuccesEvent;
        }

        private void BotAllTrades_PositionOpeningSuccesEvent(Position position)
        {
            TabsSimple[0].CloseAtStop(position, position.EntryPrice - 20 * TabsSimple[0].Securiti.PriceStep,
                position.EntryPrice - 20 * TabsSimple[0].Securiti.PriceStep);
            TabsSimple[0].CloseAtProfit(position, position.EntryPrice + 20 * TabsSimple[0].Securiti.PriceStep,
                position.EntryPrice + 20 * TabsSimple[0].Securiti.PriceStep);
        }

        private void BotAllTrades_NewTickEvent(Trade trade)
        {//если за секунду прошло более 50 сделок на покупку по заходим в лонг

            List<Position> positions = TabsSimple[0].PositionsOpenAll;

            if(positions != null && positions.Count > 0)
            {
                return;
            }
            if(trade.Time == timeTrade)
            {
                if(trade.Side == Side.Buy)
                {
                    _countTradeInSecond++;
                }                
            }
            else
            {
                timeTrade = trade.Time;
                _countTradeInSecond = 0;
            }
            if(_countTradeInSecond > 50)
            {
                TabsSimple[0].BuyAtMarket(1);
            }
            
        }
        private DateTime timeTrade;
        private int _countTradeInSecond;

        public override string GetNameStrategyType()
        {
            return "BotAllTrades";
        }

        public override void ShowIndividualSettingsDialog()
        {
            
        }
    }

    /*public class BotHftStyle : BotPanel
    {
        public BotHftStyle(string name, StartProgram startProgram) : base(name, startProgram)
        {
            ServerMaster.ServerCreateEvent += ServerMaster_ServerCreateEvent;
        }
        private IServer server;

        void ServerMaster_ServerCreateEvent()
        {
            server = ServerMaster.GetServers()[0];
            server.NewTradeEvent += Server_NewTradeEvent;
        }

        private void Server_NewTradeEvent(List<Trade> trades)
        {
            
        }

        public override string GetNameStrategyType()
        {
            return "BotHftStyle";
        }

        public override void ShowIndividualSettingsDialog()
        {
            
        }
    }*/

    public class BotArbitrage : BotPanel
    {
        public IvashovRange _range;
        private MovingAverage _ma;

        public BotArbitrage(string name, StartProgram startProgram) : base(name, startProgram)
        {
            TabCreate(BotTabType.Index);

            _range = new IvashovRange("range", false);

            _range = (IvashovRange)TabsIndex[0].CreateCandleIndicator(_range, "RangeArea") ;
            _range.Save();

            _ma = new MovingAverage("moving", false);
            _ma = (MovingAverage)TabsIndex[0].CreateCandleIndicator(_ma, "Prime");
            _ma.Save();

            TabCreate(BotTabType.Simple);
            TabsIndex[0].SpreadChangeEvent += BotArbitrage_SpreadChangeEvent;
            TabsSimple[0].CandleFinishedEvent += BotArbitrage_CandleFinishedEvent;
        }

        private void BotArbitrage_SpreadChangeEvent(List<Candle> candles)
        {
            if(candles.Count < 501)
            {
                return;
            }
            List<Candle> candlesTab = TabsSimple[0].CandlesFinishedOnly;
            if(candles[candles.Count - 1].TimeStart == candlesTab[candlesTab.Count-1].TimeStart)
            {
                TradeLogic(candles, candlesTab);
            }
        }

        private void BotArbitrage_CandleFinishedEvent(List<Candle> candlesTab)
        {
            if(candlesTab.Count < 501)
            {
                return;
            }
            List<Candle> candles = TabsIndex[0].Candles;
            if (candles[candles.Count - 1].TimeStart == candlesTab[candlesTab.Count - 1].TimeStart)
            {
                TradeLogic(candles, candlesTab);
            }
        }

        public void TradeLogic(List<Candle> index, List<Candle> tabCandles)
        {
            if(tabCandles.Count < 501)
            {
                return;
            }
            List<Position> positions = TabsSimple[0].PositionsOpenAll;

            if(positions == null || positions.Count == 0)
            {//открытие позиции
                if(_range.LenghtAverage > tabCandles.Count + 5 || _range.LenghtMa > tabCandles.Count + 5 || _ma.Lenght > tabCandles.Count + 5)
                {
                    return;
                }
                if(index[index.Count - 1].Close > _ma.Values[_ma.Values.Count - 1] + _range.Values[_range.Values.Count - 1])
                {//шорт. Находимся выше канала среднеквадратичного отклонения
                    TabsSimple[0].SellAtLimit(1, tabCandles[tabCandles.Count - 1].Close);
                }
                else if(index[index.Count - 1].Close < _ma.Values[_ma.Values.Count - 1] - _range.Values[_range.Values.Count - 1])
                {//лонг
                    TabsSimple[0].BuyAtLimit(1, tabCandles[tabCandles.Count - 1].Close);
                }
            }
            else
            if(positions[0].State != PositionStateType.Open)
            {
                return;
            }
            {//закрытие позиции
                if (positions[0].Direction == Side.Buy && index[index.Count - 1].Close > _ma.Values[_ma.Values.Count - 1] + _range.Values[_range.Values.Count - 1])
                {
                    TabsSimple[0].CloseAtLimit(positions[0], tabCandles[tabCandles.Count - 1].Close, positions[0].OpenVolume);
                }
                else if (positions[0].Direction == Side.Sell && index[index.Count - 1].Close < _ma.Values[_ma.Values.Count - 1] - _range.Values[_range.Values.Count - 1])
                {//лонг
                    TabsSimple[0].CloseAtLimit(positions[0], tabCandles[tabCandles.Count - 1].Close, positions[0].OpenVolume);
                }
            }

        }

        public override string GetNameStrategyType()
        {
            return "BotArbitrage";
        }

        public override void ShowIndividualSettingsDialog()
        {
            
        }
    }
        
    public class PairBot : BotPanel
    {
        public decimal n_long;//количество свечей для расчёта автовеса для лонга
        public decimal n_short;//количество свечей для расчёта автовеса для шорта
        public decimal w1;//вес первого инструмента
        public decimal w2;//вес второго инструмента
        public double Spf;//спрэд
        public double In_long;//коэф на вход для лонга
        public double In_short;//коэф на вход для шорта
        public double Out_long;//коэф на выход для лонга
        public double Out_short;//коэф на выход для шорта
        public decimal Ma1_long = 0;//ма первой ноги для лонга
        public decimal Ma2_long = 0;//ма второй ноги для лонга
        public decimal Ma1_short = 0;//ма первой ноги для шорта
        public decimal Ma2_short = 0;//ма второй ноги для шорта
        public decimal v1 = -1; //коэф веса в спреде первой ноги
        public decimal v2 = 1;//коэф веса в спреде второй ноги
        public decimal ma_long = 0; //ма спреда для лонга
        public decimal ma_short = 0; //ма спреда для шорта
        public decimal C1; //закрытие первой ноги
        public decimal C2; //закрытие второй ноги
        public decimal c1_sec; //закрытие первой ноги секунды
        public decimal c2_sec; //закрытие второй ноги секунды
        public double cbsp;
        public double PriceCB;
        public double cssp;
        public double PriceCS;
        public double PriceOB;
        public double PriceOS;
        public double obsp;
        public double ossp;
        public bool only_long;
        public bool only_short;
        public decimal TekGo;
        public decimal volume;
        public double Vgo;
        public decimal depo;
        
        
        public PairBot(string name, StartProgram startProgram) : base(name, startProgram)
        {
            Load(); //загружаем настройки
            LoadGO();//загружаем го

            TabCreate(BotTabType.Simple);
            TabCreate(BotTabType.Simple);
            TabCreate(BotTabType.Simple);
            TabCreate(BotTabType.Simple);
            
                     
            TabsSimple[0].CandleFinishedEvent += PairBot_CandleFinishedEvent;//sec first legs
            TabsSimple[1].CandleFinishedEvent += PairBot_CandleFinishedEvent1;//sec second legs
            TabsSimple[2].CandleFinishedEvent += PairBot_min_CandleFinishedEvent;
            TabsSimple[3].CandleFinishedEvent += PairBot_min2_CandleFinishedEvent;

            
            TabsSimple[0].PositionOpeningSuccesEvent += PairBot_PositionOpeningSuccesEvent;//sec first legs
            TabsSimple[1].PositionOpeningSuccesEvent += PairBot_PositionOpeningSuccesEvent1;//sec second legs
            TabsSimple[2].PositionOpeningSuccesEvent += PairBot_min_PositionOpeningSuccesEvent;
            TabsSimple[3].PositionOpeningSuccesEvent += PairBot_min2_PositionOpeningSuccesEvent;
            
        }

        private void PairBot_min2_PositionOpeningSuccesEvent(Position position)
        {            
        }

        private void PairBot_min2_CandleFinishedEvent(List<Candle> candles)
        {
            c2_sec = candles[candles.Count - 1].Close;
        }

        private void PairBot_min_PositionOpeningSuccesEvent(Position position)
        {            
        }

        private void PairBot_min_CandleFinishedEvent(List<Candle> candles)
        {
            c1_sec = candles[candles.Count - 1].Close;
            
        }

        private void PairBot_long_PositionOpeningSuccesEvent2(Position positions)
        {            
        }

        public void Save()
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(@"Engine\" + NameStrategyUniq + @"SettingsBot.txt", false)
                    )
                {
                    writer.WriteLine(n_long);
                    writer.WriteLine(n_short);
                    writer.WriteLine(w1);
                    writer.WriteLine(w2);
                    writer.WriteLine(In_long);
                    writer.WriteLine(In_short);
                    writer.WriteLine(Out_long);
                    writer.WriteLine(Out_short);
                    writer.WriteLine(only_short);
                    writer.WriteLine(only_long);
                    writer.WriteLine(depo);
                    writer.WriteLine(Vgo);

                    writer.Close();
                }
            }
            catch (Exception)
            {
                // ignore
            }
        }
        private void Load()
        {
            if (!File.Exists(@"Engine\" + NameStrategyUniq + @"SettingsBot.txt"))
            {
                return;
            }
            try
            {
                using (StreamReader reader = new StreamReader(@"Engine\" + NameStrategyUniq + @"SettingsBot.txt"))
                {
                    n_long = Convert.ToDecimal(reader.ReadLine());
                    n_short = Convert.ToDecimal(reader.ReadLine());
                    w1 = Convert.ToDecimal(reader.ReadLine());
                    w2 = Convert.ToDecimal(reader.ReadLine());
                    In_long = Convert.ToDouble(reader.ReadLine());
                    In_short = Convert.ToDouble(reader.ReadLine());
                    Out_long = Convert.ToDouble(reader.ReadLine());
                    Out_short = Convert.ToDouble(reader.ReadLine());
                    only_short = Convert.ToBoolean(reader.ReadLine());
                    only_long = Convert.ToBoolean(reader.ReadLine());
                    depo = Convert.ToDecimal(reader.ReadLine());
                    Vgo = Convert.ToDouble(reader.ReadLine());



                    reader.Close();
                }
            }
            catch (Exception)
            {
                // ignore
            }
        }
        public Dictionary<string, decimal> Go = new Dictionary<string, decimal>();
        private void LoadGO()
        {
            var file = "Si.txt";
            var path = $"{Environment.CurrentDirectory}\\Data\\GO\\{file}";

            if (!File.Exists(path)) { return; }

            var line = "";
            try
            {
                var reader = new StreamReader(path);
                line = reader.ReadLine();

                while (line != null)
                {
                    line = reader.ReadLine();
                    if (line != null)
                    {
                        var str = line.Split(',');

                        if (str.Length > 10 && !string.IsNullOrEmpty(str[10]))
                        {
                            var date = str[2];
                            var go = Convert.ToDecimal(str[10]);

                            if (go > 0)
                            {
                                Go[date] = go;
                            }
                        }
                    }
                }
                reader.Close();
            }
            catch (Exception)
            {

            }            
        }

        private void PairBot_PositionOpeningSuccesEvent1(Position position)
        {                
        }
        private void PairBot_CandleFinishedEvent1(List<Candle> candles)
        {
            if(candles.Count > n_short)
            {
                decimal c2 = candles[candles.Count - 1].Close;
                int x = 1;
                decimal sum_short = 0;
                while (x <= n_short)
                {
                    sum_short += candles[candles.Count - x].Close;
                    x++;
                }
                decimal ma2_short = sum_short / n_short;
                Ma2_short = ma2_short;
                v2 = Ma1_short / Ma2_short * w2 / w1;
                C2 = c2;
            }
            else
            {
                return;
            }
            if (candles.Count > n_long)
            {
                decimal c2 = candles[candles.Count - 1].Close;
                int x = 1;
                decimal sum_long = 0;
                while (x <= n_long)
                {
                    sum_long += candles[candles.Count - x].Close;
                    x++;
                }
                decimal ma2_long = sum_long / n_long;
                Ma2_long = ma2_long;
                v2 = Ma1_long / Ma2_long * w2 / w1;
                C2 = c2;
            }
            else
            {
                return;
            }
        }
        private void PairBot_PositionOpeningSuccesEvent(Position positions)
        {            
        }
        private void PairBot_CandleFinishedEvent(List<Candle> candles)
        {
            if (candles.Count > n_short)
            {
                decimal c1 = candles[candles.Count - 1].Close;
                DateTime date = candles[candles.Count - 1].TimeStart;
                var dt = date.ToString("yyyyMMdd");
                if (Go.ContainsKey(dt))
                {
                    TekGo = Go[dt];
                }
                
                int x = 1;
                decimal sum_short = 0;
                while (x <= n_short)
                {
                    sum_short += candles[candles.Count - x].Close;
                    x++;
                }
                decimal ma1_short = sum_short / n_short;
                Ma1_short = ma1_short;
                v1 = w1;
                C1 = c1;
                ma_short = Ma1_short - Ma2_short * v2;                
            }
            else
            {
                return;
            }
            
            if (candles.Count > n_long)
            {
                decimal c1 = candles[candles.Count - 1].Close;
                int x = 1;
                decimal sum_long = 0;
                while (x <= n_long - 1)
                {
                    sum_long += candles[candles.Count - x].Close;
                    x++;
                }
                decimal ma1_long = (sum_long + c1_sec) / n_long;
                Ma1_long = ma1_long;
                v1 = w1;
                C1 = c1;                
            }
            else
            {
                return;
            }
            decimal spf = c1_sec - c2_sec * v2;
            double inn_long = In_long / 100 * Convert.ToDouble(c1_sec);
            double outt_long = Out_long / 100 * Convert.ToDouble(c1_sec);
            double inn_short = In_short / 100 * Convert.ToDouble(c1_sec);
            double outt_short = Out_short / 100 * Convert.ToDouble(c1_sec);
            double OBSP = Convert.ToDouble(ma_long) - inn_long;
            double priceOB = Math.Floor((OBSP + Convert.ToDouble(c2_sec * v2)) / Convert.ToDouble(TabsSimple[0].Securiti.PriceStep))
                * Convert.ToDouble(TabsSimple[0].Securiti.PriceStep);
            double CBSP = Convert.ToDouble(ma_long) + outt_long;
            double priceCB = Math.Ceiling((CBSP + Convert.ToDouble(c2_sec * v2)) / Convert.ToDouble(TabsSimple[0].Securiti.PriceStep))
                * Convert.ToDouble(TabsSimple[0].Securiti.PriceStep);
            double OSSP = Convert.ToDouble(ma_short) + inn_short;
            double priceOS = Math.Ceiling((OSSP + Convert.ToDouble(c2_sec * v2)) / Convert.ToDouble(TabsSimple[0].Securiti.PriceStep))
                * Convert.ToDouble(TabsSimple[0].Securiti.PriceStep);
            double CSSP = Convert.ToDouble(ma_short) - outt_short;
            double priceCS = Math.Ceiling((CSSP + Convert.ToDouble(c2_sec * v2)) / Convert.ToDouble(TabsSimple[0].Securiti.PriceStep))
                * Convert.ToDouble(TabsSimple[0].Securiti.PriceStep);
            PriceOS = priceOS;
            PriceCS = priceCS;
            PriceOB = priceOB;
            PriceCB = priceCB;
            ossp = OSSP;
            cssp = CSSP;
            obsp = OBSP;
            cbsp = CBSP;
            Spf = Convert.ToDouble(spf);
            ma_long = Ma1_long - Ma2_long * v2;
            volume = Math.Round(depo * Convert.ToDecimal(Vgo) / TekGo);

            c1_sec = candles[candles.Count - 1].Close;
            if (candles.Count > n_short && only_short == true)
            {
                TradeLogic_short();
            }
            if (candles.Count > n_long && only_long == true)
            {
                TradeLogic_long();
            }

        }
        public void TradeLogic_short()

        {
            List<Position> positions = TabsSimple[0].PositionsOpenAll;  
            
            if(Convert.ToDouble(Spf) >= ossp && positions.Count == 0)
            {
                TabsSimple[0].SellAtLimit(volume, Convert.ToDecimal(PriceOS),"sell");
            }
            if (positions != null && positions.Count > 0 && positions[0].State == PositionStateType.Open && positions[0].SignalTypeOpen == "sell")
            {//закрытие позиции

                if (Convert.ToDouble(Spf) <= cssp)
                {
                    TabsSimple[0].CloseAtLimit(positions[0], Convert.ToDecimal(PriceCS), positions[0].OpenVolume);
                }
            }            
        }
        public void TradeLogic_long()

        {
            List<Position> positions = TabsSimple[0].PositionsOpenAll;
            
            if (Convert.ToDouble(Spf) <= obsp && positions.Count == 0)
            {
                TabsSimple[0].BuyAtLimit(volume, Convert.ToDecimal(PriceOB), "buy");
            }
            if (positions != null && positions.Count > 0 && positions[0].State == PositionStateType.Open && positions[0].SignalTypeOpen == "buy")
            {
                if (Convert.ToDouble(Spf) >= cbsp)
                {
                    TabsSimple[0].CloseAtLimit(positions[0], Convert.ToDecimal(PriceCB), positions[0].OpenVolume);
                }
            }
        }


        public override string GetNameStrategyType()
        {
            return "PairBot";
        }

        public override void ShowIndividualSettingsDialog()
        {
            PairBotUi ui = new PairBotUi(this);
            ui.ShowDialog();

              
        }
    }

    public class PairBot2 : BotPanel
    {
        public decimal n_long;//количество свечей для расчёта автовеса для лонга минуты
        public decimal n_short;//количество свечей для расчёта автовеса для шорта минуты
        public decimal w1;//вес первого инструмента
        public decimal w2;//вес второго инструмента
        public double Spf;//спрэд
        public double In_long;//коэф на вход для лонга
        public double In_short;//коэф на вход для шорта
        public double Out_long;//коэф на выход для лонга
        public double Out_short;//коэф на выход для шорта
        public decimal Ma1_long = 0;//ма первой ноги для лонга
        public decimal Ma2_long = 0;//ма второй ноги для лонга
        public decimal Ma1_short = 0;//ма первой ноги для шорта
        public decimal Ma2_short = 0;//ма второй ноги для шорта
        public decimal v1 = -1; //коэф веса в спреде первой ноги
        public decimal v2 = 1;//коэф веса в спреде второй ноги
        public decimal ma_long = 0; //ма спреда для лонга
        public decimal ma_short = 0; //ма спреда для шорта
        public decimal C1; //закрытие первой ноги минуты
        public decimal C2; //закрытие второй ноги минуты
        public decimal c1_sec; //закрытие первой ноги секунды
        public decimal c2_sec; //закрытие второй ноги секунды
        public double cbsp;
        public double PriceCB;
        public double cssp;
        public double PriceCS;
        public double PriceOB;
        public double PriceOS;
        public double obsp;
        public double ossp;
        public bool only_long;
        public bool only_short;
        public decimal TekGo;
        public decimal volume;
        public double Vgo;
        public decimal depo;        
        public PairBot2(string name, StartProgram startProgram) : base(name, startProgram)
        {
            Load();
            LoadGO();
            TabCreate(BotTabType.Simple);
            TabCreate(BotTabType.Simple);
            TabCreate(BotTabType.Simple);
            TabCreate(BotTabType.Simple);

            TabsSimple[0].CandleFinishedEvent += PairBot2_first_sec_CandleFinishedEvent;//секунды первая нога
            TabsSimple[1].CandleFinishedEvent += PairBot2_second_sec_CandleFinishedEvent;//вторая нога секунда
            TabsSimple[0].CandleFinishedEvent += PairBot2_first_min_CandleFinishedEvent;//минуты первая нога
            TabsSimple[1].CandleFinishedEvent += PairBot2_second_min_CandleFinishedEvent;//минуты вторая нога

            TabsSimple[0].PositionOpeningSuccesEvent += PairBot2_first_sec_PositionOpeningSuccesEvent;//первая нога секунды
            TabsSimple[1].PositionOpeningSuccesEvent += PairBot2_second_sec_PositionOpeningSuccesEvent;//вторая нога секунды
            TabsSimple[2].PositionOpeningSuccesEvent += PairBot2_first_min_PositionOpeningSuccesEvent;//первая нога минуты
            TabsSimple[3].PositionOpeningSuccesEvent += PairBot2_second_min_PositionOpeningSuccesEvent;//вторая нога минуты
        }
        
        private void PairBot2_second_min_PositionOpeningSuccesEvent(Position position)
        {            
        }

        private void PairBot2_first_min_PositionOpeningSuccesEvent(Position position)
        {         
        }

        private void PairBot2_second_sec_PositionOpeningSuccesEvent(Position position)
        {            
        }

        private void PairBot2_first_sec_PositionOpeningSuccesEvent(Position position)
        {              
        }
        
        public Dictionary<string, decimal> Go = new Dictionary<string, decimal>();
        private void LoadGO()
        {
            var file = "Si.txt";
            var path = $"{Environment.CurrentDirectory}\\Data\\GO\\{file}";

            if (!File.Exists(path)) { return; }

            var line = "";
            try
            {
                var reader = new StreamReader(path);
                line = reader.ReadLine();

                while (line != null)
                {
                    line = reader.ReadLine();
                    if (line != null)
                    {
                        var str = line.Split(',');

                        if (str.Length > 10 && !string.IsNullOrEmpty(str[10]))
                        {
                            var date = str[2];
                            var go = Convert.ToDecimal(str[10]);

                            if (go > 0)
                            {
                                Go[date] = go;
                            }
                        }
                    }
                }
                reader.Close();
            }
            catch (Exception)
            {

            }
        }
        public void Save()
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(@"Engine\" + NameStrategyUniq + @"SettingsBot.txt", false)
                    )
                {
                    writer.WriteLine(n_long);
                    writer.WriteLine(n_short);
                    writer.WriteLine(w1);
                    writer.WriteLine(w2);
                    writer.WriteLine(In_long);
                    writer.WriteLine(In_short);
                    writer.WriteLine(Out_long);
                    writer.WriteLine(Out_short);
                    writer.WriteLine(only_short);
                    writer.WriteLine(only_long);
                    writer.WriteLine(depo);
                    writer.WriteLine(Vgo);

                    writer.Close();
                }
            }
            catch (Exception)
            {
                // ignore
            }
        }
        private void Load()
        {
            if (!File.Exists(@"Engine\" + NameStrategyUniq + @"SettingsBot.txt"))
            {
                return;
            }
            try
            {
                using (StreamReader reader = new StreamReader(@"Engine\" + NameStrategyUniq + @"SettingsBot.txt"))
                {
                    n_long = Convert.ToDecimal(reader.ReadLine());
                    n_short = Convert.ToDecimal(reader.ReadLine());
                    w1 = Convert.ToDecimal(reader.ReadLine());
                    w2 = Convert.ToDecimal(reader.ReadLine());
                    In_long = Convert.ToDouble(reader.ReadLine());
                    In_short = Convert.ToDouble(reader.ReadLine());
                    Out_long = Convert.ToDouble(reader.ReadLine());
                    Out_short = Convert.ToDouble(reader.ReadLine());
                    only_short = Convert.ToBoolean(reader.ReadLine());
                    only_long = Convert.ToBoolean(reader.ReadLine());
                    depo = Convert.ToDecimal(reader.ReadLine());
                    Vgo = Convert.ToDouble(reader.ReadLine());



                    reader.Close();
                }
            }
            catch (Exception)
            {
                // ignore
            }
        }
        private void PairBot2_second_min_CandleFinishedEvent(List<Candle> candles)
        {
            if (candles.Count > n_short)
            {
                decimal c2 = candles[candles.Count - 1].Close;
                int x = 1;
                decimal sum_short = 0;
                while (x <= n_short)
                {
                    sum_short += candles[candles.Count - x].Close;
                    x++;
                }
                decimal ma2_short = sum_short / n_short;
                Ma2_short = ma2_short;
                v2 = Ma1_short / Ma2_short * w2 / w1;
                C2 = c2;
            }
            else
            {
                return;
            }

            if (candles.Count > n_long)
            {
                decimal c2 = candles[candles.Count - 1].Close;
                int x = 1;
                decimal sum_long = 0;
                while (x <= n_long)
                {
                    sum_long += candles[candles.Count - x].Close;
                    x++;
                }
                decimal ma2_long = sum_long / n_long;
                Ma2_long = ma2_long;
                v2 = Ma1_long / Ma2_long * w2 / w1;
                C2 = c2;
            }
            else
            {
                return;
            }
        }
        private void PairBot2_first_min_CandleFinishedEvent(List<Candle> candles)
        {
            if (candles.Count > n_short)
            {
                decimal c1 = candles[candles.Count - 1].Close;
                DateTime date = candles[candles.Count - 1].TimeStart;
                var dt = date.ToString("yyyyMMdd");
                if (Go.ContainsKey(dt))
                {
                    TekGo = Go[dt];
                }

                int x = 1;
                decimal sum_short = 0;
                while (x <= n_short)
                {
                    sum_short += candles[candles.Count - x].Close;
                    x++;
                }
                decimal ma1_short = sum_short / n_short;
                Ma1_short = ma1_short;
                v1 = w1;
                C1 = c1;
                ma_short = Ma1_short - Ma2_short * v2;
            }
            else
            {
                return;
            }

            if (candles.Count > n_long)
            {
                decimal c1 = candles[candles.Count - 1].Close;
                int x = 1;
                decimal sum_long = 0;
                while (x <= n_long)
                {
                    sum_long += candles[candles.Count - x].Close;
                    x++;
                }
                decimal ma1_long = sum_long / n_long;
                Ma1_long = ma1_long;
                v1 = w1;
                C1 = c1;
            }
            else
            {
                return;
            }

        }
        private void PairBot2_second_sec_CandleFinishedEvent(List<Candle> candles)
        {
            c2_sec = candles[candles.Count - 1].Close;
        }

        private void PairBot2_first_sec_CandleFinishedEvent(List<Candle> candles)
        {
            if (candles.Count > n_long && TekGo > 0)
            {
                c1_sec = candles[candles.Count - 1].Close;

                decimal spf = c1_sec - c2_sec * v2;
                double inn_long = In_long / 100 * Convert.ToDouble(c1_sec);
                double outt_long = Out_long / 100 * Convert.ToDouble(c1_sec);
                double inn_short = In_short / 100 * Convert.ToDouble(c1_sec);
                double outt_short = Out_short / 100 * Convert.ToDouble(c1_sec);
                double OBSP = Convert.ToDouble(ma_long) - inn_long;
                double priceOB = Math.Ceiling((OBSP + Convert.ToDouble(c2_sec * v2)) / Convert.ToDouble(TabsSimple[0].Securiti.PriceStep))
                    * Convert.ToDouble(TabsSimple[0].Securiti.PriceStep);
                double CBSP = Convert.ToDouble(ma_long) + outt_long;
                double priceCB = Math.Ceiling((CBSP + Convert.ToDouble(c2_sec * v2)) / Convert.ToDouble(TabsSimple[0].Securiti.PriceStep))
                    * Convert.ToDouble(TabsSimple[0].Securiti.PriceStep);
                double OSSP = Convert.ToDouble(ma_short) + inn_short;
                double priceOS = Math.Ceiling((OSSP + Convert.ToDouble(c2_sec * v2)) / Convert.ToDouble(TabsSimple[0].Securiti.PriceStep))
                    * Convert.ToDouble(TabsSimple[0].Securiti.PriceStep);
                double CSSP = Convert.ToDouble(ma_short) - outt_short;
                double priceCS = Math.Ceiling((CSSP + Convert.ToDouble(c2_sec * v2)) / Convert.ToDouble(TabsSimple[0].Securiti.PriceStep))
                    * Convert.ToDouble(TabsSimple[0].Securiti.PriceStep);
                PriceOS = priceOS;
                PriceCS = priceCS;
                PriceOB = priceOB;
                PriceCB = priceCB;
                ossp = OSSP;
                cssp = CSSP;
                obsp = OBSP;
                cbsp = CBSP;
                Spf = Convert.ToDouble(spf);
                ma_long = Ma1_long - Ma2_long * v2;
                volume = Math.Round(depo * Convert.ToDecimal(Vgo) / TekGo);
            }

            if (candles.Count > n_short && only_short == true)
            {
                TradeLogic_short();
            }
            if (candles.Count > n_long && only_long == true)
            {
                TradeLogic_long();
            }
        }
        public void TradeLogic_short()

        {
            List<Position> positions = TabsSimple[0].PositionsOpenAll;

            if (Convert.ToDouble(Spf) >= ossp && positions.Count == 0)
            {
                TabsSimple[0].SellAtLimit(volume, Convert.ToDecimal(PriceOS), "sell");
            }
            if (positions != null && positions.Count > 0 && positions[0].State == PositionStateType.Open && positions[0].SignalTypeOpen == "sell")
            {//закрытие позиции

                if (Convert.ToDouble(Spf) <= cssp)
                {
                    TabsSimple[0].CloseAtLimit(positions[0], Convert.ToDecimal(PriceCS), positions[0].OpenVolume);
                }
            }
        }
        public void TradeLogic_long()

        {
            List<Position> positions = TabsSimple[0].PositionsOpenAll;

            if (Convert.ToDouble(Spf) <= obsp && positions.Count == 0)
            {
                TabsSimple[0].BuyAtLimit(volume, Convert.ToDecimal(PriceOB), "buy");
            }
            if (positions != null && positions.Count > 0 && positions[0].State == PositionStateType.Open && positions[0].SignalTypeOpen == "buy")
            {
                if (Convert.ToDouble(Spf) >= cbsp)
                {
                    TabsSimple[0].CloseAtLimit(positions[0], Convert.ToDecimal(PriceCB), positions[0].OpenVolume);
                }
            }
        }
        public override string GetNameStrategyType()
        {
            return "PairBot2";
        }

        public override void ShowIndividualSettingsDialog()
        {
            PairBot2Ui ui = new PairBot2Ui(this);
            ui.ShowDialog();
        }
    }

    public class BotFactory
    {
        private static readonly Dictionary<string, Type> BotsWithAttribute = GetTypesWithBotAttribute();

        /// <summary>
        /// list robots name / 
        /// список доступных роботов
        /// </summary>
        public static List<string> GetNamesStrategy()
        {
            List<string> result = new List<string>();

            //result.Add("BotHftStyle");
            result.Add("PairBot2");
            result.Add("PairBot");
            result.Add("BotArbitrage");
            result.Add("BotAllTrades");
            result.Add("BotMarketDepth");
            result.Add("NewRobot7");
            result.Add("NewRobot6");
            result.Add("NewRobotHW3");
            result.Add("NewRobot5");
            result.Add("NewRobotHW2");
            result.Add("NewRobot4");
            result.Add("NewRobot3");
            result.Add("NewRobot2");
            result.Add("RobotHW1");
            result.Add("HummerBot");
            result.Add("SmaScreener");
            result.Add("Fisher");
            result.Add("Engine");
            result.Add("ScreenerEngine");
            result.Add("ClusterEngine");
            result.Add("SmaTrendSample");
            result.Add("FundBalanceDivergenceBot");
            result.Add("PairTraderSimple");
            result.Add("MomentumMACD");
            result.Add("MarketMakerBot");
            result.Add("PatternTrader");
            result.Add("HighFrequencyTrader");
            result.Add("EnvelopTrend");
            result.Add("Williams Band");
            result.Add("TwoLegArbitrage");
            result.Add("ThreeSoldier");
            result.Add("TimeOfDayBot");
            result.Add("PriceChannelTrade");
            result.Add("SmaStochastic");
            result.Add("ClusterCountertrend");
            result.Add("PairTraderSpreadSma");
            result.Add("WilliamsRangeTrade");
            result.Add("ParabolicSarTrade");
            result.Add("PivotPointsRobot");
            result.Add("RsiContrtrend");
            result.Add("PinBarTrade");
            result.Add("BbPowerTrade");
            result.Add("BollingerRevers");
            result.Add("BollingerTrailing");
            result.Add("CciTrade");
            result.Add("MacdRevers");
            result.Add("MacdTrail");
            result.Add("OneLegArbitrage");
            result.Add("PairRsiTrade");
            result.Add("PriceChannelBreak");
            result.Add("PriceChannelVolatility");
            result.Add("RsiTrade");
            result.Add("RviTrade");
            result.AddRange(BotsWithAttribute.Keys);

            List<string> resultTrue = new List<string>();

            for (int i = 0; i < result.Count; i++)
            {
                bool isInArray = false;

                for (int i2 = 0; i2 < resultTrue.Count; i2++)
                {
                    if (resultTrue[i2][0] > result[i][0])
                    {
                        resultTrue.Insert(i2, result[i]);
                        isInArray = true;
                        break;
                    }
                }

                if (isInArray == false)
                {
                    resultTrue.Add(result[i]);
                }
            }


            return resultTrue;
        }

        /// <summary>
        /// create robot
        /// создать робота
        /// </summary>
        public static BotPanel GetStrategyForName(string nameClass, string name, StartProgram startProgram, bool isScript)
        {
            BotPanel bot = null;

            // примеры и бесплатные боты
            if (isScript && bot == null)
            {
                bot = CreateScriptStrategyByName(nameClass, name, startProgram);
                return bot;
            }
            /*if (nameClass == "BotHftStyle")
            {
                bot = new BotHftStyle(name, startProgram);
            }*/
            if (nameClass == "PairBot2")
            {
                bot = new PairBot2(name, startProgram);
            }
            if (nameClass == "PairBot")
            {
                bot = new PairBot(name, startProgram);
            }
            if (nameClass == "BotArbitrage")
            {
                bot = new BotArbitrage(name, startProgram);
            }
            if (nameClass == "BotAllTrades")
            {
                bot = new BotAllTrades(name, startProgram);
            }
            if (nameClass == "BotMarketDepth")
            {
                bot = new BotMarketDepth(name, startProgram);
            }
            if (nameClass == "NewRobot7")
            {
                bot = new NewRobot7(name, startProgram);
            }
            if (nameClass == "NewRobot6")
            {
                bot = new NewRobot6(name, startProgram);
            }
            if (nameClass == "NewRobotHW3")
            {
                bot = new NewRobotHW3(name, startProgram);
            }
            if (nameClass == "NewRobot5")
            {
                bot = new NewRobot5(name, startProgram);
            }
            if (nameClass == "NewRobotHW2")
            {
                bot = new NewRobotHW2(name, startProgram);
            }
            if (nameClass == "NewRobot4")
            {
                bot = new NewRobot4(name, startProgram);
            }
            if (nameClass == "HummerBot")
            {
                bot = new RobotHammer(name, startProgram);
            }
            if (nameClass == "NewRobot3")
            {
                bot = new NewRobot3(name, startProgram);
            }
            if (nameClass == "NewRobot2")
            {
                bot = new NewRobot2(name, startProgram);
            }
            if (nameClass == "RobotHW1")
            {
                bot = new RobotHW1(name, startProgram);
            }
            if (nameClass == "SmaScreener")
            {
                bot = new SmaScreener(name, startProgram);
            }
            if (nameClass == "ScreenerEngine")
            {
                bot = new ScreenerEngine(name, startProgram);
            }
            if (nameClass == "SmaTrendSample")
            {
                bot = new SmaTrendSample(name, startProgram);
            }
            if (nameClass == "TimeOfDayBot")
            {
                bot = new TimeOfDayBot(name, startProgram);
            }
            if (nameClass == "Fisher")
            {
                bot = new Fisher(name, startProgram);
            }
            if (nameClass == "FundBalanceDivergenceBot")
            {
                bot = new FundBalanceDivergenceBot(name, startProgram);
            }
            if (nameClass == "BbPowerTrade")
            {
                bot = new BbPowerTrade(name, startProgram);
            }
            if (nameClass == "BollingerRevers")
            {
                bot = new BollingerRevers(name, startProgram);
            }
            if (nameClass == "BollingerTrailing")
            {
                bot = new BollingerTrailing(name, startProgram);
            }
            if (nameClass == "CciTrade")
            {
                bot = new CciTrade(name, startProgram);
            }
            if (nameClass == "MacdRevers")
            {
                bot = new MacdRevers(name, startProgram);
            }
            if (nameClass == "MacdTrail")
            {
                bot = new MacdTrail(name, startProgram);
            }
            if (nameClass == "OneLegArbitrage")
            {
                bot = new OneLegArbitrage(name, startProgram);
            }
            if (nameClass == "PairRsiTrade")
            {
                bot = new PairRsiTrade(name, startProgram);
            }
            if (nameClass == "PriceChannelBreak")
            {
                bot = new PriceChannelBreak(name, startProgram);
            }
            if (nameClass == "PriceChannelVolatility")
            {
                bot = new PriceChannelVolatility(name, startProgram);
            }
            if (nameClass == "RsiTrade")
            {
                bot = new RsiTrade(name, startProgram);
            }
            if (nameClass == "RviTrade")
            {
                bot = new RviTrade(name, startProgram);
            }

            if (nameClass == "MomentumMACD")
            {
                bot = new MomentumMacd(name, startProgram);
            }

            if (nameClass == "Engine")
            {
                bot = new CandleEngine(name, startProgram);
            }
            if (nameClass == "ClusterEngine")
            {
                bot = new ClusterEngine(name, startProgram);
            }

            if (nameClass == "PairTraderSimple")
            {
                bot = new PairTraderSimple(name, startProgram);
            }
            if (nameClass == "EnvelopTrend")
            {
                bot = new EnvelopTrend(name, startProgram);
            }
            if (nameClass == "ClusterCountertrend")
            {
                bot = new ClusterCountertrend(name, startProgram);
            }
            if (nameClass == "PatternTrader")
            {
                bot = new PatternTrader(name, startProgram);
            }
            if (nameClass == "HighFrequencyTrader")
            {
                bot = new HighFrequencyTrader(name, startProgram);
            }
            if (nameClass == "PivotPointsRobot")
            {
                bot = new PivotPointsRobot(name, startProgram);
            }
            if (nameClass == "Williams Band")
            {
                bot = new StrategyBillWilliams(name, startProgram);
            }
            if (nameClass == "MarketMakerBot")
            {
                bot = new MarketMakerBot(name, startProgram);
            }
            if (nameClass == "ParabolicSarTrade")
            {
                bot = new ParabolicSarTrade(name, startProgram);
            }
            if (nameClass == "PriceChannelTrade")
            {
                bot = new PriceChannelTrade(name, startProgram);
            }
            if (nameClass == "WilliamsRangeTrade")
            {
                bot = new WilliamsRangeTrade(name, startProgram);
            }
            if (nameClass == "SmaStochastic")
            {
                bot = new SmaStochastic(name, startProgram);
            }
            if (nameClass == "PinBarTrade")
            {
                bot = new PinBarTrade(name, startProgram);
            }
            if (nameClass == "TwoLegArbitrage")
            {
                bot = new TwoLegArbitrage(name, startProgram);
            }
            if (nameClass == "ThreeSoldier")
            {
                bot = new ThreeSoldier(name, startProgram);
            }
            if (nameClass == "RsiContrtrend")
            {
                bot = new RsiContrtrend(name, startProgram);
            }
            if (nameClass == "PairTraderSpreadSma")
            {
                bot = new PairTraderSpreadSma(name, startProgram);
            }
            if (BotsWithAttribute.ContainsKey(nameClass))
            {
                Type botType = BotsWithAttribute[nameClass];
                bot = (BotPanel)Activator.CreateInstance(botType, name, startProgram);
            }

            return bot;
        }

        static Dictionary<string, Type> GetTypesWithBotAttribute()
        {
            Assembly assembly = Assembly.GetAssembly(typeof(BotPanel));
            Dictionary<string, Type> bots = new Dictionary<string, Type>();
            foreach (Type type in assembly.GetTypes())
            {
                object[] attributes = type.GetCustomAttributes(typeof(BotAttribute), false);
                if (attributes.Length > 0)
                {
                    bots[((BotAttribute)attributes[0]).Name] = type;
                }
            }

            return bots;
        }

        // Scripts
        public static List<string> GetScriptsNamesStrategy()
        {

            if (Directory.Exists(@"Custom") == false)
            {
                Directory.CreateDirectory(@"Custom");
            }

            if (Directory.Exists(@"Custom\Robots") == false)
            {
                Directory.CreateDirectory(@"Custom\Robots");
            }

            List<string> resultOne = GetFullNamesFromFolder(@"Custom\Robots");

            for (int i = 0; i < resultOne.Count; i++)
            {
                resultOne[i] = resultOne[i].Split('\\')[resultOne[i].Split('\\').Length - 1];
                resultOne[i] = resultOne[i].Split('.')[0];
            }

            // resultOne.Add("Ssma");

            List<string> resultTrue = new List<string>();

            for (int i = 0; i < resultOne.Count; i++)
            {
                bool isInArray = false;

                for (int i2 = 0; i2 < resultTrue.Count; i2++)
                {
                    if (resultTrue[i2][0] > resultOne[i][0])
                    {
                        resultTrue.Insert(i2, resultOne[i]);
                        isInArray = true;
                        break;
                    }
                }

                if (isInArray == false)
                {
                    resultTrue.Add(resultOne[i]);
                }
            }

            return resultTrue;
        }

        private static List<string> GetFullNamesFromFolder(string directory)
        {
            List<string> results = new List<string>();

            string[] subDirectories = Directory.GetDirectories(directory);

            for (int i = 0; i < subDirectories.Length; i++)
            {
                results.AddRange(GetFullNamesFromFolder(subDirectories[i]));
            }

            string[] files = Directory.GetFiles(directory);

            results.AddRange(files.ToList());

            for (int i = 0; i < results.Count; i++)
            {
                if (results[i].EndsWith("cs") == false)
                {
                    results.RemoveAt(i);
                    i--;
                }
            }

            for (int i = 0; i < results.Count; i++)
            {
                if (results.Contains("Dlls"))
                {
                    results.RemoveAt(i);
                    i--;
                }
            }

            return results;
        }

        public static BotPanel CreateScriptStrategyByName(string nameClass, string name, StartProgram startProgram)
        {
            BotPanel bot = null;

            if (bot == null)
            {
                List<string> fullPaths = GetFullNamesFromFolder(@"Custom\Robots");

                string longNameClass = nameClass + ".txt";
                string longNameClass2 = nameClass + ".cs";

                string myPath = "";

                for (int i = 0; i < fullPaths.Count; i++)
                {
                    string nameInFile =
                        fullPaths[i].Split('\\')[fullPaths[i].Split('\\').Length - 1];

                    if (nameInFile == longNameClass ||
                        nameInFile == longNameClass2)
                    {
                        myPath = fullPaths[i];
                        break;
                    }
                }

                if (myPath == "")
                {
                    return null;
                }

                bot = Serialize(myPath, nameClass, name, startProgram);
            }

            bot.IsScript = true;
            bot.FileName = nameClass;

            return bot;
        }

        private static bool _isFirstTime = true;

        private static string[] linksToDll;

        private static List<BotPanel> _serializedPanels = new List<BotPanel>();

        private static BotPanel Serialize(string path, string nameClass, string name, StartProgram startProgram)
        {
            // 1 пробуем клонировать из ранее сериализованных объектов. Это быстрее чем подымать из файла

            for (int i = 0; i < _serializedPanels.Count; i++)
            {
                if (_serializedPanels[i].GetType().Name == nameClass)
                {
                    object[] param = new object[] { name, startProgram };
                    BotPanel newPanel = (BotPanel)Activator.CreateInstance(_serializedPanels[i].GetType(), param);
                    return newPanel;
                }
            }

            // сериализуем из файла
            try
            {
                if (linksToDll == null)
                {
                    var assemblies = AppDomain.CurrentDomain.GetAssemblies();

                    string[] res = Array.ConvertAll<Assembly, string>(assemblies, (x) =>
                    {
                        if (!x.IsDynamic)
                        {
                            return x.Location;
                        }

                        return null;
                    });

                    for (int i = 0; i < res.Length; i++)
                    {
                        if (string.IsNullOrEmpty(res[i]))
                        {
                            List<string> list = res.ToList();
                            list.RemoveAt(i);
                            res = list.ToArray();
                            i--;
                        }
                        else if (res[i].Contains("System.Runtime.Serialization")
                                 || i > 24)
                        {
                            List<string> list = res.ToList();
                            list.RemoveAt(i);
                            res = list.ToArray();
                            i--;
                        }
                    }

                    string dllPath = AppDomain.CurrentDomain.BaseDirectory + "System.Runtime.Serialization.dll";

                    List<string> listRes = res.ToList();
                    listRes.Add(dllPath);
                    res = listRes.ToArray();
                    linksToDll = res;
                }

                List<string> dllsToCompiler = linksToDll.ToList();

                List<string> dllsFromPath = GetDllsPathFromFolder(path);

                if (dllsFromPath != null && dllsFromPath.Count != 0)
                {
                    for (int i = 0; i < dllsFromPath.Count; i++)
                    {
                        string dll = dllsFromPath[i].Split('\\')[dllsFromPath[i].Split('\\').Length - 1];

                        if (dllsToCompiler.Find(d => d.Contains(dll)) == null)
                        {
                            dllsToCompiler.Add(dllsFromPath[i]);
                        }
                    }
                }

                CompilerParameters cp = new CompilerParameters(dllsToCompiler.ToArray());

                // Помечаем сборку, как временную
                cp.GenerateInMemory = true;
                cp.IncludeDebugInformation = true;
                cp.TempFiles.KeepFiles = false;


                string folderCur = AppDomain.CurrentDomain.BaseDirectory + "Engine\\Temp";

                if (Directory.Exists(folderCur) == false)
                {
                    Directory.CreateDirectory(folderCur);
                }

                folderCur += "\\Bots";

                if (Directory.Exists(folderCur) == false)
                {
                    Directory.CreateDirectory(folderCur);
                }

                if (_isFirstTime)
                {
                    _isFirstTime = false;

                    string[] files = Directory.GetFiles(folderCur);

                    for (int i = 0; i < files.Length; i++)
                    {
                        try
                        {
                            File.Delete(files[i]);
                        }
                        catch
                        {
                            // ignore
                        }
                    }
                }

                cp.TempFiles = new TempFileCollection(folderCur, false);

                BotPanel result = null;

                string fileStr = ReadFile(path);

                //Объявляем провайдер кода С#
                CSharpCodeProvider prov = new CSharpCodeProvider();

                // Обрабатываем CSC компилятором
                CompilerResults results = prov.CompileAssemblyFromSource(cp, fileStr);

                if (results.Errors != null && results.Errors.Count != 0)
                {
                    string errorString = "Error! Robot script runTime compilation problem! \n";
                    errorString += "Path to Robot: " + path + " \n";

                    int errorNum = 1;

                    foreach (var error in results.Errors)
                    {
                        errorString += "Error Number: " + errorNum + " \n";
                        errorString += error.ToString() + "\n";
                        errorNum++;
                    }

                    throw new Exception(errorString);
                }
                //string name, StartProgram startProgram)

                List<object> param = new List<object>();
                param.Add(name);
                param.Add(startProgram);

                result = (BotPanel)results.CompiledAssembly.CreateInstance(
                    results.CompiledAssembly.DefinedTypes.ElementAt(0).FullName, false, BindingFlags.CreateInstance, null,
                    param.ToArray(), CultureInfo.CurrentCulture, null);

                if (result == null)
                {
                    string errorString = "Error! Robot script runTime compilation problem! \n";
                    errorString += "Path to robot: " + path + " \n";

                    int errorNum = 1;

                    foreach (var error in results.Errors)
                    {
                        errorString += "Error Number: " + errorNum + " \n";
                        errorString += error.ToString() + "\n";
                        errorNum++;
                    }

                    throw new Exception(errorString);
                }

                bool isInArray = false;

                for (int i = 0; i < _serializedPanels.Count; i++)
                {
                    if (_serializedPanels[i].GetType().Name == nameClass)
                    {
                        isInArray = true;
                        break;
                    }
                }

                if (isInArray == false)
                {
                    _serializedPanels.Add(result);
                }

                return result;
            }
            catch (Exception e)
            {
                string errorString = e.ToString();
                throw new Exception(errorString);
            }
        }

        private static List<string> GetDllsPathFromFolder(string path)
        {
            string folderPath = path.Remove(path.LastIndexOf('\\'), path.Length - path.LastIndexOf('\\'));

            if (Directory.Exists(folderPath + "\\Dlls") == false)
            {
                return null;
            }

            string[] filesInFolder = Directory.GetFiles(folderPath + "\\Dlls");

            List<string> dlls = new List<string>();

            for (int i = 0; i < filesInFolder.Length; i++)
            {
                if (filesInFolder[i].EndsWith(".dll") == false)
                {
                    continue;
                }

                string dllPath = AppDomain.CurrentDomain.BaseDirectory + filesInFolder[i];

                dlls.Add(dllPath);
            }

            return dlls;
        }

        private static string ReadFile(string path)
        {
            String result = "";

            using (StreamReader reader = new StreamReader(path))
            {
                result = reader.ReadToEnd();
                reader.Close();
            }

            return result;
        }

        // Names Include Bots With Params

        public static List<string> GetNamesStrategyWithParametersSync()
        {
            if (NeadToReload == false &&
                (_namesWithParam == null ||
                 _namesWithParam.Count == 0))
            {
                LoadBotsNames();
            }

            if (NeadToReload == false &&
                _namesWithParam != null &&
                _namesWithParam.Count != 0)
            {
                return _namesWithParam;
            }

            NeadToReload = false;

            List<Thread> workers = new List<Thread>();

            _namesWithParam = new List<string>();

            for (int i = 0; i < 3; i++)
            {
                Thread worker = new Thread(LoadNamesWithParam);
                worker.Name = i.ToString();
                workers.Add(worker);
                worker.Start();
            }

            while (workers.Find(w => w.IsAlive) != null)
            {
                Thread.Sleep(100);
            }

            SaveBotsNames();

            return _namesWithParam;
        }

        public static bool NeadToReload;

        private static void LoadBotsNames()
        {
            _namesWithParam.Clear();

            if (File.Exists("Engine\\OptimizerBots.txt") == false)
            {
                return;
            }

            using (StreamReader reader = new StreamReader("Engine\\OptimizerBots.txt"))
            {
                while (reader.EndOfStream == false)
                {
                    _namesWithParam.Add(reader.ReadLine());

                }
                reader.Close();
            }
        }

        private static void SaveBotsNames()
        {
            using (StreamWriter writer = new StreamWriter("Engine\\OptimizerBots.txt"))
            {
                for (int i = 0; i < _namesWithParam.Count; i++)
                {
                    writer.WriteLine(_namesWithParam[i]);
                }

                writer.Close();
            }
        }

        private static List<string> _namesWithParam = new List<string>();

        private static void LoadNamesWithParam()
        {
            List<string> names = GetNamesStrategy();

            int numThread = Convert.ToInt32(Thread.CurrentThread.Name);

            for (int i = numThread; i < names.Count; i += 3)
            {
                try
                {
                    BotPanel bot = GetStrategyForName(names[i], numThread.ToString(), StartProgram.IsOsOptimizer, false);

                    if (bot.Parameters == null ||
                        bot.Parameters.Count == 0)
                    {
                        //SendLogMessage("We are not optimizing. Without parameters/Не оптимизируем. Без параметров: " + bot.GetNameStrategyType(), LogMessageType.System);
                    }
                    else
                    {
                        if (bot.TabsScreener == null ||
                            bot.TabsScreener.Count == 0)
                        {
                            _namesWithParam.Add(names[i]);
                        }

                        // SendLogMessage("With parameters/С параметрами: " + bot.GetNameStrategyType(), LogMessageType.System);
                    }
                    if (numThread == 2)
                    {

                    }
                    bot.Delete();
                }
                catch
                {
                    continue;
                }
            }

            if (LoadNamesWithParamEndEvent != null)
            {
                LoadNamesWithParamEndEvent(_namesWithParam);
            }
        }

        public static event Action<List<string>> LoadNamesWithParamEndEvent;

    }
}