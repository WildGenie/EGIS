using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using GeoDecisions.Esb.Client.Core.Interfaces;
using GeoDecisions.Esb.Common;
using GeoDecisions.Esb.Common.Messages;
using ServiceStack.Text;

namespace GeoDecisions.Esb.Client.Core.ComHandlers
{

    /// <summary>
    /// Handles some basic shared logic for the comhandlers.  Mostly to hold some properties and handle the pulse calls.
    /// </summary>
    public abstract class ComHandler : IComHandler
    {
        private Configuration _configuration;
        private Timer _pulseTimer;

        protected ComHandler(Configuration configuration)
        {
            _configuration = configuration;
        }

        public abstract string Name { get; }

        public virtual int Priority { get; set; }

        public IComHandler Successor { get; set; }

        // I think this can be removed from the interface... do we need it?
        public IComHandler CreateHandler(Configuration config)
        {
            throw new NotImplementedException();
        }

        public virtual bool Connect()
        {
            throw new NotImplementedException();
        }

        public virtual bool CanHandle(Subscribe subscribe)
        {
            throw new NotImplementedException();
        }

        public virtual bool CanHandle(EsbMessage message)
        {
            throw new NotImplementedException();
        }

        public virtual string DispatchMessage(EsbMessage message, string uri = null)
        {
            throw new NotImplementedException();
        }

        public virtual void DispatchSubscribe<T>(Subscribe subscribe, Action<T, EsbMessage> callback)
        {
            throw new NotImplementedException();
        }

        public virtual void DispatchUnSubscribe(UnSubscribe unSubscribe)
        {
            throw new NotImplementedException();
        }

        public virtual bool Shutdown()
        {
            throw new NotImplementedException();
        }

        public void Replay<T>(string uri, double dDays, Action<List<T>, List<EsbMessage>> callback)
        {
            throw new NotImplementedException();
        }

        protected abstract void HandlePulse(Pulse pulse);

        private void StartPulse()
        {
            var json = new Pulse {ClientId = this._configuration.Id}; //.ToJson();

            if (_pulseTimer == null)
                _pulseTimer = new Timer(TimeSpan.FromSeconds(60).TotalMilliseconds); // fire once every 60 seconds

            _pulseTimer.Elapsed += (sender, args) =>
                {
                    _pulseTimer.Stop();

                    // call abstract
                    HandlePulse(json);

                    _pulseTimer.Start();
                };

            _pulseTimer.Start();
        }
    }
}
