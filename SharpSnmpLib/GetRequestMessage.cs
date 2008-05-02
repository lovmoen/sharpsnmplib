﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace SharpSnmpLib
{
	public class GetRequestMessage: ISnmpMessage
	{
        VersionCode _version;
        IList<Variable> _variables;
        UdpClient udp = new UdpClient();
        byte[] _bytes;
        IPAddress _agent;
        string _community;
        ISnmpPdu _pdu;

		public GetRequestMessage(VersionCode version, IPAddress agent, string community, IList<Variable> variables)
		{
			_version = version;
			_agent = agent;
			_community = community;
            _variables = variables; 
            GetRequestPdu pdu = new GetRequestPdu(
                new Int(0),
                new Int(0),
                _variables);
            _bytes = pdu.ToMessageBody(_version, _community).ToBytes();
		}

        public GetRequestMessage(SnmpArray body)
        {
            if (body == null)
            {
                throw new ArgumentNullException("body");
            }
            if (body.Items.Count != 3)
            {
                throw new ArgumentException("wrong message body");
            }
            _pdu = (ISnmpPdu)body.Items[2];
            if (_pdu.TypeCode != TypeCode)
            {
                throw new ArgumentException("wrong message type");
            }
            _community = body.Items[1].ToString();
            _version = (VersionCode)((Int)body.Items[0]).ToInt32();
            GetRequestPdu pdu = (GetRequestPdu)_pdu;
            _variables = pdu.Variables;
        }
        
        public IList<Variable> Variables
        {
            get
            {
                return _variables;
            }
        }

		public IList<Variable> Send(int timeout)
		{
			byte[] bytes = _bytes;
			IPEndPoint agent = new IPEndPoint(_agent, 161);
			udp.Send(bytes,bytes.Length,agent);
			IPEndPoint from = new IPEndPoint(IPAddress.Any,0);
	            IAsyncResult result = udp.BeginReceive(null, this);
	            result.AsyncWaitHandle.WaitOne(timeout, false);
                if (!result.IsCompleted)
                {
                    SharpTimeoutException ex = new SharpTimeoutException();
                    ex.Agent = _agent;
                    ex.Timeout = timeout;
                    throw ex;
                }
	            bytes = udp.EndReceive(result, ref from);
	            MemoryStream m = new MemoryStream(bytes, false);
	        ISnmpMessage message = MessageFactory.ParseMessage(m);
	        if (message.TypeCode != SnmpType.GetResponsePDU) {
	        	SharpOperationException ex = new SharpOperationException("wrong response");
                ex.Agent = _agent;
                throw ex;
	        }
	        GetResponseMessage response = (GetResponseMessage)message;	
	        return response.Variables;
		}
		
		public byte[] ToBytes()
		{
			return _bytes;
		}

		public ISnmpPdu Pdu {
			get {
                return _pdu;
			}
		}
		
		public SnmpType TypeCode {
			get {
				return SnmpType.GetRequestPDU;
			}
		}
	}
}
