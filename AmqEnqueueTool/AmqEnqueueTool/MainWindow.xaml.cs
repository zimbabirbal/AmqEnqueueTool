using Newtonsoft.Json;
using Smarsh.Core.Messaging;
using Smarsh.Core.Messaging.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
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
        public MainWindow()
        {
            InitializeComponent();
            _items = new List<string>();
            _items = Enum.GetNames(typeof(MessageQueueType)).ToList();
            combox.ItemsSource = _items;
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

        private void button_Click(object sender, RoutedEventArgs e)
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
    }
}
