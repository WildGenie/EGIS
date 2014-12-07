using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using GeoDecisions.Esb.Common;
using GeoDecisions.Esb.Common.Messages;
using GeoDecisions.Esb.Common.Storage;
using GeoDecisions.Esb.Server.Core.Interfaces;
using GeoDecisions.Esb.Server.Core.Models;
using ServiceStack.Text;

namespace GeoDecisions.Esb.Server.Core
{
    internal class TransportManager : ITransportManager
    {
        private readonly Bus _bus;
        private readonly Configuration _configuration;

        private TransportManager(Configuration configuration, Bus bus)
        {
            CurrentSubscribers = new SynchronizedCollection<ISubscriber>();
            CurrentPublishers = new SynchronizedCollection<IPublisher>();
            CurrentMessages = new SynchronizedCollection<EsbMessage>();
            CurrentClients = new SynchronizedCollection<Client>();

            _configuration = configuration;
            _bus = bus;
        }

        public SynchronizedCollection<ISubscriber> CurrentSubscribers { get; private set; }

        //Added the CurrentMessages property so that messages can be accesible from the bus, these should probably also be saved and persisted, rxs, 04/12/2013
        public SynchronizedCollection<EsbMessage> CurrentMessages { get; private set; }

        //Added the CurrentPublishers property so that active publishers can be accesible from the bus, these should probably also be saved and persisted, rxs, 04/18/2013
        public SynchronizedCollection<IPublisher> CurrentPublishers { get; private set; }

        public SynchronizedCollection<Client> CurrentClients { get; private set; }

        public ITransportManager CreateManager(Bus bus, Configuration config)
        {
            return new TransportManager(config, bus);
        }

        public void RePublish(string channel, EsbMessage message)
        {
            IEnumerable<IServerTransporter> transporters = _bus.FindInterestedTransporters(channel);

            var task = new Task(() =>
                {
                    var stopwatch = new Stopwatch();
                    stopwatch.Start();

                    transporters.ToList().ForEach(trans =>
                        {
                            try
                            {
                                //_configuration.DefaultLogger.Log("Starting Republish from " + trans.Name);
                                trans.RePublish(channel, message);
                                //_configuration.DefaultLogger.Log("Ending Republish from " + trans.Name);
                            }
                            catch (Exception ex)
                            {
                                _configuration.DefaultLogger.LogError(string.Format("An error occurred while attempting [{0}] republish. Error dump: {1}", trans.Name, ex.Dump()));
                            }
                        });

                    stopwatch.Stop();
                    //Debug.WriteLine("Republish time: {0}[ms] {1}[s]", stopwatch.ElapsedMilliseconds, (stopwatch.ElapsedMilliseconds / 1000));
                });

            task.Start(); // kick off the republish on a seperate thread.

        }

        //Note: To receive a replayed message, client should subscribe with the below string prefixed to the original channel
        public void RePlay(string channel, EsbMessage message)
        {
            IEnumerable<IServerTransporter> transporters = _bus.FindInterestedTransporters(Names.REPLAY_CHANNEL_PREFIX + channel);

            var task = new Task(() =>
            {
                transporters.ToList().ForEach(trans =>
                {
                    try
                    {
                        trans.RePlay(channel, message);
                    }
                    catch (Exception ex)
                    {
                        _configuration.DefaultLogger.LogError(string.Format("An error occurred while attempting [{0}] republish. Error dump: {1}", trans.Name, ex.Dump()));
                    }
                });

            });

            task.Start(); // kick off the replay on a seperate thread.

        }


        public void AddSubscriber(IServerTransporter srcTransporter, ISubscriber subscriber)
        {
            //Debug.WriteLine("AddSubscriber");
            //_configuration.DefaultLogger.Log("AddSubscriber");
            //Console.WriteLine("Console.WriteLine->AddSubscriber");

            // need to check what other transporters are around and see if they want in on this message
            // for example if someone subs from http rest, we need to tell the other transporters about it

            CurrentSubscribers.Add(subscriber);

            var task = new Task(() =>
                {
                    var stopwatch = new Stopwatch();
                    stopwatch.Start();

                    StorageManager.SaveSubscriber(subscriber);

                    IEnumerable<IServerTransporter> transporters = _bus.AllTransporters;

                    transporters.ToList().ForEach(trans =>
                        {
                            try
                            {

                                trans.HandleSubscribe(srcTransporter, subscriber);
                            }
                            catch (Exception ex)
                            {
                                _configuration.DefaultLogger.Log("HandleSubscribe: " + ex.Message);
                            }
                        });


                    stopwatch.Stop();
                    //Debug.WriteLine("AddSub time: {0}[ms] {1}[s]", stopwatch.ElapsedMilliseconds, (stopwatch.ElapsedMilliseconds / 1000));
                    //_configuration.DefaultLogger.Log(string.Format("{2} - AddSub time: {0}[ms] {1}[s]", stopwatch.ElapsedMilliseconds, (stopwatch.ElapsedMilliseconds/1000)));
                });


            task.Start(); // kick off the republish on a seperate thread.
      
        }

