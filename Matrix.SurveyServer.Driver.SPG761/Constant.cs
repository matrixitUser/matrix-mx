using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Matrix.SurveyServer.Driver.SPG761
{
    public partial class Driver
    {
        private dynamic GetConstants(byte dad, byte sad, bool needDad, byte ch, byte version, DateTime date)
        {
            Dictionary<string, string> categories = new Dictionary<string, string>()
            {
                { "099", "00" }, //Тип прибора
                { "020", "00" }, //Дата ввода корректора в эксплуатацию  
                { "021", "00" }, //Календарное время ввода корректора в эксплуатацию
                { "037", "00" }, //Константа барометрического давления,  мм.рт.ст.
                { "024", "00" }, //расчетный час
                { "003", "00" } //спецификация-1 внешнего оборудования
            };

            dynamic parameters = GetParameters(dad, sad, categories, needDad);
            if (!parameters.success)
                return parameters;

            List<dynamic> records = new List<dynamic>();

            dynamic constants = new ExpandoObject();
            constants.success = true;
            constants.error = string.Empty;


            records.Add(MakeConstantRecord("тип прибора", parameters.categories[0][0], date));

            string sdate = parameters.categories[1][0];
            string stime = parameters.categories[2][0];

            DateTime dateOfEntry;
            if (DateTime.TryParse(string.Format("{0} {1}", stime.Replace("-", ":"), sdate.Replace("-", ".")), out dateOfEntry))
            {
                records.Add(MakeConstantRecord("дата ввода корректора в эксплуатацию", dateOfEntry.ToString("dd.MM.yy HH:mm:ss"), date));
            }
            //DateTime dateOfEntry = DateTime.Parse(string.Format("{0} {1}", stime.Replace("-", ":"), sdate.Replace("-", ".")));

            // DateTime dateOfEntry = DateTime.ParseExact((string)parameters.categories[1][0] + (string)parameters.categories[2][0], "dd-MM-yyHH-mm-ss", null);
            
            records.Add(MakeConstantRecord("константа барометрического давления,  мм.рт.ст.", parameters.categories[3][0], date));

            int contractHour;
            if (!int.TryParse(parameters.categories[4][0], out contractHour))
            {
                constants.success = false;
                constants.error = "неверный формат значения контрактного часа";
                return constants;
            }
            constants.contractHour = contractHour;
            records.Add(MakeConstantRecord("расчетный час", contractHour, date));
            constants.p003 = parameters.categories[5][0];
            records.Add(MakeConstantRecord("спецификация-1 внешнего оборудования", constants.p003, date));

            ///Компонентный состав газа
            dynamic array = GetIndexArray(dad, sad, needDad, 1, 125, 0, 9);
            if (!array.success)
                return array;

            if (array.categories.Count >= 8)
            {
                string data1 = array.categories[0][0];
                if (data1 != "Нет данных?") records.Add(MakeConstantRecord("доля метана, %", data1, date));
                string data2 = array.categories[1][0];
                if (data2 != "Нет данных?") records.Add(MakeConstantRecord("доля этана, %", data2, date));
                string data3 = array.categories[2][0];
                if (data3 != "Нет данных?") records.Add(MakeConstantRecord("доля пропана, %", data3, date));
                string data4 = array.categories[3][0];
                if (data4 != "Нет данных?") records.Add(MakeConstantRecord("доля н-бутана, %", data4, date));
                string data5 = array.categories[4][0];
                if (data5 != "Нет данных?") records.Add(MakeConstantRecord("доля и-бутана, %", data5, date));
                string data6 = array.categories[5][0];
                if (data6 != "Нет данных?") records.Add(MakeConstantRecord("доля азота, %", data6, date));
                string data7 = array.categories[6][0];
                if (data7 != "Нет данных?") records.Add(MakeConstantRecord("доля диоксида углерода, %", data7, date));
                string data8 = array.categories[7][0];
                if (data8 != "Нет данных?") records.Add(MakeConstantRecord("доля сероводорода, %", data8, date));
            }

            ///физические характеристики газа

            byte s = 2;
            switch (version)
            {
                case 0: s = 8; break;
                case 1: s = 2; break;
                case 2: s = 2; break;
            }
            array = GetIndexArray(dad, sad, needDad, ch, 149, s, 1);
            if (!array.success)
                return array;

            records.Add(MakeConstantRecord("плотность сухой части газа при стандартных условиях, кг/м³", array.categories[0][0], date));
            constants.records = records;

            constants.records = records;
            return constants;
        }

        private dynamic MakeConstantRecord(string name, object value, DateTime date)
        {
            dynamic record = new ExpandoObject();
            record.type = "Constant";
            record.s1 = name;
            record.s2 = value.ToString();
            record.date = date;
            record.dt1 = DateTime.Now;
            return record;
        }
    }
}
