using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;
using Microsoft.Extensions.Logging;
using RaiffaisenBot.Logic.Handlers.Abstractions;
using System.Text;
using System.Text.RegularExpressions;
using Telegram.Bot;
using Telegram.Bot.Requests;
using Telegram.Bot.Types;

namespace RaiffaisenBot.Logic.Handlers.Messages.Files
{
    public class AccountStatementFilesHandler : MessageHandlerBase
    {
        private readonly ILogger<AccountStatementFilesHandler> _logger;
        private readonly ITelegramBotClient _botClient;

        public AccountStatementFilesHandler(ILogger<AccountStatementFilesHandler> logger, ITelegramBotClient botClient)
        {
            _logger = logger;
            _botClient = botClient;
        }

        public override int Priority => 1;

        public override HandlerMessageType MessageType => HandlerMessageType.Document;

        public override async Task<bool> CanHandleAsync(Update update)
        {
            var message = update.Message;

            if (message == null)
            {
                return false;
            }
            if (MessageType != Mapper.MapToHandlerMessageType(message.Type))
            {
                _logger.LogInformation($"Message type is not supported for this handler, it is {Mapper.MapToHandlerMessageType(message.Type)}");
                return false;
            }
            if (message.Document == null)
            {
                _logger.LogInformation($"I have got an message with empty document");
                return false;
            }
            if (string.IsNullOrEmpty(message.Document!.FileName))
            {
                _logger.LogInformation($"I have got an document with empty file name");
                return false;
            }
            if (!message.Document.FileName!.EndsWith("pdf", StringComparison.InvariantCultureIgnoreCase))
            {
                _logger.LogInformation($"I have got an file that not pdf, it named {message.Document.FileName}");
                return false;
            }
            return true;
        }

        public override async Task<RequestBase<Message>?> HandleAsync(Update update, CancellationToken cancellationToken)
        {
            var message = update.Message;


            var fileId = message!.Document!.FileId;
            _logger.LogInformation($"Got file id {fileId}");
            var fileInfo = await _botClient.GetFileAsync(fileId, cancellationToken);
            if (string.IsNullOrEmpty(fileInfo.FilePath))
            {
                _logger.LogWarning($"For client {message!.From?.Id} uploaded file have empty file path");
                return CreateTextMessage(message!.Chat.Id, "Unable to process file, sorry");
            }
            _logger.LogInformation($"Got file path {fileInfo.FilePath}");
            using MemoryStream ms = new MemoryStream();
            await _botClient.DownloadFileAsync(fileInfo.FilePath, ms, cancellationToken);
            var content = await ConvertAccountStatementPdfToCsvAsync(ms.ToArray());
            string fileName = System.IO.Path.GetFileNameWithoutExtension(message.Document.FileName) + ".csv";
            _logger.LogInformation($"Sending back file {fileName}");
            return CreateDocumentMessage(message!.Chat.Id, InputFile.FromStream(content, fileName));
        }

