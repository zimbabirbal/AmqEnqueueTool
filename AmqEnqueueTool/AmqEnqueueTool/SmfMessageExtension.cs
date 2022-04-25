using Newtonsoft.Json;
using Smarsh.Core.Smf;
using Smarsh.Core.Smf.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AmqEnqueueTool
{
    public static class SmfMessageExtension
    {
        public static SmfMessage DeepCopy(this SmfMessage self)
        {
            var path = @"C:\Users\btamang\Desktop\temp-Slack\123.smf";
            SmfSerializer.Serialize(path, self, CancellationToken.None);

            return SmfSerializer.Deserialize(path, CancellationToken.None);
        }
    }
}
