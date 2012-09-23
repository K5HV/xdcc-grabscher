// 
//  BackendPlugin.cs
//  
//  Author:
//       Lars Formella <ich@larsformella.de>
// 
//  Copyright (c) 2012 Lars Formella
// 
//  This program is free software; you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation; either version 2 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//  GNU General Public License for more details.
//  
//  You should have received a copy of the GNU General Public License
//  along with this program; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
// 

using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;

using log4net;

using XG.Core;
using XG.Server.Helper;

namespace XG.Server.Plugin.Backend.File
{
	public class BackendPlugin : ABackendPlugin
	{
		#region VARIABLES

		static readonly ILog _log = LogManager.GetLogger(typeof(BackendPlugin));

		BinaryFormatter _formatter = new BinaryFormatter();

		Thread _saveLoopThread;

		bool _isSaveFile = false;
		object _saveObjectsLock = new object();
		object _saveFilesLock = new object();
		object _saveSearchesLock = new object();

		#endregion

		#region ABackendPlugin

		public override XG.Core.Servers LoadServers ()
		{
			XG.Core.Servers _servers = null;
			try
			{
				_servers = (XG.Core.Servers)Load(Settings.Instance.DataBinary);
				_servers.AttachChildEvents();
			}
			catch {}
			if (_servers == null)
			{
				_servers = new XG.Core.Servers();
			}
			return _servers;
		}

		public override Files LoadFiles ()
		{
			Files _files = null;
			try
			{
				_files = (Files)Load(Settings.Instance.FilesBinary);
				_files.AttachChildEvents();
			}
			catch {}
			if (_files == null)
			{
				_files = new Files();
			}
			return _files;
		}

		public override Objects LoadSearches ()
		{
			Objects _searches = null;
			try
			{
				_searches = (Objects)Load(Settings.Instance.SearchesBinary);
				_searches.AttachChildEvents();
			}
			catch {}
			if (_searches == null)
			{
				_searches = new Objects();
			}
			return _searches;
		}

		#endregion

		#region RUN STOP

		public override void Start ()
		{
			// start data saving routine
			_saveLoopThread = new Thread(new ThreadStart(StartSaveLoop));
			_saveLoopThread.Start();
		}

		public override void Stop ()
		{
			_saveLoopThread.Abort();
		}
		
		#endregion

		#region EVENTHANDLER

		protected override void FileAdded (AObject aParentObj, AObject aObj)
		{
			SaveFiles();
		}

		protected override void FileRemoved (AObject aParentObj, AObject aObj)
		{
			SaveFiles();
		}

		protected override void FileChanged(AObject aObj)
		{
			if (aObj is XG.Core.File)
			{
				SaveFiles();
			}
			else if (aObj is FilePart)
			{
				FilePart part = aObj as FilePart;
				// if this change is lost, the data might be corrupt, so save it now
				if (part.State != FilePart.States.Open)
				{
					SaveFiles();
				}
				// the data saving can be scheduled
				else
				{
					_isSaveFile = true;
				}
			}
		}

		protected override void SearchAdded(AObject aParent, AObject aObj)
		{
			SaveSearches();
		}

		protected override void SearchRemoved(AObject aParent, AObject aObj)
		{
			SaveSearches();
		}

		protected override void ObjectAdded(AObject aParent, AObject aObj)
		{
			if (aObj is XG.Core.Server || aObj is Channel)
			{
				SaveObjects();
			}
		}

		protected override void ObjectRemoved(AObject aParent, AObject aObj)
		{
			if (aObj is XG.Core.Server || aObj is Channel)
			{
				SaveObjects();
			}
		}

		#endregion

		#region SAVE + LOAD

		/// <summary>
		/// Serializes an object into a file
		/// </summary>
		/// <param name="aObj"></param>
		/// <param name="aFile"></param>
		bool Save(object aObj, string aFile)
		{
			try
			{
				Stream streamWrite = System.IO.File.Create(aFile + ".new");
				_formatter.Serialize(streamWrite, aObj);
				streamWrite.Close();
				FileSystem.DeleteFile(aFile + ".bak");
				FileSystem.MoveFile(aFile, aFile + ".bak");
				FileSystem.MoveFile(aFile + ".new", aFile);
				_log.Debug("Save(" + aFile + ")");
			}
			catch (Exception ex)
			{
				_log.Fatal("Save(" + aFile + ")", ex);
				return false;
			}
			return true;
		}

		/// <summary>
		/// Deserializes an object from a file
		/// </summary>
		/// <param name="aFile">Name of the File</param>
		/// <returns>the object or null if the deserializing failed</returns>
		object Load(string aFile)
		{
			object obj = null;
			if (System.IO.File.Exists(aFile))
			{
				try
				{
					Stream streamRead = System.IO.File.OpenRead(aFile);
					obj = _formatter.Deserialize(streamRead);
					streamRead.Close();
					_log.Debug("Load(" + aFile + ")");
				}
				catch (Exception ex)
				{
					_log.Fatal("Load(" + aFile + ")" , ex);
					// try to load the backup
					try
					{
						Stream streamRead = System.IO.File.OpenRead(aFile + ".bak");
						obj = _formatter.Deserialize(streamRead);
						streamRead.Close();
						_log.Debug("Load(" + aFile + ".bak)");
					}
					catch (Exception)
					{
						_log.Fatal("Load(" + aFile + ".bak)", ex);
					}
				}
			}
			return obj;
		}

		void StartSaveLoop()
		{
			DateTime timeIrc = DateTime.Now;
			DateTime timeStats = DateTime.Now;

			while (true)
			{
				// Objects
				if ((DateTime.Now - timeIrc).TotalMilliseconds > Settings.Instance.BackupDataTime)
				{
					timeIrc = DateTime.Now;

					SaveObjects();
				}

				// Files
				if (_isSaveFile)
				{
					SaveFiles();
				}

				// Statistics
				if ((DateTime.Now - timeStats).TotalMilliseconds > Settings.Instance.BackupStatisticTime)
				{
					timeStats = DateTime.Now;
					Statistic.Instance.Save();
				}

				Thread.Sleep((int)Settings.Instance.TimerSleepTime);
			}
		}

		bool SaveFiles()
		{
			lock (_saveFilesLock)
			{
				_isSaveFile = false;
				return Save(Files, Settings.Instance.FilesBinary);
			}
		}

		bool SaveObjects()
		{
			lock (_saveObjectsLock)
			{
				return Save(Servers, Settings.Instance.DataBinary);
			}
		}

		bool SaveSearches()
		{
			lock (_saveSearchesLock)
			{
				return Save(Searches, Settings.Instance.SearchesBinary);
			}
		}

		#endregion
	}
}

