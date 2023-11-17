using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types;
using Telegram.Bot;
using static System.Net.Mime.MediaTypeNames;
using DemoBot.Models;
using System.IO;
using NAudio.Wave;
using System.Net.Http;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech;
using Newtonsoft.Json;

namespace DemoBot.Services
{
    public class TelegramBotService
    {
        private readonly ILogger<TelegramBotService> logger;
        private readonly ITelegramBotClient telegramBotClient;

        public TelegramBotService(
            ITelegramBotClient telegramBotClient,
            ILogger<TelegramBotService> logger)
        {
            this.telegramBotClient = telegramBotClient;
            this.logger = logger;
        }

        public async ValueTask EchoAsync(Update update)
        {
            var handler = update.Type switch
            {
                UpdateType.Message => BotOnMessageRecieved(update.Message),
                UpdateType.CallbackQuery => BotOnCallBackQueryRecieved(update.CallbackQuery),
                _ => UnknownUpdateTypeHandler(update)
            };

            try
            {
                await handler;
            }
            catch (Exception ex)
            {
                await HandleErrorAsync(ex);
            }
        }

        public ValueTask HandleErrorAsync(Exception ex)
        {
            var errorMessage = ex switch
            {
                ApiRequestException apiRequestException =>
                    $"Telegram API Error :\n{apiRequestException.ErrorCode}",
                _ => ex.ToString()
            };

            this.logger.LogInformation(errorMessage);

            return ValueTask.CompletedTask;
        }

        private ValueTask UnknownUpdateTypeHandler(Update update)
        {
            this.logger.LogInformation($"Unknown upodate type: {update.Type}");

            return ValueTask.CompletedTask;
        }

        private async ValueTask BotOnCallBackQueryRecieved(CallbackQuery callbackQuery)
        {
            await telegramBotClient.SendTextMessageAsync(
                chatId: callbackQuery.Message.Chat.Id,
                text: $"{callbackQuery.Data}");
        }


        private async ValueTask BotOnMessageRecieved(Message message)
        {
            this.logger.LogInformation($"A message has arrived: {message.Type}");

            if (message.Audio is not null)
            {
                var audioFile = await telegramBotClient.GetFileAsync(message.Audio.FileId);

                using (var httpClient = new HttpClient())
                using (var responseStream = await httpClient.GetStreamAsync(audioFile.FilePath))
                {
                    var wavStream = AudioConverter.ConvertToWav(responseStream);

                    var convertedWavFilePath = "converted.wav";

                    using (var fileStream = new FileStream(convertedWavFilePath, FileMode.Create))
                    {
                        await wavStream.CopyToAsync(fileStream);
                    }

                    var config = SpeechConfig.FromSubscription("0eee4723c1a9413b949e940688aea53f", "koreacentral");

                    string language = "en-US";
                    string topic = "your own topic";

                    var audioConfig = AudioConfig.FromWavFileInput(convertedWavFilePath);
                    var speechRecognizer = new SpeechRecognizer(config, language.Replace("_", "-"), audioConfig);

                    var connection = Connection.FromRecognizer(speechRecognizer);

                    var phraseDetectionConfig = new
                    {
                        enrichment = new
                        {
                            pronunciationAssessment = new
                            {
                                referenceText = "",
                                gradingSystem = "HundredMark",
                                granularity = "Word",
                                dimension = "Comprehensive",
                                enableMiscue = "False",
                                enableProsodyAssessment = "True"
                            },
                            contentAssessment = new
                            {
                                topic = topic
                            }
                        }
                    };
                    connection.SetMessageProperty("speech.context", "phraseDetection", JsonConvert.SerializeObject(phraseDetectionConfig));

                    var phraseOutputConfig = new
                    {
                        format = "Detailed",
                        detailed = new
                        {
                            options = new[]
                            {
                                "WordTimings",
                                "PronunciationAssessment",
                                "ContentAssessment",
                                "SNR",
                            }
                        }
                    };
                    connection.SetMessageProperty("speech.context", "phraseOutput", JsonConvert.SerializeObject(phraseOutputConfig));

                    var done = false;
                    var fullRecognizedText = "";

                    speechRecognizer.SessionStopped += (s, e) =>
                    {
                        Console.WriteLine("Closing on {0}", e);
                        done = true;
                    };

                    speechRecognizer.Canceled += (s, e) =>
                    {
                        Console.WriteLine("Closing on {0}", e);
                        done = true;
                    };

                    connection.MessageReceived += async (s, e) =>
                    {
                        if (e.Message.IsTextMessage())
                        {
                            var messageText = e.Message.GetTextMessage();
                            var json = Newtonsoft.Json.Linq.JObject.Parse(messageText);
                            if (json.ContainsKey("NBest"))
                            {
                                var nBest = json["NBest"][0];
                                if (json["NBest"][0]["Display"].ToString().Trim().Length > 1)
                                {
                                    var recognizedText = json["DisplayText"];
                                    fullRecognizedText += $" {recognizedText}";
                                    Console.WriteLine($"Pronunciation Assessment Results for: {recognizedText}");

                                    var accuracyScore = nBest["PronunciationAssessment"]["AccuracyScore"].ToString();
                                    var fluencyScore = nBest["PronunciationAssessment"]["FluencyScore"].ToString();
                                    var prosodyScore = nBest["PronunciationAssessment"]["ProsodyScore"].ToString();
                                    var completenessScore = nBest["PronunciationAssessment"]["CompletenessScore"].ToString();
                                    var pronScore = nBest["PronunciationAssessment"]["PronScore"].ToString();

                                    await this.telegramBotClient.SendTextMessageAsync(
                                        chatId: message.Chat.Id,
                                        text:
                                        $"AccuracyScore {accuracyScore}\n" +
                                        $"FluencyScore {fluencyScore}\n" +
                                        $"ProsodyScore {prosodyScore}\n" +
                                        $"CompletenessScore {completenessScore}" +
                                        $"PronScore {pronScore}");
                                }
                                else
                                {
                                    Console.WriteLine($"Content Assessment Results for: {fullRecognizedText}");
                                }
                                string jsonText = JsonConvert.SerializeObject(json, Newtonsoft.Json.Formatting.Indented, new JsonSerializerSettings());
                                Console.WriteLine(jsonText);
                            }
                        }
                    };

                    await speechRecognizer.StartContinuousRecognitionAsync().ConfigureAwait(false);

                    while (!done)
                    {
                        await Task.Delay(1000);
                    }

                    await speechRecognizer.StopContinuousRecognitionAsync().ConfigureAwait(false);
                }
            }
        }
        public static class AudioConverter
        {
            public static Stream ConvertToWav(Stream audioStream)
            {
                using (var mp3Reader = new Mp3FileReader(audioStream))
                {
                    var wavStream = new MemoryStream();

                    using (var waveWriter = new WaveFileWriter(wavStream, mp3Reader.WaveFormat))
                    {
                        byte[] buffer = new byte[4096];
                        int bytesRead;
                        while ((bytesRead = mp3Reader.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            waveWriter.Write(buffer, 0, bytesRead);
                        }
                    }

                    wavStream.Position = 0;
                    return wavStream;
                }
            }
        }
    }
}
