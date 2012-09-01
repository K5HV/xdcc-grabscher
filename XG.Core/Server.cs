//  
//  Copyright (C) 2009 Lars Formella <ich@larsformella.de>
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
// 
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.
// 

using System;
using System.Collections.Generic;
using System.Linq;

namespace XG.Core
{
	[Serializable()]
	public class Server : AObjects
	{
		#region VARIABLES

		public new Servers Parent
		{
			get { return base.Parent as Servers; }
			set { base.Parent = value; }
		}

		int _port = 0;
		public int Port
		{
			get { return _port; }
			set
			{
				if (_port != value)
				{
					_port = value;
					Modified = true;
				}
			}
		}

		SocketErrorCode _errorCode = SocketErrorCode.None;
		public SocketErrorCode ErrorCode
		{
			get { return _errorCode; }
			set
			{
				if (_errorCode != value)
				{
					_errorCode = value;
					Modified = true;
				}
			}
		}

		#endregion

		#region CHILDREN

		public IEnumerable<Channel> Channels
		{
			get { return base.All.Cast<Channel>(); }
		}

		public Channel this[string name]
		{
			get
			{
				return (Channel)base.ByName(name);
			}
		}

		public Bot GetBot(string aName)
		{
			Bot tBot = null;
			foreach (Channel chan in base.All)
			{
				tBot = chan[aName];
				if (tBot != null){ break; }
			}
			return tBot;
		}

		public void AddChannel(Channel aChannel)
		{
			base.Add(aChannel);
		}

		public void AddChannel(string aChannel)
		{
			aChannel = aChannel.Trim().ToLower();
			if (!aChannel.StartsWith("#")) { aChannel = "#" + aChannel; }
			if (this[aChannel] == null)
			{
				Channel tChannel = new Channel();
				tChannel.Name = aChannel;
				tChannel.Enabled = Enabled;
				AddChannel(tChannel);
			}
		}

		public void RemoveChannel(Channel aChannel)
		{
			base.Remove(aChannel);
		}

		#endregion
	}
}
