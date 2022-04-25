using Smarsh.Core.Messaging.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AmqEnqueueTool.Models
{
    public class SmfTestModel
    {
        public int ClientId { get; set; }
        public int FasaId { get; set; }
        public string SmfPath { get; set; }
        public MessageQueueType MessageQueueType { get; set; }
        public string MappingSetname { get; set; }

        public string Channel { get; set; }
        public string ApplicationName { get; set; }
    }
}
