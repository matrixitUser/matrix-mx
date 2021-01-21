using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TuesPechkin;

namespace Matrix.Reports
{
    class Html2PdfConvertor
    {
        public byte[] Convert(string html)
        {
            //замечание от разработчиков библиотеки
            // Keep the converter somewhere static, or as a singleton instance!
            // Do NOT run the above code more than once in the application lifecycle!

            var document = new HtmlToPdfDocument
            {
                GlobalSettings =
                {
                    ProduceOutline = true,
                    DocumentTitle = "Report",
                    PaperSize = new PechkinPaperSize("210", "297"),//PaperKind.A4, // Implicit conversion to PechkinPaperSize
                    Margins =
                    {
                        All = 1.375,
                        Unit = Unit.Centimeters
                    }
                },
                Objects = {
                        new ObjectSettings { HtmlText = html },
                    }
            };

            return converter.Convert(document);
        }

        private static readonly IConverter converter;

        private Html2PdfConvertor()
        {

        }
        static Html2PdfConvertor()
        {
            converter =
                       new ThreadSafeConverter(
                           new PdfToolset(
                               new Win32EmbeddedDeployment( //или 64 бита где как
                                   new TempFolderDeployment())));// если крашится, то установите vc++ redist 2013 x86
        }
        private static Html2PdfConvertor instance = new Html2PdfConvertor();
        public static Html2PdfConvertor Instance
        {
            get
            {
                return instance;
            }
        }
    }
}
