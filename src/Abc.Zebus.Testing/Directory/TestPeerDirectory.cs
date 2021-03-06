﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abc.Zebus.Directory;
using Abc.Zebus.Util;
using Abc.Zebus.Util.Extensions;

namespace Abc.Zebus.Testing.Directory
{
    public class TestPeerDirectory : IPeerDirectory
    {
        public readonly ConcurrentDictionary<PeerId, PeerDescriptor> Peers = new ConcurrentDictionary<PeerId, PeerDescriptor>();
        public Peer Self;
        private readonly Peer _remote = new Peer(new PeerId("remote"), "endpoint");

        public event Action Registered = delegate { };
        public event Action<PeerId, PeerUpdateAction> PeerUpdated = delegate { };

        public TimeSpan TimeSinceLastPing => TimeSpan.Zero;

        public Task RegisterAsync(IBus bus, Peer self, IEnumerable<Subscription> subscriptions)
        {
            Self = self;
            Peers[self.Id] = self.ToPeerDescriptor(true, subscriptions);

            Registered();
            return Task.CompletedTask;
        }

        public Task UpdateSubscriptionsAsync(IBus bus, IEnumerable<SubscriptionsForType> subscriptionsForTypes)
        {
            var newSubscriptions = SubscriptionsForType.CreateDictionary(Peers[Self.Id].Subscriptions);
            foreach (var subscriptionsForType in subscriptionsForTypes)
                newSubscriptions[subscriptionsForType.MessageTypeId] = subscriptionsForType;
            
            Peers[Self.Id] = Self.ToPeerDescriptor(true, newSubscriptions.Values.SelectMany(subForType => subForType.ToSubscriptions()));
            PeerUpdated(Self.Id, PeerUpdateAction.Updated);
            return Task.CompletedTask;
        }

        public Task UnregisterAsync(IBus bus)
        {
            PeerUpdated(Self.Id, PeerUpdateAction.Stopped);
            return Task.CompletedTask;
        }

        public void RegisterRemoteListener<TMEssage>() where TMEssage : IMessage
        {
            var peerDescriptor = Peers.GetValueOrAdd(_remote.Id, () => new PeerDescriptor(_remote.Id, _remote.EndPoint, true, _remote.IsUp, _remote.IsResponding, SystemDateTime.UtcNow));
            var subscriptions = new List<Subscription>(peerDescriptor.Subscriptions)
            {
                new Subscription(new MessageTypeId(typeof(TMEssage)))
            };

            peerDescriptor.Subscriptions = subscriptions.ToArray();
        }

        public IList<Peer> GetPeersHandlingMessage(IMessage message)
        {
            return GetPeersHandlingMessage(MessageBinding.FromMessage(message));
        }

        public IList<Peer> GetPeersHandlingMessage(MessageBinding messageBinding)
        {
            return Peers.Where(x => x.Value.Subscriptions.Any(s => s.Matches(messageBinding))).Select(x => x.Value.Peer).ToList();
        }

        public bool IsPersistent(PeerId peerId)
        {
            return Peers.TryGetValue(peerId, out var peer) && peer.IsPersistent;
        }

        public PeerDescriptor GetPeerDescriptor(PeerId peerId)
        {
            return Peers.TryGetValue(peerId, out var peer)
                ? peer
                : null;
        }

        public IEnumerable<PeerDescriptor> GetPeerDescriptors()
        {
            return Peers.Values;
        }
    }
}
