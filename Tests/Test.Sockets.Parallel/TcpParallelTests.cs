﻿using NUnit.Framework;
using SimpleSockets.Client;
using SimpleSockets.Messaging.Metadata;
using SimpleSockets.Server;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Test.Sockets.Utils;

namespace Test.Sockets.Parallel
{
	public class TcpParallelTests
	{

		private SimpleSocketTcpListener _server = null;

		private IList<SimpleSocketClient> _clients = new List<SimpleSocketClient>();

		private int _numClients = 50;
		private int _numMessages = 10000;

		[OneTimeSetUp]
		public void Setup()
		{
			Counter clientCounter = new Counter();
			ManualResetEvent mre = new ManualResetEvent(false);
			_server = new SimpleSocketTcpListener();

			new Thread(() => _server.StartListening(13000)).Start();

			ClientConnectedDelegate con = (client) =>
			{
				clientCounter.Count();
				if (clientCounter.GetCount == _numClients)
					mre.Set();
			};


			_server.ClientConnected += con;

			for (var i = 0; i < _numClients; i++)
			{
				initClient();
			}


			mre.WaitOne(new TimeSpan(0, 5, 0));

			_server.ClientConnected -= con;

			Assert.AreEqual(_numClients, clientCounter.GetCount);

		}

		private void initClient() {
			var client = new SimpleSocketTcpClient();
			_clients.Add(client);
			client.StartClient("127.0.0.1", 13000);
		}

		[OneTimeTearDown]
		public void TearDown()
		{
			foreach (var client in _clients)
			{
				client.Dispose();
			}
			_server.Dispose();
			_server = null;
			_clients.Clear();
		}

		[Test]
		public void Client_ParallelMessages_Server() {

			Counter counter = new Counter();

			ManualResetEvent mre = new ManualResetEvent(false);


			SimpleSockets.Server.MessageReceivedDelegate msgRec = (client, msg) => {
				counter.Count();

				if (counter.GetCount == _numClients * _numMessages)
					mre.Set();
			};

			_server.MessageReceived += msgRec;

			foreach (var client in _clients) {
				new Thread(() => SendMessages(client, false)).Start();
			}

			// If it can't complete in 30 minutes fail
			mre.WaitOne(new TimeSpan(0, 30, 0));

			_server.MessageReceived -= msgRec;
			Assert.AreEqual((_numMessages * _numClients), counter.GetCount); // True if all messages have been received.

		}

		[Test]
		public void Client_ParallelMessagesWithMetaData_Server()
		{

			Counter counter = new Counter();

			ManualResetEvent mre = new ManualResetEvent(false);


			SimpleSockets.Server.MessageWithMetadataReceivedDelegate msgRec = (client, msg, metadata, type) => {
				counter.Count();

				if (counter.GetCount == _numClients * _numMessages)
					mre.Set();
			};

			_server.MessageWithMetaDataReceived += msgRec;

			foreach (var client in _clients)
			{
				new Thread(() => SendMessageWithMetadata(client)).Start();
			}

			// If it can't complete in 30 minutes fail
			mre.WaitOne(new TimeSpan(0, 30, 0));

			_server.MessageWithMetaDataReceived -= msgRec;
			Assert.AreEqual((_numMessages * _numClients), counter.GetCount); // True if all messages have been received.

		}

		[Test]
		public void Client_ParallelObjects_Server()
		{
			Counter counter = new Counter();

			ManualResetEvent mre = new ManualResetEvent(false);


			SimpleSockets.Server.ObjectReceivedDelegate msgRec = (client, obj, objType) => {
				counter.Count();
				if (counter.GetCount == (_numClients * _numMessages))
					mre.Set();
			};

			_server.ObjectReceived += msgRec;

			foreach (var client in _clients)
			{
				new Thread(() => SendMessages(client, true)).Start();
			}

			// If it can't complete in 30 minutes fail
			mre.WaitOne(new TimeSpan(0, 3, 0));

			_server.ObjectReceived -= msgRec;
			Assert.AreEqual((_numMessages * _numClients), counter.GetCount); // True if all messages have been received.
		}

		private void SendMessageWithMetadata(SimpleSocketClient client) {
			string message = "This is test message nr ";
			IDictionary<object, object> dictionary = new Dictionary<object, object>();
			dictionary.Add("Key1", new DataObject("Test", "This is a text", 10.56, new DateTime(2000, 1, 1)));
			dictionary.Add("Key2", new DataObject("Test2", "This is a second test", 11, new DateTime(2000, 1, 1)));


			for (var i = 0; i < _numMessages; i++)
			{
				client.SendMessageWithMetadata(message + (i + 1), dictionary);
			}
		}

		private void SendMessages(SimpleSocketClient client, bool sendObjects) {
			string message = "This is test message nr ";

			for (var i = 0; i < _numMessages; i++) {
				if (sendObjects)
				{
					client.SendObject(new DataObject(message + (i + 1), "This is a text", 15, new DateTime(2000, 1, 1)));
				}
				else {
					client.SendMessage(message + (i + 1));
				}
			}

		}

	}

	public sealed class Counter
	{
		private int _current = 0;

		public int GetCount => _current;

		public void Count()
		{
			Interlocked.Increment(ref _current);
		}

		public void reset()
		{
			_current = 0;
		}
	}

}