        public void AddMessage(string channel, EsbMessage message)
        {
            CurrentMessages.Add(message);

            //TODO initialize static instance of database 
            //StorageManager.SaveMessage(message);


             // kick off on another thread.  This reduces publish time of actual message.
            var task = new Task(() =>
                {
                    var stopwatch = new Stopwatch();
                    stopwatch.Start();

                    //TODO initialize static instance of database 
                    StorageManager.SaveMessage(message);

                    stopwatch.Stop();
                    //Debug.WriteLine("AddMsg time: {0}[ms] {1}[s]", stopwatch.ElapsedMilliseconds, (stopwatch.ElapsedMilliseconds / 1000));
                });

            task.Start(); // kick off the republish on a seperate thread.           
        }

        public void AddPublisher(IPublisher publisher)
        {
            CurrentPublishers.Add(publisher);

            //StorageManager.SavePublisher(publisher);

            //kick off on another thread.  This reduces publish time of actual message.
            var task = new Task(() =>
                {
                    var stopwatch = new Stopwatch();
                    stopwatch.Start();

                    StorageManager.SavePublisher(publisher);

                    stopwatch.Stop();
                    //Debug.WriteLine("AddPub time: {0}[ms] {1}[s]", stopwatch.ElapsedMilliseconds, (stopwatch.ElapsedMilliseconds / 1000));
                });

            task.Start(); // kick off the republish on a seperate thread.
        }

        public void RemoveSubscriber(IServerTransporter transporter, ISubscriber subscriber)
        {

            var removeList = CurrentSubscribers.Where(sub =>
                                     sub.Uri == subscriber.Uri &&
                                     sub.MachineName == subscriber.MachineName &&
                                     sub.ClientId == subscriber.ClientId &&
                                     sub.GetType() == subscriber.GetType()).ToList();

            removeList.ForEach(sub => CurrentSubscribers.Remove(sub));
        
            //CurrentSubscribers.RemoveAll(sub =>
            //                             sub.Uri == subscriber.Uri &&
            //                             sub.MachineName == subscriber.MachineName &&
            //                             sub.ClientId == subscriber.ClientId &&
            //                             sub.GetType() == subscriber.GetType()
            //    );

            //StorageManager.SaveSubscriber((ISubscriber)subscriber);  //TODO: Add Remove Method

            IEnumerable<IServerTransporter> transporters = _bus.AllTransporters; // _bus.FindInterestedTransporters(subscriber.Uri);

            transporters.ToList().ForEach(trans =>
                {
                    try
                    {
                        trans.HandleUnSubscribe(transporter, subscriber);
                    }
                    catch (Exception ex)
                    {
                        _configuration.DefaultLogger.Log("HandleSubscribe: " + ex.Message);
                    }
                });
        }

        public void ClearClient(string clientId)
        {

        }

        public void AddClient(Client client)
        {
            CurrentClients.Add(client);
        }

        public void Shutdown()
        {
            //Handle shutdown
            //throw new NotImplementedException();
        }

        //We may not need this. If we decide to use this function in the bus then it should be added to the ITransportManager
        public List<ISubscriber> RemoveSubscriberList(string uri)
        {
            var removeList = CurrentSubscribers.Where(sub => sub.Uri == uri).ToList();

            removeList.ForEach(sub => CurrentSubscribers.Remove(sub));

            return removeList;
        }

        public void RemoveClientList(string id)
        {
            CurrentClients.Where(cli => cli.Id == id).ToList().ForEach(cli => CurrentClients.Remove(cli));
        }
    }
}