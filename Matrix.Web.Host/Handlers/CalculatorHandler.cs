
using log4net;
using Matrix.Domain.Entities;
using Matrix.Web.Host.Data;
using Microsoft.Office.Interop.Excel;
using Microsoft.Practices.ServiceLocation;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Matrix.Web.Host.Handlers
{
    class CalculatorHandler : IHandler
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(CalculatorHandler));
        private string connetionString = "Data Source=192.168.0.101;Initial Catalog=tarifsForCK;User ID=matrix;Password=matrix";

        public bool CanAccept(string what)
        {
            return what.StartsWith("calculator");
        }

        private readonly Bus bus;

        public CalculatorHandler()
        {
            bus = ServiceLocator.Current.GetInstance<Bus>();
        }
        
        public async Task<dynamic> Handle(dynamic session, dynamic message)
        {
            string what = message.head.what;
            Guid userId = Guid.Parse((string)session.user.id);
            
            if(what == "calculator-get-parameters")
            {
                var ans = Helper.BuildMessage(what);
                List<dynamic> regions = new List<dynamic>();
                List<DateTime> dates = new List<DateTime>();
                using (var con = new SqlConnection(connetionString))
                {
                    con.Open();
                    double[] arr = new double[720];
                    string sqlRegion = "SELECT* FROM [tarifsForCK].[dbo].[regions]";
                    SqlCommand cmdRegion = new SqlCommand();
                    cmdRegion.Connection = con;
                    cmdRegion.CommandText = sqlRegion;
                    List<string> idRegions = new List<string>();
                    using (SqlDataReader reader = cmdRegion.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            dynamic region = new ExpandoObject();
                            region.tag = reader.GetString(reader.GetOrdinal("idRegion"));
                            idRegions.Add(reader.GetString(reader.GetOrdinal("idRegion")));
                            region.name = reader.GetString(reader.GetOrdinal("name"));
                            regions.Add(region);
                        }
                    }
                    string strTmp = "'" + string.Join("','", idRegions.ToArray()) + "'";
                    IDictionary<string, List<dynamic>> dicRegion = new Dictionary<string, List<dynamic>>();
                    string sqlProvider = $"SELECT* FROM [tarifsForCK].[dbo].[providers] where region in ({strTmp})";
                    SqlCommand cmdProvider = new SqlCommand();
                    cmdProvider.Connection = con;
                    cmdProvider.CommandText = sqlProvider;

                    using (SqlDataReader reader = cmdProvider.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            dynamic provider = new ExpandoObject();
                            provider.id = reader.GetString(reader.GetOrdinal("identificator"));
                            provider.name = reader.GetString(reader.GetOrdinal("name"));
                            string key = reader.GetString(reader.GetOrdinal("region"));
                            if (dicRegion.ContainsKey(key))
                            {
                                dicRegion[key].Add(provider);
                            }
                            else
                            {
                                List<dynamic> listTmp = new List<dynamic>();
                                listTmp.Add(provider);
                                dicRegion.Add(key, listTmp);
                            }
                        }
                    }
                    for(int i = 0; i < regions.Count(); i++)
                    {
                        if (!dicRegion.ContainsKey(regions[i].tag)) continue;
                        regions[i].providers = dicRegion[regions[i].tag];
                    }



                    string sqlDates = "SELECT* FROM [tarifsForCK].[dbo].[dates]";
                    SqlCommand cmdDates = new SqlCommand();
                    cmdDates.Connection = con;
                    cmdDates.CommandText = sqlDates;
                    using (SqlDataReader reader = cmdDates.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            DateTime date = reader.GetDateTime(reader.GetOrdinal("Date"));
                            dates.Add(date);
                        }
                    }

                    con.Close();
                }
                ans.body.regions = regions;
                ans.body.dates = dates;
                return ans;
            }

            if (what == "calculator-get-data")
            {
                var ans = Helper.BuildMessage(what);
                ans.body.message = "";

                List<List<List<double>>> Vpotr = new List<List<List<double>>>();
                List<double> sumRecsDay = new List<double>();
                List<double> V1Day = new List<double>();
                IDictionary<Guid, string> dVoltageLevel = new Dictionary<Guid, string>();
                IDictionary<Guid, int> dKTr = new Dictionary<Guid, int>();
                List<Guid> objectIds = new List<Guid>();
                int month = Int32.Parse((string)message.body.data.month) + 1;
                int year = Int32.Parse((string)message.body.data.year);
                string monthYear = month.ToString() + year.ToString();
                int contract = Int32.Parse((string)message.body.data.contract) + 1;
                //int provider = Int32.Parse((string)message.body.data.provider) + 1;  ВРЕМЕННО ПОСТАВЩИК  = 1
                int provider = 1;
                int maxPower = Int32.Parse((string)message.body.data.maxPower);
                koef = Int32.Parse((string)message.body.data.ratio);
                double planningError = Double.Parse((string)message.body.data.planningError) / 100 + 1;
                if (provider == 1 && maxPower == 0) // provider == эскб
                {
                    maxPower = 150;
                }
                string voltageLevel = (string)message.body.data.voltageLevel;
                dynamic[] categories = new dynamic[6];

                List<double> category1 = new List<double>();
                List<dynamic> category2 = new List<dynamic>();
                List<dynamic> category3 = new List<dynamic>();
                List<dynamic> category4 = new List<dynamic>();
                List<dynamic> category5 = new List<dynamic>();
                List<dynamic> category6 = new List<dynamic>();
                int counter = 0;

                if ((string)message.body.data.enterprises == "mine")
                {
                    Vpotr.Add(consumption(message.body.data.file));
                }
                else if ((string)message.body.data.enterprises == "systemEnterprise")
                {
                    foreach (string objectId in message.body.data.objectIds)
                    {
                        var id = Guid.Parse(objectId);
                        objectIds.Add(id);
                    }
                    
                    DateTime start = new DateTime(year, month, 1);
                    DateTime end = start.AddMonths(1);
                    //Vpotr = consumptionSql();

                    //var records = Cache.Instance.GetRecords(start, end, "Hour", objectIds.ToArray()).ToDynamic();
                    List<dynamic> recordsDayStart = RecordsDecorator.Decorate(objectIds.ToArray(), start.AddDays(-1), start.AddDays(-1), "Day", userId).ToDynamic().ToList();
                    List<dynamic> recordsDayEnd = RecordsDecorator.Decorate(objectIds.ToArray(), end.AddDays(-1), end.AddDays(-1), "Day", userId).ToDynamic().ToList();
                    List<dynamic> recordsHour = RecordsDecorator.Decorate(objectIds.ToArray(), start, end, "Hour", userId).ToDynamic().ToList();
                    int krt = 1;
                    for(int i = 0; i < objectIds.Count(); i++)
                    {
                        var tube = StructureGraph.Instance.GetTube(objectIds[i], userId);
                        var dtube = (IDictionary<string, object>)tube;
                        if (dtube.ContainsKey("voltageLevel"))
                        {
                            dVoltageLevel.Add(objectIds[i], (string)tube.voltageLevel);
                        }
                        if (dtube.ContainsKey("KTr"))
                        {
                            dKTr.Add(objectIds[i], Int32.Parse((string)tube.KTr));
                            krt = Int32.Parse((string)tube.KTr);
                        }
                        else
                        {
                            krt = 1;
                            dKTr.Add(objectIds[i], 1);
                        }
                        List<List<double>> Vday = new List<List<double>>();
                        for (int j = 0; start.AddDays(j) < end; j++)
                        {
                            DateTime dtTmp = start.AddDays(j);
                            List<double> listHours = new List<double>();
                            for (int k = 0; dtTmp.AddHours(k) < dtTmp.AddDays(1); k++)
                            {
                                var recs = recordsHour.FindAll(x => ((x.date == dtTmp.AddHours(k)) && ((string)x.s1 == "01") && ((Guid)x.objectId == objectIds[i])));
                                if (!recs.Any())
                                {
                                    counter++;
                                }
                                listHours.Add((recs.Any()) ? recs.Sum(x => (double)x.d1) * krt : 0);
                            }
                            Vday.Add(listHours);
                        }
                        Vpotr.Add(Vday);
                        var recsDayStart = recordsDayStart.FindAll(x => (((string)x.s1 == "ЭЭ") && ((Guid)x.objectId == objectIds[i])));
                        double daystart = (recsDayStart.Any())?recsDayStart.Sum(x => (double)x.d1):0;
                        if (!recsDayStart.Any())
                        {
                            var recsDayStart1 = recordsDayStart.FindAll(x => (((string)x.s1 == "Тариф1ЭЭ") && ((Guid)x.objectId == objectIds[i])));
                            var recsDayStart2 = recordsDayStart.FindAll(x => (((string)x.s1 == "Тариф2ЭЭ") && ((Guid)x.objectId == objectIds[i])));
                            var recsDayStart3 = recordsDayStart.FindAll(x => (((string)x.s1 == "Тариф3ЭЭ") && ((Guid)x.objectId == objectIds[i])));
                            double daystart1 = (recsDayStart1.Any())? recsDayStart1.Sum(x => (double)x.d1):0;
                            double daystart2 = (recsDayStart2.Any())? recsDayStart2.Sum(x => (double)x.d1):0;
                            double daystart3 = (recsDayStart3.Any()) ? recsDayStart3.Sum(x => (double)x.d1) : 0;
                            daystart = daystart1 + daystart2 + daystart3;
                        }
                        var recsDayEnd = recordsDayEnd.FindAll(x => (((string)x.s1 == "ЭЭ") && ((Guid)x.objectId == objectIds[i])));
                        double dayEnd = recsDayEnd.Sum(x => (double)x.d1);
                        if (!recsDayStart.Any())
                        {
                            var recsDayEnd1 = recordsDayEnd.FindAll(x => (((string)x.s1 == "Тариф1ЭЭ") && ((Guid)x.objectId == objectIds[i])));
                            var recsDayEnd2 = recordsDayEnd.FindAll(x => (((string)x.s1 == "Тариф2ЭЭ") && ((Guid)x.objectId == objectIds[i])));
                            var recsDayEnd3 = recordsDayEnd.FindAll(x => (((string)x.s1 == "Тариф3ЭЭ") && ((Guid)x.objectId == objectIds[i])));
                            double dayEnd1 = (recsDayEnd1.Any()) ? recsDayEnd1.Sum(x => (double)x.d1) : 0;
                            double dayEnd2 = (recsDayEnd2.Any()) ? recsDayEnd2.Sum(x => (double)x.d1) : 0;
                            double dayEnd3 = (recsDayEnd3.Any()) ? recsDayEnd3.Sum(x => (double)x.d1) : 0;
                            dayEnd = dayEnd1 + dayEnd2 + dayEnd3;
                        }
                        V1Day.Add((dayEnd - daystart) * krt);
                    }
                }
                else
                {
                    Vpotr.Add(consumptionSql());
                }

                double V1Sum = 0;
                
                using (var con = new SqlConnection(connetionString))
                {
                    con.Open();

                    countDays = Vpotr[0].Count();
                    if ((string)message.body.data.enterprises == "systemEnterprise")
                    {
                        for (int i = 0; i < objectIds.Count(); i++)
                        {
                            if (dVoltageLevel.Any())
                            {
                                voltageLevel = VoltageLevelParsing(dVoltageLevel[objectIds[i]]);
                                if (voltageLevel == null || voltageLevel == "")
                                {
                                    return ans;
                                }
                            }
                            double V1 = 0;
                            for (int j = 0; j < Vpotr[i].Count(); j++)
                            {
                                for (int k = 0; k < 24; k++)
                                {
                                    V1 = V1 + Vpotr[i][j][k];
                                }
                            }
                            V1Sum += V1;

                            if (V1Day[i] != 0 && (string)message.body.data.enterprises == "systemEnterprise" && Math.Abs(V1 - V1Day[i]) > 0.5)
                            {
                                if (counter > 0)
                                {
                                    ans.body.message = "Неполнота часовых данных";
                                }
                                else
                                {
                                    ans.body.message = "Неправильная обработка данных";
                                }
                            }
                            var T1 = tarifsFromSqlFor1(provider, contract, voltageLevel, con, monthYear);
                            tarifsFromSqlFor2(provider, contract, voltageLevel, con, monthYear);
                            List<List<double>> tarifsForCK3 = tarifsFromSqlFor3(provider, maxPower, contract, voltageLevel, con, monthYear);
                            List<List<double>> tarifsForCK4 = tarifsFromSqlFor4(provider, maxPower, contract, voltageLevel, con, monthYear);
                            List<List<double>> tarifsForCK5 = tarifsFromSqlFor5(provider, maxPower, contract, voltageLevel, con, monthYear);
                            List<List<double>> tarifsForCK6 = tarifsFromSqlFor6(provider, maxPower, contract, voltageLevel, con, monthYear);

                            category1.Add(T1 * V1 / koef);
                            dynamic categoryTmp = new ExpandoObject();
                            categoryTmp.energy = Category2(Vpotr[i]);
                            if (contract == 1)
                            {
                                categoryTmp.energyDayNight = Category22(Vpotr[i]);
                            }
                            category2.Add(categoryTmp);

                            otherTarifs(provider, maxPower, 1, voltageLevel, 3, con, monthYear);  //для 3 категории в таблице otherTarifs только для contract = 1, тк они совпадают
                            category3.Add(Category3(Vpotr[i], tarifsForCK3));

                            otherTarifs(provider, maxPower, contract, voltageLevel, 4, con, monthYear);
                            category4.Add(Category4(Vpotr[i], tarifsForCK4));

                            otherTarifs(provider, maxPower, contract, voltageLevel, 5, con, monthYear);
                            List<List<double>> tarifsForCK5PlanFact = tarifsFromSqlFor5PlanFact(provider, maxPower, con, monthYear);
                            List<List<double>> tarifsForCK5FactPlan = tarifsFromSqlFor5FactPlan(provider, maxPower, con, monthYear);
                            category5.Add(Category5(planningError, Vpotr[i], tarifsForCK5, tarifsForCK5FactPlan, tarifsForCK5PlanFact));

                            otherTarifs(provider, maxPower, contract, voltageLevel, 6, con, monthYear);
                            List<List<double>> tarifsForCK6PlanFact = tarifsFromSqlFor6PlanFact(provider, maxPower, con, monthYear);
                            List<List<double>> tarifsForCK6FactPlan = tarifsFromSqlFor6FactPlan(provider, maxPower, con, monthYear);
                            category6.Add(Category6(planningError, Vpotr[i], tarifsForCK6, tarifsForCK6FactPlan, tarifsForCK6PlanFact));
                        }
                    }
                    else
                    {
                        double V1 = 0;
                        for (int j = 0; j < Vpotr[0].Count(); j++)
                        {
                            for (int k = 0; k < 24; k++)
                            {
                                V1 = V1 + Vpotr[0][j][k];
                            }
                        }
                        V1Sum += V1;
                        var T1 = tarifsFromSqlFor1(provider, contract, voltageLevel, con, monthYear);
                        tarifsFromSqlFor2(provider, contract, voltageLevel, con, monthYear);
                        List<List<double>> tarifsForCK3 = tarifsFromSqlFor3(provider, maxPower, contract, voltageLevel, con, monthYear);
                        List<List<double>> tarifsForCK4 = tarifsFromSqlFor4(provider, maxPower, contract, voltageLevel, con, monthYear);
                        List<List<double>> tarifsForCK5 = tarifsFromSqlFor5(provider, maxPower, contract, voltageLevel, con, monthYear);
                        List<List<double>> tarifsForCK6 = tarifsFromSqlFor6(provider, maxPower, contract, voltageLevel, con, monthYear);

                        category1.Add(T1 * V1 / koef);

                        dynamic categoryTmp = new ExpandoObject();
                        categoryTmp.energy = Category2(Vpotr[0]);
                        if (contract == 1)
                        {
                            categoryTmp.energyDayNight = Category22(Vpotr[0]);
                        }
                        category2.Add(categoryTmp);

                        otherTarifs(provider, maxPower, 1, voltageLevel, 3, con, monthYear);  //для 3 категории в таблице otherTarifs только для contract = 1, тк они совпадают
                        category3.Add(Category3(Vpotr[0], tarifsForCK3));

                        otherTarifs(provider, maxPower, contract, voltageLevel, 4, con, monthYear);
                        category4.Add(Category4(Vpotr[0], tarifsForCK4));

                        otherTarifs(provider, maxPower, contract, voltageLevel, 5, con, monthYear);
                        List<List<double>> tarifsForCK5PlanFact = tarifsFromSqlFor5PlanFact(provider, maxPower, con, monthYear);
                        List<List<double>> tarifsForCK5FactPlan = tarifsFromSqlFor5FactPlan(provider, maxPower, con, monthYear);
                        category5.Add(Category5(planningError, Vpotr[0], tarifsForCK5, tarifsForCK5FactPlan, tarifsForCK5PlanFact));

                        otherTarifs(provider, maxPower, contract, voltageLevel, 6, con, monthYear);
                        List<List<double>> tarifsForCK6PlanFact = tarifsFromSqlFor6PlanFact(provider, maxPower, con, monthYear);
                        List<List<double>> tarifsForCK6FactPlan = tarifsFromSqlFor6FactPlan(provider, maxPower, con, monthYear);
                        category6.Add(Category6(planningError, Vpotr[0], tarifsForCK6, tarifsForCK6FactPlan, tarifsForCK6PlanFact));
                    }
                    
                    categories[0] = new ExpandoObject();
                    categories[0].energy = category1.Sum();

                    categories[1] = new ExpandoObject();
                    categories[1].energy = category2.Sum(x => (double)x.energy);
                    if(contract == 1)
                    {
                        categories[1].energyDayNight = category2.Sum(x => (double)x.energyDayNight);
                    }

                    categories[2] = tempCategorySum(category3);
                    
                    categories[3] = tempCategorySum(category4);
                    
                    categories[4] = tempCategorySum(category5);
                    
                    categories[5] = tempCategorySum(category6);
                    con.Close();
                }

                ans.body.consumption = V1Sum;
                ans.body.categories = categories;
                return ans;
            }
            return Helper.BuildMessage("unhandled");
        }
        #region calculator
        int countDays = 31;
        int koef = 1;  // ставка в пиковые часы
        static double TPic = 0;  // ставка в пиковые часы
        static double TPP = 0;  // ставка в полупик
        static double TN = 0;  // ставка в ночь
        static double TDay = 0; // дневная ставка для вдух зон
        static double TNight = 0; // ночная ставка для двух зон

        static double Tpower = 0; // ставка на мощность
                
        static double TnetWorkPower = 0;

        static double TsumVplan = 0; // Ставка для суммы плановых почасовых объемов
        static double TdifAbsPlanFact = 0; // Ставка для суммы абсолютных значений разностей фактических и плановых
        string VoltageLevelParsing(string vl)
        {
            switch (vl.ToLower())
            {
                case "hh":  
                case "нн":
                case "нh":
                case "hн":
                    return "hh";
                case "ch1": // англ англ
                case "сн1": // рус рус
                case "cн1": // англ рус
                case "сh1": // рус англ
                    return "ch1";
                case "ch2": // англ англ
                case "сн2": // рус рус
                case "cн2": // англ рус
                case "сh2": // рус англ
                    return "ch2";
                case "bh": // англ англ
                case "вн": // рус рус
                case "bн": // англ рус
                case "вh": // рус англ
                    return "bh";
                default:
                    return "";
            }
        }
        List<List<double>> consumption(dynamic file)
        {
            byte[] body = Convert.FromBase64String(file.base64);
            MemoryStream ms = new MemoryStream(body);

            // Get temp file name
            var temp = Path.GetTempPath(); // Get %TEMP% path
            var path = Path.Combine(temp, file.filename);// file + ".xlsx"); // Get random file path

            using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write))
            {
                // Write content of your memory stream into file stream
                ms.WriteTo(fs);
            }

            // Create Excel app
            Application xlApp = new Application();

            // Open Workbook
            Workbook xlWorkBook = xlApp.Workbooks.Open(path, ReadOnly: true);
            //Workbook xlWorkBook = xlApp.Workbooks.Open(@filePaths[0], 0, true, 5, "", "", true, Microsoft.Office.Interop.Excel.XlPlatform.xlWindows, "\t", false, false, 0, true, 1, 0);
            
            /////// чтение файла потребителя
            Worksheet xlWorkSheet = (Worksheet)xlWorkBook.Worksheets.get_Item(1);
            Range range = xlWorkSheet.UsedRange;
            List<List<double>> days = new List<List<double>>();
            for (int i = 5; i <= 4 + countDays; i++)
            {
                List<double> hours = new List<double>();
                for (int j = 2; j <= 25; j++)
                {
                    if ((range.Cells[i, j] as Range).Value2 == null)
                    {
                        hours.Add(0);
                    }
                    else
                    {
                        hours.Add((double)(range.Cells[i, j] as Range).Value2);
                        //vPotr[i - 5, j - 2] = (double)(range.Cells[i, j] as Range).Value2;
                    }
                }
                days.Add(hours);
            }

            xlWorkBook.Close(true, null, null);
            xlApp.Quit();
            Marshal.ReleaseComObject(xlWorkSheet);
            Marshal.ReleaseComObject(xlWorkBook);
            Marshal.ReleaseComObject(xlApp);
            ///////////// конец чтения файла потребителя
            return days;
        }
        List<List<double>> consumptionSql()
        {
            SqlConnection cnn;
            string connetionString = "Data Source=192.168.0.101;Initial Catalog=tarifsForCK;User ID=matrix;Password=matrix";
            cnn = new SqlConnection(connetionString);
            cnn.Open();
            double[] arr = new double[720];
            List<List<double>> days = new List<List<double>>();
            string sql = "SELECT h0,h1,h2,h3,h4,h5,h6,h7,h8,h9,h10,h11,h12,h13,h14,h15,h16,h17,h18,h19,h20,h21,h22,h23 FROM [tarifsForCK].[dbo].[consumption] ";
            SqlCommand cmd = new SqlCommand();
            cmd.Connection = cnn;
            cmd.CommandText = sql;

            using (SqlDataReader reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    List<double> hours = new List<double>();
                    for (int i = 0; i < 24; i++)
                    {
                        hours.Add(reader.GetDouble(i));
                    }
                    days.Add(hours);
                }
                cnn.Close();
            }
            return days;
        }
        double tarifsFromSqlFor1(int provider, int contract, string voltLevel, SqlConnection con, string date)
        {
            double T1 = 0; // Тариф за 1Квтч (рублей/кВтч)   для 1 категории цен
            //*****************************  1    СЧИТЫВАЕМ ТАРИФ ДЛЯ ПЕРВОЙ КАТЕГОРИИ   ************//
            string sql1 = "SELECT " + voltLevel + $" FROM [tarifsForCK].[dbo].[C1_{date}]   where contract = @contract AND provider = @provider";
            SqlCommand cmd1 = new SqlCommand();
            cmd1.Connection = con;
            cmd1.CommandText = sql1;
            SqlParameter nameParam1 = new SqlParameter("@contract", contract);
            SqlParameter nameParam2 = new SqlParameter("@provider", provider);
            cmd1.Parameters.Add(nameParam1);
            cmd1.Parameters.Add(nameParam2);

            using (DbDataReader reader = cmd1.ExecuteReader())
            {
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        T1 = reader.GetDouble(0);       //  тариф для 1 категории
                    }
                }
            }
            return T1;
        }
        void tarifsFromSqlFor2(int provider, int contract, string voltLevel, SqlConnection con, string date)
        {
            ////*****************************   2   СЧИТЫВАЕМ ТАРИФ ДЛЯ ВТОРОЙ КАТЕГОРИИ   ************//
            string sql = "SELECT " + voltLevel + $" FROM [tarifsForCK].[dbo].[C2_{date}]   where contract = @contract AND provider = @provider and (zone = @zone1 or zone = @zone2 or zone = @zone3 or zone = @zone4 or zone = @zone5)";
            SqlCommand cmd = new SqlCommand();
            cmd.Connection = con;
            cmd.CommandText = sql;
            SqlParameter nameParam1 = new SqlParameter("@contract", contract);
            SqlParameter nameParam2 = new SqlParameter("@provider", provider);
            SqlParameter nameParam3 = new SqlParameter("@zone1", 1);
            SqlParameter nameParam4 = new SqlParameter("@zone2", 2);
            SqlParameter nameParam5 = new SqlParameter("@zone3", 3);
            SqlParameter nameParam6 = new SqlParameter("@zone4", 4);
            SqlParameter nameParam7 = new SqlParameter("@zone5", 5);
            cmd.Parameters.Add(nameParam1);
            cmd.Parameters.Add(nameParam2);
            cmd.Parameters.Add(nameParam3);
            cmd.Parameters.Add(nameParam4);
            cmd.Parameters.Add(nameParam5);
            cmd.Parameters.Add(nameParam6);
            cmd.Parameters.Add(nameParam7);
            List<double> tarifsCK2 = new List<double>();
            using (DbDataReader reader = cmd.ExecuteReader())
            {
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        tarifsCK2.Add(reader.GetDouble(0));
                    }
                }
            }
            TN = tarifsCK2[0];
            TPP = tarifsCK2[1];
            TPic = tarifsCK2[2];
            if(contract == 1)
            {
                TNight = tarifsCK2[3];
                TDay = tarifsCK2[4];
            }
        }
        List<List<double>> tarifsFromSqlFor3(int provider, int idPower, int contract, string voltLevel, SqlConnection con, string date)
        {
            ////*****************************   3   СЧИТЫВАЕМ ТАРИФ ДЛЯ ТРЕТЬЕЙ КАТЕГОРИИ   ************//
            string sql = $"SELECT h0,h1,h2,h3,h4,h5,h6,h7,h8,h9,h10,h11,h12,h13,h14,h15,h16,h17,h18,h19,h20,h21,h22,h23 FROM [tarifsForCK].[dbo].[C3_{date}] where provider = @provider and bh = @bh and idPower = @idPower";
            SqlCommand cmd = new SqlCommand();
            cmd.Connection = con;
            cmd.CommandText = sql;
            int bh = 1;
            if (voltLevel == "ch1") bh = 2;
            if (voltLevel == "ch2") bh = 3;
            if (voltLevel == "hh") bh = 4;
            if (contract == 2) bh = 5;

            SqlParameter nameParam2 = new SqlParameter("@provider", provider);
            SqlParameter nameParam3 = new SqlParameter("@bh", bh);
            SqlParameter nameParam4 = new SqlParameter("@idPower", idPower);
            cmd.Parameters.Add(nameParam2);
            cmd.Parameters.Add(nameParam3);
            cmd.Parameters.Add(nameParam4);
            List<List<double>> tarifsForCK3 = new List<List<double>>();
            using (SqlDataReader reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    List<double> hours = new List<double>();
                    for (int i = 0; i < 24; i++)
                    {
                        hours.Add(reader.GetDouble(i));
                    }
                    tarifsForCK3.Add(hours);
                }
            }
            return tarifsForCK3;
        }
        List<List<double>> tarifsFromSqlFor4(int provider, int idPower, int contract, string voltLevel, SqlConnection con, string date)
        {
            ////*****************************   4   СЧИТЫВАЕМ ТАРИФ ДЛЯ ТРЕТЬЕЙ КАТЕГОРИИ   ************//
            string sql = $"SELECT h0,h1,h2,h3,h4,h5,h6,h7,h8,h9,h10,h11,h12,h13,h14,h15,h16,h17,h18,h19,h20,h21,h22,h23 FROM [tarifsForCK].[dbo].[C4_{date}] where provider = @provider and bh = @bh and idPower = @idPower";
            SqlCommand cmd = new SqlCommand();
            cmd.Connection = con;
            cmd.CommandText = sql;
            int bh = 1;
            if (voltLevel == "bh") bh = 1;
            if (voltLevel == "ch1") bh = 2;
            if (voltLevel == "ch2") bh = 3;
            if (voltLevel == "hh") bh = 4;
            if (contract == 2) bh = 5;

            SqlParameter nameParam2 = new SqlParameter("@provider", provider);
            SqlParameter nameParam3 = new SqlParameter("@bh", bh);
            SqlParameter nameParam4 = new SqlParameter("@idPower", idPower);
            cmd.Parameters.Add(nameParam2);
            cmd.Parameters.Add(nameParam3);
            cmd.Parameters.Add(nameParam4);
            List<List<double>> tarifsForCK4 = new List<List<double>>();
            using (SqlDataReader reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    List<double> hours = new List<double>();
                    for (int i = 0; i < 24; i++)
                    {
                        hours.Add(reader.GetDouble(i));
                    }
                    tarifsForCK4.Add(hours);
                }
            }
            return tarifsForCK4;
        }
        List<List<double>> tarifsFromSqlFor5(int provider, int idPower, int contract, string voltLevel, SqlConnection con, string date)
        {
            ////*****************************   5   СЧИТЫВАЕМ ТАРИФ ДЛЯ ПЯТОЙ КАТЕГОРИИ, для электроэнергии   ************//

            List<List<double>> tarifsForCK5 = new List<List<double>>();
            string sql = $"SELECT h0,h1,h2,h3,h4,h5,h6,h7,h8,h9,h10,h11,h12,h13,h14,h15,h16,h17,h18,h19,h20,h21,h22,h23 FROM [tarifsForCK].[dbo].[C5_{date}] where provider = @provider and bh = @bh and idPower = @idPower and planfact = 0";
            SqlCommand cmd = new SqlCommand();
            cmd.Connection = con;
            cmd.CommandText = sql;
            int bh = 1;
            if (voltLevel == "bh") bh = 1;
            if (voltLevel == "ch1") bh = 2;
            if (voltLevel == "ch2") bh = 3;
            if (voltLevel == "hh") bh = 4;
            if (contract == 2) bh = 5;

            SqlParameter nameParam2 = new SqlParameter("@provider", provider);
            SqlParameter nameParam4 = new SqlParameter("@bh", bh);
            SqlParameter nameParam5 = new SqlParameter("@idPower", idPower);
            cmd.Parameters.Add(nameParam2);
            cmd.Parameters.Add(nameParam4);
            cmd.Parameters.Add(nameParam5);
            tarifsForCK5.Clear();
            using (SqlDataReader reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    List<double> hours = new List<double>();
                    for (int i = 0; i < 24; i++)
                    {
                        hours.Add(reader.GetDouble(i));
                    }
                    tarifsForCK5.Add(hours);
                }
            }
            return tarifsForCK5;
        }
        List<List<double>> tarifsFromSqlFor5PlanFact(int provider, int idPower, SqlConnection con, string date)
        {
            ////*****************************   5   СЧИТЫВАЕМ ТАРИФ ДЛЯ ПЯТОЙ КАТЕГОРИИ, для электроэнергии   ************//
            string sql = $"SELECT h0,h1,h2,h3,h4,h5,h6,h7,h8,h9,h10,h11,h12,h13,h14,h15,h16,h17,h18,h19,h20,h21,h22,h23 FROM [tarifsForCK].[dbo].[C5_{date}] where provider = @provider and planfact = @planfact and idPower = @idPower";
            SqlCommand cmd = new SqlCommand();
            cmd.Connection = con;
            cmd.CommandText = sql;
            SqlParameter nameParam2 = new SqlParameter("@provider", provider);
            SqlParameter nameParam4 = new SqlParameter("@planfact", 1);
            SqlParameter nameParam5 = new SqlParameter("@idPower", idPower);
            cmd.Parameters.Add(nameParam2);
            cmd.Parameters.Add(nameParam4);
            cmd.Parameters.Add(nameParam5);

            List<List<double>> tarifsForCK5PlanFact = new List<List<double>>();  // тарифы для превышения плана над фактом
            using (SqlDataReader reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    List<double> hours = new List<double>();
                    for (int i = 0; i < 24; i++)
                    {
                        hours.Add(reader.GetDouble(i));
                    }
                    tarifsForCK5PlanFact.Add(hours);
                }
            }
            return tarifsForCK5PlanFact;
        }
        List<List<double>> tarifsFromSqlFor5FactPlan(int provider, int idPower, SqlConnection con, string date)
        {
            List<List<double>> tarifsForCK5FactPlan = new List<List<double>>();  // тарифы для превышения факта над планом
            ////*****************************   5   СЧИТЫВАЕМ ТАРИФ ДЛЯ ПЯТОЙ КАТЕГОРИИ, для электроэнергии   ************//
            //List<List<double>> tarifsForCK3 = new List<List<double>>();
            string sql = $"SELECT h0,h1,h2,h3,h4,h5,h6,h7,h8,h9,h10,h11,h12,h13,h14,h15,h16,h17,h18,h19,h20,h21,h22,h23 FROM [tarifsForCK].[dbo].[C5_{date}] where provider = @provider and planfact = @planfact and idPower = @idPower";
            SqlCommand cmd = new SqlCommand();
            cmd.Connection = con;
            cmd.CommandText = sql;
            SqlParameter nameParam2 = new SqlParameter("@provider", provider);
            SqlParameter nameParam4 = new SqlParameter("@planfact", 2);
            SqlParameter nameParam5 = new SqlParameter("@idPower", idPower);
            cmd.Parameters.Add(nameParam2);
            cmd.Parameters.Add(nameParam4);
            cmd.Parameters.Add(nameParam5);
            using (SqlDataReader reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    List<double> hours = new List<double>();
                    for (int i = 0; i < 24; i++)
                    {
                        hours.Add(reader.GetDouble(i));
                    }
                    tarifsForCK5FactPlan.Add(hours);
                }
            }
            return tarifsForCK5FactPlan;
        }
        List<List<double>> tarifsFromSqlFor6(int provider, int idPower, int contract, string voltLevel, SqlConnection con, string date)
        {
            ////*****************************   6   СЧИТЫВАЕМ ТАРИФ ДЛЯ ПЯТОЙ КАТЕГОРИИ, для электроэнергии   ************//
            //List<List<double>> tarifsForCK3 = new List<List<double>>();
            string sql = $"SELECT h0,h1,h2,h3,h4,h5,h6,h7,h8,h9,h10,h11,h12,h13,h14,h15,h16,h17,h18,h19,h20,h21,h22,h23 FROM [tarifsForCK].[dbo].[C6_{date}] where provider = @provider and bh = @bh and idPower = @idPower and planfact = 0";
            SqlCommand cmd = new SqlCommand();
            cmd.Connection = con;
            cmd.CommandText = sql;
            int bh = 1;
            if (voltLevel == "bh") bh = 1;
            if (voltLevel == "ch1") bh = 2;
            if (voltLevel == "ch2") bh = 3;
            if (voltLevel == "hh") bh = 4;
            if (contract == 2) bh = 5;

            SqlParameter nameParam2 = new SqlParameter("@provider", provider);
            SqlParameter nameParam4 = new SqlParameter("@bh", bh);
            SqlParameter nameParam5 = new SqlParameter("@idPower", idPower);
            cmd.Parameters.Add(nameParam2);
            cmd.Parameters.Add(nameParam4);
            cmd.Parameters.Add(nameParam5);
            List<List<double>> tarifsForCK6 = new List<List<double>>();
            using (SqlDataReader reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    List<double> hours = new List<double>();
                    for (int i = 0; i < 24; i++)
                    {
                        hours.Add(reader.GetDouble(i));
                    }
                    tarifsForCK6.Add(hours);
                }
            }
            return tarifsForCK6;
        }
        void otherTarifs(int provider, int idPower, int contract, string voltLevel, int ck, SqlConnection con, string date)
        {
            //*****************************   СЧИТЫВАЕМ ОСТАЛЬНЫЕ ТАРИФЫ   ************//
            string netWork = "network1";
            if (voltLevel == "ch1") netWork = "network2";
            if (voltLevel == "ch2") netWork = "network3";
            if (voltLevel == "hh") netWork = "network4";
            double[] tarifs = new double[4];
            string sql1 = "SELECT power, " + netWork + $", sumplan, abs FROM [tarifsForCK].[dbo].[otherTarifs_{date}] where contract = @contract AND provider = @provider and ck = @ck and idPower = @idPower";
            SqlCommand cmd1 = new SqlCommand();
            cmd1.Connection = con;
            cmd1.CommandText = sql1;
            SqlParameter nameParam1 = new SqlParameter("@contract", contract);
            SqlParameter nameParam2 = new SqlParameter("@provider", provider);
            SqlParameter nameParam3 = new SqlParameter("@ck", ck);
            SqlParameter nameParam4 = new SqlParameter("@idPower", idPower);
            cmd1.Parameters.Add(nameParam1);
            cmd1.Parameters.Add(nameParam2);
            cmd1.Parameters.Add(nameParam3);
            cmd1.Parameters.Add(nameParam4);

            using (DbDataReader reader = cmd1.ExecuteReader())
            {
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        for (int i = 0; i < 4; i++)
                        {
                            tarifs[i] = reader.GetDouble(i);
                        }
                    }
                }
            }
            Tpower = tarifs[0];
            TnetWorkPower = tarifs[1];
            TsumVplan = tarifs[2];
            TdifAbsPlanFact = tarifs[3];
        }
        List<List<double>> tarifsFromSqlFor6PlanFact(int provider, int idPower, SqlConnection con, string date)
        {
            ////*****************************   6   СЧИТЫВАЕМ ТАРИФ ДЛЯ ПЯТОЙ КАТЕГОРИИ, для превышения плана над фактом   ************//
            //List<List<double>> tarifsForCK3 = new List<List<double>>();
            string sql = $"SELECT h0,h1,h2,h3,h4,h5,h6,h7,h8,h9,h10,h11,h12,h13,h14,h15,h16,h17,h18,h19,h20,h21,h22,h23 FROM [tarifsForCK].[dbo].[C6_{date}] where provider = @provider and planfact = @planfact and idPower = @idPower";
            SqlCommand cmd = new SqlCommand();
            cmd.Connection = con;
            cmd.CommandText = sql;
            SqlParameter nameParam2 = new SqlParameter("@provider", provider);
            SqlParameter nameParam4 = new SqlParameter("@planfact", 1);
            SqlParameter nameParam5 = new SqlParameter("@idPower", idPower);
            cmd.Parameters.Add(nameParam2);
            cmd.Parameters.Add(nameParam4);
            cmd.Parameters.Add(nameParam5);
            List<List<double>> tarifsForCK6PlanFact = new List<List<double>>();  // тарифы для превышения плана над фактом
            using (SqlDataReader reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    List<double> hours = new List<double>();
                    for (int i = 0; i < 24; i++)
                    {
                        hours.Add(reader.GetDouble(i));
                    }
                    tarifsForCK6PlanFact.Add(hours);
                }
            }
            return tarifsForCK6PlanFact;
        }
        List<List<double>> tarifsFromSqlFor6FactPlan(int provider, int idPower, SqlConnection con, string date)
        {
            ////*****************************   6   СЧИТЫВАЕМ ТАРИФ ДЛЯ ПЯТОЙ КАТЕГОРИИ, для превышения факта над планом   ************//
            //List<List<double>> tarifsForCK3 = new List<List<double>>();
            string sql = $"SELECT h0,h1,h2,h3,h4,h5,h6,h7,h8,h9,h10,h11,h12,h13,h14,h15,h16,h17,h18,h19,h20,h21,h22,h23 FROM [tarifsForCK].[dbo].[C6_{date}] where provider = @provider and planfact = @planfact and idPower = @idPower";
            SqlCommand cmd = new SqlCommand();
            cmd.Connection = con;
            cmd.CommandText = sql;
            SqlParameter nameParam2 = new SqlParameter("@provider", provider);
            SqlParameter nameParam4 = new SqlParameter("@planfact", 2);
            SqlParameter nameParam5 = new SqlParameter("@idPower", idPower);
            cmd.Parameters.Add(nameParam2);
            cmd.Parameters.Add(nameParam4);
            cmd.Parameters.Add(nameParam5);
            List<List<double>> tarifsForCK6FactPlan = new List<List<double>>();  // тарифы для превышения факта над планом
            using (SqlDataReader reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    List<double> hours = new List<double>();
                    for (int i = 0; i < 24; i++)
                    {
                        hours.Add(reader.GetDouble(i));
                    }
                    tarifsForCK6FactPlan.Add(hours);
                }
            }
            return tarifsForCK6FactPlan;
        }
        
        double Category2(List<List<double>> Vpotr)
        {
            // общая цена за потребляемую энергию по 2 к.ц. C = C(пик) + С(полупик) + С(ночь)
            double VPic = 0; // объем потребляемой энергии в пиковой зоне
            double VPP = 0; // объем потребляемой энергии в полупиковой зоне
            double VN = 0; // объем потребляемой энергии в ночной зоне
            ////// высчитываем объемы потребления
            for (int i = 0; i < countDays; i++)
            {
                for (int j = 0; j <= 6; j++)
                {
                    VN += Vpotr[i][j];
                }
                VN += Vpotr[i][23];       ////////// Вычислили объем потребления в ночь
            }
            for (int i = 0; i < countDays; i++)
            {
                for (int j = 7; j <= 9; j++)
                {
                    VPic += Vpotr[i][j];
                }
                for (int j = 17; j <= 20; j++)
                {
                    VPic += Vpotr[i][j];   /////////////  в пик
                }
            }
            for (int i = 0; i < countDays; i++)
            {
                for (int j = 10; j <= 16; j++)
                {
                    VPP += Vpotr[i][j];
                }
                for (int j = 21; j <= 22; j++)
                {
                    VPP += Vpotr[i][j];        ///////в полупик
                }
            }
            return (TN * VN + TPP * VPP + TPic * VPic) / koef;
        }
        double Category22(List<List<double>> Vpotr)
        {
            // общая цена за потребляемую энергию по 2 к.ц. C = C(пик) + С(полупик) + С(ночь)
            double VDay = 0; // объем потребляемой энергии  в дневное время суток
            double VNight = 0; // оьъем потреблямой энергии в ночное время суток  
            for (int i = 0; i < countDays; i++)
            {
                for (int j = 0; j <= 6; j++)
                {
                    VNight += Vpotr[i][j];          /////// вночь
                }
                VNight += Vpotr[i][23];
                for (int j = 7; j <= 22; j++)
                {
                    VDay += Vpotr[i][j];              /////// в день
                }
            }
            return (TNight * VNight + TDay * VDay) / koef;
        }
        dynamic tempCategory(double energy, double? power, double? network)
        {
            dynamic category = new ExpandoObject();
            category.energy = energy / koef;
            if(power != null)
            {
                category.power = power / koef;
            }
            if (network != null)
            {
                category.network = network / koef;
            }
            return category;
        }

        dynamic tempCategory(double energy, double power, double? network, double factPlan, double planFact, double vSumPlan, double difFactPlan)
        {
            dynamic category = new ExpandoObject();
            category.energy = energy / koef;
            category.power = power / koef;
            if (network != null)
            {
                category.network = network / koef;
            }
            category.factPlan = factPlan / koef;
            category.planFact = planFact / koef;
            category.vSumPlan = vSumPlan / koef;
            category.difFactPlan = difFactPlan / koef;
            return category;
        }
        dynamic tempCategorySum(List<dynamic> listCategory)
        {
            dynamic category = new ExpandoObject();

            category.energy = listCategory.Sum(x => (double)x.energy);
            var cat = (IDictionary<string, object>)listCategory[0];
            if (cat.ContainsKey("power"))
            {
                category.power = listCategory.Sum(x => (double)x.power);
            }
            if (cat.ContainsKey("network"))
            {
                category.network = listCategory.Sum(x => (double)x.network);
            }
            if (cat.ContainsKey("factPlan"))
            {
                category.factPlan = listCategory.Sum(x => (double)x.factPlan);
            }
            if (cat.ContainsKey("planFact"))
            {
                category.planFact = listCategory.Sum(x => (double)x.planFact);
            }
            if (cat.ContainsKey("vSumPlan"))
            {
                category.vSumPlan = listCategory.Sum(x => (double)x.vSumPlan);
            }
            if (cat.ContainsKey("difFactPlan"))
            {
                category.difFactPlan = listCategory.Sum(x => (double)x.difFactPlan);
            }
            return category;
        }
        dynamic Category3(List<List<double>> Vpotr, List<List<double>> tarifsForCK3)
        {
            double energy = 0;
            double VaveragePower = 0;
            int workingDays = 21;
            //////////////     вычисляем стоимость за потребления электрической энергии (ЭЭ)
            for (int i = 0; i < tarifsForCK3.Count; i++)
            {
                for (int j = 0; j < 24; j++)
                {
                    energy += tarifsForCK3[i][j] * Vpotr[i][j];
                }
            }
            ///////////////   вычисляем стоимость за потребляемую мощность
            //////////////  для этого высчитываем среднее потребление в пиковые часы нагрузки 
            for (int i = 0; i < 30; i++)
            {
                if ((i == 6) || (i == 7) || (i == 13) || (i == 14) || (i == 20) || (i == 21) || (i == 27) || (i == 28) || (i == 29)) continue;
                if ((i == 9) || (i == 17) || (i == 19) || (i == 24) || (i == 26))                                                
                {
                    VaveragePower += Vpotr[i][7];
                }
                else
                {
                    VaveragePower += Vpotr[i][16];
                }
            }
            VaveragePower /= workingDays;
            return tempCategory(energy, VaveragePower * Tpower, null);
        }
        dynamic Category4(List<List<double>> Vpotr, List<List<double>> tarifsForCK4)
        {
            double energy = 0;
            double VaveragePower = 0;
            int workingDays = 21;
            //////////////     вычисляем стоимость за потребления электрической энергии (ЭЭ)
            for (int i = 0; i < tarifsForCK4.Count; i++)
            {
                for (int j = 0; j < 24; j++)
                {
                    energy += tarifsForCK4[i][j] * Vpotr[i][j];
                }
            }
            ///////////////   вычисляем стоимость за потребляемую мощность
            //////////////  для этого высчитываем среднее потребление в пиковые часы нагрузки 
            for (int i = 0; i < 30; i++)
            {
                if ((i == 6) || (i == 7) || (i == 13) || (i == 14) || (i == 20) || (i == 21) || (i == 27) || (i == 28) || (i == 29)) continue;
                if ((i == 9) || (i == 17) || (i == 19) || (i == 24) || (i == 26))
                {
                    VaveragePower += Vpotr[i][7];
                }
                else
                {
                    VaveragePower += Vpotr[i][16];
                }
            }
            VaveragePower /= workingDays;
            return tempCategory(energy, VaveragePower * Tpower, VaveragePower * TnetWorkPower);
        }
        dynamic Category5(double miss, List<List<double>> Vpotr, List<List<double>> tarifsForCK5, List<List<double>> tarifsForCK5FactPlan, List<List<double>> tarifsForCK5PlanFact)
        {
            double energy = 0;
            double factPlan = 0;
            double planFact = 0;

            double VaveragePower = 0;
            int workingDays = 21;
            
            //////////////     вычисляем стоимость за потребления электрической энергии (ЭЭ)                 ЭНЕРГИЯ
            for (int i = 0; i < tarifsForCK5.Count; i++)
            {
                for (int j = 0; j < 24; j++)
                {
                    energy += tarifsForCK5[i][j] * Vpotr[i][j];
                }
            }
            ///////////////   вычисляем стоимость за потребляемую мощность                                      МОЩНОСТь
            //////////////  для этого высчитываем среднее потребление в пиковые часы нагрузки 
            for (int i = 0; i < tarifsForCK5.Count; i++)
            {
                if ((i == 6) || (i == 7) || (i == 13) || (i == 14) || (i == 20) || (i == 21) || (i == 27) || (i == 28) || (i == 29)) continue;
                if ((i == 9) || (i == 17) || (i == 19) || (i == 24) || (i == 26))
                {
                    VaveragePower += Vpotr[i][7];
                }
                else
                {
                    VaveragePower += Vpotr[i][16];
                }
            }
            //////////////  конец расчета мощности
            ///   высчтываем CfactPlan
            for (int i = 0; i < tarifsForCK5.Count; i++)
            {
                for (int j = 0; j < 24; j++)
                {
                    factPlan += Math.Abs(Vpotr[i][j] * miss - Vpotr[i][j]) * tarifsForCK5FactPlan[i][j];
                    //CfactPlan = 0;
                    //CPlanFact = CPlanFact + Vpotr[i][j] * miss * tarifsForCK5PlanFact[i][j];
                    planFact = 0;
                }
            }
            ////////// расчет объема плановых работ ддя "объема небаланса"
            double Vplan = 0;
            for (int i = 0; i < tarifsForCK5.Count; i++)
            {
                for (int j = 0; j < 24; j++)
                {
                    //Vplan += Vpotr[i][j] - Vpotr[i][j] * miss;
                    Vplan += Math.Abs(Vpotr[i][j] - Vpotr[i][j] * miss);
                }
            }
            VaveragePower /= workingDays;
            
            return tempCategory(energy, VaveragePower * Tpower, null, factPlan, planFact, Vplan * TsumVplan, Vplan * miss * TdifAbsPlanFact);
        }
       
        dynamic Category6(double miss, List<List<double>> Vpotr, List<List<double>> tarifsForCK6, List<List<double>> tarifsForCK6FactPlan, List<List<double>> tarifsForCK6PlanFact)
        {
            double energy = 0;
            double factPlan = 0;
            double planFact = 0;
            
            double VaveragePower = 0;
            int workingDays = 21;
            List<List<double>> missVpotr = new List<List<double>>();
            for (int i = 0; i < tarifsForCK6.Count; i++)
            {
                List<double> tmp = new List<double>();
                for (int j = 0; j < 24; j++)
                {
                    tmp.Add(Vpotr[i][j] * miss);
                }
                missVpotr.Add(tmp);
            }
            //////////////     вычисляем стоимость за потребления электрической энергии (ЭЭ)                 ЭНЕРГИЯ
            for (int i = 0; i < tarifsForCK6.Count; i++)
            {
                for (int j = 0; j < 24; j++)
                {
                    energy += tarifsForCK6[i][j] * Vpotr[i][j];
                }
            }
            ///////////////   вычисляем стоимость за потребляемую мощность                                      МОЩНОСТь
            //////////////  для этого высчитываем среднее потребление в пиковые часы нагрузки 
            for (int i = 0; i < tarifsForCK6.Count; i++)
            {
                if ((i == 6) || (i == 7) || (i == 13) || (i == 14) || (i == 20) || (i == 21) || (i == 27) || (i == 28) || (i == 29)) continue;
                if ((i == 9) || (i == 17) || (i == 19) || (i == 24) || (i == 26))
                {
                    VaveragePower += Vpotr[i][7];
                }
                else
                {
                    VaveragePower += Vpotr[i][16];
                }
            }
            //////////////  конец расчета мощности
            ///   высчтываем CfactPlan
            for (int i = 0; i < tarifsForCK6.Count; i++)
            {
                for (int j = 0; j < 24; j++)
                {
                    factPlan += (Vpotr[i][j] * miss - Vpotr[i][j]) * tarifsForCK6FactPlan[i][j];
                    //CfactPlan = 0;
                    //planFact += (Vpotr[i][j] * miss - Vpotr[i][j]) * tarifsForCK6PlanFact[i][j];
                    planFact = 0;
                }
            }
            ////////// расчет объема плановых работ ддя "объема небаланса"
            double Vplan = 0;
            for (int i = 0; i < tarifsForCK6.Count; i++)
            {
                for (int j = 0; j < 24; j++)
                {
                    Vplan += Math.Abs(Vpotr[i][j] - Vpotr[i][j] * miss);
                }
            }
            VaveragePower /= workingDays;
            
            return tempCategory(energy, VaveragePower * Tpower, VaveragePower * TnetWorkPower, factPlan, planFact, Vplan * TsumVplan, Vplan * miss * TdifAbsPlanFact);
        }
        #endregion
    }
}
