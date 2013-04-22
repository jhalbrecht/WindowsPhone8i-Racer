using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Networking.Proximity;

namespace EightRacer.Model
{
    /// <summary>
    ///  Class to hold all paired device information
    /// </summary>
    public class PairedDeviceInfo
    {
        internal PairedDeviceInfo(PeerInformation peerInformation)
        {
            this.PeerInfo = peerInformation;
            this.DisplayName = this.PeerInfo.DisplayName;
            this.HostName = this.PeerInfo.HostName.DisplayName;
            this.ServiceName = this.PeerInfo.ServiceName;
        }

        public string DisplayName { get; private set; }
        public string HostName { get; private set; }
        public string ServiceName { get; private set; }
        public PeerInformation PeerInfo { get; private set; }
    }
}
