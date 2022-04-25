using AmqEnqueueTool.Models;
using Microsoft.Win32;
using Newtonsoft.Json;
using Smarsh.Core.IO;
using Smarsh.Core.Messaging;
using Smarsh.Core.Messaging.Models;
using Smarsh.Core.Messaging.Pooling;
using Smarsh.Core.Smf;
using Smarsh.Services.QueueContracts;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace AmqEnqueueTool
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private List<string> _items;
        CancellationTokenSource _cts;
        IMessageQueueClient _amqClient;
        MessageQueueClientPool _activeMqClientPool;
        Dictionary<string, string> _channelsCollection;
        int _numberOfEnqueuedItem;
        public MainWindow()
        {
            InitializeComponent();
            _items = new List<string>();
            _items = Enum.GetNames(typeof(MessageQueueType)).ToList();
            _cts = new CancellationTokenSource();
            _channelsCollection = new Dictionary<string, string>();
            _channelsCollection.Add("Slack", "SlackArchiver"); //channel and application name

            combox.ItemsSource = comboxM.ItemsSource = _items;
            comboxChannel.ItemsSource = _channelsCollection.Keys.ToList();

        }

        private void sampleText_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Assembly assembly;
            StreamReader textStreamReader;

            try
            {
                assembly = Assembly.GetExecutingAssembly();
                textStreamReader = new StreamReader(assembly.GetManifestResourceStream("AmqEnqueueTool.SlackSample.txt"));
                MessageBox.Show(textStreamReader.ReadToEnd());
            }
            catch
            {
                MessageBox.Show("Error accessing resources!!!");
            }
        }

        private void btn_SingleEnqueue(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(textBox2.Text) || string.IsNullOrEmpty(textBox2_Copy.Text))
            {
                MessageBox.Show("Please fill up all fields");
                return;
            }
            try
            {
                var queueMessage = JsonConvert.DeserializeObject<Message>(textBox2_Copy.Text);

                using (IMessageQueueClient client = MessagingHelper.GetMessageQueueClient())
                {
                    client.Enqueue((MessageQueueType)Enum.Parse(typeof(MessageQueueType), combox.SelectedValue.ToString()),
                        textBox2.Text,
                        new List<Message>
                        {
                            queueMessage
                        }, CancellationToken.None);
                    client.Commit();
                }

                //old way to directly enqueue message
                //using (_queueManager = new ActiveMQManager(textBox2_Copy1.Text))
                //{
                //    //_queueManager.Enqueue(textBox2.Text,
                //    //                new List<Object> { JsonConvert.SerializeObject(queueMessage) },
                //    //                CancellationToken.None);

                //    //_queueManager.Commit();
                //}

                MessageBox.Show("Successfully Enqueued data.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error occured: {ex.Message}");
            }
        }

        private void btn_smfLoadTest_Enqueue(object sender, RoutedEventArgs e)
        {
            _cts = new CancellationTokenSource();
            bool runInfinitelly = false;
            bool generateNewMessageEverytime, stopsAt = false;
            _numberOfEnqueuedItem = 0; //reset the counter
            int stopAtValue = 1;

            if (string.IsNullOrEmpty(textBox2M.Text) || string.IsNullOrEmpty(clientIdU.Text) || string.IsNullOrEmpty(smfPathU.Text) ||
                string.IsNullOrEmpty(fasaIdU.Text))
            {
                MessageBox.Show("Please fill up all fields");
                return;
            }

            var clientId = int.Parse(clientIdU.Text);
            var fasaId = int.Parse(fasaIdU.Text);
            var smfPath = smfPathU.Text;

            if (GenerateNewMessageEverytimeCheckBox.IsChecked != null)
            {
                generateNewMessageEverytime = (bool)GenerateNewMessageEverytimeCheckBox.IsChecked;
            }
            if (RunInfinitelyCheckBox.IsChecked != null)
            {
                runInfinitelly = (bool)RunInfinitelyCheckBox.IsChecked;
            }
            if (StopAtCheckBox.IsChecked != null)
            {
                stopsAt = (bool)StopAtCheckBox.IsChecked;
            }
            if (!string.IsNullOrEmpty(textBox2MStopAt.Text))
            {
                stopAtValue = int.Parse(textBox2MStopAt.Text);
            }

            label2MStatus2.Text = "running...";
            label2MStatus1.Text = "ItemsEnqueued: " + _numberOfEnqueuedItem;

            try
            {
                _activeMqClientPool = MessagingHelper.GetMessageQueueClientPool
                        (100, QueueExceptionHandler);

                var smfTestModel = new SmfTestModel()
                {
                    ClientId = clientId,
                    FasaId = fasaId,
                    MessageQueueType = (MessageQueueType)Enum.Parse(typeof(MessageQueueType), combox.SelectedValue.ToString()),
                    MappingSetname = textBox2M.Text,
                    Channel = comboxChannel.SelectedValue.ToString(),
                    ApplicationName = _channelsCollection[comboxChannel.SelectedValue.ToString()]
                };
                //var processorCount = Environment.ProcessorCount;

                //int minWorker, minIOC;
                //// Get the current settings.
                //ThreadPool.GetMinThreads(out minWorker, out minIOC);


                //int maxWorker, maxIOC;
                //// Get the current settings.
                //ThreadPool.GetMaxThreads(out maxWorker, out maxIOC);
                //// Change the minimum number of worker threads to four, but
                //// keep the old setting for minimum asynchronous I/O 
                //// completion threads.

                //if (ThreadPool.SetMaxThreads(6, 6))
                //{
                //    // The minimum number of threads was set successfully.
                //}

                //while (!_cts.IsCancellationRequested || (stopAtValue != 0 && stopAtValue <= _numberOfEnqueuedItem))
                //{
                //    ThreadPool.QueueUserWorkItem(x => DoTheMessageEnqueueWork(smfPath, smfTestModel));
                //    //label2MStatus1.Text = "Enqueued Items: " + _numberOfEnqueuedItem.ToString();
                //}
                //for (int i = 0; i < 5; i++)
                //{
                //    ThreadPool.QueueUserWorkItem(x => DoTheMessageEnqueueWork(smfPath, smfTestModel));
                //    numItemEnqueued++;
                //}

                var thread = new Thread(() =>
                {
                    _amqClient = _activeMqClientPool.GetAvailableClient(TimeSpan.FromSeconds(120),
                                CancellationToken.None);
                    while (!_cts.IsCancellationRequested)
                    {
                        if (!runInfinitelly)
                        {
                            if (stopsAt && _numberOfEnqueuedItem > stopAtValue)
                            {
                                break;
                            }
                        }
                        Parallel.For(0, 1000, new ParallelOptions { MaxDegreeOfParallelism = 50 }, i =>
                        {
                            DoTheMessageEnqueueWork(smfPath, smfTestModel, _amqClient);
                            if (_numberOfEnqueuedItem % 100 == 0 && _numberOfEnqueuedItem > 1) //commit the items in batch
                                _amqClient.Commit();
                        });

                        _amqClient.Commit();

                        Application.Current.Dispatcher.Invoke(new Action(() =>
                        {
                            label2MStatus1.Text = "ItemsEnqueued: " + _numberOfEnqueuedItem;
                        }));
                    }

                    _amqClient.Commit();
                    Application.Current.Dispatcher.Invoke(new Action(() =>
                    {
                        label2MStatus1.Text = "ItemsEnqueued: " + _numberOfEnqueuedItem;
                        label2MStatus2.Text = "Task Completed.";
                    }));
                });

                thread.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error occured: {ex.Message}");
            }
        }

        private void DoTheMessageEnqueueWork(string smfPath, SmfTestModel smfTestModel, IMessageQueueClient amqClient)
        {
            var smfMessage = SmfSerializer.Deserialize(smfPath, CancellationToken.None);

            var curDate = DateTime.UtcNow;

            var guid = Guid.NewGuid().ToString();
            var testId = "LoadTest2022-" + guid;
            smfMessage.Header.DocumentDate = curDate;
            smfMessage.Header.SourceObjectId = testId;
            smfMessage.Header.RevisionId = testId;
            smfMessage.Header.ThreadId = testId;
            smfMessage.Header.ActionTime = curDate;

            var getItem = smfMessage.Content.Items[0];
            getItem.Data = JsonConvert.SerializeObject(new { message = $"LoadTest2022-msg-{guid}" });

            var getItem1 = smfMessage.Content.Items[1];
            getItem1.Data = string.Format($"LoadTest2022-msg-{guid}");

            smfMessage.RelatedObjects.Clear();
            var destinationPath = ConfigurationManager.AppSettings["SMFDestinationPath"];

            string smfRootPath = PathHelper.Combine(destinationPath,
               smfTestModel.FasaId.ToString(), curDate.ToString("yyyyMMddHHmm"));

            if (!Directory.Exists(smfRootPath))
                Directory.CreateDirectory(smfRootPath);

            var path = PathHelper.Combine(smfRootPath, $"{guid:N}.smf");

            try
            {
                // Save smf to the storage location 
                SmfSerializer.Serialize(path, smfMessage, CancellationToken.None);
                // Add item to ActiveMQ
                var messageQueueItem = new MessageQueueItem
                {
                    ClientId = smfTestModel.ClientId,
                    FasaId = smfTestModel.FasaId,
                    FilePath = path,
                    Application = smfTestModel.ApplicationName,
                    UpdateDate = DateTime.UtcNow,
                    Channel = smfTestModel.Channel
                };

                var queueHeaders = new Dictionary<string, string>
                {
                    {"ClientId", messageQueueItem.ClientId.ToString()},
                    {"FasaId", messageQueueItem.FasaId.ToString()},
                    {"Channel", smfTestModel.Channel}
                };

                amqClient.Enqueue(MessageQueueType.Item,
                    smfTestModel.MappingSetname,
                    new List<Message>
                    {
                        new Message {Body = JsonConvert.SerializeObject(messageQueueItem), Headers = queueHeaders}
                    },
                    CancellationToken.None);

                Interlocked.Increment(ref _numberOfEnqueuedItem);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error occured. error = {ex}");
            }
        }
        protected void QueueExceptionHandler(object sender, ExceptionThrownEventArgs args)
        {
            //Logger.Error("ActiveMQ exception occured", args.Exception);
            throw new Exception();
        }
        private void stopBtn_Click(object sender, RoutedEventArgs e)
        {
            _cts.Cancel();
            label2MStatus2.Text = "stopped.";
        }

        private void loadSmf_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Multiselect = true;
            openFileDialog.Filter = "Text documents (.smf)|*.smf";
            openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            bool? result = openFileDialog.ShowDialog();
            // Process open file dialog box results
            if (result == true)
            {
                // Open document
                smfPathU.Text = openFileDialog.FileName;
            }
        }
    }
}