        private async Task<MemoryStream> ConvertAccountStatementPdfToCsvAsync(byte[] pdfContent)
        {
            string startText = "Datum prijema/ Datum Broj kartice Opis promene Iznos u Iznos u orig. Isplata Uplata Stanje";
            string startText2 = "Datum transakcije izvršenja ref. valuti valuti";

            try
            {
                // Create a StringBuilder to store the extracted text
                var sb = new StringBuilder();

                // Open the PDF file
                using (var pdfReader = new PdfReader(pdfContent))
                {
                    // Iterate through each page of the PDF
                    for (int page = 1; page <= pdfReader.NumberOfPages; page++)
                    {
                        // Extract text from the current page
                        sb.Append(PdfTextExtractor.GetTextFromPage(pdfReader, page));
                    }
                }

                // Display the extracted text
                string text = sb.ToString();
                int indexStart = text.IndexOf(startText) + startText.Length + startText2.Length + Environment.NewLine.Length;
                text = text.Substring(indexStart, text.Length - indexStart);

                string[] lines = text.Split('\n');

                // начало эффективной строки всегда идёт с даты, конец всегда с .dd
                // следующие до эфффективной строки будут содержать инфу о том где и что купили
                sb = new StringBuilder();
                string currentEffectiveLine = "";
                string merchantText = "";
                HashSet<string> garbageLines = new()
                {
                    "Za devizne transakcije napravljene debitnim karticama koje se knjiže po dinarskom računu za konverziju iznosa iz referentne valute (EUR) se koristi",
                    "Za dinarske transakcije napravljene debitnim karticama koje se knjiže po deviznom računu za konverziju u valutu zaduženja primenjuje se kupovni kurs Raiffeisen banke a.d. Beograd za devize koji važi na dan knjiženja (izvršenja)..",
                    "prodajni/kupovni kurs Raiffeisen banke a.d. Beograd za devize koji važi na dan knjiženja (izvršenja).",
                    "Za dinarske transakcije napravljene debitnim karticama koje se knjiže po deviznom računu se koristi kupovni kurs Raiffeisen banke a.d. Beograd za devize koji",
                    "važi na dan knjiženja (izvršenja).",
                    "Poštovani korisniče, za sve dodatne informacije o Vašem računu možete da kontaktirate naš Kontakt centar koji vam stoji na raspolaganju 24h na broju +381",
                    "11 3202 100.",
                    "Datum prijema",
                    "Datum transakcije",
                    " Za dinarske transakcije napravljene"
                };
                MemoryStream ms = new MemoryStream();
                await ms.WriteAsync(ReadStringToBytes("sep=;"));
                await ms.WriteAsync(ReadStringToBytes("Start date;End date;Card number;Description;Sum in destination currency;Spent;Added;Leftover"));
                foreach (var line in lines)
                {
                    bool goOn = true;
                    foreach (var garbage in garbageLines)
                    {
                        if (line.StartsWith(garbage))
                        {
                            goOn = false;
                        }
                    }
                    if (!goOn)
                    {
                        continue;
                    }
                    // нас интересует имеет ли строка вид 01.07.2023 01.07.2023 0.00 360.66 0.00 10,603.97
                    int whiteSpaceIndex = line.IndexOf(" ");
                    if (whiteSpaceIndex != -1)
                    {
                        string probablyDate = line.Substring(0, whiteSpaceIndex);
                        if (DateTime.TryParseExact(probablyDate, "dd.MM.yyyy", null, System.Globalization.DateTimeStyles.None, out var date))
                        {
                            if (!string.IsNullOrEmpty(currentEffectiveLine))
                            {
                                // это уже не первое родео
                                // заменим #message на сообщение о покупке
                                currentEffectiveLine = currentEffectiveLine.Replace("#message", merchantText);
                                foreach (var garbage in garbageLines)
                                {
                                    if (merchantText.Contains(garbage))
                                    {
                                        merchantText = merchantText.Replace(garbage, "");
                                    }
                                }
                                await ms.WriteAsync(ReadStringToBytes(currentEffectiveLine));
                                currentEffectiveLine = "";
                                merchantText = "";
                            }
                            // вот он, язь, здоровенный
                            currentEffectiveLine = line;

                            // теперь надо вставить плейсхолдер для названия покупки в нужное место
                            // иногда в строке с датой есть номер карты, а иногда нет. 

                            whiteSpaceIndex = currentEffectiveLine.IndexOf(" ", whiteSpaceIndex + 1);
                            bool cardNumberPresent = true;
                            // возьмём следующие 4 символа
                            string probablyNum = currentEffectiveLine.Substring(whiteSpaceIndex + 1, 4);
                            if (!int.TryParse(probablyNum, out _))
                            {
                                // если не было карты, надо вставить пробел, который потом заменим на разделитель чтоб была пустая колонка
                                cardNumberPresent = false;
                                currentEffectiveLine = currentEffectiveLine.Insert(whiteSpaceIndex, ";");
                            }

                            // теперь надо дойти до колонки где была сумма оригинальной валюты
                            // чтобы вставить перед ней наш плейсхолдер.
                            // проблема в том что сумма может быть 0.00, 42.42 44444.11 и тд
                            // попробуем использовать регулярку
                            // НО в строке может быть сообщение о покупке, сука ебучий райфайзен как же ты заебал уже меня к этой строке
                            // эти пидорасы ещё и сказали что хер, эксельку они мне не сделают,
                            // говножуи 
                            // хуй с ним, попробуем чекнуть есть ли в строке азбучные символы
                            string pattern = @"[a-zA-Z]";
                            var match = Regex.Match(currentEffectiveLine, pattern);
                            if (match.Success)
                            {
                                // есть бля, вы охуели! 
                                // придётся применять магию чтоб их вычислить. 
                                // строка имеет вид
                                // 05.07.2023 06.07.2023 0720 MAMA SHELTER BGD BAR 3 0.00 5,180.00 0.00 717,348.59
                                // и я жутко охуею если будут строки где такая же строка но нет номера карты
                                // часом позже: они были
                                int num = 0;
                                int startIndex = 0;
                                // попробуем найти второй или третий пробел (что? да)
                                int max = 2;
                                if (cardNumberPresent)
                                {
                                    max++;
                                }
                                while (num < max)
                                {
                                    if (currentEffectiveLine[startIndex] == ' ')
                                    {
                                        num++;
                                    }
                                    startIndex++;
                                }
                                // теперь индекс 4 пробела в переменной index
                                // надо найти конец этой ёбаной строки, а она может кончится и не азбучным символом! 
                                // поищем  сумму 
                                pattern = @"\b\d{1,8}\.\d{2} ";
                                var str = currentEffectiveLine.Substring(startIndex);
                                match = Regex.Match(str, pattern);
                                int endIndex = 0;
                                if (match.Success)
                                {
                                    endIndex = currentEffectiveLine.IndexOf(match.Value);
                                }

                                string message = string.Empty;
                                // сука щастье привалило
                                if (startIndex >= 0 && endIndex > startIndex)
                                {
                                    message = currentEffectiveLine.Substring(startIndex, endIndex - startIndex);
                                }
                                try
                                {
                                    string subStr = currentEffectiveLine;
                                    if (!string.IsNullOrEmpty(message))
                                    {
                                        // теперь давай вырежем эту строку из исходной и заменим там пробелы на разделители
                                        subStr = currentEffectiveLine.Replace(message, string.Empty);
                                    }
                                    subStr = subStr.Replace(' ', ';');
                                    // добавим к нашему сообщению плейсхолдер чтоб потом вставить сообщения 
                                    // из других строк, нам же нехуй больше делать да 
                                    message = message + " #message;";

                                    // и вставим эту ебанину назад
                                    // надо ещё и ; вставить тоже ибо мы ж заменим его
                                    currentEffectiveLine = subStr.Insert(startIndex, message);
                                }
                                catch (Exception)
                                {

                                    throw;
                                }
                            }
                            else
                            {
                                int num = 0;
                                int startIndex = 0;
                                // попробуем найти второй или третий пробел (что? да)
                                int max = 2;
                                if (cardNumberPresent)
                                {
                                    max++;
                                }
                                while (num < max)
                                {
                                    if (currentEffectiveLine[startIndex] == ' ')
                                    {
                                        num++;
                                    }
                                    startIndex++;
                                }
                                currentEffectiveLine = currentEffectiveLine.Insert(startIndex, "#message;");
                                currentEffectiveLine = currentEffectiveLine.Replace(' ', ';');
                            }
                        }
                        else
                        {
                            merchantText += line.Trim() + " ";
                        }
                    }

                }
                _logger.LogInformation($"Created array length of {ms.Length}");
                return ms;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "всё плохо");
            }
            _logger.LogInformation("Shouldn't be there");
            return new MemoryStream();
        }

        private byte[] ReadStringToBytes(string input)
        {
            if (!input.EndsWith(Environment.NewLine))
            {
                input = input + Environment.NewLine;
            }
            return Encoding.UTF8.GetBytes(input);
        }
    }
}
